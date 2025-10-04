using ECS.Components;
using Unity.Entities;
using UnityEngine;

namespace ECS.Authoring
{
    public class SpawnPoolConfigAuthoring : MonoBehaviour
    {
        [Header("References")]
        public GameObject zombiePrefab;

        [Header("Pool Sizes")]
        public int initialCapacity = 1;   // стартовый размер пула
        public int refillThreshold = 2;    // при таком остатке можно будет дозаполнять (на следующих шагах)
        public int refillCount = 1; 
        
        private class SpawnPoolConfigAuthoringBaker : Baker<SpawnPoolConfigAuthoring>
        {
            public override void Bake(SpawnPoolConfigAuthoring authoring)
            {
                var e = GetEntity(TransformUsageFlags.None);

                var prefabEntity = GetEntity(authoring.zombiePrefab, TransformUsageFlags.None);

                AddComponent(e, new SpawnPoolConfig
                {
                    ZombiePrefabEntity = prefabEntity,
                    InitialCapacity = Mathf.Max(0, authoring.initialCapacity),
                    RefillThreshold = Mathf.Max(0, authoring.refillThreshold),
                    RefillCount = Mathf.Max(0, authoring.refillCount)
                });
            }
        }
    }
    
}