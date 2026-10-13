using UnityEngine;
using AllinDefense.UI;

namespace AllinDefense.Presentation
{
    [CreateAssetMenu(menuName = "ALLIN: Defense/Frontend Presentation Theme",
        fileName = "FrontendPresentationTheme")]
    public sealed class FrontendPresentationTheme : ScriptableObject
    {
        [Header("Palette")]
        public Color night = UiTokens.Night;
        public Color nightDeep = UiTokens.NightDeep;
        public Color defenderBlue = UiTokens.ButtonBlue;
        public Color moonEdge = UiTokens.Moon;
        public Color commandGold = UiTokens.Gold;
        public Color roadEarth = UiTokens.RoadEarth;
        public Color enemyRed = UiTokens.Danger;
        public Color textPrimary = UiTokens.TextPrimary;
        public Color textMuted = UiTokens.TextSecondary;
        public Color panel = UiTokens.GlassStrong;
        public Color panelRaised = UiTokens.HudPanel;

        [Header("Motion")]
        [Min(0f)] public float fadeDuration = UiTokens.FadeDuration;
        [Min(0f)] public float slideDuration = UiTokens.SlideDuration;
        [Min(0f)] public float reducedMotionDuration = UiTokens.ReducedMotionDuration;
        [Min(0f)] public float buttonPressDuration = UiTokens.ButtonPressDuration;

        [Header("Frontend Artwork")]
        public Sprite brandLogo;
        public Sprite brandMark;
        public Texture2D welcomeBackdrop;
        public Texture2D warTableBackdrop;
        public Shader warTableShader;
        public Sprite panelSprite;
        public Sprite buttonSprite;
        public UiDesignSystemAssets designSystem;

        [Header("Optional Frontend Audio")]
        public AudioClip menuAmbience;
        public AudioClip mapOpen;
        public AudioClip levelSelect;
        public AudioClip starReveal;

        [Header("External Sign In")]
        public string googleIosClientId;
        public Sprite appleSignInLogo;
        public Sprite googleSignInLogo;

        [Header("Legal Links")]
        public string privacyPolicyUrl;
        public string termsOfServiceUrl;

        private void OnEnable() => ApplyDesignTokens();

        public void ApplyDesignTokens()
        {
            night = UiTokens.Night;
            nightDeep = UiTokens.NightDeep;
            defenderBlue = UiTokens.ButtonBlue;
            moonEdge = UiTokens.Moon;
            commandGold = UiTokens.Gold;
            roadEarth = UiTokens.RoadEarth;
            enemyRed = UiTokens.Danger;
            textPrimary = UiTokens.TextPrimary;
            textMuted = UiTokens.TextSecondary;
            panel = UiTokens.GlassStrong;
            panelRaised = UiTokens.HudPanel;
            fadeDuration = UiTokens.FadeDuration;
            slideDuration = UiTokens.SlideDuration;
            reducedMotionDuration = UiTokens.ReducedMotionDuration;
            buttonPressDuration = UiTokens.ButtonPressDuration;
        }

        public static FrontendPresentationTheme CreateRuntimeFallback()
        {
            FrontendPresentationTheme theme = CreateInstance<FrontendPresentationTheme>();
            theme.hideFlags = HideFlags.DontSave;
            theme.brandLogo = Resources.Load<Sprite>("Presentation/Brand/AllinDefense-Logo-v2");
            theme.brandMark = Resources.Load<Sprite>("Presentation/Brand/AllinDefense-Mark-v2");
            theme.welcomeBackdrop = Resources.Load<Texture2D>("Presentation/Frontend/GreenwatchWelcome");
            theme.warTableBackdrop = Resources.Load<Texture2D>("Presentation/Frontend/GreenwatchWarTable");
            theme.warTableShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("UI/Default");
            return theme;
        }
    }
}
