using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using ECS.Components;

namespace ECS.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct SyncLocalTransformFromPositionSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (pos, lt) in SystemAPI.Query<RefRO<Position>, RefRW<LocalTransform>>())
            {
                // Position главнее, чем LocalTransform! 
                lt.ValueRW.Position = pos.ValueRO.Value;
            }
        }
    }
}