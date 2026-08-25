using UnityEngine;

namespace DefenderChronicles.Presentation
{
    [CreateAssetMenu(menuName = "Defender Chronicles/Frontend Presentation Theme",
        fileName = "FrontendPresentationTheme")]
    public sealed class FrontendPresentationTheme : ScriptableObject
    {
        [Header("Palette")]
        public Color night = new(0.024f, 0.063f, 0.125f, 1f);
        public Color nightDeep = new(0.008f, 0.020f, 0.040f, 1f);
        public Color defenderBlue = new(0.031f, 0.216f, 0.522f, 1f);
        public Color moonEdge = new(0.608f, 0.796f, 1f, 1f);
        public Color commandGold = new(0.949f, 0.722f, 0.294f, 1f);
        public Color roadEarth = new(0.545f, 0.416f, 0.290f, 1f);
        public Color enemyRed = new(0.698f, 0.227f, 0.180f, 1f);
        public Color textPrimary = new(0.957f, 0.980f, 1f, 1f);
        public Color textMuted = new(0.647f, 0.733f, 0.800f, 1f);
        public Color panel = new(0.018f, 0.043f, 0.078f, 0.94f);
        public Color panelRaised = new(0.035f, 0.082f, 0.133f, 0.96f);

        [Header("Motion")]
        [Min(0f)] public float fadeDuration = 0.18f;
        [Min(0f)] public float slideDuration = 0.22f;
        [Min(0f)] public float reducedMotionDuration = 0.10f;
        [Min(0f)] public float buttonPressDuration = 0.09f;

        [Header("Frontend Artwork")]
        public Sprite brandLogo;
        public Sprite brandMark;
        public Texture2D welcomeBackdrop;
        public Texture2D warTableBackdrop;
        public Shader warTableShader;
        public Sprite panelSprite;
        public Sprite buttonSprite;

        [Header("Optional Frontend Audio")]
        public AudioClip menuAmbience;
        public AudioClip mapOpen;
        public AudioClip levelSelect;
        public AudioClip starReveal;

        [Header("Legal Links")]
        public string privacyPolicyUrl;
        public string termsOfServiceUrl;

        public static FrontendPresentationTheme CreateRuntimeFallback()
        {
            FrontendPresentationTheme theme = CreateInstance<FrontendPresentationTheme>();
            theme.hideFlags = HideFlags.DontSave;
            theme.brandLogo = Resources.Load<Sprite>("Presentation/Brand/DefenderChronicles-Logo-v2");
            theme.brandMark = Resources.Load<Sprite>("Presentation/Brand/DefenderChronicles-Mark-v2");
            theme.welcomeBackdrop = Resources.Load<Texture2D>("Presentation/Frontend/GreenwatchWelcome");
            theme.warTableBackdrop = Resources.Load<Texture2D>("Presentation/Frontend/GreenwatchWarTable");
            theme.warTableShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("UI/Default");
            return theme;
        }
    }
}
