using ECS.Components;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst.Intrinsics;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs.LowLevel.Unsafe;

namespace ECS.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
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
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var statsRW = SystemAPI.GetSingletonRW<DeathStats>();
            int diedThisFrame = 0;

            // Обрабатываем только активных зомби (InactiveTag выключен)
            foreach (var (health, velocity, inactiveTag, damageEvents) in SystemAPI
                         .Query<RefRW<Health>, RefRW<Velocity>, EnabledRefRW<InactiveTag>, DynamicBuffer<DamageEvent>>()
                         .WithAll<ZombieTag>()
                         .WithDisabled<InactiveTag>())
            {
                if (health.ValueRO.Value > 0)
                    continue;

                // Возврат в пул: включаем InactiveTag
                inactiveTag.ValueRW = true;

                // Сброс состояния
                velocity.ValueRW.Value = float3.zero;
                if (damageEvents.IsCreated && damageEvents.Length > 0)
                    damageEvents.Clear();

                // Нормализуем здоровье
                health.ValueRW.Value = 0;

                diedThisFrame++;
            }

            // Обновить статистику
            statsRW.ValueRW.DeathsThisFrame = diedThisFrame;
            statsRW.ValueRW.TotalDeaths     += diedThisFrame;
        }
    }
}