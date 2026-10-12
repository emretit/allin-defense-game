using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;

namespace AllinDefense.UI
{
    public static class UiMetrics
    {
        public const float MinTouchPt = 44f;
        public const float PrimaryCtaPt = 52f;
        public const float AbilityPt = 72f;

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern double AllinUiLogicalScreenWidth();
        [DllImport("__Internal")] private static extern double AllinUiLogicalScreenHeight();
#endif
        private static Vector2Int cachedScreen;
        private static Vector2 cachedLogicalSize;

        // Touch targets query this before every canvas render; the native lookup only runs when the screen changes.
        public static Vector2 LogicalScreenSize
        {
            get
            {
                var screen = new Vector2Int(Screen.width, Screen.height);
                if (screen != cachedScreen || cachedLogicalSize == Vector2.zero)
                {
                    cachedLogicalSize = ReadLogicalScreenSize();
                    cachedScreen = screen;
                }
                return cachedLogicalSize;
            }
        }

        private static Vector2 ReadLogicalScreenSize()
        {
#if UNITY_IOS && !UNITY_EDITOR
            var size = new Vector2((float)AllinUiLogicalScreenWidth(), (float)AllinUiLogicalScreenHeight());
            if (Screen.width > Screen.height && size.x < size.y || Screen.width < Screen.height && size.x > size.y)
                size = new Vector2(size.y, size.x);
            return size;
#elif UNITY_ANDROID && !UNITY_EDITOR
            using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
            using var resources = activity.Call<AndroidJavaObject>("getResources");
            using var metrics = resources.Call<AndroidJavaObject>("getDisplayMetrics");
            float density = Mathf.Max(.01f, metrics.Get<float>("density"));
            return new Vector2(metrics.Get<int>("widthPixels"), metrics.Get<int>("heightPixels")) / density;
#else
            return new Vector2(Screen.width, Screen.height);
#endif
        }

        public static float CanvasScale(Vector2 screenPixels, Vector2 referenceResolution, float match,
            CanvasScaler.ScreenMatchMode mode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight)
        {
            float width = screenPixels.x / Mathf.Max(1f, referenceResolution.x);
            float height = screenPixels.y / Mathf.Max(1f, referenceResolution.y);
            if (mode == CanvasScaler.ScreenMatchMode.Expand) return Mathf.Min(width, height);
            if (mode == CanvasScaler.ScreenMatchMode.Shrink) return Mathf.Max(width, height);
            return Mathf.Pow(2f, Mathf.Lerp(Mathf.Log(width, 2f), Mathf.Log(height, 2f), match));
        }

        public static Vector2 PointsPerCanvasUnit(Vector2 screenPixels, Vector2 logicalSize,
            Vector2 referenceResolution, float match,
            CanvasScaler.ScreenMatchMode mode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight) =>
            new Vector2(logicalSize.x / screenPixels.x, logicalSize.y / screenPixels.y) *
            CanvasScale(screenPixels, referenceResolution, match, mode);

        public static Vector2 PointsPerCanvasUnit(Canvas canvas)
        {
            if (canvas == null) return Vector2.one;
            canvas = canvas.rootCanvas;
            var logical = LogicalScreenSize;
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize && canvas.renderMode != RenderMode.WorldSpace)
                return PointsPerCanvasUnit(new Vector2(Screen.width, Screen.height), logical,
                    scaler.referenceResolution, scaler.matchWidthOrHeight, scaler.screenMatchMode);
            return new Vector2(logical.x / Mathf.Max(1, Screen.width), logical.y / Mathf.Max(1, Screen.height)) * canvas.scaleFactor;
        }

        public static Vector2 CanvasUnitsForPoints(float points, Vector2 pointsPerUnit) =>
            new(points / Mathf.Max(.0001f, pointsPerUnit.x), points / Mathf.Max(.0001f, pointsPerUnit.y));
    }
}
