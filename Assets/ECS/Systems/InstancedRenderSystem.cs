using ECS.Authoring;
using ECS.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

namespace ECS.Systems
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class InstancedRenderSystem : SystemBase
    {
        // Персистентные буферы под матрицы (без GC)
        private NativeList<Matrix4x4> _playerMatrices;
        private NativeList<Matrix4x4> _zombieMatrices;

        // MPB для цвета/параметров (по батчу)
        private MaterialPropertyBlock _mpbPlayer;
        private MaterialPropertyBlock _mpbZombie;

        // Кэш ссылок на ресурсы (managed singleton)
        private RenderResources _res;
        // Переиспользуемый managed-буфер под DrawMeshInstanced
        private Matrix4x4[] _batchBuffer;
        
        protected override void OnCreate()
        {
            RequireForUpdate<RenderResources>();   // ждём ресурсы
            _playerMatrices = new NativeList<Matrix4x4>(Allocator.Persistent);
            _zombieMatrices = new NativeList<Matrix4x4>(Allocator.Persistent);

            _mpbPlayer = new MaterialPropertyBlock();
            _mpbZombie = new MaterialPropertyBlock();

            // Цвета по умолчанию (URP/Lit: _BaseColor)
            _mpbPlayer.SetColor("_BaseColor", new Color(0.2f, 0.6f, 1f, 1f));
            _mpbZombie.SetColor("_BaseColor", new Color(0.9f, 0.3f, 0.3f, 1f));
            _batchBuffer = new Matrix4x4[256]; // стартовый размер; будет расширяться по мере нужды
        }
        
        protected override void OnUpdate()
        {
            if (_res == null)
            {
                _res = SystemAPI.ManagedAPI.GetSingleton<RenderResources>();
                if (_res == null) return;
                if (_res.InstancedMaterial != null && !_res.InstancedMaterial.enableInstancing)
                    _res.InstancedMaterial.enableInstancing = true;
            }

            _playerMatrices.Clear();
            _zombieMatrices.Clear();

            // Собираем матрицы трансформа (мейн-тред, без Burst, без GC)
            foreach (var (ltw, _) in SystemAPI.Query<RefRO<LocalToWorld>, RefRO<PlayerTag>>())
                _playerMatrices.Add(ltw.ValueRO.Value);

            foreach (var (ltw, _) in SystemAPI.Query<RefRO<LocalToWorld>, RefRO<ZombieTag>>()
                         .WithAll<InactiveTag>()
                         .WithDisabled<InactiveTag>()) // рисуем только активных (InactiveTag выключен)
            {
                _zombieMatrices.Add(ltw.ValueRO.Value);
            }

            // Рисуем батчами ≤1023
            DrawBatched(_res.PlayerMesh, _playerMatrices, _res.InstancedMaterial, _mpbPlayer, ref _batchBuffer);
            DrawBatched(_res.ZombieMesh, _zombieMatrices, _res.InstancedMaterial, _mpbZombie, ref _batchBuffer);
            
        }
        
        protected override void OnDestroy()
        {
            if (_playerMatrices.IsCreated) _playerMatrices.Dispose();
            if (_zombieMatrices.IsCreated) _zombieMatrices.Dispose();
        }
        
        private static void EnsureCapacity(ref Matrix4x4[] buffer, int needed)
        {
            if (buffer != null && buffer.Length >= needed)
                return;
            int newLen = (buffer == null || buffer.Length == 0) ? 256 : buffer.Length;
            while (newLen < needed) newLen *= 2;
            System.Array.Resize(ref buffer, newLen); // редкая аллокация только при росте
        }

        private static void DrawBatched(
            Mesh mesh,
            NativeList<Matrix4x4> matrices,
            Material mat,
            MaterialPropertyBlock mpb,
            ref Matrix4x4[] batchBuffer)
        {
            if (mesh == null || mat == null) return;

            const int MaxPerBatch = 1023;
            int total = matrices.Length;
            if (total == 0) return;

            int offset = 0;
            var src = matrices.AsArray();
            while (offset < total)
            {
                int count = math.min(MaxPerBatch, total - offset);
                EnsureCapacity(ref batchBuffer, count);

                // Копируем из NativeList в managed буфер
                for (int i = 0; i < count; i++)
                    batchBuffer[i] = src[offset + i];

                Graphics.DrawMeshInstanced(
                    mesh, 0, mat,
                    batchBuffer, count,
                    mpb,
                    ShadowCastingMode.Off, receiveShadows: false,
                    layer: 0, camera: null,
                    LightProbeUsage.Off, lightProbeProxyVolume: null
                );

                offset += count;
            }
        }
    }
}