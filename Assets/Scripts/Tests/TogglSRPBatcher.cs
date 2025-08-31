using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Tests
{
    public class ToggleSRPBatcher : MonoBehaviour
    {
        [SerializeField] bool disableSRPBatcher = false;

        void Awake()
        {
            if (UniversalRenderPipeline.asset != null)
            {
                UniversalRenderPipeline.asset.useSRPBatcher = !disableSRPBatcher;
                Debug.Log("SRP Batcher " + (disableSRPBatcher ? "выключен" : "включен"));
            }
            else
            {
                Debug.LogWarning("URP Asset не найден!");
            }
        }
    }
}