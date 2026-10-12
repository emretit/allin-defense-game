using UnityEngine;

namespace AllinDefense.Presentation
{
    /// <summary>
    /// Keeps the playable battlefield large on wide phones while preserving every critical
    /// lane inside the device safe area on narrower screens.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AdaptiveBattlefieldCamera : MonoBehaviour
    {
        public const float TargetHorizontalWorldSize = 29.6f;
        public const float MinimumVerticalHalfSize = 8.25f;
        public const float MinimumOrthographicSize = 8.25f;
        public const float MaximumOrthographicSize = 18f;

        private const float UpwardScreenFramingOffset = -0.90f;
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

            // Reserve the top kit controls without hiding the keep's roof on wide phones.
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
            targetCamera.orthographicSize = CalculateOrthographicSize(
                new Vector2(Screen.width, Screen.height), Screen.safeArea);
            float safeOffset = Screen.safeArea.height > 0 && Screen.height > 0
                ? (Screen.safeArea.center.y / Screen.height - .5f) * 2 * targetCamera.orthographicSize : 0;
            targetCamera.transform.position = unframedWorldPosition - unframedWorldRotation * Vector3.up *
                (UpwardScreenFramingOffset + safeOffset);
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
