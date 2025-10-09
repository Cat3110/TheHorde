using ECS.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;

namespace ECS.Systems
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(ZombieSteeringSystem))]
    public partial struct SteeringStatsSystem : ISystem
    {
        private EntityQuery _activeZombie;

        // Cached type handles to avoid per-frame lookups/sync-points
        private ComponentTypeHandle<Velocity> _velHandleRO;

        // Throttling HUD stats to reduce cost (5 Hz is enough)
        private float _accum;
        private const float kInterval = 0.2f; // seconds

        // Threshold^2 for "standing"
        private float _standingEps2;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SteeringParams>();
            state.RequireForUpdate<ZombieTag>();

            // Ensure stats singleton exists
            if (!SystemAPI.TryGetSingletonEntity<SteeringStats>(out _))
            {
                var e = state.EntityManager.CreateEntity();
                state.EntityManager.AddComponentData(e, new SteeringStats());
            }

            _activeZombie = SystemAPI.QueryBuilder()
                .WithAll<ZombieTag, Velocity>()
                .WithDisabled<InactiveTag>() // select only active (InactiveTag disabled)
                .Build();

            _velHandleRO = state.GetComponentTypeHandle<Velocity>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _accum += SystemAPI.Time.DeltaTime;
            if (_accum < kInterval)
                return;
            _accum = 0f;

            var cfg = SystemAPI.GetSingleton<SteeringParams>();
            _standingEps2 = cfg.StopVelEps * cfg.StopVelEps;

            // Update cached handles
            _velHandleRO.Update(ref state);

            int active = 0;
            int standing = 0;

            foreach (var vel in SystemAPI.Query<RefRO<Velocity>>()
                                         .WithAll<ZombieTag>()
                                         .WithDisabled<InactiveTag>())
            {
                active++;
                float3 v = vel.ValueRO.Value;
                if (math.lengthsq(v) < _standingEps2)
                    standing++;
            }

            float ratio = active > 0 ? (float)standing / active : 0f;
            SystemAPI.SetSingleton(new SteeringStats
            {
                ActiveZombies = active,
                StandingZombies = standing,
                StandingRatio = ratio
            });
        }
    }
}