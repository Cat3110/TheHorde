using Unity.Entities;
using Unity.Mathematics;

namespace ECS.Components
{
    public struct Projectile : IComponentData
    {
        public float3 PrevPosition; // позиция в прошлом тике
        public int Damage;          // если 0 — берём из DamageConfig.ProjectileDamage
        public float Radius;        // радиус хитсперы снаряда
        public bool HitOnce;        // если true — снаряд уже нанёс урон (на чистку)
    }
}