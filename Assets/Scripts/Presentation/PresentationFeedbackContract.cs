using UnityEngine;

namespace DefenderChronicles.Presentation
{
    public enum PresentationFeedbackIntensityMode
    {
        Full,
        Reduced,
        Off
    }

    public readonly struct PresentationFeedbackSpec
    {
        public readonly float cameraShakeAmplitude;
        public readonly float cameraShakeSeconds;
        public readonly float hitStopSeconds;
        public readonly bool touchInputBlocked;

        public PresentationFeedbackSpec(float cameraShakeAmplitude, float cameraShakeSeconds,
            float hitStopSeconds, bool touchInputBlocked)
        {
            this.cameraShakeAmplitude = cameraShakeAmplitude;
            this.cameraShakeSeconds = cameraShakeSeconds;
            this.hitStopSeconds = hitStopSeconds;
            this.touchInputBlocked = touchInputBlocked;
        }
    }

    public static class PresentationFeedbackContract
    {
        public static PresentationFeedbackSpec Describe(PresentationSignal signal,
            PresentationFeedbackIntensityMode mode = PresentationFeedbackIntensityMode.Full)
        {
            if (mode == PresentationFeedbackIntensityMode.Off)
                return new PresentationFeedbackSpec(0f, 0f, 0f, false);

            PresentationFeedbackSpec full = FullSpec(signal);
            if (mode == PresentationFeedbackIntensityMode.Full) return full;

            return new PresentationFeedbackSpec(
                full.cameraShakeAmplitude * 0.45f,
                full.cameraShakeSeconds * 0.75f,
                0f,
                false);
        }

        private static PresentationFeedbackSpec FullSpec(PresentationSignal signal) => signal.cue switch
        {
            PresentationCue.EnemyHit => new PresentationFeedbackSpec(0.025f, 0.08f, 0f, false),
            PresentationCue.HeroAttacked => new PresentationFeedbackSpec(0.04f, 0.10f, 0f, false),
            PresentationCue.HeroAbility => new PresentationFeedbackSpec(0.05f, 0.12f, 0f, false),
            PresentationCue.OutpostAttacked => new PresentationFeedbackSpec(0.02f, 0.07f, 0f, false),
            PresentationCue.MageImpact => new PresentationFeedbackSpec(0.032f, 0.09f, 0f, false),
            PresentationCue.BossAttack => new PresentationFeedbackSpec(0.12f, 0.18f, 0f, false),
            PresentationCue.BossPhase => new PresentationFeedbackSpec(0.16f, 0.26f, 0f, false),
            PresentationCue.Defeat => new PresentationFeedbackSpec(0.09f, 0.24f, 0f, false),
            PresentationCue.Victory => new PresentationFeedbackSpec(0.035f, 0.18f, 0f, false),
            _ => new PresentationFeedbackSpec(0f, 0f, 0f, false)
        };
    }
}
