using System.Collections.Generic;
using UnityEngine;

namespace AllinDefense.Core
{
    public static class CombatMath
    {
        public static int ApplyDamage(int health, int damage) =>
            Mathf.Max(0, health - Mathf.Max(0, damage));

        public static int Refund(int investment, float ratio = 0.60f) =>
            Mathf.FloorToInt(Mathf.Max(0, investment) * Mathf.Clamp01(ratio));

        public static float RouteProgress(Vector3 from, Vector3 to, Vector3 position)
        {
            Vector3 segment = to - from;
            float length = segment.magnitude;
            if (length <= 0.001f) return 0f;
            return length * Mathf.Clamp01(Vector3.Dot(position - from, segment) / (length * length));
        }

        public static float TickCooldown(float remaining, float deltaTime) =>
            Mathf.Max(0f, remaining - Mathf.Max(0f, deltaTime));

        public static float ClosestRouteDistance(IReadOnlyList<Vector3> route, Vector3 position)
        {
            float best = float.MaxValue, progress = 0f, result = 0f;
            for (int i = 0; i + 1 < route.Count; i++)
            {
                Vector3 segment = route[i + 1] - route[i];
                float length = segment.magnitude;
                if (length < .001f) continue;
                float along = Mathf.Clamp(Vector3.Dot(position - route[i], segment / length), 0, length);
                float distance = (position - (route[i] + segment / length * along)).sqrMagnitude;
                if (distance < best) { best = distance; result = progress + along; }
                progress += length;
            }
            return result;
        }

        public static Vector3 PointOnRoute(IReadOnlyList<Vector3> route, float distance, out Vector3 direction)
        {
            direction = Vector3.forward;
            distance = Mathf.Max(0, distance);
            for (int i = 0; i + 1 < route.Count; i++)
            {
                Vector3 segment = route[i + 1] - route[i];
                float length = segment.magnitude;
                if (length < .001f) continue;
                direction = segment / length;
                if (distance <= length) return route[i] + direction * distance;
                distance -= length;
            }
            return route.Count > 0 ? route[route.Count - 1] : Vector3.zero;
        }
    }
}
