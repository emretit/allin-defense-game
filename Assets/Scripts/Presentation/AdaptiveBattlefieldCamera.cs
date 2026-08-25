using UnityEngine;

namespace DefenderChronicles.Presentation
{
    /// <summary>
    /// Keeps the playable battlefield large on wide phones while preserving every critical
    /// lane inside the device safe area on narrower screens.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AdaptiveBattlefieldCamera : MonoBehaviour
    {
        public const float TargetHorizontalWorldSize = 29.6f;
        public const float MinimumVerticalHalfSize = 7.25f;
        public const float MinimumOrthographicSize = 7.25f;
        public const float MaximumOrthographicSize = 18f;

        private const float UpwardScreenFramingOffset = 0.60f;
        private Camera targetCamera;
        private int previousWidth;
        private int previousHeight;
        private Rect previousSafeArea;
        private bool initialized;
        private Vector3 unframedWorldPosition;
        private Quaternion unframedWorldRotation;

        public void Configure(Camera value)
        {
            if (initialized && targetCamera == value)
            {
                ApplyCurrentScreen();
                return;
            }

            if (initialized && targetCamera != null)
            {
                targetCamera.transform.position = unframedWorldPosition;
                targetCamera.transform.rotation = unframedWorldRotation;
            }

            targetCamera = value;
            if (targetCamera == null || !targetCamera.orthographic) return;

            // The lower action rail is taller than the top status rail. Moving the camera
            // down along its local up axis places the battlefield in the visual centre of
            // the remaining playfield without changing the authored isometric angle.
            unframedWorldPosition = targetCamera.transform.position;
            unframedWorldRotation = targetCamera.transform.rotation;
            initialized = true;
            ApplyCurrentScreen();
        }

        private void Update()
        {
            if (!initialized || targetCamera == null) return;
            if (previousWidth == Screen.width && previousHeight == Screen.height &&
                previousSafeArea == Screen.safeArea) return;
            ApplyCurrentScreen();
        }

        private void ApplyCurrentScreen()
        {
            previousWidth = Screen.width;
            previousHeight = Screen.height;
            previousSafeArea = Screen.safeArea;
            targetCamera.transform.rotation = unframedWorldRotation;
            targetCamera.transform.position =
                unframedWorldPosition - unframedWorldRotation * Vector3.up * UpwardScreenFramingOffset;
            targetCamera.orthographicSize = CalculateOrthographicSize(
                new Vector2(Screen.width, Screen.height), Screen.safeArea);
        }

        public static float CalculateOrthographicSize(Vector2 screenSize, Rect safeArea)
        {
            if (screenSize.x <= 0f || screenSize.y <= 0f)
                return MinimumOrthographicSize;

            float safeWidth = safeArea.width > 0f ? safeArea.width : screenSize.x;
            float safeHeight = safeArea.height > 0f ? safeArea.height : screenSize.y;
            float safeAspect = Mathf.Max(0.1f, safeWidth / safeHeight);
            float widthDrivenSize = TargetHorizontalWorldSize / (2f * safeAspect);
            float fittedSize = Mathf.Max(MinimumVerticalHalfSize, widthDrivenSize);
            return Mathf.Clamp(fittedSize, MinimumOrthographicSize, MaximumOrthographicSize);
        }
    }
}
