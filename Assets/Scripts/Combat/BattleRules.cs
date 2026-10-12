using UnityEngine;

namespace AllinDefense.Combat
{
    public enum DamageType { Physical, Magic }

    /// <summary>All values are seconds at the player's 1x battle speed.</summary>
    public static class BattleRules
    {
        public const float StepSeconds = 1f / 60f;
        public const float LegacyPace = 2f;
        public static float LegacySeconds(float value) => value / LegacyPace;
        public static int StarsForLives(int lives, int startingLives) =>
            lives <= 0 || startingLives <= 0 ? 0 :
            lives * 4 >= startingLives * 3 ? 3 : lives * 5 >= startingLives * 2 ? 2 : 1;
        public static int LivesAfterBossBreach(int lives, int startingLives, int bossDamage) =>
            lives <= 0 || startingLives <= 0 ? 0 :
            Mathf.Min(Mathf.Max(0, lives - Mathf.Max(1, bossDamage)), (int)((startingLives * 2L - 1) / 5));
        public static int ResolveDamage(int amount, DamageType type, float physicalResistance) =>
            DamageResolver.Resolve(new DamageRequest(amount,
                resistance: type == DamageType.Physical ? physicalResistance : 0f)).FinalDamage;
    }

    public sealed class SlowEffect
    {
        public float Factor { get; private set; } = 1f;
        public float Remaining { get; private set; }
        public void Reset()
        {
            Factor = 1f;
            Remaining = 0f;
        }
        public void Apply(float factor, float duration)
        {
            if (duration <= 0f) return;
            factor = Mathf.Clamp(factor, .1f, 1f);
            if (Remaining > 0f && factor > Factor) return;
            Remaining = Mathf.Approximately(factor, Factor) ? Mathf.Max(Remaining, duration) : duration;
            Factor = factor;
        }
        public void Tick(float delta)
        {
            Remaining = Mathf.Max(0f, Remaining - delta);
            if (Remaining <= 0f) Factor = 1f;
        }
    }
}
