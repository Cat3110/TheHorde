using Unity.Entities;

namespace ECS.Components
{
    public struct RenderConfig : IComponentData
    {
        public Entity ZombieMeshEntity;
        public Entity PlayerMeshEntity;
        public Entity ZombieMatEntity;
        public Entity PlayerMatEntity;
    }
}