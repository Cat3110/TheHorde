using ECS.Components;
using Unity.Burst;
using Unity.Entities;

namespace ECS.Systems
{
    [BurstCompile]
    public partial struct DamageBootstrapSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // Синглтон статистики урона
            if (!SystemAPI.HasSingleton<DamageStats>())
            {
                var e = state.EntityManager.CreateEntity();
                state.EntityManager.AddComponent<DamageStats>(e);
                state.EntityManager.SetComponentData(e, new DamageStats { ProcessedThisFrame = 0 });
            }

            // Синглтон конфигурации урона (дефолт)
            if (!SystemAPI.HasSingleton<DamageConfig>())
            {
                var e = state.EntityManager.CreateEntity();
                state.EntityManager.AddComponent<DamageConfig>(e);
                state.EntityManager.SetComponentData(e, new DamageConfig
                {
                    ZombieTouchDamageToPlayer = 0,
                    PlayerTouchDamageToZombie = 0, // включено для проверки убийств от контакта
                    ProjectileDamage = 10
                });
            }
        }
    }
}