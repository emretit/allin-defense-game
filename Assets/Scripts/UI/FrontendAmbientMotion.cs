using UnityEngine;

namespace DefenderChronicles.UI
{
    /// <summary>
    /// Adds restrained, unscaled-time motion to decorative frontend elements.
    /// Reduced-motion preferences always restore the authored resting pose.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class FrontendAmbientMotion : MonoBehaviour
    {
        private RectTransform rectTransform;
        private Vector2 restingPosition;
        private Vector3 restingScale;
        private Quaternion restingRotation;
        private Vector2 driftAmplitude;
        private float scaleAmplitude;
        private float rotationAmplitude;
        private float speed = 1f;
        private float phase;
        private bool configured;

        public void Configure(Vector2 positionDrift, float scalePulse, float rotationDrift,
            float cyclesPerSecond, float phaseOffset = 0f)
        {
            rectTransform = GetComponent<RectTransform>();
            restingPosition = rectTransform.anchoredPosition;
            restingScale = rectTransform.localScale;
            restingRotation = rectTransform.localRotation;
            driftAmplitude = positionDrift;
            scaleAmplitude = Mathf.Max(0f, scalePulse);
            rotationAmplitude = Mathf.Max(0f, rotationDrift);
            speed = Mathf.Max(0.01f, cyclesPerSecond) * Mathf.PI * 2f;
            phase = phaseOffset;
            configured = true;
        }

        private void LateUpdate()
        {
            if (!configured) return;
            if (FrontendPreferences.ReducedMotion)
            {
                RestorePose();
                return;
            }

            float wave = Mathf.Sin(Time.unscaledTime * speed + phase);
            float crossWave = Mathf.Cos(Time.unscaledTime * speed * 0.73f + phase);
            rectTransform.anchoredPosition = restingPosition +
                                             new Vector2(driftAmplitude.x * wave, driftAmplitude.y * crossWave);
            rectTransform.localScale = restingScale * (1f + scaleAmplitude * wave);
            rectTransform.localRotation = restingRotation * Quaternion.Euler(0f, 0f, rotationAmplitude * crossWave);
        }

        private void OnDisable()
        {
            if (configured) RestorePose();
        }

        private void RestorePose()
        {
            rectTransform.anchoredPosition = restingPosition;
            rectTransform.localScale = restingScale;
            rectTransform.localRotation = restingRotation;
        }
    }
}
