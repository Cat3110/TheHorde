using Unity.Entities;

namespace ECS.Components
{
    public struct DeathStats : IComponentData
    {
        public int DeathsThisFrame;
        public int TotalDeaths;
    }
}