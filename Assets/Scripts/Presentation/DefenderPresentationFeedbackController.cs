using System.Collections;
using AllinDefense.UI;
using UnityEngine;

namespace AllinDefense.Presentation
{
    public sealed class DefenderPresentationFeedbackController : MonoBehaviour
    {
        private Camera targetCamera;
        private Coroutine shakeRoutine;
        private Vector3 restLocalPosition;
        private bool hasRestPose;
        private PresentationFeedbackIntensityMode mode = PresentationFeedbackIntensityMode.Full;

        public PresentationFeedbackIntensityMode Mode
        {
            get => mode;
            set => mode = value;
        }

        public void Initialize(Camera camera)
        {
            targetCamera = camera != null ? camera : Camera.main;
            if (targetCamera == null) return;
            restLocalPosition = targetCamera.transform.localPosition;
            hasRestPose = true;
        }

        public void Play(PresentationSignal signal)
        {
            if (!FrontendPreferences.CameraShake || FrontendPreferences.ReducedMotion)
            {
                if (shakeRoutine != null)
                {
                    StopCoroutine(shakeRoutine);
                    shakeRoutine = null;
                }
                RestoreRestPose();
                return;
            }
            PresentationFeedbackSpec spec = PresentationFeedbackContract.Describe(signal, mode);
            if (spec.cameraShakeAmplitude > 0f && spec.cameraShakeSeconds > 0f && targetCamera != null)
            {
                if (shakeRoutine != null)
                {
                    StopCoroutine(shakeRoutine);
                    RestoreRestPose();
                }
                shakeRoutine = StartCoroutine(Shake(spec.cameraShakeAmplitude, spec.cameraShakeSeconds));
            }
        }

        private IEnumerator Shake(float amplitude, float seconds)
        {
            Transform cameraTransform = targetCamera.transform;
            if (!hasRestPose)
            {
                restLocalPosition = cameraTransform.localPosition;
                hasRestPose = true;
            }
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float envelope = 1f - Mathf.Clamp01(elapsed / seconds);
                float x = (Mathf.PerlinNoise(elapsed * 37f, 0.13f) - 0.5f) * 2f;
                float y = (Mathf.PerlinNoise(0.73f, elapsed * 41f) - 0.5f) * 2f;
                cameraTransform.localPosition = restLocalPosition + new Vector3(x, y, 0f) * amplitude * envelope;
                yield return null;
            }
            RestoreRestPose();
            shakeRoutine = null;
        }

        private void OnDisable()
        {
            if (shakeRoutine != null)
            {
                StopCoroutine(shakeRoutine);
                shakeRoutine = null;
            }
            RestoreRestPose();
        }

        private void RestoreRestPose()
        {
            if (!hasRestPose || targetCamera == null) return;
            targetCamera.transform.localPosition = restLocalPosition;
        }
    }
}
