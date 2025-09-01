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

        void Awake()
        {
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        void Update()
        {
            if (Time.unscaledTime >= _nextRead)
            {
                _nextRead = Time.unscaledTime + 0.5f;

                var q_tagCounters = _em.CreateEntityQuery(ComponentType.ReadOnly<ECS.Components.TagCounters>());
                if (!q_tagCounters.IsEmpty)
                {
                    var e = q_tagCounters.GetSingletonEntity();
                    _last = _em.GetComponentData<ECS.Components.TagCounters>(e);
                }

                var q_WaveData = _em.CreateEntityQuery(ComponentType.ReadOnly<ECS.Components.SpawnDebug>());

                if (!q_WaveData.IsEmpty)
                {
                    var e = q_WaveData.GetSingletonEntity();
                    _debug = _em.GetComponentData<ECS.Components.SpawnDebug>(e);
                }
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
                             $"Time to next: {_debug.TimeToNextWave}";

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