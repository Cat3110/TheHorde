using ECS.Components;
using Unity.Entities;
using UnityEngine;

namespace ECS.Authoring
{
    public class RenderBootstrapAuthoring : MonoBehaviour
    {
        [Header("Meshes")]
        public Mesh ZombieMesh;   // Cylinder
        public Mesh PlayerMesh;   // Sphere

        [Header("Materials (SRP Batcher compatible)")]
        public Material ZombieMat; // красный
        public Material PlayerMat; // синий
        
        private class RenderBootstrapAuthoringBaker : Baker<RenderBootstrapAuthoring>
        {
            public override void Bake(RenderBootstrapAuthoring authoring)
            {
                var cfg = CreateAdditionalEntity(TransformUsageFlags.None);
                AddComponent(cfg, new RenderConfig
                {
                    ZombieMeshEntity = TryMakeMesh(authoring.ZombieMesh),
                    PlayerMeshEntity = TryMakeMesh(authoring.PlayerMesh),
                    ZombieMatEntity = TryMakeMat(authoring.ZombieMat),
                    PlayerMatEntity = TryMakeMat(authoring.PlayerMat)
                });
            }
            
            Entity TryMakeMesh(Mesh m)
            {
                var e = CreateAdditionalEntity(TransformUsageFlags.None);
                AddComponent(e, new MeshRef { Value = m });
                return e;
            }

            Entity TryMakeMat(Material m)
            {
                var e = CreateAdditionalEntity(TransformUsageFlags.None);
                AddComponent(e, new MaterialRef { Value = m });
                return e;
            }
        }
    }
}