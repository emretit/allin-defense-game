using System.Collections.Generic;
using UnityEngine;

namespace AllinDefense.UI
{
    public sealed class FrontendPerformanceProbe : MonoBehaviour
    {
        private readonly HashSet<string> visited = new();
        private string route;
        private double started;
        private float maxFrame;
        public void Begin(string name)
        {
            route = name; started = Time.realtimeSinceStartupAsDouble; maxFrame = 0;
        }
        private void Update()
        {
            if (route != null) maxFrame = Mathf.Max(maxFrame, Time.unscaledDeltaTime * 1000);
        }
        public void Complete()
        {
            if (route == null) return;
            if (Debug.isDebugBuild)
                Debug.Log($"[DC_FRONTEND_TIMING] route={route} cold={visited.Add(route)} " +
                          $"usable_ms={(Time.realtimeSinceStartupAsDouble - started) * 1000:0.0} max_frame_ms={maxFrame:0.0}");
            route = null;
        }
    }
}
