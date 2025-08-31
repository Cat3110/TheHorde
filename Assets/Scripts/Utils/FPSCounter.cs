using TMPro;
using UnityEngine;

namespace Utils
{
    public class FPSCounter : MonoBehaviour
    {
        public TextMeshProUGUI label;

        private int _ticks;
        private float _timer;

        void FixedUpdate()
        {
            _ticks++;
        }

        void Update()
        {
            _timer += Time.unscaledDeltaTime;
            if (_timer >= 1f)
            {
                label.text = $"{_ticks} FixedTicks/s";
                _ticks = 0;
                _timer = 0f;
            }
        }
    }
}