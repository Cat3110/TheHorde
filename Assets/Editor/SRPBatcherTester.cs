using Unity.Profiling;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Editor
{
    public class SRPBatcherTester : EditorWindow
    {
        private static readonly ProfilerRecorder RenderLoopDraw =
            ProfilerRecorder.StartNew(ProfilerCategory.Render, "RenderLoop.Draw");

        private const int MaxFrames = 60;
        private readonly float[] _history = new float[MaxFrames];
        private int _frameIndex = 0;

        [MenuItem("Tools/SRP Batcher Tester")]
        public static void ShowWindow()
        {
            GetWindow<SRPBatcherTester>("SRP Batcher Tester");
        }

        void Update()
        {
            if (RenderLoopDraw.Valid)
            {
                // Convert nanoseconds → milliseconds
                float ms = RenderLoopDraw.LastValue / 1_000_000f;
                _history[_frameIndex] = ms;
                _frameIndex = (_frameIndex + 1) % MaxFrames;
                Repaint();
            }
        }

        void OnGUI()
        {
            GUILayout.Label("SRP Batcher Control", EditorStyles.boldLabel);

            if (UniversalRenderPipeline.asset == null)
            {
                EditorGUILayout.HelpBox("URP Asset не назначен!", MessageType.Error);
                return;
            }

            EditorGUILayout.LabelField("Состояние:",
                UniversalRenderPipeline.asset.useSRPBatcher ? "Включен" : "Выключен");

            GUILayout.Space(10);

            if (GUILayout.Button("Включить SRP Batcher"))
            {
                UniversalRenderPipeline.asset.useSRPBatcher = true;
                Debug.Log("SRP Batcher включен");
            }

            if (GUILayout.Button("Выключить SRP Batcher"))
            {
                UniversalRenderPipeline.asset.useSRPBatcher = false;
                Debug.Log("SRP Batcher выключен");
            }

            GUILayout.Space(20);

            // Текущее значение
            float current = RenderLoopDraw.Valid ? RenderLoopDraw.LastValue / 1_000_000f : 0f;
            EditorGUILayout.LabelField("RenderLoop.Draw CPU Time:", current.ToString("F3") + " ms");

            GUILayout.Space(10);

            // Мини-график последних 60 кадров
            Rect rect = GUILayoutUtility.GetRect(position.width - 20, 100);
            EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f));

            if (Event.current.type == EventType.Repaint && RenderLoopDraw.Valid)
            {
                Handles.color = Color.green;
                float maxVal = 0.1f; // минимальная шкала
                foreach (var v in _history)
                    if (v > maxVal) maxVal = v;

                for (int i = 0; i < MaxFrames - 1; i++)
                {
                    int idx0 = (_frameIndex + i) % MaxFrames;
                    int idx1 = (_frameIndex + i + 1) % MaxFrames;

                    float x0 = rect.x + (i / (float)(MaxFrames - 1)) * rect.width;
                    float x1 = rect.x + ((i + 1) / (float)(MaxFrames - 1)) * rect.width;

                    float y0 = rect.yMax - (_history[idx0] / maxVal) * rect.height;
                    float y1 = rect.yMax - (_history[idx1] / maxVal) * rect.height;

                    Handles.DrawLine(new Vector3(x0, y0), new Vector3(x1, y1));
                }

                // подпись шкалы
                GUI.Label(new Rect(rect.x, rect.y, 80, 20), maxVal.ToString("F2") + " ms");
            }
        }
    }
}