using ECS.Components;
using Unity.Burst;
using Unity.Entities;

namespace ECS.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(ProjectileHitSystem))]
    [UpdateAfter(typeof(CollisionContactDamageSystem))]
    public partial struct DamageSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            int processed = 0;

            foreach (var (health, dmgBuffer) in SystemAPI.Query<RefRW<Health>, DynamicBuffer<DamageEvent>>())
            {
                int total = 0;
                
                for (int i = 0; i < dmgBuffer.Length; i++)
                {
                    total += dmgBuffer[i].Amount;
                }

                if (total > 0)
                {
                    var h = health.ValueRO.Value;
                    h -= total;
                    if (h < 0) h = 0;
                    health.ValueRW.Value = h;

                    processed += dmgBuffer.Length;
                    dmgBuffer.Clear();
                }
            }

            if (SystemAPI.TryGetSingletonRW<DamageStats>(out var stats))
            {
                stats.ValueRW.ProcessedThisFrame += processed;
            }
        }
    }
}