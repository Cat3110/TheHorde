using Unity.Entities;
using UnityEngine;

namespace ECS.Components
{
    public struct MeshRef : IComponentData
    {
        public UnityObjectRef<Mesh> Value;
    }
}