namespace AllinDefense.Presentation
{
    public readonly struct PresentationActionTimingSpec
    {
        public readonly string animationTrigger;
        public readonly float impactNormalizedTime;
        public readonly string impactSocket;
        public readonly bool gameplayAuthoritative;

        public PresentationActionTimingSpec(string animationTrigger, float impactNormalizedTime,
            string impactSocket, bool gameplayAuthoritative)
        {
            this.animationTrigger = animationTrigger;
            this.impactNormalizedTime = impactNormalizedTime;
            this.impactSocket = impactSocket;
            this.gameplayAuthoritative = gameplayAuthoritative;
        }
    }

    public static class PresentationActionTimingContract
    {
        public static PresentationActionTimingSpec Describe(PresentationSignal signal)
        {
            DefenderVfxSpec vfx = DefenderVfxPool.Describe(signal);
            return new PresentationActionTimingSpec(
                DefenderPresentationDirector.AnimationTriggerFor(signal),
                ImpactNormalizedTime(signal),
                vfx.sourceSocket,
                false);
        }

        public static float ImpactNormalizedTime(PresentationSignal signal) => signal.cue switch
        {
            PresentationCue.HeroAttacked when signal.actor == PresentationActorKind.Knight => 0.45f,
            PresentationCue.HeroAbility when signal.actor == PresentationActorKind.Knight => 0.18f,
            PresentationCue.EnemyHit => 0.05f,
            PresentationCue.EnemyDefeated or PresentationCue.UnitDefeated => 0f,
            PresentationCue.UnitSpawned => 0f,
            PresentationCue.OutpostAttacked or PresentationCue.MageImpact => 0.42f,
            PresentationCue.HealPulse => 0.25f,
            PresentationCue.OutpostDisrupted or PresentationCue.SlowApplied => 0.1f,
            PresentationCue.BossAttack => 0.55f,
            PresentationCue.BossPhase => 0.15f,
            PresentationCue.Victory or PresentationCue.Defeat => 0.2f,
            _ => 0f
        };
    }
}
