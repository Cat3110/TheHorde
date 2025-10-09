using System;
using Unity.Profiling;

namespace Utils
{
public static class PerformanceMonitor
    {
        private const int Capacity = 300; // ~5 seconds @ 60 FPS

        private static ProfilerRecorder _cpuTime = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 1);
        private static ProfilerRecorder _gpuTime = ProfilerRecorder.StartNew(ProfilerCategory.Render, "GPU Frame Time", 1);
        private static ProfilerRecorder _usedMem = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Total Used Memory", 1);

        private static readonly float[] _cpu = new float[Capacity];
        private static readonly float[] _gpu = new float[Capacity];
        private static readonly float[] _tmp = new float[Capacity]; // reusable for percentile calc
        private static int _count;
        private static int _index;

        public static void Sample()
        {
            // recorders may be unsupported on some platforms; guard with Valid
            float cpuMs = _cpuTime.Valid ? (float)(_cpuTime.LastValue * 1e-6) : 0f;
            float gpuMs = _gpuTime.Valid ? (float)(_gpuTime.LastValue * 1e-6) : 0f;

            _cpu[_index] = cpuMs;
            _gpu[_index] = gpuMs;

            _index = (_index + 1) % Capacity;
            if (_count < Capacity) _count++;
        }

        public static float CpuMsAvg() => Avg(_cpu, _count);
        public static float GpuMsAvg() => Avg(_gpu, _count);
        public static float CpuMsP95() => Pctl95(_cpu, _count);
        public static float GpuMsP95() => Pctl95(_gpu, _count);

        public static float MemoryMB()
        {
            if (_usedMem.Valid)
            {
                // _usedMem reports bytes
                return (float)(_usedMem.LastValue / (1024.0 * 1024.0));
            }
            // Fallback: managed heap only
            return (float)(GC.GetTotalMemory(false) / (1024.0 * 1024.0));
        }

        private static float Avg(float[] buf, int count)
        {
            if (count == 0) return 0f;
            double sum = 0;
            for (int i = 0; i < count; i++) sum += buf[i];
            return (float)(sum / count);
        }

        private static float Pctl95(float[] buf, int count)
        {
            if (count == 0) return 0f;
            // copy to reusable temp to avoid GC; O(n log n) but small n
            for (int i = 0; i < count; i++) _tmp[i] = buf[i];
            Array.Sort(_tmp, 0, count);
            int idx = (int)Math.Floor((count - 1) * 0.95f);
            return _tmp[idx];
        }
    }
}