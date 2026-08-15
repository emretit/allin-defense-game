using UnityEngine;

namespace DefenderChronicles.Core
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
    }
}
