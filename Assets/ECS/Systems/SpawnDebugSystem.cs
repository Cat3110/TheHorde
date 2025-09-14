using ECS.Components;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

namespace ECS.Systems
{
    
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct SpawnDebugSystem : ISystem
    {
        EntityQuery _activeQ;
        EntityQuery _inactiveQ;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SpawnDebug>();

            _activeQ = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<ZombieTag>()
                .WithDisabled<InactiveTag>()
                .Build(ref state);

            _inactiveQ = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<ZombieTag, InactiveTag>()
                .Build(ref state);

            // создаём синглтон, если вдруг его нет
            if (!SystemAPI.HasSingleton<SpawnDebug>())
            {
                var dbg = state.EntityManager.CreateEntity();
                state.EntityManager.AddComponent<SpawnDebug>(dbg);
            }

            state.RequireForUpdate<SpawnWavesConfig>();
            state.RequireForUpdate<SpawnWaveState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            ref var cfg = ref SystemAPI.GetSingletonRW<SpawnWavesConfig>().ValueRW;
            ref var st  = ref SystemAPI.GetSingletonRW<SpawnWaveState>().ValueRW;

            var e = SystemAPI.GetSingletonEntity<SpawnDebug>();
            state.EntityManager.SetComponentData(e, new SpawnDebug
            {
                ActiveZombies   = _activeQ.CalculateEntityCount(),
                InactiveZombies = _inactiveQ.CalculateEntityCount(),
                WaveIndex       = st.WaveIndex,
                TimeToNextWave  = math.max(0, cfg.WaveInterval - st.Timer)
            });
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
}