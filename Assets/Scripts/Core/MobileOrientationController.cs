using UnityEngine;

namespace AllinDefense.Core
{
    public enum MobileOrientationMode
    {
        PortraitFrontend,
        LandscapeBattle
    }

    public readonly struct MobileOrientationSpec
    {
        public MobileOrientationSpec(bool portrait, bool landscapeLeft, bool landscapeRight,
            ScreenOrientation requestedOrientation)
        {
            Portrait = portrait;
            LandscapeLeft = landscapeLeft;
            LandscapeRight = landscapeRight;
            RequestedOrientation = requestedOrientation;
        }

        public bool Portrait { get; }
        public bool LandscapeLeft { get; }
        public bool LandscapeRight { get; }
        public ScreenOrientation RequestedOrientation { get; }
    }

    /// <summary>
    /// Keeps portrait frontend screens and landscape battle screens on one explicit policy.
    /// Player settings advertise every supported orientation; this runtime policy decides
    /// which subset is allowed for the screen currently being presented.
    /// </summary>
    public static class MobileOrientationController
    {
        public static MobileOrientationMode CurrentMode { get; private set; } =
            MobileOrientationMode.PortraitFrontend;

        public static bool UsesRuntimeRotation => Application.isMobilePlatform;

        public static MobileOrientationSpec SpecFor(MobileOrientationMode mode)
        {
            return mode == MobileOrientationMode.LandscapeBattle
                ? new MobileOrientationSpec(false, true, true, ScreenOrientation.AutoRotation)
                : new MobileOrientationSpec(true, false, false, ScreenOrientation.Portrait);
        }

        public static void Request(MobileOrientationMode mode)
        {
            CurrentMode = mode;
            if (!UsesRuntimeRotation) return;

            MobileOrientationSpec spec = SpecFor(mode);
            Screen.autorotateToPortrait = spec.Portrait;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = spec.LandscapeLeft;
            Screen.autorotateToLandscapeRight = spec.LandscapeRight;
            Screen.orientation = spec.RequestedOrientation;
            Debug.Log($"[DC_ORIENTATION] requested={mode} screen={Screen.width}x{Screen.height}");
        }

        public static bool IsSettled(MobileOrientationMode mode)
        {
            return !UsesRuntimeRotation || MatchesDimensions(mode, Screen.width, Screen.height);
        }

        public static bool MatchesDimensions(MobileOrientationMode mode, int width, int height)
        {
            if (width <= 0 || height <= 0) return false;
            return mode == MobileOrientationMode.LandscapeBattle ? width > height : height >= width;
        }
    }
}
