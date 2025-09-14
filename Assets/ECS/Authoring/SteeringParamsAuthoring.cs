using ECS.Components;
using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;

namespace ECS.Authoring
{
    public class SteeringParamsAuthoring : MonoBehaviour
    {
        public float cellSize       = 1.0f;
        public float neighborRadius = 1.5f;
        public int   maxNeighbors   = 6;
        public float avoidWeight    = 1.8f;
        public float targetWeight   = 1.0f;
        public float maxSpeed       = 2.6f;
        public float turnRate       = 6.0f;
        public float stopVelEps     = 0.05f;

        private class SteeringParamsAuthoringBaker : Baker<SteeringParamsAuthoring>
        {
            public override void Bake(SteeringParamsAuthoring authoring)
            {
                var e = GetEntity(Unity.Entities.TransformUsageFlags.None);
                AddComponent(e, new SteeringParams
                {
                    CellSize       = math.max(0.25f, authoring.cellSize),
                    NeighborRadius = math.max(0.3f,  authoring.neighborRadius),
                    MaxNeighbors   = math.max(1,     authoring.maxNeighbors),
                    AvoidWeight    = authoring.avoidWeight,
                    TargetWeight   = authoring.targetWeight,
                    MaxSpeed       = math.max(0.01f, authoring.maxSpeed),
                    TurnRate       = math.max(0.01f, authoring.turnRate),
                    StopVelEps     = math.max(0.0f,  authoring.stopVelEps)
                });
            }
        }
    }
}