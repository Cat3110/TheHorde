using TMPro;
using UnityEngine;

namespace Utils
{
    public class FPSCounter : MonoBehaviour 
    {
        public TextMeshProUGUI label;
        private int _frames;
        private float _time;
        
        void Update()
        { 
            _frames++; 
            _time += Time.unscaledDeltaTime;
            if (_time >= 1f)
            {
                label.text = $"{Mathf.RoundToInt(_frames/_time)} FPS"; 
                _frames = 0; 
                _time = 0; 
            } 
        }
    }
}