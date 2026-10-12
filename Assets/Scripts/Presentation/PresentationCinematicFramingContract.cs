using UnityEngine;

namespace AllinDefense.Presentation
{
    public readonly struct PresentationCinematicFramingSpec
    {
        public readonly bool enabled;
        public readonly Vector2 normalizedFocusOffset;
        public readonly float orthographicSizeMultiplier;
        public readonly float blendInSeconds;
        public readonly float holdSeconds;
        public readonly float blendOutSeconds;

        public PresentationCinematicFramingSpec(bool enabled, Vector2 normalizedFocusOffset,
            float orthographicSizeMultiplier, float blendInSeconds, float holdSeconds, float blendOutSeconds)
        {
            this.enabled = enabled;
            this.normalizedFocusOffset = normalizedFocusOffset;
            this.orthographicSizeMultiplier = orthographicSizeMultiplier;
            this.blendInSeconds = blendInSeconds;
            this.holdSeconds = holdSeconds;
            this.blendOutSeconds = blendOutSeconds;
        }
    }

    public static class PresentationCinematicFramingContract
    {
        public static PresentationCinematicFramingSpec Describe(PresentationSignal signal,
            PresentationFeedbackIntensityMode mode = PresentationFeedbackIntensityMode.Full)
        {
            if (mode == PresentationFeedbackIntensityMode.Off)
                return Disabled();

            PresentationCinematicFramingSpec full = FullSpec(signal);
            if (!full.enabled || mode == PresentationFeedbackIntensityMode.Full) return full;

            return new PresentationCinematicFramingSpec(
                true,
                full.normalizedFocusOffset * 0.45f,
                Mathf.Lerp(1f, full.orthographicSizeMultiplier, 0.45f),
                full.blendInSeconds * 0.75f,
                full.holdSeconds * 0.50f,
                full.blendOutSeconds * 0.75f);
        }

        private static PresentationCinematicFramingSpec FullSpec(PresentationSignal signal) => signal.cue switch
        {
            PresentationCue.BossPhase => new PresentationCinematicFramingSpec(
                true, new Vector2(0.12f, 0.07f), 0.92f, 0.18f, 0.42f, 0.28f),
            PresentationCue.BossAttack => new PresentationCinematicFramingSpec(
                true, new Vector2(0.10f, 0.04f), 0.96f, 0.10f, 0.18f, 0.16f),
            PresentationCue.Victory => new PresentationCinematicFramingSpec(
                true, new Vector2(0.08f, 0.02f), 0.97f, 0.20f, 0.62f, 0.32f),
            PresentationCue.Defeat => new PresentationCinematicFramingSpec(
                true, new Vector2(-0.04f, -0.03f), 0.98f, 0.18f, 0.52f, 0.30f),
            _ => Disabled()
        };

        private static PresentationCinematicFramingSpec Disabled() =>
            new(false, Vector2.zero, 1f, 0f, 0f, 0f);
    }
}
