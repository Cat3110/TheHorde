using ECS.Components;
using Unity.Burst;
using Unity.Entities;

namespace ECS.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(DeathCleanupSystem))]
    public partial struct DeathStatsResetSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.EntityManager.AddComponentData(state.EntityManager.CreateEntity(), new DeathStats { DeathsThisFrame = 0 });
            state.Enabled = true;
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var stats = SystemAPI.GetSingletonRW<DeathStats>();
            stats.ValueRW.DeathsThisFrame = 0;
        }
    }
}