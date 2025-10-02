using Unity.Entities;

namespace ECS.Components
{
    // Синглтон с настройками урона (можно править числа в рантайме)
    public struct DamageConfig : IComponentData
    {
        public int ZombieTouchDamageToPlayer; // урон игроку за касание зомби (за тик)
        public int PlayerTouchDamageToZombie; // урон зомби за касание игрока (за тик)
        public int ProjectileDamage;          // дефолтный урон снаряда (если у Projectile нет своего)
    }
}