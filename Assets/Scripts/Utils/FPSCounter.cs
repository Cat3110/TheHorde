using TMPro;
using UnityEngine;

namespace Utils
{
    public class FPSCounter : MonoBehaviour
    {
        public TextMeshProUGUI label;
        private float _deltaTime;

        void Update()
        {
            // Экспоненциальное сглаживание
            _deltaTime += (Time.unscaledDeltaTime - _deltaTime) * 0.1f;

            float fps = 1f / _deltaTime;
            float ms = _deltaTime * 1000f;

            label.text = $"{fps:0.} FPS ({ms:0.0} ms)";

            // Цветовая подсветка
            if (fps >= 55) label.color = Color.green;
            else if (fps >= 40) label.color = Color.yellow;
            else label.color = Color.red;
        }
    }
}