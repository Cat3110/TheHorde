using ECS.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace ECS.Systems
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(ZombieSteeringSystem))]
    public partial struct SteeringStatsSystem : ISystem
    {
        private EntityQuery _activeZombie;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SteeringParams>();
            state.RequireForUpdate<ZombieTag>();
            // создаём синглтон метрик, если его ещё нет
            if (!SystemAPI.TryGetSingletonEntity<SteeringStats>(out _))
            {
                var e = state.EntityManager.CreateEntity();
                state.EntityManager.AddComponentData(e, new SteeringStats());
            }

            _activeZombie = SystemAPI.QueryBuilder()
                .WithAll<ZombieTag, Velocity>()
                .WithDisabled<InactiveTag>()
                .Build();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var cfg = SystemAPI.GetSingleton<SteeringParams>();

            int active = _activeZombie.CalculateEntityCount();
            
            if (active == 0)
            {
                SystemAPI.SetSingleton(new SteeringStats { ActiveZombies = 0, StandingZombies = 0, StandingRatio = 0 });
                return;
            }
            
            float eps2 = cfg.StopVelEps * cfg.StopVelEps;
            int standing = 0;

            // Без аллокаций: напрямую итерируем по активным зомби
            foreach (var vel in SystemAPI.Query<RefRO<Velocity>>()
                                         .WithAll<ZombieTag>()
                                         .WithDisabled<InactiveTag>())
            {
                if (math.lengthsq(vel.ValueRO.Value) < eps2) standing++;
            }
            
            float ratio = (float)standing / active;

            SystemAPI.SetSingleton(new SteeringStats
            {
                ActiveZombies   = active,
                StandingZombies = standing,
                StandingRatio   = ratio
            });
        }
    }
}