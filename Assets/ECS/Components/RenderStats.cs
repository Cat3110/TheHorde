using Unity.Entities;

namespace ECS.Components
{
    public struct RenderStats : IComponentData
    {
        public int BatchesThisFrame;
        public int InstancesThisFrame;
    }
}