using UnityEngine;

namespace DefenderChronicles.Combat
{
    public static class DamageResolver
    {
        public static DamageResult Resolve(DamageRequest request)
        {
            if (request.RawDamage <= 0)
                return new DamageResult(0);

            int amount = Mathf.Max(0, request.RawDamage);

            if (request.Armor > 0)
                amount = Mathf.Max(request.MinimumDamage, amount - request.Armor);

            if (request.Resistance > 0f)
                amount = Mathf.RoundToInt(amount * (1f - Mathf.Clamp01(request.Resistance)));

            if (request.IsCritical)
                amount = Mathf.RoundToInt(amount * Mathf.Max(1f, request.CriticalMultiplier));

            if (request.Shield > 0)
                amount = Mathf.Max(0, amount - request.Shield);

            amount = Mathf.Max(request.MinimumDamage, amount);
            return new DamageResult(amount);
        }
    }

    public readonly struct DamageRequest
    {
        public DamageRequest(
            int rawDamage,
            int armor = 0,
            float resistance = 0f,
            bool isCritical = false,
            float criticalMultiplier = 1.5f,
            int shield = 0,
            int minimumDamage = 1)
        {
            RawDamage = rawDamage;
            Armor = armor;
            Resistance = resistance;
            IsCritical = isCritical;
            CriticalMultiplier = criticalMultiplier;
            Shield = shield;
            MinimumDamage = minimumDamage;
        }

        public int RawDamage { get; }
        public int Armor { get; }
        public float Resistance { get; }
        public bool IsCritical { get; }
        public float CriticalMultiplier { get; }
        public int Shield { get; }
        public int MinimumDamage { get; }
    }

    public readonly struct DamageResult
    {
        public DamageResult(int finalDamage)
        {
            FinalDamage = finalDamage;
        }

        public int FinalDamage { get; }
    }
}
