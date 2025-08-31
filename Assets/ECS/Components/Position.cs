using Unity.Entities;
using Unity.Mathematics;

namespace ECS.Components
{
    public struct Position : IComponentData
    {
        public float3 Value;
    }
}