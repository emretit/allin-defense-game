using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Profiling;

namespace DefenderChronicles.Core
{
    /// <summary>
    /// Development-build telemetry for the physical iPhone acceptance gate.
    /// It records performance only; it never changes gameplay or release builds.
    /// </summary>
    public sealed class DeviceAcceptanceProbe : MonoBehaviour
    {
        private const float ReportIntervalSeconds = 10f;
        private const float TargetFrameMilliseconds = 1000f / 30f;
        private readonly List<float> frameTimes = new(65536);
        private GameSessionController session;
        private float samplingStartsAt;
        private float nextReport;
        private float consecutiveUnderThirty;
        private float maxUnderThirty;
        private long peakAllocatedBytes;
        private long firstWaveAllocatedBytes;
        private bool lowMemoryWarning;
        private bool finalReported;
        private string currentScenario = "preparation";

        public float MedianFps { get; private set; }
        public float P95FrameMilliseconds { get; private set; }

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern int DCGetThermalState();
#endif

        private void Awake()
        {
            session = GetComponent<GameSessionController>();
            samplingStartsAt = Time.realtimeSinceStartup + 3f;
            nextReport = samplingStartsAt + ReportIntervalSeconds;
            Application.lowMemory += OnLowMemory;
            Debug.Log(FormattableString.Invariant(
                $"[DC_DEVICE_STARTUP] preparing_seconds={Time.realtimeSinceStartup:F2}"));
        }

        private void OnDestroy()
        {
            Application.lowMemory -= OnLowMemory;
        }

        private void Update()
        {
            float frameMilliseconds = Time.unscaledDeltaTime * 1000f;
            if (Time.realtimeSinceStartup < samplingStartsAt) return;

            if (frameMilliseconds > 0f && frameMilliseconds < 1000f) frameTimes.Add(frameMilliseconds);

            if (frameMilliseconds > TargetFrameMilliseconds)
            {
                consecutiveUnderThirty += Time.unscaledDeltaTime;
                maxUnderThirty = Mathf.Max(maxUnderThirty, consecutiveUnderThirty);
            }
            else
            {
                consecutiveUnderThirty = 0f;
            }

            long allocatedBytes = Profiler.GetTotalAllocatedMemoryLong();
            peakAllocatedBytes = Math.Max(peakAllocatedBytes, allocatedBytes);
            if (firstWaveAllocatedBytes == 0 && session != null && session.CurrentWaveIndex >= 1 &&
                session.Phase == GamePhase.Wave)
                firstWaveAllocatedBytes = allocatedBytes;

            if (Time.realtimeSinceStartup >= nextReport)
            {
                LogMetrics(currentScenario, allocatedBytes);
                nextReport += ReportIntervalSeconds;
            }

            if (!finalReported && session != null &&
                (session.Phase == GamePhase.Victory || session.Phase == GamePhase.Defeat))
            {
                finalReported = true;
                LogMetrics(session.Phase.ToString().ToLowerInvariant(), allocatedBytes);
            }
        }

        public void MarkScenario(string scenario)
        {
            currentScenario = string.IsNullOrWhiteSpace(scenario) ? "unknown" : scenario;
            Debug.Log(FormattableString.Invariant(
                $"[DC_DEVICE_SCENARIO] stage={currentScenario} realtime_seconds={Time.realtimeSinceStartup:F2}"));
        }

        public void LogCheckpoint(string stage)
        {
            LogMetrics(string.IsNullOrWhiteSpace(stage) ? currentScenario : stage,
                Profiler.GetTotalAllocatedMemoryLong());
        }

        public bool HasLowMemoryWarning => lowMemoryWarning;

        public bool MeetsPerformanceGate => frameTimes.Count > 0 && MedianFps >= 55f &&
                                            P95FrameMilliseconds <= 18f && !lowMemoryWarning;

        private void OnLowMemory()
        {
            lowMemoryWarning = true;
            Debug.LogError("[DC_DEVICE_MEMORY_WARNING] iOS reported low memory.");
        }

        private void LogMetrics(string stage, long allocatedBytes)
        {
            if (frameTimes.Count == 0) return;
            var sorted = frameTimes.ToArray();
            Array.Sort(sorted);
            float medianMs = Percentile(sorted, 0.50f);
            float p95Ms = Percentile(sorted, 0.95f);
            float medianFps = medianMs <= 0f ? 0f : 1000f / medianMs;
            MedianFps = medianFps;
            P95FrameMilliseconds = p95Ms;
            float memoryGrowth = firstWaveAllocatedBytes <= 0
                ? 0f
                : (allocatedBytes - firstWaveAllocatedBytes) * 100f / firstWaveAllocatedBytes;
            Debug.Log(string.Format(CultureInfo.InvariantCulture,
                "[DC_DEVICE_METRICS] stage={0} median_fps={1:F1} p95_frame_ms={2:F1} " +
                "peak_memory_mb={3:F1} memory_growth_pct={4:F1} max_under_30_seconds={5:F2} " +
                "thermal={6} low_memory_warning={7}", stage, medianFps, p95Ms,
                BytesToMegabytes(peakAllocatedBytes), memoryGrowth, maxUnderThirty, ThermalState(), lowMemoryWarning));
        }

        private static float Percentile(float[] sorted, float percentile)
        {
            int index = Mathf.Clamp(Mathf.CeilToInt(sorted.Length * percentile) - 1, 0, sorted.Length - 1);
            return sorted[index];
        }

        private static float BytesToMegabytes(long bytes) => bytes / (1024f * 1024f);

        private static string ThermalState()
        {
#if UNITY_IOS && !UNITY_EDITOR
            return DCGetThermalState() switch
            {
                0 => "nominal",
                1 => "fair",
                2 => "serious",
                3 => "critical",
                _ => "unknown"
            };
#else
            return "unavailable";
#endif
        }
    }
}
