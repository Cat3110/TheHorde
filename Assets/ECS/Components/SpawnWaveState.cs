using Unity.Entities;

namespace ECS.Components
{
    public struct SpawnWaveState : IComponentData
    {
        public float Timer;
        public uint  RngState;  // для Unity.Mathematics.Random
        public int   WaveIndex;
    }
}