using ECS.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

namespace ECS.Systems
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class InstancedRenderSystem : SystemBase
    {
        EntityQuery _zombiesQ;
        EntityQuery _playersQ;

        List<Matrix4x4> _matrices;

        protected override void OnCreate()
        {
            RequireForUpdate<RenderConfig>();
            _zombiesQ = SystemAPI.QueryBuilder()
                .WithAll<LocalTransform, ZombieTag>()
                .WithDisabled<InactiveTag>()
                .Build();

            _playersQ = SystemAPI.QueryBuilder()
                .WithAll<LocalTransform, PlayerTag>()
                .Build();

            // Синглтон для HUD
            if (!SystemAPI.TryGetSingleton<RenderStats>(out _))
            {
                var e = EntityManager.CreateEntity();
                EntityManager.AddComponentData(e, new RenderStats());
            }

            _matrices = new List<Matrix4x4>(1023);
        }

        protected override void OnDestroy()
        {
        }

        protected override void OnUpdate()
        {
            var cfg = SystemAPI.GetSingleton<RenderConfig>();

            var zombieMeshRef = SystemAPI.GetComponent<MeshRef>(cfg.ZombieMeshEntity).Value;
            var playerMeshRef = SystemAPI.GetComponent<MeshRef>(cfg.PlayerMeshEntity).Value;
            Mesh zombieMesh = zombieMeshRef.IsValid() ? zombieMeshRef.Value : null;
            Mesh playerMesh = playerMeshRef.IsValid() ? playerMeshRef.Value : null;

            var zombieMatRef = SystemAPI.GetComponent<MaterialRef>(cfg.ZombieMatEntity).Value;
            var playerMatRef = SystemAPI.GetComponent<MaterialRef>(cfg.PlayerMatEntity).Value;
            Material zombieMat = zombieMatRef.IsValid() ? zombieMatRef.Value : null;
            Material playerMat = playerMatRef.IsValid() ? playerMatRef.Value : null;

            if (zombieMat != null && !zombieMat.enableInstancing) zombieMat.enableInstancing = true;
            if (playerMat != null && !playerMat.enableInstancing) playerMat.enableInstancing = true;

            int batches = 0, instances = 0;

            // Зомби
            batches += DrawGroup(_zombiesQ, zombieMesh, zombieMat, ref instances);

            // Игрок (сферу тоже рисуем инстансом — единый путь)
            batches += DrawGroup(_playersQ, playerMesh, playerMat, ref instances);

            // Обновляем RenderStats для HUD
            var statsEntity = SystemAPI.GetSingletonEntity<RenderStats>();
            EntityManager.SetComponentData(statsEntity, new RenderStats
            {
                BatchesThisFrame = batches,
                InstancesThisFrame = instances
            });
        }

        int DrawGroup(EntityQuery q, Mesh mesh, Material mat, ref int instancesTotal)
        {
            if (mesh == null || mat == null) return 0;

            var xforms = q.ToComponentDataArray<LocalTransform>(Allocator.Temp);
            int count = xforms.Length;
            if (count == 0) { xforms.Dispose(); return 0; }

            int batches = 0;
            _matrices.Clear();

            // Пакуем по 1023
            for (int i = 0; i < count; i++)
            {
                float3 pos = xforms[i].Position;
                quaternion rot = xforms[i].Rotation;
                float s = xforms[i].Scale;
                _matrices.Add(Matrix4x4.TRS(pos, rot, new float3(s, s, s)));

                if (_matrices.Count == 1023 || i == count - 1)
                {
                    Graphics.DrawMeshInstanced(
                        mesh, 0, mat,
                        _matrices,
                        null,
                        ShadowCastingMode.Off,
                        false,
                        0, null,
                        LightProbeUsage.Off, null
                    );
                    batches++;
                    instancesTotal += _matrices.Count;
                    _matrices.Clear();
                }
            }

            xforms.Dispose();
            return batches;
        }
    }
}