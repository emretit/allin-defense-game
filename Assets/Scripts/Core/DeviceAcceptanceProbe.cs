using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Profiling;

namespace AllinDefense.Core
{
    /// <summary>
    /// Development-build telemetry for the physical iPhone acceptance gate.
    /// It records performance only; it never changes gameplay or release builds.
    /// </summary>
    public sealed class DeviceAcceptanceProbe : MonoBehaviour
    {
        private const float ReportIntervalSeconds = 10f;
        private const float TargetFrameMilliseconds = 1000f / 30f;
        private List<float> frameTimes = new(4096);
        private readonly Dictionary<string,List<float>> stageFrames = new();
        private float seriousThermalSeconds;
        private float longestSeriousThermalSeconds;
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
            foreach (var entry in stageFrames)
            {
                frameTimes = entry.Value;
                LogMetrics(entry.Key, Profiler.GetTotalAllocatedMemoryLong());
            }
        }

        private void Update()
        {
            float frameMilliseconds = Time.unscaledDeltaTime * 1000f;
            if (Time.realtimeSinceStartup < samplingStartsAt) return;

            if (session != null && session.Phase is GamePhase.Paused or GamePhase.Victory or GamePhase.Defeat) return;
            string stage = session == null || session.Phase != GamePhase.Wave ? "preparation"
                : session.Enemies.Any(e => e != null && e.Archetype == Data.EnemyArchetype.Boss) ? "boss"
                : session.Enemies.Count >= 18 ? "dense" : "normal";
            currentScenario = stage;
            if (!stageFrames.TryGetValue(stage, out frameTimes)) stageFrames[stage] = frameTimes = new List<float>(4096);
            string thermal = ThermalState();
            seriousThermalSeconds = thermal is "serious" or "critical" ? seriousThermalSeconds + Time.unscaledDeltaTime : 0f;
            longestSeriousThermalSeconds = Mathf.Max(longestSeriousThermalSeconds, seriousThermalSeconds);
            if (frameMilliseconds > 0f) frameTimes.Add(frameMilliseconds);

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

        public bool MeetsPerformanceGate => !lowMemoryWarning && longestSeriousThermalSeconds < 30f &&
            new[] { "preparation", "normal", "dense", "boss" }.All(stage =>
            {
                if (!stageFrames.TryGetValue(stage,out var samples) || samples.Count < 60) return false;
                var sorted = samples.OrderBy(x=>x).ToArray();
                return 1000f/Percentile(sorted,.5f) >= 55f && Percentile(sorted,.95f) <= 18f;
            });

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
            Debug.Log($"[DC_PROFILE_COST] build={ClosedTestBuild.Id} stage={stage} simulation_ms={session?.LastSimulationMilliseconds:F3} hud_ms={session?.LastHudMilliseconds:F3} enemies={session?.Enemies.Count} samples={frameTimes.Count}");
            Debug.Log(FormattableString.Invariant($"[DC_MEMORY_SAMPLE] level={session?.CampaignLevelNumber} wave={session?.CurrentWaveIndex} battle_seconds={session?.BattleElapsedSeconds:F1} current_mb={BytesToMegabytes(allocatedBytes):F1} serious_thermal_seconds={longestSeriousThermalSeconds:F1}"));
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
