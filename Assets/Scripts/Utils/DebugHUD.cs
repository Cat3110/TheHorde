using System;
using TMPro;
using Unity.Entities;
using UnityEngine;

namespace Utils
{
    public class DebugHUD : MonoBehaviour
    {
        [Header("UI")]
        public TextMeshProUGUI label;

        private float _deltaTime;
        private int _ticks;
        private float _timer;
        
        private Unity.Entities.EntityManager _em;
        private float _nextRead;
        private ECS.Components.TagCounters _last;
        private ECS.Components.SpawnDebug _debug;
        private ECS.Components.SteeringStats _steering;
        
        // поля класса
        private EntityQuery _qTagCounters;
        private EntityQuery _qSpawnDebug;
        private EntityQuery _qSteering;

        void Awake()
        {
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
            
            // создаём ОДИН РАЗ
            _qTagCounters = _em.CreateEntityQuery(ComponentType.ReadOnly<ECS.Components.TagCounters>());
            _qSpawnDebug  = _em.CreateEntityQuery(ComponentType.ReadOnly<ECS.Components.SpawnDebug>());
            _qSteering    = _em.CreateEntityQuery(ComponentType.ReadOnly<ECS.Components.SteeringStats>());
        }

        void Update()
        {
            if (Time.unscaledTime >= _nextRead)
            {
                _nextRead = Time.unscaledTime + 0.5f;

                if (!_qTagCounters.IsEmpty)
                    _last = _em.GetComponentData<ECS.Components.TagCounters>(_qTagCounters.GetSingletonEntity());

                if (!_qSpawnDebug.IsEmpty)
                    _debug = _em.GetComponentData<ECS.Components.SpawnDebug>(_qSpawnDebug.GetSingletonEntity());

                if (!_qSteering.IsEmpty)
                    _steering = _em.GetComponentData<ECS.Components.SteeringStats>(_qSteering.GetSingletonEntity());
            }
            
            // FPS (экспоненциальное сглаживание)
            _deltaTime += (Time.unscaledDeltaTime - _deltaTime) * 0.1f;
            float fps = 1f / _deltaTime;
            float ms = _deltaTime * 1000f;

            // FixedUpdate частота
            _timer += Time.unscaledDeltaTime;
            if (_timer >= 1f)
            {
                label.text = $"{fps:0.} FPS ({ms:0.0} ms)\n" +
                             $"{_ticks} FixedTicks\n\n" +
                             $"Players: {_last.Players}\n" +
                             $"Zombies: {_last.Zombies}\n\n" +
                             $"Active Zombies: {_debug.ActiveZombies}\n" +
                             $"Inactive: {_debug.InactiveZombies}\n" +
                             $"Wave: {_debug.WaveIndex}\n" +
                             $"Time to next: {_debug.TimeToNextWave}\n\n" +
                             $"Standing: {_steering.StandingZombies} / {_steering.ActiveZombies} " +
                             $"({_steering.StandingRatio:P0})";

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