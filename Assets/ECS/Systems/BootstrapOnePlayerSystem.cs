using ECS.Components;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace ECS.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct BootstrapOnePlayerSystem : ISystem
    {

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // no-op
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
            
            // Создаём сущность без archetype и явно добавляем нужные компоненты
            var e = em.CreateEntity();

            em.AddComponentData(e, new Position { Value = float3.zero });
            em.AddComponentData(e, new Velocity { Value = float3.zero });
            em.AddComponentData(e, new Radius   { Value = 0.5f });
            em.AddComponentData(e, new Health   { Value = 100 });
            em.AddComponent<PlayerTag>(e);

            // Трансформы для рендера
            em.AddComponentData(e, LocalTransform.FromPositionRotationScale(float3.zero, quaternion.identity, 1f));
            em.AddComponent<LocalToWorld>(e); // чтобы Transform-системы поддерживали L2W

            state.Enabled = false;
        }
    }
}