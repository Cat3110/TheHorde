using Unity.Entities;
using UnityEngine;

namespace ECS.Components
{
    public struct MaterialRef : IComponentData
    {
        public UnityObjectRef<Material> Value;
    }
}