using Unity.Entities;

namespace ECS.Components
{
    // Синглтон для статистики урона (для HUD)
    public struct DamageStats : IComponentData
    {
        public int ProcessedThisFrame;
    }
}