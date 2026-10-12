using System;

namespace AllinDefense.Presentation
{
    public enum PresentationAudioPriority
    {
        Ambient,
        Normal,
        High,
        Critical
    }

    public enum PresentationAudioReviewLevel
    {
        Silent,
        Low,
        Normal
    }

    public readonly struct PresentationAudioCueSpec
    {
        public readonly string concurrencyGroup;
        public readonly int maxConcurrent;
        public readonly PresentationAudioPriority priority;
        public readonly float delaySeconds;
        public readonly float musicDuck;
        public readonly float duckSeconds;

        public PresentationAudioCueSpec(string concurrencyGroup, int maxConcurrent,
            PresentationAudioPriority priority, float delaySeconds = 0f, float musicDuck = 0f,
            float duckSeconds = 0f)
        {
            this.concurrencyGroup = concurrencyGroup;
            this.maxConcurrent = maxConcurrent;
            this.priority = priority;
            this.delaySeconds = delaySeconds;
            this.musicDuck = musicDuck;
            this.duckSeconds = duckSeconds;
        }
    }

    public static class PresentationAudioPolicy
    {
        public static PresentationAudioCueSpec Describe(PresentationSignal signal) => signal.cue switch
        {
            PresentationCue.UiClick => Spec("UI", 2, PresentationAudioPriority.High),
            PresentationCue.UiError when signal.value > 0 => Spec("KeepBreach", 1, PresentationAudioPriority.Critical,musicDuck:.4f,duckSeconds:.5f),
            PresentationCue.UiError => Spec("UIError", 1, PresentationAudioPriority.High),
            PresentationCue.OutpostBuilt or PresentationCue.OutpostUpgraded or PresentationCue.OutpostSold =>
                Spec("Economy", 2, PresentationAudioPriority.High),
            PresentationCue.HeroMoved => Spec("Movement", 2, PresentationAudioPriority.Ambient),
            PresentationCue.EnemyHit => Spec("CombatLight", 4, PresentationAudioPriority.Normal),
            PresentationCue.EnemyDefeated or PresentationCue.UnitDefeated =>
                Spec("DefeatImpact", 3, PresentationAudioPriority.Normal),
            PresentationCue.UnitSpawned => Spec("Spawn", 3, PresentationAudioPriority.Normal),
            PresentationCue.OutpostAttacked => Spec("Projectile", 4, PresentationAudioPriority.Normal),
            PresentationCue.MageImpact => Spec("MagicImpact", 3, PresentationAudioPriority.High),
            PresentationCue.HealPulse => Spec("Heal", 3, PresentationAudioPriority.High),
            PresentationCue.OutpostDisrupted or PresentationCue.SlowApplied =>
                Spec("Status", 2, PresentationAudioPriority.High),
            PresentationCue.HeroAttacked => Spec("HeroAction", 2, PresentationAudioPriority.High),
            PresentationCue.HeroAbility => Spec("HeroAbility", 1, PresentationAudioPriority.Critical,
                0f, 0.42f, 0.48f),
            PresentationCue.BossAttack => Spec("BossImpact", 2, PresentationAudioPriority.Critical,
                0.20f, 0.48f, 0.55f),
            PresentationCue.BossPhase => Spec("BossPhase", 1, PresentationAudioPriority.Critical,
                0.05f, 0.55f, 0.75f),
            PresentationCue.WaveStarted => Spec("Wave", 1, PresentationAudioPriority.High,
                musicDuck: 0.24f, duckSeconds: 0.32f),
            PresentationCue.Victory or PresentationCue.Defeat => Spec("Result", 1,
                PresentationAudioPriority.Critical, 0.08f, 0.62f, 1.15f),
            _ => throw new ArgumentOutOfRangeException(nameof(signal), signal.cue, "Unknown presentation cue.")
        };

        public static float ReviewGain(PresentationAudioReviewLevel level) => level switch
        {
            PresentationAudioReviewLevel.Silent => 0f,
            PresentationAudioReviewLevel.Low => 0.25f,
            PresentationAudioReviewLevel.Normal => 1f,
            _ => throw new ArgumentOutOfRangeException(nameof(level), level, null)
        };

        public static float MusicTransitionSeconds(MusicState state) => state switch
        {
            MusicState.Preparation => 1.10f,
            MusicState.Combat => 0.85f,
            MusicState.Intense => 0.70f,
            MusicState.Boss => 0.50f,
            MusicState.Victory => 0.35f,
            MusicState.Defeat => 0.28f,
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
        };

        public static bool CanReplace(PresentationAudioPriority existing, PresentationAudioPriority incoming)
        {
            return incoming >= existing;
        }

        public static int UnityPriority(PresentationAudioPriority priority) => priority switch
        {
            PresentationAudioPriority.Critical => 32,
            PresentationAudioPriority.High => 80,
            PresentationAudioPriority.Normal => 128,
            PresentationAudioPriority.Ambient => 192,
            _ => 128
        };

        private static PresentationAudioCueSpec Spec(string group, int maxConcurrent,
            PresentationAudioPriority priority, float delaySeconds = 0f, float musicDuck = 0f,
            float duckSeconds = 0f)
        {
            return new PresentationAudioCueSpec(group, maxConcurrent, priority, delaySeconds,
                musicDuck, duckSeconds);
        }
    }
}
