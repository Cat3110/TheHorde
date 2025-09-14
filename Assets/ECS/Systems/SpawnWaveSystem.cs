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
        
        private EntityTypeHandle _entityTypeHandle;
        
        private ComponentLookup<LocalTransform> _ltLookup;

        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _ltLookup = state.GetComponentLookup<LocalTransform>(isReadOnly: true);
            _entityTypeHandle = state.GetEntityTypeHandle();

            _inactiveZombiesQ = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<ZombieTag, InactiveTag>()
                .Build(ref state);

            state.RequireForUpdate<SpawnWavesConfig>();
            state.RequireForUpdate<SpawnPoolConfig>();
            state.RequireForUpdate<PlayerTag>();
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // ✅ обновляем handle на кадр
            _entityTypeHandle.Update(ref state);
            
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

            // Сколько неактивных есть в пуле сейчас?
            var inactiveCount = _inactiveZombiesQ.CalculateEntityCount();

            // ECB для безопасных структурных изменений
            var ecb = SystemAPI
                .GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            
            // 2.1 Дозаполнить пул, если надо
            if (inactiveCount < poolCfg.RefillThreshold)
            {
                var need = math.max(0, poolCfg.RefillCount);
                for (int i = 0; i < need; i++)
                {
                    var z = ecb.Instantiate(poolCfg.ZombiePrefabEntity);
                    ecb.AddComponent<InactiveTag>(z);
                    ecb.SetComponentEnabled<InactiveTag>(z, true);
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

            using var chunks = _inactiveZombiesQ.ToArchetypeChunkArray(Allocator.TempJob);
            int remaining = toActivate;

            // RNG в стейте
            var rng = new Unity.Mathematics.Random(st.RngState == 0 ? 1u : st.RngState);

            for (int ci = 0; ci < chunks.Length && remaining > 0; ci++)
            {
                var chunk = chunks[ci];
                var entities = chunk.GetNativeArray(_entityTypeHandle);

                int take = math.min(entities.Length, remaining);
                for (int i = 0; i < take; i++)
                {
                    var e = entities[i];

                    // Случайная позиция вокруг игрока
                    float angle = rng.NextFloat(0, 2f * math.PI);
                    float r = cfg.SpawnRadius + rng.NextFloat(-cfg.SpawnJitter, cfg.SpawnJitter);
                    float3 pos = playerPos + new float3(math.cos(angle) * r, 0, math.sin(angle) * r);

                    // Направление к игроку, стартовая скорость
                    float3 dir = math.normalize(playerPos - pos);
                    float3 vel = dir * cfg.InitialSpeed;

                    // Применяем через ECB
                    ecb.SetComponentEnabled<InactiveTag>(e, false);

                    ecb.SetComponent(e, LocalTransform.FromPositionRotationScale(pos, quaternion.identity, 1f));
                    ecb.SetComponent(e, new Position { Value = pos });
                    ecb.SetComponent(e, new Velocity { Value = vel });
                    ecb.SetComponent(e, new Health { Value = cfg.SpawnHealth });
                }

                remaining -= take;
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