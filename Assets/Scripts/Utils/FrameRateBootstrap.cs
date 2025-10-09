using System;
using UnityEngine;

namespace Utils
{
    public class FrameRateBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            QualitySettings.vSyncCount = 1;
            Application.targetFrameRate = 60;
            
            // Если захочешь 120:
#if UNITY_ANDROID
            var rr = Screen.currentResolution.refreshRateRatio.value; // Unity 6 API
            if (rr >= 120f) Application.targetFrameRate = 120;
#endif
            
            // опционально:
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }
    }
}