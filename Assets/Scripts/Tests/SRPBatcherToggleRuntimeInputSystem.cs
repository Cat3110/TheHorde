using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

// <-- новый инпут

namespace Tests
{
    public class SRPBatcherToggleRuntimeInputSystem : MonoBehaviour
    {
        private InputAction _toggleAction;

        void OnEnable()
        {
            // Клавиатура: клавиша B, плюс на всякий случай Start на геймпаде
            _toggleAction = new InputAction("ToggleSRPB",
                binding: "<Keyboard>/b");
            _toggleAction.AddBinding("<Gamepad>/start");

            _toggleAction.performed += ctx => Toggle();
            _toggleAction.Enable();
        }

        void OnDisable()
        {
            // ReSharper disable once EventUnsubscriptionViaAnonymousDelegate
            _toggleAction.performed -= ctx => Toggle();
            _toggleAction.Disable();
        }

        public void Toggle() // публичный метод — удобно вешать на UI-кнопку
        {
            if (UniversalRenderPipeline.asset == null) return;

            UniversalRenderPipeline.asset.useSRPBatcher = !UniversalRenderPipeline.asset.useSRPBatcher;
            Debug.Log("SRP Batcher: " + (UniversalRenderPipeline.asset.useSRPBatcher ? "ON" : "OFF"));
        }
    }
}