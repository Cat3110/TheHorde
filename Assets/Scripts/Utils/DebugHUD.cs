using System;
using TMPro;
using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using ECS.Components;
using Unity.Profiling;

namespace Utils
{
    public class DebugHUD : MonoBehaviour
    {
        [Header("UI")]
        public TextMeshProUGUI label;

        private float _deltaTime;
        private int _ticks;
        private float _timer;
        
        private EntityManager _em;
        private float _nextRead;
        private TagCounters _last;
        private SpawnDebug _debug;
        private SteeringStats _steering;
        private DamageStats _damage;
        private Health _health;
        private DeathStats _deaths;
        private RenderStats _render;
        
        private EntityQuery _qTagCounters;
        private EntityQuery _qSpawnDebug;
        private EntityQuery _qSteering;
        private EntityQuery _qPlayerFull;
        private EntityQuery _qDamageStats;
        private EntityQuery _qDeathStats;
        private EntityQuery _qRenderStats;
        private EntityQuery _qSpawnConfig;
        private EntityQuery _qSpawnState;
        private SpawnWavesConfig _spawnCfg;
        private SpawnWaveState _spawnState;
        private float3 _playerPos;

        void Awake()
        {
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
            
            // создаём ОДИН РАЗ
            _qTagCounters = _em.CreateEntityQuery(ComponentType.ReadOnly<TagCounters>());
            _qSpawnDebug  = _em.CreateEntityQuery(ComponentType.ReadOnly<SpawnDebug>());
            _qSteering    = _em.CreateEntityQuery(ComponentType.ReadOnly<SteeringStats>());
            _qPlayerFull  = _em.CreateEntityQuery(ComponentType.ReadOnly<PlayerTag>(), ComponentType.ReadOnly<Position>(), ComponentType.ReadOnly<Health>());
            _qDamageStats = _em.CreateEntityQuery(ComponentType.ReadOnly<DamageStats>());
            _qDeathStats = _em.CreateEntityQuery(ComponentType.ReadOnly<DeathStats>());
            _qRenderStats = _em.CreateEntityQuery(ComponentType.ReadOnly<RenderStats>());
            _qSpawnConfig = _em.CreateEntityQuery(ComponentType.ReadOnly<SpawnWavesConfig>());
            _qSpawnState  = _em.CreateEntityQuery(ComponentType.ReadOnly<SpawnWaveState>());
        }

        void Update()
        {
            if (label == null)
                return;

            PerformanceMonitor.Sample();

            if (Time.unscaledTime >= _nextRead)
            {
                _nextRead = Time.unscaledTime + 0.5f;

                if (!_qTagCounters.IsEmpty)
                    _last = _em.GetComponentData<TagCounters>(_qTagCounters.GetSingletonEntity());

                if (!_qSpawnDebug.IsEmpty)
                    _debug = _em.GetComponentData<SpawnDebug>(_qSpawnDebug.GetSingletonEntity());

                if (!_qSteering.IsEmpty)
                    _steering = _em.GetComponentData<SteeringStats>(_qSteering.GetSingletonEntity());

                if (!_qPlayerFull.IsEmpty)
                {
                    var playerEntity = _qPlayerFull.GetSingletonEntity();
                    _playerPos = _em.GetComponentData<Position>(playerEntity).Value;
                    _health    = _em.GetComponentData<Health>(playerEntity);
                }
                
                if (!_qDamageStats.IsEmpty)
                    _damage = _em.GetComponentData<DamageStats>(_qDamageStats.GetSingletonEntity());

                if (!_qDeathStats.IsEmpty)
                    _deaths = _em.GetComponentData<DeathStats>(_qDeathStats.GetSingletonEntity());
                
                if (!_qRenderStats.IsEmpty)
                    _render = _em.GetComponentData<RenderStats>(_qRenderStats.GetSingletonEntity());
                
                if (!_qSpawnConfig.IsEmpty)
                    _spawnCfg = _em.GetComponentData<SpawnWavesConfig>(_qSpawnConfig.GetSingletonEntity());

                if (!_qSpawnState.IsEmpty)
                    _spawnState = _em.GetComponentData<SpawnWaveState>(_qSpawnState.GetSingletonEntity());
            }
            
            // FPS (экспоненциальное сглаживание)
            _deltaTime += (Time.unscaledDeltaTime - _deltaTime) * 0.1f;
            float fps = 1f / _deltaTime;
            float ms = _deltaTime * 1000f;

            // FixedUpdate частота
            _timer += Time.unscaledDeltaTime;
            if (_timer >= 0.5f)
            {
                float ticksPerSec = _ticks / _timer;

            string envInfo;
#if UNITY_WEBGL && UNITY_WEBGL_THREADS
            envInfo = "WebGL: threads ON\n";
#elif UNITY_WEBGL
            envInfo = "WebGL: threads OFF\n";
#else
            envInfo = $"Platform: {Application.platform}\n";
#endif

            string seedInfo = (!_qSpawnState.IsEmpty) ? $"Seed (RngState): {_spawnState.RngState}\n" : string.Empty;

                label.SetText(
                    $"{fps:0.} FPS ({ms:0.0} ms)\n" +
                    $"{ticksPerSec:0.} FixedTicks/s\n" +
                    $"CPU: {PerformanceMonitor.CpuMsAvg():0.00} ms (p95 {PerformanceMonitor.CpuMsP95():0.00})\n" +
                    $"GPU: {PerformanceMonitor.GpuMsAvg():0.00} ms (p95 {PerformanceMonitor.GpuMsP95():0.00})\n" +
                    $"Mem: {PerformanceMonitor.MemoryMB():0.0} MB\n\n" +
                    $"Display Hz: {Screen.currentResolution.refreshRateRatio.value:0}\n" +
                    $"targetFrameRate: {Application.targetFrameRate}\n" +
                    $"vSyncCount: {QualitySettings.vSyncCount}\n\n" +
                    $"Players: {_last.Players}\n" +
                    $"Player Pos: {_playerPos.x:0.0}, {_playerPos.y:0.0}, {_playerPos.z:0.0}\n" +
                    $"Player HP: {_health.Value}\n" +
                    $"Zombies: {_last.Zombies}\n\n" +
                    $"Active Zombies: {_debug.ActiveZombies}\n" +
                    $"Inactive: {_debug.InactiveZombies}\n" +
                    $"Wave: {_debug.WaveIndex}\n" +
                    $"Time to next: {_debug.TimeToNextWave}\n\n" +
                    $"Standing: {_steering.StandingZombies} / {_steering.ActiveZombies} (" +
                    $"{_steering.StandingRatio:P0})\n\n" +
                    $"{envInfo}{seedInfo}" +
                    $"DamageEvents/frame: {_damage.ProcessedThisFrame}" +
                    $"\nDeaths/frame: {_deaths.DeathsThisFrame}" +
                    $"\nDeaths total: {_deaths.TotalDeaths}" +
                    $"\nBatches: {_render.BatchesThisFrame}" +
                    $"\nInstances: {_render.InstancesThisFrame}"
                );
                 

                // Цвет по FPS
                if (fps >= 55) label.color = Color.green;
                else if (fps >= 40) label.color = Color.yellow;
                else label.color = Color.red;

                _ticks = 0;
                _timer = 0f;
            }
        }

        void FixedUpdate()
        {
            _ticks++;
        }
    }
}