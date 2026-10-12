namespace AllinDefense.Combat
{
    public enum StatusEffectType
    {
        Slow,
        Stun,
        Burn,
        Poison,
        ArmorBreak
    }

    public enum StatusEffectStackingPolicy
    {
        RefreshDuration,
        StrongestOnly,
        StackIntensity
    }
}
