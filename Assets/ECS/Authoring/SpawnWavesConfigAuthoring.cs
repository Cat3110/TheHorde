using ECS.Components;
using Unity.Entities;
using UnityEngine;

namespace ECS.Authoring
{
    public class SpawnWavesConfigAuthoring : MonoBehaviour
    {        
        [Header("Wave")]
        public int zombiesPerWave = 50;     // сколько активировать за волну
        public float waveInterval = 2.5f;   // период между волнами, сек

        [Header("Placement")]
        public float spawnRadius = 18f;     // радиус окружности вокруг игрока
        public float spawnJitter = 2f;      // разброс

        [Header("Defaults")]
        public int spawnHealth = 10;        // базовое здоровье спавна
        public float initialSpeed = 2.0f;
        
        private class SpawnWavesConfigAuthoringBaker : Baker<SpawnWavesConfigAuthoring>
        {
            public override void Bake(SpawnWavesConfigAuthoring authoring)
            {
                var e = GetEntity(TransformUsageFlags.None);
                AddComponent(e, new SpawnWavesConfig
                {
                    ZombiesPerWave = Mathf.Max(1, authoring.zombiesPerWave),
                    WaveInterval   = Mathf.Max(0.1f, authoring.waveInterval),
                    SpawnRadius    = Mathf.Max(0f, authoring.spawnRadius),
                    SpawnJitter    = Mathf.Max(0f, authoring.spawnJitter),
                    SpawnHealth    = Mathf.Max(1, authoring.spawnHealth),
                    InitialSpeed   = authoring.initialSpeed
                });
                // Стартовое состояние (seed любой ≠ 0)
                AddComponent(e, new SpawnWaveState { Timer = 0f, RngState = 0xA2C2_79B5u, WaveIndex = 0 });
            }
        }
    }
}