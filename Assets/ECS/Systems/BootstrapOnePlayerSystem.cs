using ECS.Components;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace ECS.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct BootstrapOnePlayerSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var em = state.EntityManager;
            
            // Уже есть игрок? — выходим и выключаемся.
            if (SystemAPI.HasSingleton<PlayerTag>())
            {
                state.Enabled = false;
                return;
            }
            
            // Создаём сущность БЕЗ params: CreateEntity() + generic AddComponent*
            var e = em.CreateEntity();
            em.AddComponentData(e, new Position { Value = float3.zero });
            em.AddComponentData(e, new Velocity { Value = float3.zero });
            em.AddComponentData(e, new Radius   { Value = 0.5f });
            em.AddComponentData(e, new Health   { Value = 100 });
            em.AddComponent<PlayerTag>(e);

            state.Enabled = false;
        }
    }
}