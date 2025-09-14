using ECS.Components;
using Unity.Burst;
using Unity.Entities;

namespace ECS.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct TagCountSystem : ISystem
    {
        private EntityQuery _playersQ;
        private EntityQuery _zombiesActiveQ;
        private Entity _countersEntity;

        [BurstCompile] public void OnCreate(ref SystemState state)
        {
            var em = state.EntityManager;
            
            _countersEntity = em.CreateEntity();
            em.AddComponent<TagCounters>(_countersEntity);
            em.SetComponentData(_countersEntity, new TagCounters { Players = 0, Zombies = 0 });
            
            // Кэшируем запросы без CreateEntityQuery(params ...)
            _playersQ = SystemAPI.QueryBuilder()
                .WithAll<PlayerTag>()
                .Build();

            _zombiesActiveQ = SystemAPI.QueryBuilder()
                .WithAll<ZombieTag>()
                .WithDisabled<InactiveTag>() // считаем только активных
                .Build();

        }

        [BurstCompile] public void OnUpdate(ref SystemState state)
        {
            var em = state.EntityManager;

            int players = _playersQ.CalculateEntityCount();
            int zombies = _zombiesActiveQ.CalculateEntityCount();

            em.SetComponentData(_countersEntity, new TagCounters
            {
                Players = players, 
                Zombies = zombies
            });
        }
    }
}