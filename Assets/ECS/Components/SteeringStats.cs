using Unity.Entities;

namespace ECS.Components
{
    public struct SteeringStats : IComponentData
    {
        public int ActiveZombies;
        public int StandingZombies;
        public float StandingRatio; // 0..1
    }
}