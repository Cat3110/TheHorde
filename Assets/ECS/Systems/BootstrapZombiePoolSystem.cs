using ECS.Components;
using Unity.Burst;
using Unity.Entities;

namespace ECS.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct BootstrapZombiePoolSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SpawnPoolConfig>();
            state.RequireForUpdate<BeginInitializationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Если уже инициализировали — выходим
            if (SystemAPI.HasSingleton<ZombiePoolState>())
                return;

            var cfg = SystemAPI.GetSingleton<SpawnPoolConfig>();

            // ECB из BeginInitialization — избегаем временных аллокаций
            var ecb = SystemAPI
                .GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            // Создаём стартовый пул
            for (int i = 0; i < cfg.InitialCapacity; i++)
            {
                var z = ecb.Instantiate(cfg.ZombiePrefabEntity);
                ecb.AddComponent<InactiveTag>(z);
                ecb.SetComponentEnabled<InactiveTag>(z, true);
                // Позицию/скорость будем выставлять при "активации" волны в SpawnSystem (1.1.3 продолжение)
            }

            // Помечаем, что пул создан
            var marker = ecb.CreateEntity();
            ecb.AddComponent<ZombiePoolState>(marker);

            // Готово — система больше не нужна
            state.Enabled = false;
        }
    }
}