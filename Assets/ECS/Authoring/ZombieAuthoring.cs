using ECS.Components;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace ECS.Authoring
{
    public class ZombieAuthoring : MonoBehaviour
    {        
        [Header("Stats")]
        public float radius = 0.4f;
        public int health = 10;
        public float3 initialVelocity = 0;
        
        private class ZombieAuthoringBaker : Baker<ZombieAuthoring>
        {
            public override void Bake(ZombieAuthoring authoring)
            {
                // Превращаем сам префаб в энтити-префаб (важно: на объекте должен быть этот скрипт, а не в сцене)
                var e = GetEntity(TransformUsageFlags.None);

                AddComponent<ZombieTag>(e);
                AddComponent(e, new Radius { Value = authoring.radius });
                AddComponent(e, new Health { Value = authoring.health });
                AddComponent(e, new Velocity { Value = authoring.initialVelocity });
                
                // Эти два добавим, чтобы в спавне делать SetComponent без проверок
                AddComponent(e, new Position { Value = float3.zero });
                AddComponent(e, LocalTransform.FromPositionRotationScale(float3.zero, quaternion.identity, 1f));

                // Делаем его именно префабом ECS:
                AddComponent<Prefab>(e);
                // По умолчанию активные; при инстансе в пуле мы добавим InactiveTag.
            }
        }
    }
}