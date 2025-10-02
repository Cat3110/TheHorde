using Unity.Entities;

namespace ECS.Components
{
    // Событие урона, которое накапливается на цели
    public struct DamageEvent : IBufferElementData
    {
        public int Amount; // Сколько урона нанести
        public Entity Source; // Кто нанёс (опционально)
    }
}