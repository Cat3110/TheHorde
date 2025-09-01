using Unity.Entities;

namespace ECS.Components
{
    public struct SpawnDebug : IComponentData
    {
        public int ActiveZombies;
        public int InactiveZombies;
        public int WaveIndex;
        public float TimeToNextWave;
    }
}