using Unity.Entities;

namespace ECS.Components
{
    public struct SpawnPoolConfig : IComponentData
    {
        public Entity ZombiePrefabEntity;
        public int InitialCapacity;
        public int RefillThreshold;
        public int RefillCount;
    }
}