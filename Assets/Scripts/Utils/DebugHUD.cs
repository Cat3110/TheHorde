using System;
using TMPro;
using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using ECS.Components;

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
        
        private EntityQuery _qTagCounters;
        private EntityQuery _qSpawnDebug;
        private EntityQuery _qSteering;
        private EntityQuery _qPlayerFull;
        private EntityQuery _qDamageStats;
        private EntityQuery _qDeathStats;
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
            
        }

        void Update()
        {
            if (label == null)
                return;

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
                label.SetText(
                    $"{fps:0.} FPS ({ms:0.0} ms)\n" +
                    $"{ticksPerSec:0.} FixedTicks/s\n\n" +
                    $"Players: {_last.Players}\n" +
                    $"Player Pos: {_playerPos.x:0.0}, {_playerPos.y:0.0}, {_playerPos.z:0.0}\n" +
                    $"Player HP: {_health.Value}\n" +
                    $"Zombies: {_last.Zombies}\n\n" +
                    $"Active Zombies: {_debug.ActiveZombies}\n" +
                    $"Inactive: {_debug.InactiveZombies}\n" +
                    $"Wave: {_debug.WaveIndex}\n" +
                    $"Time to next: {_debug.TimeToNextWave}\n\n" +
                    $"Standing: {_steering.StandingZombies} / {_steering.ActiveZombies} " +
                    $"({_steering.StandingRatio:P0})\n\n" +
                    $"DamageEvents/frame: {_damage.ProcessedThisFrame}" +
                    $"\nDeaths/frame: {_deaths.DeathsThisFrame}" +
                    $"\nDeaths total: {_deaths.TotalDeaths}"
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