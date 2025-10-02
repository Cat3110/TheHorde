using ECS.Components;
using Unity.Burst;
using Unity.Entities;

namespace ECS.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    public partial struct DamageStatsResetSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (SystemAPI.TryGetSingletonRW<DamageStats>(out var s))
                s.ValueRW.ProcessedThisFrame = 0;
        }
    }
}
