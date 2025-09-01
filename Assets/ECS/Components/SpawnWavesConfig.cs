using Unity.Entities;

namespace ECS.Components
{
    public struct SpawnWavesConfig : IComponentData
    {
        public int ZombiesPerWave;
        public float WaveInterval;
        public float SpawnRadius;
        public float SpawnJitter;
        public int SpawnHealth;
        public float InitialSpeed;
    }
}