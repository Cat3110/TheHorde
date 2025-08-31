using Unity.Entities;

namespace ECS.Components
{
    public struct TagCounters : IComponentData
    {
        public int Players;
        public int Zombies;
    }
}