using ECS.Components;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace ECS.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(DamageSystem))] // важно: сначала применяем урон
    public partial struct DeathCleanupSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // Не тикаем, если нет зомби или нет Health
            state.RequireForUpdate(SystemAPI.QueryBuilder()
                .WithAll<ZombieTag, Health>()
                .Build());
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var statsRW = SystemAPI.GetSingletonRW<DeathStats>();

            var job = new DeathCleanupJob
            {
                // всегда считаем deaths заново за текущий кадр
                DeathsThisFrame = 0
            };

            state.Dependency = job.Schedule(state.Dependency);
            state.Dependency.Complete(); // нам нужно вернуть число смертей, потому завершаем джоб
            statsRW.ValueRW.DeathsThisFrame = job.DeathsThisFrame;   // per-frame metric (сбрасывается в Reset-системе)
            statsRW.ValueRW.TotalDeaths     += job.DeathsThisFrame;   // кумулятивная метрика за сессию
        }

        [BurstCompile]
        private partial struct DeathCleanupJob : IJobEntity
        {
            // Счётчик смертей за кадр (через capture-by-value)
            public int DeathsThisFrame;

            // Обрабатываем только активных зомби (у которых InactiveTag выключен)
            private void Execute(
                ref Health health,
                ref Velocity velocity,
                EnabledRefRW<InactiveTag> inactiveTag,
                DynamicBuffer<DamageEvent> damageEvents,
                in ZombieTag _ // фильтр
            )
            {
                // Уже в пуле — ничего не делаем
                if (inactiveTag.ValueRO)
                    return;

                if (health.Value > 0)
                    return;

                // Помечаем как неактивного (реюз через пул)
                inactiveTag.ValueRW = true;

                // Чистим состояние
                velocity.Value = float3.zero;
                if (damageEvents.IsCreated && damageEvents.Length > 0)
                    damageEvents.Clear();

                // Важно: Health оставляем как есть (<=0). При последующем реюзе SpawnWaveSystem
                // всё равно задаёт новое значение Health. Можно сбросить в 0 для читаемости:
                health.Value = 0;

                // Счётчик смертей (локальная переменная в джобе)
                DeathsThisFrame++;
            }
        }
    }
}