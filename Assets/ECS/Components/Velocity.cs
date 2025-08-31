using Unity.Entities;
using Unity.Mathematics;

namespace ECS.Components
{
    public struct Velocity : IComponentData
    {
        public float3 Value;
    }
}