using UnityEngine;

namespace DefenderChronicles.UI
{
    /// <summary>Temiz Fantastik design system. Points are Canvas reference units (8 pt grid).
    /// Colors use sRGB; translucent glass requires a dark backing surface for text contrast.</summary>
    public static class UiTokens
    {
        public static readonly Color Night = new Color32(6, 16, 32, 255);
        public static readonly Color NightDeep = new Color32(2, 5, 10, 255);
        public static readonly Color Surface = new Color32(9, 18, 34, 255);
        public static readonly Color SurfaceRaised = new Color32(8, 15, 28, 255);
        public static readonly Color Glass = WithAlpha(Surface, GlassAlpha);
        public static readonly Color GlassStrong = WithAlpha(SurfaceRaised, GlassStrongAlpha);
        public static readonly Color Gold = new Color32(242, 184, 75, 255);
        public static readonly Color GoldHigh = new Color32(255, 217, 138, 255);
        public static readonly Color GoldLow = new Color32(212, 146, 44, 255);
        public static readonly Color GoldText = new Color32(29, 19, 5, 255);
        public static readonly Color Moon = new Color32(155, 203, 255, 255);
        public static readonly Color Blue = new Color32(46, 117, 201, 255);
        public static readonly Color Danger = new Color32(239, 106, 79, 255);
        public static readonly Color Success = new Color32(73, 224, 123, 255);
        public static readonly Color Violet = new Color32(167, 123, 255, 255);
        public static readonly Color TextPrimary = new Color32(244, 250, 255, 255);
        public static readonly Color TextSecondary = new Color32(165, 187, 204, 255);
        // Decorative only: do not use Faint for essential text.
        public static readonly Color Faint = new Color32(111, 133, 153, 255);
        public static readonly Color Hairline = WithAlpha(Moon, .16f);
        public static readonly Color HairlineStrong = WithAlpha(Moon, .28f);
        public static readonly Color HairlineGold = WithAlpha(Gold, .5f);
        public static readonly Color GlassHighlight = WithAlpha(Color.white, .07f);
        public static readonly Color Shadow = WithAlpha(Color.black, .38f);
        public static readonly Color GoldShadow = WithAlpha(Gold, .35f);
        public const float GlassAlpha = .68f;
        public const float GlassStrongAlpha = .9f;

        public const float SpaceUnit = 8f;
        public const float Space1 = SpaceUnit;
        public const float Space2 = 2 * SpaceUnit;
        public const float Space3 = 3 * SpaceUnit;
        public const float Space4 = 4 * SpaceUnit;
        public const float Space6 = 6 * SpaceUnit;
        public const float Space8 = 8 * SpaceUnit;
        public const float RadiusSmall = 10f;
        public const float RadiusMedium = 14f;
        public const float RadiusCard = 20f;
        public const float RadiusPill = 64f;
        public static readonly Vector2 ShadowOffset = new Vector2(0f, -8f);
        public const float ShadowSoftness = 24f;

        // Existing reference-canvas typography; screen-specific sizes remain explicit.
        public const float TypeCaption = 20f;
        public const float TypeLabel = 24f;
        public const float TypeBody = 28f;
        public const float TypeSubheading = 36f;
        public const float TypeHeading = 48f;
        public const float TypeDisplay = 64f;
        public const int WeightRegular = 400;
        public const int WeightSemiBold = 600;
        public const int WeightBold = 700;
        public const string TurkishCharacters = "çğıİöşüÇĞÖŞÜ";

        public const float FadeDuration = .18f;
        public const float SlideDuration = .22f;
        public const float ReducedMotionDuration = .10f;
        public const float ButtonPressDuration = .09f;
        public const float ButtonColorDuration = .08f;
        public const float PressScale = .975f;
        public const float FocusScale = 1.025f;
        // A new curve per read prevents callers from mutating shared design tokens.
        public static AnimationCurve EaseOut => new AnimationCurve(new Keyframe(0, 0, 3, 3), new Keyframe(1, 1, 0, 0));
        public static AnimationCurve EaseInOut => AnimationCurve.EaseInOut(0, 0, 1, 1);
        public static float EvaluateEaseOut(float t) => 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);

        public static Color WithAlpha(Color color, float alpha) { color.a = alpha; return color; }
        public static Color TintSurface(Color accent, float amount, float alpha) =>
            WithAlpha(accent * amount, alpha);
        public static Color PreviewSurface(Color accent) =>
            WithAlpha(PreviewBase + accent * .10f, .88f);

