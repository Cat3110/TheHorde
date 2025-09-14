using ECS.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace ECS.Systems
{
    /// <summary>
    /// Система строит spatial hash и применяет steering:
    /// притяжение к игроку + избегание соседей. Обновляет Velocity.
    /// </summary>
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial struct ZombieSteeringSystem : ISystem
    {
        private EntityQuery _zombieQuery;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SteeringParams>();
            state.RequireForUpdate<PlayerTag>();
            state.RequireForUpdate<ZombieTag>();
            
            _zombieQuery = SystemAPI.QueryBuilder()
                .WithAll<ZombieTag, Position, Velocity, Radius>()
                .WithDisabled<InactiveTag>()
                .Build();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var dt = SystemAPI.Time.DeltaTime;

            // Считываем конфиг
            var cfg = SystemAPI.GetSingleton<SteeringParams>();

            // Позиция игрока (берём первого PlayerTag — в твоём проекте он один)
            var playerEntity = SystemAPI.GetSingletonEntity<PlayerTag>();
            var playerPos = SystemAPI.GetComponent<Position>(playerEntity).Value;

            int zombieCount = _zombieQuery.CalculateEntityCount();
            if (zombieCount == 0) return;

            // Spatial Hash: ключ -> Entity
            var capacity = max(zombieCount * 2, 128);
            var hash = new NativeParallelMultiHashMap<int, Entity>(capacity, Allocator.TempJob);

            // Lookups для рандомного доступа по Entity
            var posLookup = SystemAPI.GetComponentLookup<Position>(true);
            var radLookup = SystemAPI.GetComponentLookup<Radius>(true);

            // 1) Наполняем хеш
            var buildJob = new BuildHashJob
            {
                CellSize = cfg.CellSize,
                HashMap  = hash.AsParallelWriter(),
                PosLookupRO = posLookup
            };
            state.Dependency = buildJob.ScheduleParallel(_zombieQuery, state.Dependency);

            // 2) Steering

            var steerJob = new SteeringJob
            {
                Dt             = dt,
                PlayerPos      = playerPos,
                NeighborRadius = cfg.NeighborRadius,
                MaxNeighbors   = cfg.MaxNeighbors,
                AvoidWeight    = cfg.AvoidWeight,
                TargetWeight   = cfg.TargetWeight,
                MaxSpeed       = cfg.MaxSpeed,
                TurnRate       = cfg.TurnRate,

                HashMap        = hash,
                PosLookupRO    = posLookup,
                RadLookupRO    = radLookup
            };
            state.Dependency = steerJob.ScheduleParallel(_zombieQuery, state.Dependency);

            // 3) Освобождаем хеш
            state.Dependency = hash.Dispose(state.Dependency);
        }
        
        // ---------- Jobs ----------

        [BurstCompile]
        partial struct BuildHashJob : IJobEntity
        {
            [ReadOnly] public float CellSize;
            [ReadOnly] public ComponentLookup<Position> PosLookupRO;

            public NativeParallelMultiHashMap<int, Entity>.ParallelWriter HashMap;

            [BurstCompile]
            static int Hash(int x, int y)
            {
                return x * 73856093 ^ y * 19349663;
            }

            void Execute(Entity e, in ZombieTag tag)
            {
                var pos = PosLookupRO[e].Value;
                var cell = (int2)floor(pos.xy / CellSize);
                HashMap.Add(Hash(cell.x, cell.y), e);
            }
        }
        
            [BurstCompile]
        partial struct SteeringJob : IJobEntity
        {
            public float Dt;
            public float3 PlayerPos;

            [ReadOnly] public float NeighborRadius;
            [ReadOnly] public int   MaxNeighbors;
            [ReadOnly] public float AvoidWeight;
            [ReadOnly] public float TargetWeight;
            [ReadOnly] public float MaxSpeed;
            [ReadOnly] public float TurnRate;

            [ReadOnly] public NativeParallelMultiHashMap<int, Entity> HashMap;
            [ReadOnly] public ComponentLookup<Position> PosLookupRO;
            [ReadOnly] public ComponentLookup<Radius>   RadLookupRO;

            [BurstCompile]
            static int Hash(int x, int y) => x * 73856093 ^ y * 19349663;

            void Execute(Entity e, ref Velocity v, in Position p, in Radius r, in ZombieTag tag)
            {
                // Притяжение к игроку
                float3 toPlayerDir = normalize(PlayerPos - p.Value);
                if (any(!isfinite(toPlayerDir))) toPlayerDir = float3(0,0,0);

                // Избегание соседей через 3x3 окрестность ячеек
                float cellSize = max(NeighborRadius, 0.01f); // чуть крупнее радиуса поиска — устойчивее
                int2 myCell = (int2)floor(p.Value.xy / cellSize);

                float3 avoid = 0;
                int neighbors = 0;
                float rr = max(0.01f, r.Value);

                for (int oy = -1; oy <= 1; oy++)
                {
                    for (int ox = -1; ox <= 1; ox++)
                    {
                        int2 cell = myCell + new int2(ox, oy);
                        var key = Hash(cell.x, cell.y);

                        NativeParallelMultiHashMapIterator<int> it;
                        Entity other;
                        if (HashMap.TryGetFirstValue(key, out other, out it))
                        {
                            do
                            {
                                if (other == e) continue;

                                float3 op = PosLookupRO[other].Value;
                                float  orr = RadLookupRO.HasComponent(other) ? RadLookupRO[other].Value : rr;

                                float3 diff = p.Value - op;
                                float dist2 = lengthsq(diff);
                                float minDist = rr + orr;

                                if (dist2 > 1e-6f && dist2 < (NeighborRadius * NeighborRadius))
                                {
                                    // Сильнее отталкиваем при пересечении "личных пространств"
                                    float inv = 1.0f / dist2;
                                    float push = (dist2 < minDist * minDist) ? 2.0f : 1.0f;
                                    avoid += normalize(diff) * inv * push;
                                    neighbors++;
                                    if (neighbors >= MaxNeighbors)
                                        break;
                                }
                            }
                            while (HashMap.TryGetNextValue(out other, ref it));
                        }
                        if (neighbors >= MaxNeighbors)
                            break;
                    }
                    if (neighbors >= MaxNeighbors)
                        break;
                }

                float3 desiredDir = TargetWeight * toPlayerDir + AvoidWeight * avoid;
                if (all(abs(desiredDir) < 1e-6f))
                {
                    // ничего не меняем
                    return;
                }

                desiredDir = normalize(desiredDir);
                float3 curVel = v.Value;

                // Поворот к желаемому направлению с ограничением TurnRate
                float3 desiredVel = desiredDir * MaxSpeed;

                // Линейное приближение к desiredVel
                float3 newVel = curVel + (desiredVel - curVel) * saturate(TurnRate * Dt);
                // Нормируем до MaxSpeed (устойчивость)
                float speed = length(newVel);
                if (speed > MaxSpeed) newVel = newVel * (MaxSpeed / speed);

                v.Value = newVel;
            }
        }
        
    }
    
}