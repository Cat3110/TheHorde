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

            // Препроверяем компоненты на префабе
            var prefabHasDamage = state.EntityManager.HasBuffer<DamageEvent>(cfg.ZombiePrefabEntity);
            var prefabHasInactive = state.EntityManager.HasComponent<InactiveTag>(cfg.ZombiePrefabEntity);

            // ECB из BeginInitialization — избегаем временных аллокаций
            var ecb = SystemAPI
                .GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            // Создаём стартовый пул
            for (int i = 0; i < cfg.InitialCapacity; i++)
            {
                var z = ecb.Instantiate(cfg.ZombiePrefabEntity);
                if (!prefabHasDamage)
                    ecb.AddBuffer<DamageEvent>(z); // если буфера нет на префабе — добавим на инстансе

                if (!prefabHasInactive)
                    ecb.AddComponent<InactiveTag>(z); // если тега нет на префабе — добавим

                // гарантируем, что инстанс уходит в пул (InactiveTag включён)
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