        // Named compatibility treatments preserve the current layout while all color authoring
        // moves here. Component modernization can progressively use the core palette above.
        public static readonly Color LoadingSurface = new Color(0.018f, 0.032f, 0.052f);
        public static readonly Color HeroNameScrim = new Color(0, 0, 0, .8f);
        public static readonly Color ProfileSurface = new Color(.02f, .04f, .07f, .62f);
        public static readonly Color ViewportScrim = new Color(0, 0, 0, .02f);
        public static readonly Color TacticalSurface = new Color(.015f, .035f, .055f, .82f);
        public static readonly Color RouteViewport = new Color(.02f, .04f, .07f, .10f);
        public static readonly Color RouteLine = new Color(.56f, .72f, .62f, .65f);
        public static readonly Color AccountScrim = new Color(0, 0, 0, .52f);
        public static readonly Color SettingsScrim = new Color(0, 0, 0, .7f);
        public static readonly Color CinematicShade = new Color(0.005f, 0.012f, 0.025f, 0.46f);
        public static readonly Color TopShade = new Color(0.006f, 0.014f, 0.030f, 0.72f);
        public static readonly Color BottomShade = new Color(0.006f, 0.014f, 0.030f, 0.76f);
        public static readonly Color ResultScrim = new Color(0.004f, 0.010f, 0.020f, 0.76f);
        public static readonly Color LockedSurface = new Color(0.11f, 0.13f, 0.16f, 0.92f);
        public static readonly Color ButtonBlue = new Color(0.031f, 0.216f, 0.522f, 1f);
        public static readonly Color ButtonDisabled = new Color(0.12f, 0.14f, 0.16f, 0.75f);
        public static readonly Color ButtonOutline = new Color(0.55f, 0.72f, 0.94f, 0.22f);
        public static readonly Color RoadEarth = new Color(0.545f, 0.416f, 0.290f, 1f);
        public static readonly Color HudPanel = WithAlpha(Surface, .94f);
        public static readonly Color HudButtonReady = new Color(0.060f, 0.145f, 0.225f, 0.90f);
        public static readonly Color HudButtonPressed = new Color(0.120f, 0.280f, 0.400f, 1f);
        public static readonly Color HudButtonDisabled = new Color(0.026f, 0.042f, 0.058f, 0.72f);
        public static readonly Color PreviewBase = new Color(.025f, .045f, .065f, 0f);
        public static readonly Color DefeatText = new Color(.92f,.74f,.72f,1f);
        public static readonly Color HudResultSurface = new Color(0.005f, 0.012f, 0.02f, 0.90f);
        public static readonly Color HudHeader = new Color(0.055f, 0.11f, 0.16f, 0.98f);
        public static readonly Color BuildNoSlot = new Color(0.040f, 0.068f, 0.090f, 0.78f);
        public static readonly Color CooldownStart = new Color(0.10f, 0.18f, 0.26f, 0.72f);
        public static readonly Color CooldownEnd = new Color(0.28f, 0.75f, 0.58f, 0.34f);
        public static readonly Color AbilityBlocked = new Color(0.050f, 0.075f, 0.100f, 0.82f);
        public static readonly Color AbilityReady = new Color(0.095f, 0.225f, 0.165f, 0.92f);
        public static readonly Color AbilityIdle = new Color(0.050f, 0.095f, 0.135f, 0.86f);
        public static readonly Color HudFrame = new Color(0.42f, 0.65f, 0.92f, 0.30f);
        public static readonly Color HudTextShadow = WithAlpha(Color.black, .66f);
        public static readonly Color HudButtonShadow = WithAlpha(Color.black, .72f);
        public static readonly Color HudGoldButton = new Color(0.20f, 0.135f, 0.045f, 0.92f);
        public static readonly Color HudGoldHighlight = new Color(0.34f, 0.235f, 0.070f, 0.98f);
        public static readonly Color HudGoldPressed = new Color(0.50f, 0.325f, 0.080f, 1f);
        public static readonly Color HudGoldSelected = new Color(0.28f, 0.185f, 0.055f, 0.96f);
        public static readonly Color HudLabelBacking = new Color(0.025f, 0.060f, 0.100f, 0.76f);
        public static readonly Color CommandDeck = new Color(0.030f, 0.065f, 0.100f, 0.38f);
        public static readonly Color HeroDeck = new Color(0.025f, 0.080f, 0.095f, 0.38f);
        public static readonly Color BuildDeck = new Color(0.030f, 0.060f, 0.095f, 0.38f);
        public static readonly Color HudHintSurface = new Color(0.020f, 0.050f, 0.080f, 0.84f);
        public static readonly Color SelectedHeader = new Color(0.045f, 0.11f, 0.17f, 0.96f);
        public static readonly Color PreviewChrome = new Color(0.04f, 0.08f, 0.13f, 0.86f);
        public static readonly Color PhaseChrome = new Color(0.05f, 0.09f, 0.15f, 0.84f);
        public static readonly Color CooldownFill = new Color(0.12f, 0.24f, 0.34f, 0.56f);
        public static readonly Color ResultCard = new Color(0.05f, 0.09f, 0.14f, 0.96f);
        public static readonly Color PauseCard = new Color(0.05f, 0.09f, 0.14f, 0.94f);
        public static readonly Color PauseVeil = new Color(0.01f, 0.02f, 0.03f, 0.36f);
        public static readonly Color SelectedEmptyHeader = new Color(0.045f, 0.10f, 0.16f, 0.98f);
        public static readonly Color SelectedEmptyIcon = new Color(0.22f, 0.58f, 0.78f, 0.9f);
        public static readonly Color SelectedIdleHeader = new Color(0.035f, 0.08f, 0.13f, 0.95f);
        public static readonly Color HudTopBar = new Color(0.025f, 0.055f, 0.095f, 0.96f);
        public static readonly Color HudBottomBar = new Color(0.025f, 0.055f, 0.095f, 0.97f);
        public static readonly Color HudResultBacking = new Color(0.01f, 0.02f, 0.035f, 0.92f);
        public static readonly Color HudBuilderButton = new Color(0.06f, 0.18f, 0.30f, 0.98f);
        public static readonly Color HudBuilderHighlight = new Color(0.20f, 0.48f, 0.78f, 1f);
        public static readonly Color HudBuilderPressed = new Color(1f, 0.48f, 0.13f, 1f);
        public static readonly Color HudBuilderDisabled = new Color(0.08f, 0.10f, 0.14f, 0.72f);
    }
}
