using ECS.Components;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace ECS.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(CollisionContactDamageSystem))] // порядок не критичен, главное после движения
    public partial struct ProjectileHitSystem : ISystem
    {
        private BufferLookup<DamageEvent> _damageLookup;
        private ComponentLookup<Position> _posLookup;
        private ComponentLookup<Radius> _radLookup;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _damageLookup = state.GetBufferLookup<DamageEvent>();
            _posLookup    = state.GetComponentLookup<Position>(true);
            _radLookup    = state.GetComponentLookup<Radius>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Проверка: есть ли вообще снаряды
            if (SystemAPI.QueryBuilder().WithAll<Projectile>().Build().IsEmpty)
                return;
    
            var cfg = SystemAPI.HasSingleton<DamageConfig>() 
                ? SystemAPI.GetSingleton<DamageConfig>() 
                : new DamageConfig { ProjectileDamage = 10 };

            _damageLookup.Update(ref state);
            _posLookup.Update(ref state);
            _radLookup.Update(ref state);

            var damageLookup = _damageLookup;
            var posLookup = _posLookup;
            var radLookup = _radLookup;
            
            // Пробегаем снаряды → проверяем пересечение сегмент-круг со всеми зомби.
            // Примечание: O(P*Z). Для MVP ОК. Дальше подключим spatial hash.
            foreach (var (proj, projEntity) in SystemAPI.Query<RefRW<Projectile>>().WithEntityAccess())
            {
                if (proj.ValueRO.HitOnce) continue;

                var p1 = SystemAPI.GetComponent<Position>(projEntity).Value;
                var p0 = proj.ValueRO.PrevPosition;
                float pr = proj.ValueRO.Radius;

                bool hit = false;

                foreach (var (zTag, zEntity) in SystemAPI.Query<RefRO<ZombieTag>>().WithNone<InactiveTag>().WithEntityAccess())
                {
                    var c = posLookup[zEntity].Value;
                    float cr = radLookup[zEntity].Value;

                    {
                        float2 a = p0.xz;
                        float2 b = p1.xz;
                        float2 o = c.xz;
                        float  r = pr + cr;

                        float2 ab = b - a;
                        float2 ao = o - a;
                        float   abLenSq = math.max(1e-6f, math.lengthsq(ab));
                        float   t = math.saturate(math.dot(ao, ab) / abLenSq);
                        float2  closest = a + t * ab;
                        float   distSq = math.lengthsq(o - closest);

                        if (distSq <= r * r)
                        {
                            var dmg = proj.ValueRO.Damage > 0 ? proj.ValueRO.Damage : cfg.ProjectileDamage;
                            var buf = damageLookup[zEntity];
                            buf.Add(new DamageEvent { Amount = dmg, Source = projEntity });

                            hit = true;
                            // Можно разрешить «пробитие» нескольких целей — тогда не ставим HitOnce.
                            break;
                        }
                    }
                }

                if (hit)
                {
                    proj.ValueRW.HitOnce = true; // очистит другая система (1.1.7)
                }
                else
                {
                    // Обновляем PrevPosition на текущую — для непрерывного теста в следующем тике
                    proj.ValueRW.PrevPosition = p1;
                }
            }
        }

    }
}