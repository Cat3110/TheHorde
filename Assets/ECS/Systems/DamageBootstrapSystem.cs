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
                var e = state.EntityManager.CreateEntity(typeof(DamageStats));
                state.EntityManager.SetComponentData(e, new DamageStats { ProcessedThisFrame = 0 });
            }

            // Синглтон конфигурации урона (дефолт)
            if (!SystemAPI.HasSingleton<DamageConfig>())
            {
                var e = state.EntityManager.CreateEntity(typeof(DamageConfig));
                state.EntityManager.SetComponentData(e, new DamageConfig
                {
                    ZombieTouchDamageToPlayer = 1,
                    PlayerTouchDamageToZombie = 0, // можно включить "шипы" у игрока позже
                    ProjectileDamage = 10
                });
            }
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {

        }
    }
}