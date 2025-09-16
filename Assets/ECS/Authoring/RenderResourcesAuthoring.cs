using Unity.Entities;
using UnityEngine;

namespace ECS.Authoring
{
    
    // Managed-компонент с ссылками на Mesh/Material для инстансинга
    public sealed class RenderResources : IComponentData
    {
        public Material InstancedMaterial;
        public Mesh PlayerMesh;
        public Mesh ZombieMesh;
    }
    
    public class RenderResourcesAuthoring : MonoBehaviour
    {
        public Material InstancedMaterial;  // URP/Lit или URP/Unlit, включи Enable GPU Instancing
        public Mesh PlayerMesh;             // Capsule
        public Mesh ZombieMesh;             // Cube
        private class RenderResourcesAuthoringBaker : Baker<RenderResourcesAuthoring>
        {
            public override void Bake(RenderResourcesAuthoring authoring)
            {
                var e = GetEntity(TransformUsageFlags.None);
                
                var capsule = authoring.PlayerMesh ?? Resources.GetBuiltinResource<Mesh>("Capsule.fbx");
                var cube    = authoring.ZombieMesh ?? Resources.GetBuiltinResource<Mesh>("Cube.fbx");
                var mat     = authoring.InstancedMaterial ?? new Material(Shader.Find("Universal Render Pipeline/Lit"))
                {
                    enableInstancing = true
                };
                
                AddComponentObject(e, new RenderResources
                {
                    InstancedMaterial = mat,
                    PlayerMesh  = capsule,
                    ZombieMesh  = cube
                });
            }
        }
    }
}