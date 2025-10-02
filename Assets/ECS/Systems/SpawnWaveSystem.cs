using ECS.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace ECS.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct SpawnWaveSystem : ISystem
    {
        private EntityQuery _inactiveZombiesQ;
        private ComponentLookup<LocalTransform> _ltLookup;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _ltLookup = state.GetComponentLookup<LocalTransform>(isReadOnly: true);

            _inactiveZombiesQ = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<ZombieTag, InactiveTag>()
                .Build(ref state);

            state.RequireForUpdate<SpawnWavesConfig>();
            state.RequireForUpdate<SpawnPoolConfig>();
            state.RequireForUpdate<PlayerTag>();
            state.RequireForUpdate<SpawnWaveState>();
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var dt = SystemAPI.Time.DeltaTime;

            ref var cfg  = ref SystemAPI.GetSingletonRW<SpawnWavesConfig>().ValueRW;
            ref var st   = ref SystemAPI.GetSingletonRW<SpawnWaveState>().ValueRW;
            var poolCfg  = SystemAPI.GetSingleton<SpawnPoolConfig>();

            // Тикаем таймер
            st.Timer += dt;
            if (st.Timer < cfg.WaveInterval)
                return;

            st.Timer = 0f;
            st.WaveIndex++;

            // Сколько неактивных есть в пуле сейчас? (InactiveTag enabled = в пуле)
            int inactiveCount = 0;
            foreach (var (inactive, _) in SystemAPI.Query<EnabledRefRO<InactiveTag>, RefRO<ZombieTag>>())
            {
                if (inactive.ValueRO)
                    inactiveCount++;
            }

            // ECB для безопасных структурных изменений
            var ecb = SystemAPI
                .GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            
            // 2.1 Дозаполнить пул, если надо (добираем недостающее, но не больше лимита за тик)
            if (inactiveCount < poolCfg.RefillThreshold)
            {
                int deficit = poolCfg.RefillThreshold - inactiveCount;
                int need = math.min(deficit, poolCfg.RefillCount);
                for (int i = 0; i < need; i++)
                {
                    var z = ecb.Instantiate(poolCfg.ZombiePrefabEntity);
                    ecb.AddComponent<InactiveTag>(z);
                    ecb.SetComponentEnabled<InactiveTag>(z, true); // в пуле
                }
                inactiveCount += need;
            }
            
            // 2.2 Подготовим данные игрока (точка притяжения)
            _ltLookup.Update(ref state);

            float3 playerPos = float3.zero;
            if (SystemAPI.TryGetSingletonEntity<PlayerTag>(out var player) && _ltLookup.HasComponent(player))
            {
                playerPos = _ltLookup[player].Position;
            }
            
            // 2.3 Активируем волну: снимаем InactiveTag с N первых сущностей и расставляем позицию/скорость/здоровье
            int toActivate = math.min(cfg.ZombiesPerWave, inactiveCount);
            if (toActivate == 0) return;

            int remaining = toActivate;
            var rng = new Random(st.RngState == 0 ? 1u : st.RngState);

            foreach (var (inactive, lt, pos, vel, hp) in SystemAPI.Query<EnabledRefRW<InactiveTag>, RefRW<LocalTransform>, RefRW<Position>, RefRW<Velocity>, RefRW<Health>>()
                                                                  .WithAll<ZombieTag>())
            {
                if (remaining == 0) break;

                // Берём только реально неактивных (в пуле)
                if (!inactive.ValueRO)
                    continue;

                // Случайная позиция вокруг игрока
                float angle = rng.NextFloat(0, 2f * math.PI);
                float r = cfg.SpawnRadius + rng.NextFloat(-cfg.SpawnJitter, cfg.SpawnJitter);
                float3 spawnPos = playerPos + new float3(math.cos(angle) * r, 0, math.sin(angle) * r);

                // Направление к игроку, стартовая скорость (устойчиво при совпадении точек)
                float3 d = playerPos - spawnPos;
                float lenSq = math.lengthsq(d);
                float3 dir = lenSq > 1e-6f ? d * math.rsqrt(lenSq) : new float3(0, 0, 1);
                float3 startVel = dir * cfg.InitialSpeed;

                // Записываем данные напрямую (не структурные изменения)
                lt.ValueRW = LocalTransform.FromPositionRotationScale(spawnPos, quaternion.identity, 1f);
                pos.ValueRW = new Position { Value = spawnPos };
                vel.ValueRW = new Velocity { Value = startVel };
                hp.ValueRW  = new Health   { Value = cfg.SpawnHealth };

                // Активируем (disable InactiveTag)
                inactive.ValueRW = false;
                

                remaining--;
            }

            // Сохраняем обновлённый seed
            st.RngState = rng.state;
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
}