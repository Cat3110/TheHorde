using ECS.Components;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace ECS.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(ZombieSteeringSystem))] // после движения
    public partial struct CollisionContactDamageSystem : ISystem
    {
        private ComponentLookup<Radius> _radiusLookup;
        private ComponentLookup<Position> _posLookup;
        private BufferLookup<DamageEvent> _damageLookup;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerTag>();
            _radiusLookup = state.GetComponentLookup<Radius>(true);
            _posLookup = state.GetComponentLookup<Position>(true);
            _damageLookup = state.GetBufferLookup<DamageEvent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var cfg = SystemAPI.GetSingleton<DamageConfig>();

            _radiusLookup.Update(ref state);
            _posLookup.Update(ref state);
            _damageLookup.Update(ref state);

            // Синглтон игрока
            var playerEntity = SystemAPI.GetSingletonEntity<PlayerTag>();
            var playerPos = _posLookup[playerEntity].Value;
            var playerR   = _radiusLookup[playerEntity].Value;

            var damageLookup = _damageLookup; // для Burst-переноса
            var posLookup    = _posLookup;
            var radiusLookup = _radiusLookup;

            // Идём по активным зомби (ZombieTag, без InactiveTag)
            foreach (var (zombieTag, zombieEntity) in SystemAPI.Query<RefRO<ZombieTag>>().WithDisabled<InactiveTag>().WithEntityAccess())
            {
                var zPos = posLookup[zombieEntity].Value;
                var zR   = radiusLookup[zombieEntity].Value;

                float sumR = playerR + zR;
                float3 d = zPos - playerPos;
                if (math.lengthsq(d) <= sumR * sumR)
                {
                    // Зомби касается игрока → наносим контактный урон игроку
                    var bufPlayer = damageLookup[playerEntity];
                    bufPlayer.Add(new DamageEvent { Amount = cfg.ZombieTouchDamageToPlayer, Source = zombieEntity });

                    // (опционально) Игрок "колючий": урон зомби при контакте
                    if (cfg.PlayerTouchDamageToZombie > 0)
                    {
                        var bufZombie = damageLookup[zombieEntity];
                        bufZombie.Add(new DamageEvent { Amount = cfg.PlayerTouchDamageToZombie, Source = playerEntity });
                    }
                }
            }
        }
    }
}