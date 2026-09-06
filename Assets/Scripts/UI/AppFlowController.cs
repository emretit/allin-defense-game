using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using DefenderChronicles.Core;
using DefenderChronicles.Data;
using DefenderChronicles.Presentation;
using DefenderChronicles.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DefenderChronicles.UI
{
    public enum AppScreen
    {
        Boot,
        Welcome,
        Login,
        MainMenu,
        Campaign,
        Briefing,
        Battle,
        Result
    }

    public sealed class AppFlowController : MonoBehaviour
    {
        private readonly Dictionary<AppScreen, GameObject> screens = new();
        private readonly List<Button> levelButtons = new();
        private readonly List<TextMeshProUGUI> levelStars = new();
        private DefenderPresentationCatalog catalog;
        private GameSessionController session;
        private CampaignProgressStore progress;
        private FrontendPresentationTheme theme;
        private AudioSource frontendUiSource;
        private Canvas canvas;
        private CanvasScaler canvasScaler;
        private RawImage backdrop;
        private Image backdropShade;
        private FrontendWarTableRenderer warTableRenderer;
        private ScrollRect campaignScroll;
        private TextMeshProUGUI profileLabel;
        private TextMeshProUGUI continueDetails;
        private TextMeshProUGUI loginStatus;
        private TextMeshProUGUI selectedLevelTitle;
        private TextMeshProUGUI selectedLevelDetails;
        private Button playSelectedButton;
        private TextMeshProUGUI resultTitle;
        private TextMeshProUGUI resultStars;
        private TextMeshProUGUI resultReward;
        private TextMeshProUGUI briefingTitle;
        private TextMeshProUGUI briefingRegion;
        private TextMeshProUGUI briefingObjective;
        private TextMeshProUGUI briefingEnemies;
        private TextMeshProUGUI briefingReward;
        private Button nextLevelButton;
        private Button retryButton;
        private Button turkishLanguageButton;
        private Button englishLanguageButton;
        private GameObject settingsModal;
        private GameObject orientationCurtain;
        private TextMeshProUGUI orientationCurtainLabel;
        private Coroutine transitionRoutine;
        private Coroutine orientationRoutine;
        private AppScreen previousScreen;
        private int selectedLevel = 1;
        private bool battlePaused;
        private static int pendingLevel;

        public AppScreen CurrentScreen { get; private set; }
        public CampaignProgressStore Progress => progress;
        public static bool HasPendingLevel => pendingLevel > 0;
        public event Action<PlayerAuthProvider> AuthenticationRequested;

        public static MobileOrientationMode OrientationForScreen(AppScreen screen)
        {
            return screen is AppScreen.Battle or AppScreen.Result
                ? MobileOrientationMode.LandscapeBattle
                : MobileOrientationMode.PortraitFrontend;
        }

        public void Initialize(DefenderPresentationCatalog presentationCatalog, GameSessionController owner,
            bool pauseBattle = true, string savePath = null)
        {
            catalog = presentationCatalog != null
                ? presentationCatalog
                : throw new ArgumentNullException(nameof(presentationCatalog));
            session = owner != null ? owner : throw new ArgumentNullException(nameof(owner));
            progress = new CampaignProgressStore(savePath);
            theme = catalog.frontendTheme != null
                ? catalog.frontendTheme
                : FrontendPresentationTheme.CreateRuntimeFallback();
            if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DC_FRONTEND_EVIDENCE_PATH")) ||
                !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DC_FRONTEND_EVIDENCE_SUITE")))
                Application.runInBackground = true;
            frontendUiSource = gameObject.AddComponent<AudioSource>();
            frontendUiSource.playOnAwake = false;
            frontendUiSource.spatialBlend = 0f;
            frontendUiSource.outputAudioMixerGroup = catalog.uiMixer;
            frontendUiSource.volume = FrontendPreferences.SfxVolume;
            battlePaused = pauseBattle;
            session.PhaseChanged += HandleSessionPhaseChanged;
            MobileOrientationController.Request(HasPendingLevel
                ? MobileOrientationMode.LandscapeBattle
                : MobileOrientationMode.PortraitFrontend);
            GameLocalization.LanguageChanged += HandleLanguageChanged;
            GameLocalization.ApplyStoredUnityLocale();

            EnsureEventSystem();
            BuildCanvas();
            BuildBootScreen();
            BuildWelcomeScreen();
            BuildLoginScreen();
            BuildMainMenuScreen();
            BuildCampaignScreen();
            BuildBriefingScreen();
            BuildResultScreen();
            BuildSettingsModal();
            BuildOrientationCurtain();
            ApplyPortraitLayout();
            ApplyStoredAudioPreferences();

            if (battlePaused) Time.timeScale = 0f;
            ShowScreen(AppScreen.Boot);
            if (HasPendingLevel) return;
            if (string.Equals(Environment.GetEnvironmentVariable("DC_FRONTEND_CAPTURE"), "boot",
                    StringComparison.OrdinalIgnoreCase))
            {
                ApplyDevelopmentCaptureScreen();
                return;
            }
            StartCoroutine(FinishBoot(string.IsNullOrWhiteSpace(progress.Data.playerId)
                ? AppScreen.Welcome
                : AppScreen.MainMenu));
        }

        public void CompleteServiceSignIn(PlayerAuthProvider provider, string playerId, string displayName)
        {
            if (provider == PlayerAuthProvider.None)
                throw new ArgumentOutOfRangeException(nameof(provider));
            progress.SignIn(provider, playerId, displayName);
            RefreshProfile();
            ShowScreen(AppScreen.MainMenu);
        }

        public void CompleteGuestFallback()
        {
            CompleteLocalGuestSignIn();
        }

        public void SetLoginStatus(string status)
        {
            if (loginStatus != null) loginStatus.text = status ?? string.Empty;
        }

        public void BeginAuthentication(PlayerAuthProvider provider)
        {
            if (provider == PlayerAuthProvider.Guest)
                SignInAsGuest();
            else
                RequestExternalSignIn(provider);
        }

        public void ReturnToMainMenu()
        {
            battlePaused = true;
            Time.timeScale = 0f;
            RefreshProfile();
            ShowFrontendAfterPortrait(AppScreen.MainMenu);
        }

        public void LaunchCampaignLevel(int level)
        {
            selectedLevel = Mathf.Clamp(level, 1, CampaignCatalog.LevelCount);
            progress.SelectLevel(selectedLevel);
            if (session.IsInitialized)
            {
                ReloadCampaignLevel(selectedLevel);
                return;
            }

            if (orientationRoutine != null) StopCoroutine(orientationRoutine);
            orientationRoutine = StartCoroutine(EnterBattleAfterLandscape(selectedLevel));
        }

        public static int ConsumePendingLevel()
        {
            int value = pendingLevel;
            pendingLevel = 0;
            return value;
        }

        private void BuildCanvas()
        {
            var canvasObject = new GameObject("App Flow", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            canvasScaler = canvasObject.GetComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = HasPendingLevel
                ? new Vector2(1920f, 1080f)
                : new Vector2(1080f, 1920f);
            canvasScaler.matchWidthOrHeight = 0.5f;

            GameObject background = new("Backdrop", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            background.transform.SetParent(canvasObject.transform, false);
            backdrop = background.GetComponent<RawImage>();
            backdrop.texture = theme.welcomeBackdrop;
            backdrop.color = Color.white;
            backdrop.raycastTarget = false;
            backdrop.uvRect = new Rect(0f, 0f, 1f, 1f);
            background.AddComponent<FrontendCoverImage>();
            Stretch(background.GetComponent<RectTransform>());

            GameObject shade = CreateImage("Cinematic Shade", canvasObject.transform, new Color(0.005f, 0.012f, 0.025f, 0.46f));
            backdropShade = shade.GetComponent<Image>();
            Stretch(shade.GetComponent<RectTransform>());

            GameObject topShade = CreateImage("Top Shade", canvasObject.transform, new Color(0.006f, 0.014f, 0.030f, 0.72f));
            SetAnchors(topShade.GetComponent<RectTransform>(), new Vector2(0f, 0.82f), Vector2.one, Vector2.zero, Vector2.zero);
            GameObject bottomShade = CreateImage("Bottom Shade", canvasObject.transform, new Color(0.006f, 0.014f, 0.030f, 0.76f));
            SetAnchors(bottomShade.GetComponent<RectTransform>(), Vector2.zero, new Vector2(1f, 0.16f), Vector2.zero, Vector2.zero);

            CreateAmbientSigil("Moonlit Sigil", canvasObject.transform,
                new Vector2(0.66f, 0.61f), new Vector2(1.08f, 1.05f),
                new Color(theme.moonEdge.r, theme.moonEdge.g, theme.moonEdge.b, 0.055f),
                new Vector2(10f, 7f), 0.012f, 1.4f, 0.022f, 0.4f);
            CreateAmbientSigil("Ember Sigil", canvasObject.transform,
                new Vector2(-0.10f, -0.05f), new Vector2(0.34f, 0.37f),
                new Color(theme.commandGold.r, theme.commandGold.g, theme.commandGold.b, 0.040f),
                new Vector2(8f, 5f), 0.010f, 1.0f, 0.018f, 2.1f);

            GameObject warTableObject = new("War Table Presentation");
            warTableObject.transform.SetParent(transform, false);
            warTableObject.transform.position = new Vector3(0f, -10000f, 0f);
            warTableRenderer = warTableObject.AddComponent<FrontendWarTableRenderer>();
            if (theme.warTableBackdrop != null)
                warTableRenderer.Initialize(theme.warTableBackdrop, backdrop, theme.warTableShader);

            GameObject safeRoot = new("Safe Area", typeof(RectTransform), typeof(SafeAreaFitter));
            safeRoot.transform.SetParent(canvasObject.transform, false);
            Stretch(safeRoot.GetComponent<RectTransform>());

            foreach (AppScreen screen in Enum.GetValues(typeof(AppScreen)))
            {
                GameObject root = new(screen.ToString(), typeof(RectTransform));
                root.transform.SetParent(safeRoot.transform, false);
                Stretch(root.GetComponent<RectTransform>());
                screens.Add(screen, root);
            }
        }

        private void BuildBootScreen()
        {
            Transform root = screens[AppScreen.Boot].transform;
            GameObject brandLogo = CreateBrandImage("Boot Brand Logo", root, theme.brandLogo);
            SetRect(brandLogo.GetComponent<RectTransform>(), new Vector2(0f, 54f), new Vector2(560f, 560f),
                Center(), Center());

            TextMeshProUGUI chapter = CreateText("Boot Chapter", root, "GREENWATCH SEFERİ", 18, catalog.bodyFont,
                FontStyles.Bold, TextAlignmentOptions.Center, theme.moonEdge);
            chapter.characterSpacing = 6f;
            SetRect(chapter.rectTransform, new Vector2(0f, -274f), new Vector2(520f, 40f), Center(), Center());
        }

        private IEnumerator FinishBoot(AppScreen target)
        {
            if (FrontendPreferences.ReducedMotion)
            {
                yield return new WaitForSecondsRealtime(0.28f);
            }
            else
            {
                Transform crest = screens[AppScreen.Boot].transform.Find("Boot Brand Logo");
                const float revealDuration = 0.54f;
                float elapsed = 0f;
                while (elapsed < revealDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / revealDuration);
                    t = 1f - Mathf.Pow(1f - t, 3f);
                    if (crest != null) crest.localScale = Vector3.one * Mathf.Lerp(0.92f, 1f, t);
                    yield return null;
                }
                if (crest != null) crest.localScale = Vector3.one;
                yield return new WaitForSecondsRealtime(0.16f);
            }
            ShowScreen(target);
            ApplyDevelopmentCaptureScreen();
        }

        private void BuildWelcomeScreen()
        {
            Transform root = screens[AppScreen.Welcome].transform;
            GameObject card = CreatePanel("Welcome Chronicle", root, new Color(theme.panel.r, theme.panel.g, theme.panel.b, 0.84f));
            AddCardAccent(card.transform, theme.commandGold);
            SetAnchors(card.GetComponent<RectTransform>(), new Vector2(0.045f, 0.10f), new Vector2(0.445f, 0.90f),
                Vector2.zero, Vector2.zero);

            GameObject brandLogo = CreateBrandImage("Welcome Brand Logo", card.transform, theme.brandLogo);
            SetRect(brandLogo.GetComponent<RectTransform>(), new Vector2(0f, 300f), new Vector2(430f, 430f),
                Center(), Center());

            TextMeshProUGUI eyebrow = CreateText("Eyebrow", card.transform, "GREENWATCH  •  SINIR SEFERİ", 19, catalog.bodyFont,
                FontStyles.Bold, TextAlignmentOptions.Center, theme.moonEdge);
            eyebrow.characterSpacing = 3f;
            SetRect(eyebrow.rectTransform, new Vector2(0f, 58f), new Vector2(680f, 38f), Center(), Center());

            TextMeshProUGUI subtitle = CreateText("Subtitle", card.transform,
                "Greenwatch Kalesi kuşatma altında.\nSavunma hattını kur. Şafağa kadar dayan.", 24,
                catalog.bodyFont, FontStyles.Normal, TextAlignmentOptions.Center, theme.textMuted);
            subtitle.lineSpacing = 8f;
            SetRect(subtitle.rectTransform, new Vector2(0f, -32f), new Vector2(650f, 86f), Center(), Center());

            Button start = CreateButton("Start", card.transform, "KRALLIĞA GİR  ›", new Vector2(460f, 92f),
                theme.commandGold, SignInAsGuest);
            SetRect(start.GetComponent<RectTransform>(), new Vector2(0f, -156f), new Vector2(460f, 92f), Center(), Center());

            Button account = CreateButton("Link Account", card.transform, "HESABINI BAĞLA", new Vector2(460f, 64f),
                theme.defenderBlue, () => ShowScreen(AppScreen.Login));
            SetRect(account.GetComponent<RectTransform>(), new Vector2(0f, -254f), new Vector2(460f, 64f), Center(), Center());

            TextMeshProUGUI footnote = CreateText("Guest Note", card.transform,
                "Misafir ilerlemen bu cihazda saklanır; hesabını daha sonra bağlayabilirsin.", 16,
                catalog.bodyFont, FontStyles.Normal, TextAlignmentOptions.Center, theme.textMuted);
            SetRect(footnote.rectTransform, new Vector2(0f, -334f), new Vector2(650f, 48f), Center(), Center());
        }

        private void BuildLoginScreen()
        {
            Transform root = screens[AppScreen.Login].transform;
            GameObject scrim = CreateImage("Account Scrim", root, new Color(0f, 0f, 0f, 0.44f));
            Stretch(scrim.GetComponent<RectTransform>());
            GameObject card = CreatePanel("Account Chronicle", root, theme.panel);
            AddCardAccent(card.transform, theme.defenderBlue);
            SetRect(card.GetComponent<RectTransform>(), Vector2.zero, new Vector2(760f, 760f), Center(), Center());

            Button close = CreateButton("Back", card.transform, "×", new Vector2(64f, 64f), theme.night,
                ReturnFromAccount);
            SetRect(close.GetComponent<RectTransform>(), new Vector2(-28f, -28f), new Vector2(64f, 64f), TopRight(), TopRight());

            TextMeshProUGUI heading = CreateText("Heading", card.transform, "İLERLEMENİ KORU", 46,
                catalog.titleFont, FontStyles.Bold, TextAlignmentOptions.Center, theme.textPrimary);
            SetRect(heading.rectTransform, new Vector2(0f, 235f), new Vector2(560f, 70f), Center(), Center());
            TextMeshProUGUI explanation = CreateText("Account Explanation", card.transform,
                "Misafir olarak hemen oynayabilir veya ilerlemeni cihazlar arasında taşımak için hesabını bağlayabilirsin.",
                22, catalog.bodyFont, FontStyles.Normal, TextAlignmentOptions.Center, theme.textMuted);
            SetRect(explanation.rectTransform, new Vector2(0f, 170f), new Vector2(610f, 92f), Center(), Center());

            Button guest = CreateButton("Guest Sign In", card.transform, "MİSAFİR OLARAK BAŞLA",
                new Vector2(520f, 76f), theme.commandGold, SignInAsGuest);
            SetRect(guest.GetComponent<RectTransform>(), new Vector2(0f, 64f), new Vector2(520f, 76f), Center(), Center());

            Button account = CreateButton("Player Account Sign In", card.transform, "HESABINI BAĞLA",
                new Vector2(520f, 76f), theme.defenderBlue,
                () => RequestExternalSignIn(PlayerAuthProvider.PlayerAccount));
            SetRect(account.GetComponent<RectTransform>(), new Vector2(0f, -42f), new Vector2(520f, 76f), Center(), Center());

            loginStatus = CreateText("Login Status", card.transform, "", 19, catalog.bodyFont, FontStyles.Normal,
                TextAlignmentOptions.Center, theme.commandGold);
            SetRect(loginStatus.rectTransform, new Vector2(0f, -154f), new Vector2(620f, 62f), Center(), Center());

            Button privacy = CreateButton("Privacy Policy", card.transform, "GİZLİLİK POLİTİKASI",
                new Vector2(250f, 50f), theme.night, () => OpenLegalUrl(theme.privacyPolicyUrl, "Gizlilik Politikası"));
            SetRect(privacy.GetComponent<RectTransform>(), new Vector2(-140f, -244f), new Vector2(250f, 50f), Center(), Center());
            Button terms = CreateButton("Terms Of Service", card.transform, "KULLANIM KOŞULLARI",
                new Vector2(250f, 50f), theme.night, () => OpenLegalUrl(theme.termsOfServiceUrl, "Kullanım Koşulları"));
            SetRect(terms.GetComponent<RectTransform>(), new Vector2(140f, -244f), new Vector2(250f, 50f), Center(), Center());
        }

        private void BuildMainMenuScreen()
        {
            Transform root = screens[AppScreen.MainMenu].transform;
            GameObject brandLogo = CreateBrandImage("Brand Logo", root, theme.brandLogo);
            SetRect(brandLogo.GetComponent<RectTransform>(), new Vector2(76f, -36f), new Vector2(260f, 260f),
                TopLeft(), TopLeft());

            GameObject profileCard = CreatePanel("Profile Crest", root, new Color(theme.panel.r, theme.panel.g, theme.panel.b, 0.88f));
            SetRect(profileCard.GetComponent<RectTransform>(), new Vector2(-210f, -74f), new Vector2(600f, 96f), TopRight(), TopRight());
            GameObject profileMark = CreateBrandImage("Profile Brand Mark", profileCard.transform, theme.brandMark);
            SetRect(profileMark.GetComponent<RectTransform>(), new Vector2(50f, -17f), new Vector2(62f, 62f),
                TopLeft(), TopLeft());
            profileLabel = CreateText("Profile", profileCard.transform, "", 20, catalog.bodyFont, FontStyles.Normal,
                TextAlignmentOptions.Left, theme.textMuted);
            SetRect(profileLabel.rectTransform, new Vector2(112f, -12f), new Vector2(450f, 66f), TopLeft(), TopLeft());

            Button settings = CreateButton("Settings", root, "AYAR", new Vector2(92f, 68f), theme.night, OpenSettings);
            SetRect(settings.GetComponent<RectTransform>(), new Vector2(-100f, -86f), new Vector2(92f, 68f), TopRight(), TopRight());

            GameObject continueCard = CreatePanel("Continue Chronicle", root, new Color(theme.panel.r, theme.panel.g, theme.panel.b, 0.92f));
            AddCardAccent(continueCard.transform, theme.commandGold);
            AddCardWatermark(continueCard.transform, theme.commandGold);
            SetAnchors(continueCard.GetComponent<RectTransform>(), new Vector2(0.055f, 0.16f), new Vector2(0.58f, 0.78f),
                Vector2.zero, Vector2.zero);
            TextMeshProUGUI callout = CreateText("Callout", continueCard.transform, "SAVUNMA HATTI\nSENİ BEKLİYOR", 58,
                catalog.titleFont, FontStyles.Bold, TextAlignmentOptions.Left, theme.textPrimary);
            callout.enableAutoSizing = true;
            callout.fontSizeMin = 38f;
            callout.fontSizeMax = 58f;
            SetRect(callout.rectTransform, new Vector2(54f, -60f), new Vector2(760f, 150f), TopLeft(), TopLeft());

            CampaignLevelDefinition last = CampaignCatalog.Level(Mathf.Clamp(progress.Data.lastSelectedLevel, 1, progress.Data.unlockedLevel));
            continueDetails = CreateText("Continue Details", continueCard.transform,
                $"BÖLGE {last.region}  ·  {last.displayName.ToUpperInvariant()}\n{last.waveCount} dalga  ·  ~{last.targetMinutes:0.#} dk  ·  {last.startingGold} başlangıç altını",
                23, catalog.bodyFont, FontStyles.Normal, TextAlignmentOptions.Left, theme.textMuted);
            SetRect(continueDetails.rectTransform, new Vector2(58f, -238f), new Vector2(760f, 88f), TopLeft(), TopLeft());

            Button continueButton = CreateButton("Continue", continueCard.transform, "DEVAM ET  ›", new Vector2(420f, 88f),
                theme.commandGold, ContinueCampaign);
            SetRect(continueButton.GetComponent<RectTransform>(), new Vector2(58f, 60f), new Vector2(420f, 88f), BottomLeft(), BottomLeft());

            GameObject actions = CreatePanel("Menu Actions", root, new Color(theme.panel.r, theme.panel.g, theme.panel.b, 0.90f));
            AddCardAccent(actions.transform, theme.defenderBlue);
            SetAnchors(actions.GetComponent<RectTransform>(), new Vector2(0.63f, 0.20f), new Vector2(0.945f, 0.70f),
                Vector2.zero, Vector2.zero);
            Button campaign = CreateButton("Campaign", actions.transform, "KRALLIK HARİTASI", new Vector2(470f, 84f),
                theme.defenderBlue, OpenCampaign);
            SetRect(campaign.GetComponent<RectTransform>(), new Vector2(0f, 106f), new Vector2(470f, 84f), Center(), Center());
            Button switchAccount = CreateButton("Switch Account", actions.transform, "HESABI BAĞLA / DEĞİŞTİR",
                new Vector2(470f, 70f), theme.night, () => ShowScreen(AppScreen.Login));
            SetRect(switchAccount.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(470f, 70f), Center(), Center());
            TextMeshProUGUI menuHint = CreateText("Menu Hint", actions.transform,
                "Greenwatch Bölgesi\n10 savunma noktası  ·  1 bölgesel lord", 19, catalog.bodyFont,
                FontStyles.Normal, TextAlignmentOptions.Center, theme.textMuted);
            SetRect(menuHint.rectTransform, new Vector2(0f, -112f), new Vector2(470f, 72f), Center(), Center());
        }

        private void BuildCampaignScreen()
        {
            Transform root = screens[AppScreen.Campaign].transform;
            CreateHeader(root, "KRALLIK SAVAŞ MASASI", "", () => ShowScreen(AppScreen.MainMenu));

            GameObject tabs = new("Region Tabs", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            tabs.transform.SetParent(root, false);
            SetAnchors(tabs.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(170f, -238f), new Vector2(-170f, -174f));
            var tabLayout = tabs.GetComponent<HorizontalLayoutGroup>();
            tabLayout.spacing = 12f;
            tabLayout.childControlWidth = true;
            tabLayout.childForceExpandWidth = true;
            tabLayout.childControlHeight = true;

            for (int index = 0; index < CampaignCatalog.RegionCount; index++)
            {
                int targetIndex = index;
                CampaignRegionDefinition region = CampaignCatalog.Regions[index];
                CreateButton($"Region Tab {index + 1}", tabs.transform, $"{index + 1}  {region.displayName.ToUpperInvariant()}",
                    new Vector2(260f, 60f), new Color(region.primaryColor.r, region.primaryColor.g, region.primaryColor.b, 1f),
                    () => FocusRegion(targetIndex));
            }

            GameObject scrollObject = new("Campaign Scroll", typeof(RectTransform), typeof(ScrollRect));
            scrollObject.transform.SetParent(root, false);
            SetAnchors(scrollObject.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(70f, 160f), new Vector2(-70f, -285f));
            campaignScroll = scrollObject.GetComponent<ScrollRect>();
            campaignScroll.horizontal = true;
            campaignScroll.vertical = false;
            campaignScroll.movementType = ScrollRect.MovementType.Clamped;
            campaignScroll.inertia = true;
            campaignScroll.decelerationRate = 0.08f;

            GameObject viewport = CreateImage("Viewport", scrollObject.transform, new Color(0f, 0f, 0f, 0.06f));
            Stretch(viewport.GetComponent<RectTransform>());
            viewport.AddComponent<Mask>().showMaskGraphic = false;

            GameObject content = new("Content", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 0f);
            contentRect.anchorMax = new Vector2(0f, 1f);
            contentRect.pivot = new Vector2(0f, 0.5f);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;
            var contentLayout = content.GetComponent<HorizontalLayoutGroup>();
            contentLayout.spacing = 0f;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = false;
            contentLayout.childForceExpandHeight = true;
            content.GetComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            campaignScroll.viewport = viewport.GetComponent<RectTransform>();
            campaignScroll.content = contentRect;

            for (int index = 0; index < CampaignCatalog.RegionCount; index++)
                BuildRegion(content.transform, CampaignCatalog.Regions[index]);

            GameObject selectionBar = CreatePanel("Selection Bar", root, theme.panel);
            AddCardAccent(selectionBar.transform, theme.commandGold);
            SetAnchors(selectionBar.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(70f, 46f), new Vector2(-70f, 145f));
            selectedLevelTitle = CreateText("Selected Level", selectionBar.transform, "", 30, catalog.titleFont,
                FontStyles.Bold, TextAlignmentOptions.Left, Color.white);
            SetRect(selectedLevelTitle.rectTransform, new Vector2(28f, 15f), new Vector2(440f, 58f), BottomLeft(), BottomLeft());
            selectedLevelDetails = CreateText("Selected Details", selectionBar.transform, "", 22, catalog.bodyFont,
                FontStyles.Normal, TextAlignmentOptions.Left, new Color(0.68f, 0.78f, 0.84f));
            SetRect(selectedLevelDetails.rectTransform, new Vector2(470f, 15f), new Vector2(360f, 58f), BottomLeft(), BottomLeft());
            playSelectedButton = CreateButton("Play Selected", selectionBar.transform, "BRİFİNGİ AÇ  ›", new Vector2(340f, 66f),
                theme.commandGold, OpenBriefing);
            SetRect(playSelectedButton.GetComponent<RectTransform>(), new Vector2(-348f, 16f), new Vector2(320f, 66f), BottomRight(), BottomRight());
        }

        private void BuildBriefingScreen()
        {
            Transform root = screens[AppScreen.Briefing].transform;
            CreateHeader(root, "SAVAŞ BRİFİNGİ", "Greenwatch ön hattı", () => ShowScreen(AppScreen.Campaign));

            GameObject leftCard = CreatePanel("Briefing Chronicle", root, new Color(theme.panel.r, theme.panel.g, theme.panel.b, 0.95f));
            AddCardAccent(leftCard.transform, theme.moonEdge);
            SetAnchors(leftCard.GetComponent<RectTransform>(), new Vector2(0.08f, 0.14f), new Vector2(0.59f, 0.77f),
                Vector2.zero, Vector2.zero);
            briefingRegion = CreateText("Briefing Region", leftCard.transform, "", 20, catalog.bodyFont,
                FontStyles.Bold, TextAlignmentOptions.Left, theme.moonEdge);
            SetRect(briefingRegion.rectTransform, new Vector2(54f, -54f), new Vector2(760f, 38f), TopLeft(), TopLeft());
            briefingTitle = CreateText("Briefing Title", leftCard.transform, "", 54, catalog.titleFont,
                FontStyles.Bold, TextAlignmentOptions.Left, theme.textPrimary);
            briefingTitle.enableAutoSizing = true;
            briefingTitle.fontSizeMin = 38f;
            briefingTitle.fontSizeMax = 54f;
            SetRect(briefingTitle.rectTransform, new Vector2(54f, -105f), new Vector2(800f, 92f), TopLeft(), TopLeft());
            briefingObjective = CreateText("Briefing Objective", leftCard.transform, "", 24, catalog.bodyFont,
                FontStyles.Normal, TextAlignmentOptions.Left, theme.textMuted);
            briefingObjective.lineSpacing = 7f;
            SetRect(briefingObjective.rectTransform, new Vector2(56f, -225f), new Vector2(790f, 156f), TopLeft(), TopLeft());
            TextMeshProUGUI order = CreateText("Commander Order", leftCard.transform,
                "KOMUTAN EMRİ\nDüşman yolunu oku, savunma noktalarını kur ve kaleyi ayakta tut.", 21,
                catalog.bodyFont, FontStyles.Bold, TextAlignmentOptions.Left, theme.commandGold);
            SetRect(order.rectTransform, new Vector2(56f, 62f), new Vector2(780f, 82f), BottomLeft(), BottomLeft());

            GameObject rightCard = CreatePanel("Briefing Intel", root, new Color(theme.panelRaised.r, theme.panelRaised.g,
                theme.panelRaised.b, 0.96f));
            AddCardAccent(rightCard.transform, theme.commandGold);
            SetAnchors(rightCard.GetComponent<RectTransform>(), new Vector2(0.63f, 0.20f), new Vector2(0.92f, 0.75f),
                Vector2.zero, Vector2.zero);
            briefingEnemies = CreateText("Enemy Intel", rightCard.transform, "", 22, catalog.bodyFont,
                FontStyles.Bold, TextAlignmentOptions.Left, theme.textPrimary);
            SetRect(briefingEnemies.rectTransform, new Vector2(42f, -58f), new Vector2(450f, 150f), TopLeft(), TopLeft());
            briefingReward = CreateText("Briefing Reward", rightCard.transform, "", 22, catalog.bodyFont,
                FontStyles.Bold, TextAlignmentOptions.Left, theme.commandGold);
            SetRect(briefingReward.rectTransform, new Vector2(42f, -232f), new Vector2(450f, 120f), TopLeft(), TopLeft());
            Button start = CreateButton("Start Defence", rightCard.transform, "SAVUNMAYI BAŞLAT", new Vector2(430f, 86f),
                theme.commandGold, StartSelectedLevel);
            SetRect(start.GetComponent<RectTransform>(), new Vector2(0f, 62f), new Vector2(430f, 86f), BottomCenter(), BottomCenter());
        }

        private void BuildResultScreen()
        {
            Transform root = screens[AppScreen.Result].transform;
            GameObject scrim = CreateImage("Result Scrim", root, new Color(0.004f, 0.010f, 0.020f, 0.76f));
            Stretch(scrim.GetComponent<RectTransform>());
            GameObject card = CreatePanel("Result Chronicle", root, new Color(theme.panel.r, theme.panel.g, theme.panel.b, 0.97f));
            AddCardAccent(card.transform, theme.commandGold);
            AddCardWatermark(card.transform, theme.moonEdge);
            SetRect(card.GetComponent<RectTransform>(), Vector2.zero, new Vector2(1180f, 650f), Center(), Center());

            TextMeshProUGUI badge = CreateText("Result Badge", card.transform, "SAVAŞ SONUCU", 20,
                catalog.bodyFont, FontStyles.Bold, TextAlignmentOptions.Center, theme.commandGold);
            SetRect(badge.rectTransform, new Vector2(0f, 245f), new Vector2(720f, 42f), Center(), Center());
            resultTitle = CreateText("Result Title", card.transform, "", 64, catalog.titleFont, FontStyles.Bold,
                TextAlignmentOptions.Center, theme.textPrimary);
            resultTitle.enableAutoSizing = true;
            resultTitle.fontSizeMin = 40f;
            resultTitle.fontSizeMax = 64f;
            SetRect(resultTitle.rectTransform, new Vector2(0f, 145f), new Vector2(980f, 130f), Center(), Center());

            resultStars = CreateText("Result Stars", card.transform, "", 54, catalog.titleFont, FontStyles.Bold,
                TextAlignmentOptions.Center, theme.commandGold);
            SetRect(resultStars.rectTransform, new Vector2(0f, 40f), new Vector2(760f, 78f), Center(), Center());

            resultReward = CreateText("Result Reward", card.transform, "", 25, catalog.bodyFont, FontStyles.Bold,
                TextAlignmentOptions.Center, theme.moonEdge);
            SetRect(resultReward.rectTransform, new Vector2(0f, -42f), new Vector2(850f, 52f), Center(), Center());

            retryButton = CreateButton("Retry Level", card.transform, "YENİDEN DENE", new Vector2(310f, 74f),
                theme.defenderBlue, () => ReloadCampaignLevel(session.CampaignLevelNumber));
            SetRect(retryButton.GetComponent<RectTransform>(), new Vector2(-350f, -180f), new Vector2(310f, 74f), Center(), Center());

            nextLevelButton = CreateButton("Next Level", card.transform, "SONRAKİ BÖLÜM  ›", new Vector2(340f, 82f),
                theme.commandGold, () => ReloadCampaignLevel(session.CampaignLevelNumber + 1));
            SetRect(nextLevelButton.GetComponent<RectTransform>(), new Vector2(0f, -180f), new Vector2(340f, 82f), Center(), Center());

            Button map = CreateButton("Return To Map", card.transform, "HARİTAYA DÖN", new Vector2(310f, 74f),
                theme.night, OpenCampaign);
            SetRect(map.GetComponent<RectTransform>(), new Vector2(350f, -180f), new Vector2(310f, 74f), Center(), Center());
        }

        private void BuildRegion(Transform parent, CampaignRegionDefinition region)
        {
            GameObject root = CreateImage($"Region {region.number}", parent,
                region.number == 1
                    ? new Color(theme.night.r, theme.night.g, theme.night.b, 0.10f)
                    : new Color(region.primaryColor.r, region.primaryColor.g, region.primaryColor.b, 0.70f));
            root.AddComponent<LayoutElement>().preferredWidth = 2200f;
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(2200f, 0f);
            root.AddComponent<FrontendCampaignPageFitter>().Configure(campaignScroll.viewport, 984f);

            TextMeshProUGUI name = CreateText("Region Name", root.transform,
                $"BÖLGE {region.number}  ·  {region.displayName.ToUpperInvariant()}", 30, catalog.titleFont,
                FontStyles.Bold, TextAlignmentOptions.Left, region.number == 1 ? theme.commandGold : region.accentColor);
            SetRect(name.rectTransform, new Vector2(55f, -54f), new Vector2(900f, 48f), TopLeft(), TopLeft());

            Vector2[] positions =
            {
                new(130f, 135f), new(280f, 300f), new(445f, 170f), new(600f, 340f), new(755f, 205f),
                new(910f, 360f), new(1060f, 210f), new(1205f, 345f), new(1350f, 185f), new(1500f, 330f)
            };

            for (int index = 0; index < positions.Length - 1; index++)
                CreateRouteSegment(root.transform, positions[index], positions[index + 1], region.accentColor);

            foreach (CampaignLevelDefinition level in region.levels)
            {
                bool unlocked = level.number <= progress.Data.unlockedLevel;
                Color buttonColor = unlocked
                    ? new Color(region.accentColor.r, region.accentColor.g, region.accentColor.b, 1f)
                    : new Color(0.15f, 0.18f, 0.21f, 1f);
                Vector2 size = level.isBoss ? new Vector2(106f, 106f) : new Vector2(82f, 82f);
                Button button = CreateButton($"Level {level.number}", root.transform,
                    unlocked ? level.number.ToString() : "×", size, buttonColor, () => SelectLevel(level.number));
                Vector2 position = positions[level.indexInRegion - 1];
                SetRect(button.GetComponent<RectTransform>(), position - size * 0.5f, size, BottomLeft(), BottomLeft());
                button.interactable = unlocked;
                CreateLevelSelectionRing(button.transform);
                levelButtons.Add(button);

                CampaignLevelProgress levelProgress = progress.ProgressFor(level.number);
                string starText = levelProgress == null || levelProgress.stars <= 0
                    ? (level.isBoss ? "LORD" : level.displayName.ToUpperInvariant())
                    : $"{levelProgress.stars}/3";
                TextMeshProUGUI stars = CreateText("Progress", root.transform, starText, level.isBoss ? 19 : 17,
                    catalog.bodyFont, FontStyles.Bold, TextAlignmentOptions.Center,
                    unlocked ? Color.white : new Color(0.48f, 0.52f, 0.55f));
                SetRect(stars.rectTransform, new Vector2(position.x - 85f, position.y - 76f), new Vector2(170f, 34f),
                    BottomLeft(), BottomLeft());
                levelStars.Add(stars);
            }
        }

        private void CreateRouteSegment(Transform parent, Vector2 from, Vector2 to, Color color)
        {
            Vector2 delta = to - from;
            GameObject segment = CreateImage("Route", parent, new Color(color.r, color.g, color.b, 0.38f));
            RectTransform rect = segment.GetComponent<RectTransform>();
            rect.anchorMin = BottomLeft();
            rect.anchorMax = BottomLeft();
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = from;
            rect.sizeDelta = new Vector2(delta.magnitude, 8f);
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }

        private void SignInAsGuest()
        {
            if (AuthenticationRequested != null)
            {
                AuthenticationRequested.Invoke(PlayerAuthProvider.Guest);
                return;
            }
            CompleteLocalGuestSignIn();
        }

        private void CompleteLocalGuestSignIn()
        {
            string id = string.IsNullOrWhiteSpace(progress.Data.playerId)
                ? $"guest-{Guid.NewGuid():N}"
                : progress.Data.playerId;
            progress.SignIn(PlayerAuthProvider.Guest, id, GameLocalization.Text("Misafir Muhafız", "Guest Defender"));
            RefreshProfile();
            ShowScreen(AppScreen.MainMenu);
        }

        private void RequestExternalSignIn(PlayerAuthProvider provider)
        {
            loginStatus.text = GameLocalization.Text("Güvenli hesap penceresi açılıyor...",
                "Opening the secure account window...");
            if (AuthenticationRequested == null)
            {
                loginStatus.text = GameLocalization.Text(
                    "Çevrimiçi giriş bu geliştirme sürümünde henüz etkin değil. Misafir olarak devam edebilirsin.",
                    "Online sign-in is not enabled in this development build yet. You can continue as a guest.");
                return;
            }
            AuthenticationRequested.Invoke(provider);
        }

        private void ContinueCampaign()
        {
            selectedLevel = Mathf.Clamp(progress.Data.lastSelectedLevel, 1, progress.Data.unlockedLevel);
            OpenBriefing();
        }

        private void OpenCampaign()
        {
            bool returningFromBattle = CurrentScreen is AppScreen.Battle or AppScreen.Result ||
                                       MobileOrientationController.CurrentMode == MobileOrientationMode.LandscapeBattle;
            if (returningFromBattle)
                ShowFrontendAfterPortrait(AppScreen.Campaign);
            else
                ShowScreen(AppScreen.Campaign);
            SelectLevel(progress.Data.lastSelectedLevel);
            FocusRegion((selectedLevel - 1) / CampaignCatalog.LevelsPerRegion);
            PlayFrontendCue(theme.mapOpen, PresentationCue.UiClick);
        }

        private void SelectLevel(int level)
        {
            selectedLevel = Mathf.Clamp(level, 1, progress.Data.unlockedLevel);
            CampaignLevelDefinition definition = CampaignCatalog.Level(selectedLevel);
            selectedLevelTitle.text = $"{definition.number}. {definition.displayName}";
            selectedLevelDetails.text = GameLocalization.IsEnglish
                ? $"{definition.waveCount} waves · ~{definition.targetMinutes:0.#} min · {definition.startingGold} gold\n" +
                  $"Reward: {definition.valorReward} Valor · {definition.heroXpReward} XP"
                : $"{definition.waveCount} dalga · ~{definition.targetMinutes:0.#} dk · {definition.startingGold} altın\n" +
                  $"Ödül: {definition.valorReward} Valor · {definition.heroXpReward} XP";
            playSelectedButton.interactable = selectedLevel <= progress.Data.unlockedLevel;
            RefreshSelectedLevelVisuals();
            PlayFrontendCue(theme.levelSelect, PresentationCue.UiClick);
        }

        private void OpenBriefing()
        {
            selectedLevel = Mathf.Clamp(selectedLevel, 1, progress.Data.unlockedLevel);
            RefreshBriefing();
            ShowScreen(AppScreen.Briefing);
        }

        private void StartSelectedLevel()
        {
            TriggerHaptic();
            LaunchCampaignLevel(selectedLevel);
        }

        private void RefreshBriefing()
        {
            CampaignLevelDefinition definition = CampaignCatalog.Level(selectedLevel);
            briefingRegion.text = GameLocalization.IsEnglish
                ? $"REGION {definition.region}  ·  GREENWATCH FRONT LINE"
                : $"BÖLGE {definition.region}  ·  GREENWATCH ÖN HATTI";
            briefingTitle.text = $"{definition.number}. {definition.displayName.ToUpperInvariant()}";
            briefingObjective.text = GameLocalization.IsEnglish
                ? $"Defend the fortress approach for {definition.waveCount} waves.\n" +
                  $"Estimated time: ~{definition.targetMinutes:0.#} minutes\n" +
                  $"Starting resources: {definition.startingGold} gold"
                : $"Kalenin giriş yolunu {definition.waveCount} dalga boyunca savun.\n" +
                  $"Tahmini süre: ~{definition.targetMinutes:0.#} dakika\n" +
                  $"Başlangıç kaynağı: {definition.startingGold} altın";
            briefingEnemies.text = GameLocalization.IsEnglish
                ? $"ENEMY INTELLIGENCE\n{EnemyPreviewForLevel(definition.number)}\n\n" +
                  $"THREAT LEVEL\n{ThreatLabel(definition.number)}"
                : $"DÜŞMAN İSTİHBARATI\n{EnemyPreviewForLevel(definition.number)}\n\n" +
                  $"TEHDİT SEVİYESİ\n{ThreatLabel(definition.number)}";
            briefingReward.text = GameLocalization.IsEnglish
                ? $"BATTLE REWARD\n+ {definition.valorReward} Valor\n+ {definition.heroXpReward} Hero XP"
                : $"SAVAŞ ÖDÜLÜ\n+ {definition.valorReward} Valor\n+ {definition.heroXpReward} Kahraman XP";
        }

        private void RefreshSelectedLevelVisuals()
        {
            for (int index = 0; index < levelButtons.Count; index++)
            {
                Button button = levelButtons[index];
                int level = index + 1;
                bool unlocked = level <= progress.Data.unlockedLevel;
                bool selected = level == selectedLevel;
                CampaignLevelProgress levelProgress = progress.ProgressFor(level);
                Image image = button.targetGraphic as Image;
                if (image == null) continue;
                CampaignLevelDefinition definition = CampaignCatalog.Level(level);
                CampaignRegionDefinition region = CampaignCatalog.Regions[definition.region - 1];
                TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();
                TextMeshProUGUI progressLabel = index < levelStars.Count ? levelStars[index] : null;
                if (!unlocked) image.color = new Color(0.11f, 0.13f, 0.16f, 0.92f);
                else if (selected) image.color = theme.commandGold;
                else if (levelProgress != null && levelProgress.stars >= 3) image.color = theme.moonEdge;
                else if (levelProgress != null && levelProgress.stars > 0) image.color = theme.defenderBlue;
                else image.color = region.accentColor;

                if (label != null)
                {
                    label.text = !unlocked
                        ? "×"
                        : selected
                            ? $"> {level} <"
                            : definition.isBoss ? $"BOSS {level}" : level.ToString();
                }
                if (progressLabel != null)
                {
                    progressLabel.text = !unlocked
                        ? GameLocalization.Text("KİLİTLİ", "LOCKED")
                        : levelProgress != null && levelProgress.stars > 0
                            ? GameLocalization.Text($"{levelProgress.stars}/3 YILDIZ", $"{levelProgress.stars}/3 STARS")
                            : definition.isBoss ? "LORD" : GameLocalization.Text("AÇIK", "READY");
                }
                Transform selectionRing = button.transform.Find("Selection Ring");
                if (selectionRing != null) selectionRing.gameObject.SetActive(unlocked && selected);
            }
        }

        private static string EnemyPreviewForLevel(int level) => (level, GameLocalization.IsEnglish) switch
        {
            (<= 1, false) => "Akıncılar",
            (<= 3, false) => "Akıncılar · Öncüler",
            (<= 5, false) => "Akıncılar · Öncüler · Yenileyiciler",
            (<= 7, false) => "Akıncılar · Destek Birlikleri · Kuşatma",
            (< 10, false) => "Ağır Öncüler · Destek · Kuşatma",
            (_, false) => "Bölgesel Lord · Ağır Öncüler · Kuşatma",
            (<= 1, true) => "Raiders",
            (<= 3, true) => "Raiders · Vanguards",
            (<= 5, true) => "Raiders · Vanguards · Regenerators",
            (<= 7, true) => "Raiders · Support · Siege",
            (< 10, true) => "Heavy Vanguards · Support · Siege",
            _ => "Regional Lord · Heavy Vanguards · Siege"
        };

        private static string ThreatLabel(int level) => (level, GameLocalization.IsEnglish) switch
        {
            (<= 3, false) => "I  Düşük",
            (<= 6, false) => "II  Yükseliyor",
            (< 10, false) => "III  Yüksek",
            (_, false) => "LORD SALDIRISI",
            (<= 3, true) => "I  Low",
            (<= 6, true) => "II  Rising",
            (< 10, true) => "III  High",
            _ => "LORD ASSAULT"
        };

        private void FocusRegion(int regionIndex)
        {
            if (campaignScroll == null) return;
            campaignScroll.horizontalNormalizedPosition = CampaignCatalog.RegionCount <= 1
                ? 0f
                : Mathf.Clamp01(regionIndex / (CampaignCatalog.RegionCount - 1f));
        }

        private void RefreshProfile()
        {
            if (profileLabel == null) return;
            string provider = progress.Data.authProvider switch
            {
                PlayerAuthProvider.PlayerAccount => GameLocalization.Text("Bağlı hesap", "Linked account"),
                _ => GameLocalization.Text("Misafir", "Guest")
            };
            string displayName = string.IsNullOrWhiteSpace(progress.Data.displayName)
                ? GameLocalization.Text("Greenwatch Muhafızı", "Greenwatch Defender")
                : progress.Data.displayName;
            profileLabel.text = $"{displayName}  ·  {provider}\n" +
                                GameLocalization.Text($"{progress.Data.TotalStars}/150 yıldız  ·  {progress.Data.valor} Valor",
                                    $"{progress.Data.TotalStars}/150 stars  ·  {progress.Data.valor} Valor");
            if (continueDetails != null)
            {
                int target = Mathf.Clamp(progress.Data.lastSelectedLevel, 1, progress.Data.unlockedLevel);
                CampaignLevelDefinition last = CampaignCatalog.Level(target);
                CampaignLevelProgress result = progress.ProgressFor(target);
                string stars = result == null || result.stars <= 0
                    ? GameLocalization.Text("0/3 YILDIZ", "0/3 STARS")
                    : GameLocalization.Text($"{result.stars}/3 YILDIZ", $"{result.stars}/3 STARS");
                continueDetails.text = GameLocalization.IsEnglish
                    ? $"REGION {last.region}  ·  {last.displayName.ToUpperInvariant()}  ·  {stars}\n" +
                      $"{last.waveCount} waves  ·  ~{last.targetMinutes:0.#} min  ·  {last.startingGold} starting gold"
                    : $"BÖLGE {last.region}  ·  {last.displayName.ToUpperInvariant()}  ·  {stars}\n" +
                      $"{last.waveCount} dalga  ·  ~{last.targetMinutes:0.#} dk  ·  {last.startingGold} başlangıç altını";
            }
        }

        private void HandleSessionPhaseChanged(GamePhase phase)
        {
            if (phase != GamePhase.Victory && phase != GamePhase.Defeat) return;
            bool victory = phase == GamePhase.Victory;
            int stars = 0;
            CampaignCompletionReward reward = default;
            if (victory)
            {
                float lifeRatio = session.StartingLivesForLevel <= 0
                    ? 0f
                    : session.Lives / (float)session.StartingLivesForLevel;
                stars = lifeRatio >= 0.75f ? 3 : lifeRatio >= 0.4f ? 2 : 1;
                reward = progress.CompleteLevel(session.CampaignLevelNumber, stars, session.Lives);
            }
            ShowResult(victory, stars, reward);
        }

        private void ShowResult(bool victory, int stars, CampaignCompletionReward reward)
        {
            MobileOrientationController.Request(MobileOrientationMode.LandscapeBattle);
            SetCanvasOrientation(MobileOrientationMode.LandscapeBattle);
            RefreshLevelButtons();
            selectedLevel = victory
                ? Mathf.Min(CampaignCatalog.LevelCount, session.CampaignLevelNumber + 1)
                : session.CampaignLevelNumber;
            resultTitle.text = victory
                ? GameLocalization.Text(
                    $"ZAFER\n{CampaignCatalog.Level(session.CampaignLevelNumber).displayName.ToUpperInvariant()} TAMAMLANDI",
                    $"VICTORY\n{CampaignCatalog.Level(session.CampaignLevelNumber).displayName.ToUpperInvariant()} COMPLETE")
                : GameLocalization.Text("SAVUNMA HATTI DÜŞTÜ", "THE DEFENCE LINE HAS FALLEN");
            resultTitle.color = victory ? theme.textPrimary : theme.enemyRed;
            resultStars.text = victory
                ? GameLocalization.Text($"{stars} / 3 YILDIZ", $"{stars} / 3 STARS")
                : GameLocalization.Text("0 / 3 YILDIZ", "0 / 3 STARS");
            resultReward.text = victory
                ? reward.Valor > 0 || reward.HeroXp > 0
                    ? GameLocalization.Text($"+{reward.Valor} Valor     +{reward.HeroXp} Kahraman XP",
                        $"+{reward.Valor} Valor     +{reward.HeroXp} Hero XP")
                    : GameLocalization.Text("Bölüm ödülü daha önce alındı", "Level reward already claimed")
                : GameLocalization.Text("Savunmanı güçlendir ve tekrar dene", "Strengthen your defence and try again");
            nextLevelButton.interactable = victory && session.CampaignLevelNumber < CampaignCatalog.LevelCount;
            SetButtonEmphasis(nextLevelButton, victory ? theme.commandGold : theme.night);
            SetButtonEmphasis(retryButton, victory ? theme.defenderBlue : theme.commandGold);
            canvas.gameObject.SetActive(true);
            battlePaused = true;
            ShowScreen(AppScreen.Result);
            if (victory) StartCoroutine(AnimateStars(stars));
            TriggerHaptic();
        }

        private void RefreshLevelButtons()
        {
            for (int index = 0; index < levelButtons.Count; index++)
            {
                int level = index + 1;
                bool unlocked = level <= progress.Data.unlockedLevel;
                Button button = levelButtons[index];
                button.interactable = unlocked;
                TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null) label.text = unlocked ? level.ToString() : "×";
                CampaignLevelProgress levelProgress = progress.ProgressFor(level);
                if (levelProgress != null && levelProgress.stars > 0)
                    levelStars[index].text = $"{levelProgress.stars}/3";
            }
            RefreshProfile();
        }

        private void BuildSettingsModal()
        {
            settingsModal = new GameObject("Settings Modal", typeof(RectTransform));
            settingsModal.transform.SetParent(canvas.transform, false);
            Stretch(settingsModal.GetComponent<RectTransform>());

            GameObject scrim = CreateImage("Settings Scrim", settingsModal.transform, new Color(0f, 0f, 0f, 0.72f));
            Stretch(scrim.GetComponent<RectTransform>());
            Image scrimImage = scrim.GetComponent<Image>();
            scrimImage.raycastTarget = true;
            Button scrimButton = scrim.AddComponent<Button>();
            scrimButton.targetGraphic = scrimImage;
            scrimButton.transition = Selectable.Transition.None;
            scrimButton.onClick.AddListener(CloseSettings);

            GameObject card = CreatePanel("Settings Chronicle", settingsModal.transform, theme.panel);
            AddCardAccent(card.transform, theme.commandGold);
            SetRect(card.GetComponent<RectTransform>(), Vector2.zero, new Vector2(820f, 860f), Center(), Center());
            TextMeshProUGUI title = CreateText("Settings Title", card.transform, "AYARLAR", 46, catalog.titleFont,
                FontStyles.Bold, TextAlignmentOptions.Center, theme.textPrimary);
            SetRect(title.rectTransform, new Vector2(0f, 342f), new Vector2(620f, 64f), Center(), Center());

            TextMeshProUGUI languageLabel = CreateText("Language Label", card.transform,
                "DİL", 21, catalog.bodyFont, FontStyles.Bold,
                TextAlignmentOptions.Left, theme.textPrimary);
            SetRect(languageLabel.rectTransform, new Vector2(-250f, 272f), new Vector2(220f, 44f), Center(), Center());
            turkishLanguageButton = CreateButton("Turkish Language", card.transform, "TÜRKÇE", new Vector2(190f, 54f),
                FrontendPreferences.Language == GameLanguage.Turkish ? theme.commandGold : theme.night,
                () => FrontendPreferences.Language = GameLanguage.Turkish);
            SetRect(turkishLanguageButton.GetComponent<RectTransform>(), new Vector2(40f, 266f), new Vector2(190f, 54f), Center(), Center());
            englishLanguageButton = CreateButton("English Language", card.transform, "ENGLISH", new Vector2(190f, 54f),
                FrontendPreferences.Language == GameLanguage.English ? theme.commandGold : theme.night,
                () => FrontendPreferences.Language = GameLanguage.English);
            SetRect(englishLanguageButton.GetComponent<RectTransform>(), new Vector2(246f, 266f), new Vector2(190f, 54f), Center(), Center());

            CreateSliderRow(card.transform, "MÜZİK", new Vector2(0f, 172f), FrontendPreferences.MusicVolume, value =>
            {
                FrontendPreferences.MusicVolume = value;
                AudioManager.Instance?.SetMusicVolume(value);
            });
            CreateSliderRow(card.transform, "EFEKTLER", new Vector2(0f, 78f), FrontendPreferences.SfxVolume, value =>
            {
                FrontendPreferences.SfxVolume = value;
                AudioManager.Instance?.SetSFXVolume(value);
                if (frontendUiSource != null) frontendUiSource.volume = value;
            });
            CreateToggleRow(card.transform, "TİTREŞİM", new Vector2(0f, -18f), FrontendPreferences.Haptics,
                value => FrontendPreferences.Haptics = value);
            CreateToggleRow(card.transform, "KAMERA SARSINTISI", new Vector2(0f, -102f), FrontendPreferences.CameraShake,
                value => FrontendPreferences.CameraShake = value);
            CreateToggleRow(card.transform, "AZALTILMIŞ HAREKET", new Vector2(0f, -186f), FrontendPreferences.ReducedMotion,
                value => FrontendPreferences.ReducedMotion = value);

            Button account = CreateButton("Settings Account", card.transform, "HESAP YÖNETİMİ", new Vector2(500f, 64f),
                theme.defenderBlue, () =>
                {
                    CloseSettings();
                    ShowScreen(AppScreen.Login);
                });
            SetRect(account.GetComponent<RectTransform>(), new Vector2(0f, -284f), new Vector2(500f, 64f), Center(), Center());
            Button close = CreateButton("Close Settings", card.transform, "TAMAM", new Vector2(300f, 68f),
                theme.commandGold, CloseSettings);
            SetRect(close.GetComponent<RectTransform>(), new Vector2(0f, 56f), new Vector2(300f, 68f), BottomCenter(), BottomCenter());
            settingsModal.SetActive(false);
        }

        private void BuildOrientationCurtain()
        {
            orientationCurtain = CreateImage("Orientation Curtain", canvas.transform, theme.nightDeep);
            Stretch(orientationCurtain.GetComponent<RectTransform>());

            GameObject mark = CreateBrandImage("Orientation Brand Mark", orientationCurtain.transform, theme.brandMark);
            SetRect(mark.GetComponent<RectTransform>(), new Vector2(0f, 74f), new Vector2(180f, 180f),
                Center(), Center());

            orientationCurtainLabel = CreateText("Orientation Status", orientationCurtain.transform,
                "", 24, catalog.bodyFont,
                FontStyles.Bold, TextAlignmentOptions.Center, theme.textPrimary);
            orientationCurtainLabel.characterSpacing = 2f;
            orientationCurtainLabel.lineSpacing = 8f;
            SetRect(orientationCurtainLabel.rectTransform, new Vector2(0f, -80f), new Vector2(720f, 100f),
                Center(), Center());
            orientationCurtain.transform.SetAsLastSibling();
            orientationCurtain.SetActive(false);
        }

        private IEnumerator EnterBattleAfterLandscape(int level)
        {
            SetCanvasOrientation(MobileOrientationMode.LandscapeBattle);
            canvas.gameObject.SetActive(true);
            ShowScreen(AppScreen.Battle);
            ShowOrientationCurtain(MobileOrientationMode.LandscapeBattle);
            MobileOrientationController.Request(MobileOrientationMode.LandscapeBattle);
            if (MobileOrientationController.UsesRuntimeRotation)
                yield return WaitForOrientation(MobileOrientationMode.LandscapeBattle);

            session.SetCampaignLevel(level);
            session.Initialize();
            warTableRenderer?.SetVisible(false);
            HideOrientationCurtain();
            canvas.gameObject.SetActive(false);
            battlePaused = false;
            Time.timeScale = session.CurrentGameSpeed;
            orientationRoutine = null;
        }

        private void ShowFrontendAfterPortrait(AppScreen screen)
        {
            SetCanvasOrientation(MobileOrientationMode.PortraitFrontend);
            canvas.gameObject.SetActive(true);
            ShowScreen(screen);
            ShowOrientationCurtain(MobileOrientationMode.PortraitFrontend);
            if (orientationRoutine != null) StopCoroutine(orientationRoutine);
            orientationRoutine = StartCoroutine(RevealFrontendAfterPortrait());
        }

        private IEnumerator RevealFrontendAfterPortrait()
        {
            MobileOrientationController.Request(MobileOrientationMode.PortraitFrontend);
            if (MobileOrientationController.UsesRuntimeRotation)
                yield return WaitForOrientation(MobileOrientationMode.PortraitFrontend);
            HideOrientationCurtain();
            orientationRoutine = null;
        }

        private static IEnumerator WaitForOrientation(MobileOrientationMode mode)
        {
            int stableFrames = 0;
            while (stableFrames < 2)
            {
                stableFrames = MobileOrientationController.IsSettled(mode) ? stableFrames + 1 : 0;
                yield return null;
            }
        }

        private void ShowOrientationCurtain(MobileOrientationMode mode)
        {
            if (orientationCurtain == null) return;
            orientationCurtainLabel.text = mode == MobileOrientationMode.LandscapeBattle
                ? GameLocalization.Text("SAVAŞ ALANI HAZIRLANIYOR\nCİHAZ YATAYA ALINIYOR",
                    "PREPARING THE BATTLEFIELD\nROTATING TO LANDSCAPE")
                : GameLocalization.Text("KRALLIK HARİTASI HAZIRLANIYOR\nCİHAZ PORTREYE ALINIYOR",
                    "PREPARING THE REALM MAP\nROTATING TO PORTRAIT");
            orientationCurtain.SetActive(true);
            orientationCurtain.transform.SetAsLastSibling();
        }

        private void HideOrientationCurtain()
        {
            if (orientationCurtain != null) orientationCurtain.SetActive(false);
        }

        private void SetCanvasOrientation(MobileOrientationMode mode)
        {
            if (canvasScaler == null) return;
            canvasScaler.referenceResolution = mode == MobileOrientationMode.LandscapeBattle
                ? new Vector2(1920f, 1080f)
                : new Vector2(1080f, 1920f);
        }

        private void ApplyPortraitLayout()
        {
            RectTransform welcomeCard = RequireScreenRect(AppScreen.Welcome, "Welcome Chronicle");
            SetAnchors(welcomeCard, new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.92f),
                Vector2.zero, Vector2.zero);

            RectTransform accountCard = RequireScreenRect(AppScreen.Login, "Account Chronicle");
            SetRect(accountCard, Vector2.zero, new Vector2(900f, 900f), Center(), Center());

            Transform main = screens[AppScreen.MainMenu].transform;
            RectTransform brand = RequireRect(main, "Brand Logo");
            SetRect(brand, new Vector2(0f, -115f), new Vector2(300f, 300f), TopCenter(), TopCenter());
            SetRect(RequireRect(main, "Profile Crest"), new Vector2(0f, -425f), new Vector2(760f, 96f),
                TopCenter(), TopCenter());
            SetRect(RequireRect(main, "Settings"), new Vector2(-58f, -146f), new Vector2(92f, 68f),
                TopRight(), TopRight());
            RectTransform continueChronicle = RequireRect(main, "Continue Chronicle");
            SetAnchors(continueChronicle, new Vector2(0.07f, 0.43f),
                new Vector2(0.93f, 0.735f), Vector2.zero, Vector2.zero);
            SetRect(RequireRect(continueChronicle, "Callout"), new Vector2(50f, -48f),
                new Vector2(820f, 140f), TopLeft(), TopLeft());
            SetRect(RequireRect(continueChronicle, "Continue Details"), new Vector2(50f, -205f),
                new Vector2(820f, 100f), TopLeft(), TopLeft());
            SetRect(RequireRect(continueChronicle, "Continue"), new Vector2(0f, 46f),
                new Vector2(720f, 88f), BottomCenter(), BottomCenter());

            RectTransform menuActions = RequireRect(main, "Menu Actions");
            SetAnchors(menuActions, new Vector2(0.07f, 0.11f),
                new Vector2(0.93f, 0.38f), Vector2.zero, Vector2.zero);
            SetRect(RequireRect(menuActions, "Campaign"), new Vector2(0f, 104f),
                new Vector2(700f, 84f), Center(), Center());
            SetRect(RequireRect(menuActions, "Switch Account"), new Vector2(0f, -6f),
                new Vector2(700f, 70f), Center(), Center());
            SetRect(RequireRect(menuActions, "Menu Hint"), new Vector2(0f, -120f),
                new Vector2(700f, 72f), Center(), Center());

            Transform campaign = screens[AppScreen.Campaign].transform;
            SetAnchors(RequireRect(campaign, "Region Tabs"), new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(70f, -278f), new Vector2(-70f, -204f));
            Transform tabs = campaign.Find("Region Tabs");
            for (int index = 0; index < CampaignCatalog.RegionCount; index++)
            {
                Transform tab = tabs?.Find($"Region Tab {index + 1}");
                TextMeshProUGUI label = tab != null ? tab.GetComponentInChildren<TextMeshProUGUI>(true) : null;
                if (label != null)
                {
                    label.text = (index + 1).ToString();
                    label.fontSizeMin = 18f;
                    label.fontSizeMax = 24f;
                }
            }

            SetAnchors(RequireRect(campaign, "Campaign Scroll"), Vector2.zero, Vector2.one,
                new Vector2(48f, 350f), new Vector2(-48f, -300f));
            Transform content = campaign.Find("Campaign Scroll/Viewport/Content");
            if (content != null)
            {
                for (int regionIndex = 0; regionIndex < content.childCount; regionIndex++)
                {
                    Transform region = content.GetChild(regionIndex);
                    float pageWidth = Mathf.Max(984f, campaignScroll.viewport.rect.width);
                    if (region.TryGetComponent(out LayoutElement regionLayout)) regionLayout.preferredWidth = pageWidth;
                    if (region is RectTransform regionRect) regionRect.sizeDelta = new Vector2(pageWidth, 0f);
                    ApplyPortraitRegionPath(region, regionIndex);
                }
            }

            RectTransform selectionBar = RequireRect(campaign, "Selection Bar");
            SetAnchors(selectionBar, new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(48f, 38f), new Vector2(-48f, 320f));
            SetRect(RequireRect(selectionBar, "Selected Level"), new Vector2(28f, -24f), new Vector2(900f, 48f),
                TopLeft(), TopLeft());
            SetRect(RequireRect(selectionBar, "Selected Details"), new Vector2(28f, -82f), new Vector2(900f, 76f),
                TopLeft(), TopLeft());
            SetRect(RequireRect(selectionBar, "Play Selected"), new Vector2(0f, 22f), new Vector2(620f, 76f),
                BottomCenter(), BottomCenter());

            Transform briefing = screens[AppScreen.Briefing].transform;
            SetRect(RequireRect(briefing, "Back"), new Vector2(70f, -116f), new Vector2(72f, 64f),
                TopLeft(), TopLeft());
            RectTransform briefingHeading = RequireRect(briefing, "Heading");
            SetRect(briefingHeading, new Vector2(168f, -108f), new Vector2(820f, 68f), TopLeft(), TopLeft());
            if (briefingHeading.TryGetComponent(out TextMeshProUGUI briefingHeadingLabel))
            {
                briefingHeadingLabel.enableAutoSizing = true;
                briefingHeadingLabel.fontSizeMin = 38f;
                briefingHeadingLabel.fontSizeMax = 48f;
            }
            RectTransform briefingSubheading = RequireRect(briefing, "Subheading");
            SetRect(briefingSubheading, new Vector2(170f, -178f), new Vector2(720f, 44f), TopLeft(), TopLeft());
            if (briefingSubheading.TryGetComponent(out TextMeshProUGUI briefingSubheadingLabel))
                briefingSubheadingLabel.alignment = TextAlignmentOptions.Left;

            RectTransform briefingChronicle = RequireRect(briefing, "Briefing Chronicle");
            SetAnchors(briefingChronicle, new Vector2(0.06f, 0.50f),
                new Vector2(0.94f, 0.78f), Vector2.zero, Vector2.zero);
            SetRect(RequireRect(briefingChronicle, "Briefing Region"), new Vector2(48f, -44f),
                new Vector2(820f, 38f), TopLeft(), TopLeft());
            SetRect(RequireRect(briefingChronicle, "Briefing Title"), new Vector2(48f, -96f),
                new Vector2(820f, 90f), TopLeft(), TopLeft());
            SetRect(RequireRect(briefingChronicle, "Briefing Objective"), new Vector2(50f, -208f),
                new Vector2(820f, 142f), TopLeft(), TopLeft());
            SetRect(RequireRect(briefingChronicle, "Commander Order"), new Vector2(50f, 48f),
                new Vector2(820f, 86f), BottomLeft(), BottomLeft());

            RectTransform intel = RequireRect(briefing, "Briefing Intel");
            SetAnchors(intel, new Vector2(0.06f, 0.16f), new Vector2(0.94f, 0.46f), Vector2.zero, Vector2.zero);
            SetRect(RequireRect(intel, "Enemy Intel"), new Vector2(48f, -44f), new Vector2(800f, 150f),
                TopLeft(), TopLeft());
            SetRect(RequireRect(intel, "Briefing Reward"), new Vector2(48f, -214f), new Vector2(800f, 120f),
                TopLeft(), TopLeft());
            SetRect(RequireRect(intel, "Start Defence"), new Vector2(0f, 44f), new Vector2(720f, 88f),
                BottomCenter(), BottomCenter());

            RectTransform settingsCard = settingsModal.transform.Find("Settings Chronicle") as RectTransform;
            if (settingsCard != null)
                SetRect(settingsCard, Vector2.zero, new Vector2(900f, 900f), Center(), Center());
        }

        public static Vector2 PortraitCampaignPathPosition(int levelIndexInRegion)
        {
            Vector2[] positions =
            {
                new(240f, 150f),
                new(650f, 250f),
                new(340f, 350f),
                new(720f, 450f),
                new(280f, 550f),
                new(640f, 650f),
                new(350f, 750f),
                new(710f, 850f),
                new(300f, 950f),
                new(620f, 1060f)
            };
            if (levelIndexInRegion < 0 || levelIndexInRegion >= positions.Length)
                throw new ArgumentOutOfRangeException(nameof(levelIndexInRegion));
            return positions[levelIndexInRegion];
        }

        private static void ApplyPortraitRegionPath(Transform region, int regionIndex)
        {
            var routeSegments = new List<RectTransform>();
            var progressLabels = new List<RectTransform>();
            for (int childIndex = 0; childIndex < region.childCount; childIndex++)
            {
                Transform child = region.GetChild(childIndex);
                if (child is not RectTransform childRect) continue;
                if (child.name == "Route") routeSegments.Add(childRect);
                else if (child.name == "Progress") progressLabels.Add(childRect);
            }

            for (int pathIndex = 0; pathIndex < CampaignCatalog.LevelsPerRegion - 1; pathIndex++)
            {
                Vector2 from = PortraitCampaignPathPosition(pathIndex);
                Vector2 to = PortraitCampaignPathPosition(pathIndex + 1);
                Vector2 fromLocal = new(from.x, -from.y);
                Vector2 toLocal = new(to.x, -to.y);
                Vector2 delta = toLocal - fromLocal;
                RectTransform route = routeSegments[pathIndex];
                route.anchorMin = route.anchorMax = TopLeft();
                route.pivot = new Vector2(0f, 0.5f);
                route.anchoredPosition = fromLocal;
                route.sizeDelta = new Vector2(delta.magnitude, 8f);
                route.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            }

            for (int localIndex = 0; localIndex < CampaignCatalog.LevelsPerRegion; localIndex++)
            {
                int levelNumber = regionIndex * CampaignCatalog.LevelsPerRegion + localIndex + 1;
                RectTransform level = region.Find($"Level {levelNumber}") as RectTransform;
                if (level == null || localIndex >= progressLabels.Count)
                    throw new InvalidOperationException($"Portrait campaign path is missing level {levelNumber}.");

                Vector2 position = PortraitCampaignPathPosition(localIndex);
                level.anchorMin = level.anchorMax = TopLeft();
                level.pivot = new Vector2(0.5f, 0.5f);
                level.anchoredPosition = new Vector2(position.x, -position.y);

                RectTransform progress = progressLabels[localIndex];
                progress.anchorMin = progress.anchorMax = TopLeft();
                progress.pivot = new Vector2(0.5f, 1f);
                progress.anchoredPosition = new Vector2(position.x, -(position.y + 58f));
                progress.sizeDelta = new Vector2(180f, 36f);
            }
        }

        private RectTransform RequireScreenRect(AppScreen screen, string path)
        {
            return RequireRect(screens[screen].transform, path);
        }

        private static RectTransform RequireRect(Transform parent, string path)
        {
            Transform child = parent.Find(path);
            if (child is RectTransform rect) return rect;
            throw new InvalidOperationException($"Portrait frontend layout is missing RectTransform at {parent.name}/{path}.");
        }

        private void CreateSliderRow(Transform parent, string label, Vector2 position, float value, Action<float> changed)
        {
            GameObject row = CreatePanel($"{label} Row", parent, new Color(theme.panelRaised.r, theme.panelRaised.g,
                theme.panelRaised.b, 0.82f));
            SetRect(row.GetComponent<RectTransform>(), position, new Vector2(620f, 72f), Center(), Center());
            TextMeshProUGUI text = CreateText("Label", row.transform, label, 20, catalog.bodyFont, FontStyles.Bold,
                TextAlignmentOptions.Left, theme.textPrimary);
            SetRect(text.rectTransform, new Vector2(24f, 18f), new Vector2(180f, 36f), BottomLeft(), BottomLeft());

            GameObject sliderObject = new("Slider", typeof(RectTransform), typeof(Slider));
            sliderObject.transform.SetParent(row.transform, false);
            SetRect(sliderObject.GetComponent<RectTransform>(), new Vector2(205f, 18f), new Vector2(380f, 36f),
                BottomLeft(), BottomLeft());
            Slider slider = sliderObject.GetComponent<Slider>();
            GameObject track = CreateImage("Track", sliderObject.transform, theme.nightDeep);
            Stretch(track.GetComponent<RectTransform>(), new Vector2(0f, 12f), new Vector2(0f, -12f));
            GameObject fillArea = new("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderObject.transform, false);
            Stretch(fillArea.GetComponent<RectTransform>(), new Vector2(5f, 12f), new Vector2(-5f, -12f));
            GameObject fill = CreateImage("Fill", fillArea.transform, theme.commandGold);
            Stretch(fill.GetComponent<RectTransform>());
            GameObject handleArea = new("Handle Area", typeof(RectTransform));
            handleArea.transform.SetParent(sliderObject.transform, false);
            Stretch(handleArea.GetComponent<RectTransform>(), new Vector2(8f, 0f), new Vector2(-8f, 0f));
            GameObject handle = CreateImage("Handle", handleArea.transform, theme.moonEdge);
            RectTransform handleRect = handle.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(28f, 36f);
            Image handleImage = handle.GetComponent<Image>();
            handleImage.raycastTarget = true;
            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.SetValueWithoutNotify(value);
            slider.onValueChanged.AddListener(valueChanged => changed?.Invoke(valueChanged));
        }

        private void CreateToggleRow(Transform parent, string label, Vector2 position, bool value, Action<bool> changed)
        {
            GameObject row = CreatePanel($"{label} Row", parent, new Color(theme.panelRaised.r, theme.panelRaised.g,
                theme.panelRaised.b, 0.82f));
            SetRect(row.GetComponent<RectTransform>(), position, new Vector2(620f, 66f), Center(), Center());
            TextMeshProUGUI text = CreateText("Label", row.transform, label, 20, catalog.bodyFont, FontStyles.Bold,
                TextAlignmentOptions.Left, theme.textPrimary);
            SetRect(text.rectTransform, new Vector2(24f, 15f), new Vector2(470f, 36f), BottomLeft(), BottomLeft());

            GameObject toggleObject = CreateImage("Toggle", row.transform, theme.nightDeep);
            SetRect(toggleObject.GetComponent<RectTransform>(), new Vector2(-76f, -9f), new Vector2(52f, 48f),
                TopRight(), TopRight());
            Image background = toggleObject.GetComponent<Image>();
            background.raycastTarget = true;
            Toggle toggle = toggleObject.AddComponent<Toggle>();
            GameObject markObject = CreateImage("Checkmark", toggleObject.transform, theme.commandGold);
            RectTransform mark = markObject.GetComponent<RectTransform>();
            Stretch(mark, new Vector2(8f, 8f), new Vector2(-8f, -8f));
            toggle.targetGraphic = background;
            toggle.graphic = markObject.GetComponent<Image>();
            toggle.SetIsOnWithoutNotify(value);
            toggle.onValueChanged.AddListener(valueChanged => changed?.Invoke(valueChanged));
        }

        private void OpenSettings()
        {
            if (settingsModal == null) return;
            settingsModal.SetActive(true);
            settingsModal.transform.SetAsLastSibling();
        }

        private void CloseSettings()
        {
            if (settingsModal != null) settingsModal.SetActive(false);
        }

        private void ApplyStoredAudioPreferences()
        {
            AudioManager.Instance?.SetMusicVolume(FrontendPreferences.MusicVolume);
            AudioManager.Instance?.SetSFXVolume(FrontendPreferences.SfxVolume);
            if (frontendUiSource != null) frontendUiSource.volume = FrontendPreferences.SfxVolume;
        }

        private void ReturnFromAccount()
        {
            ShowScreen(previousScreen == AppScreen.MainMenu ? AppScreen.MainMenu : AppScreen.Welcome);
        }

        private void OpenLegalUrl(string url, string label)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                SetLoginStatus($"{label} bağlantısı yayın yapılandırmasında eklenmelidir.");
                return;
            }
            Application.OpenURL(url);
        }

        private static void ReloadCampaignLevel(int level)
        {
            pendingLevel = Mathf.Clamp(level, 1, CampaignCatalog.LevelCount);
            MobileOrientationController.Request(MobileOrientationMode.LandscapeBattle);
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void ApplyDevelopmentCaptureScreen()
        {
            string captureScreen = Environment.GetEnvironmentVariable("DC_FRONTEND_CAPTURE")?.ToLowerInvariant();
            string evidencePath = Environment.GetEnvironmentVariable("DC_FRONTEND_EVIDENCE_PATH");
            string evidenceSuiteRoot = Environment.GetEnvironmentVariable("DC_FRONTEND_EVIDENCE_SUITE");
            if (!Debug.isDebugBuild && string.IsNullOrWhiteSpace(evidencePath) &&
                string.IsNullOrWhiteSpace(evidenceSuiteRoot)) return;
            if (!string.IsNullOrWhiteSpace(evidenceSuiteRoot))
            {
                StartCoroutine(CaptureFrontendEvidenceSuite(evidenceSuiteRoot));
                return;
            }
            if (captureScreen == "boot")
            {
                ShowScreen(AppScreen.Boot);
            }
            else if (captureScreen == "login")
            {
                ShowScreen(AppScreen.Login);
            }
            else if (captureScreen == "welcome")
            {
                ShowScreen(AppScreen.Welcome);
            }
            else if (captureScreen == "menu")
            {
                CompleteLocalGuestSignIn();
            }
            else if (captureScreen == "campaign")
            {
                CompleteLocalGuestSignIn();
                OpenCampaign();
            }
            else if (captureScreen == "briefing")
            {
                CompleteLocalGuestSignIn();
                ContinueCampaign();
            }
            else if (captureScreen == "battle1")
            {
                CompleteLocalGuestSignIn();
                LaunchCampaignLevel(1);
            }
            else if (captureScreen == "battle50")
            {
                CompleteLocalGuestSignIn();
                LaunchCampaignLevel(50);
            }
            else if (captureScreen == "result")
            {
                CompleteLocalGuestSignIn();
                ShowResult(true, 3, new CampaignCompletionReward(27, 68));
            }
            else if (captureScreen == "defeat")
            {
                CompleteLocalGuestSignIn();
                ShowResult(false, 0, default);
            }

            if (!string.IsNullOrWhiteSpace(evidencePath))
                StartCoroutine(CaptureFrontendEvidence(evidencePath, captureScreen ?? CurrentScreen.ToString()));
        }

        private IEnumerator CaptureFrontendEvidence(string outputPath, string screenName)
        {
            float settleDuration = screenName == "result" || screenName == "defeat" ? 1.25f : 0.45f;
            yield return new WaitForSecondsRealtime(settleDuration);
            yield return new WaitForEndOfFrame();

            string directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
            if (File.Exists(outputPath)) File.Delete(outputPath);
            ScreenCapture.CaptureScreenshot(outputPath, 1);

            float deadline = Time.realtimeSinceStartup + 5f;
            while (!File.Exists(outputPath) && Time.realtimeSinceStartup < deadline)
                yield return null;

            if (File.Exists(outputPath))
            {
                Debug.Log($"[DC_FRONTEND_EVIDENCE_PASS] screen={screenName} width={Screen.width} " +
                          $"height={Screen.height} path={outputPath}");
                if (Environment.GetEnvironmentVariable("DC_FRONTEND_EVIDENCE_QUIT") == "1")
                    Application.Quit(0);
            }
            else
            {
                Debug.LogError($"[DC_FRONTEND_EVIDENCE_FAIL] screen={screenName} path={outputPath}");
                if (Environment.GetEnvironmentVariable("DC_FRONTEND_EVIDENCE_QUIT") == "1")
                    Application.Quit(2);
            }
        }

        private IEnumerator CaptureFrontendEvidenceSuite(string outputRoot)
        {
            string[] screenNames = { "boot", "welcome", "login", "menu", "campaign", "briefing", "victory", "defeat" };
            (string name, int width, int height)[] ratios =
            {
                ("9x16", 1080, 1920),
                ("9x19_5", 1080, 2340),
                ("3x4", 1536, 2048)
            };
            Directory.CreateDirectory(outputRoot);
            int captured = 0;

            foreach ((string ratioName, int width, int height) in ratios)
            {
                Screen.SetResolution(width, height, FullScreenMode.Windowed);
                yield return new WaitForSecondsRealtime(0.55f);
                foreach (string screenName in screenNames)
                {
                    ShowEvidenceScreen(screenName);
                    yield return new WaitForSecondsRealtime(screenName == "victory" ? 1.25f : 0.45f);
                    yield return new WaitForEndOfFrame();

                    string outputPath = Path.Combine(outputRoot, $"{screenName}-{ratioName}.png");
                    if (File.Exists(outputPath)) File.Delete(outputPath);
                    ScreenCapture.CaptureScreenshot(outputPath, 1);
                    float deadline = Time.realtimeSinceStartup + 5f;
                    while (!File.Exists(outputPath) && Time.realtimeSinceStartup < deadline)
                        yield return null;
                    if (!File.Exists(outputPath))
                    {
                        Debug.LogError($"[DC_FRONTEND_EVIDENCE_FAIL] screen={screenName} ratio={ratioName}");
                        Application.Quit(2);
                        yield break;
                    }
                    captured++;
                    Debug.Log($"[DC_FRONTEND_EVIDENCE_PASS] screen={screenName} ratio={ratioName} " +
                              $"width={Screen.width} height={Screen.height} path={outputPath}");
                }
            }

            Debug.Log($"[DC_FRONTEND_EVIDENCE_SUITE_PASS] captures={captured} root={outputRoot}");
            Application.Quit(0);
        }

        private void ShowEvidenceScreen(string screenName)
        {
            switch (screenName)
            {
                case "boot":
                    ShowScreen(AppScreen.Boot);
                    break;
                case "welcome":
                    ShowScreen(AppScreen.Welcome);
                    break;
                case "login":
                    ShowScreen(AppScreen.Login);
                    break;
                case "menu":
                    CompleteLocalGuestSignIn();
                    break;
                case "campaign":
                    CompleteLocalGuestSignIn();
                    OpenCampaign();
                    break;
                case "briefing":
                    CompleteLocalGuestSignIn();
                    ContinueCampaign();
                    break;
                case "victory":
                    CompleteLocalGuestSignIn();
                    ShowResult(true, 3, new CampaignCompletionReward(27, 68));
                    break;
                case "defeat":
                    CompleteLocalGuestSignIn();
                    ShowResult(false, 0, default);
                    break;
            }
        }

        private void ShowScreen(AppScreen screen)
        {
            previousScreen = CurrentScreen;
            CurrentScreen = screen;
            foreach (KeyValuePair<AppScreen, GameObject> item in screens)
                item.Value.SetActive(item.Key == screen);
            if (screen == AppScreen.MainMenu) RefreshProfile();
            if (loginStatus != null && screen != AppScreen.Login) loginStatus.text = string.Empty;
            ApplyScreenBackdrop(screen);

            if (screens.TryGetValue(screen, out GameObject root) && root.activeSelf)
            {
                if (transitionRoutine != null) StopCoroutine(transitionRoutine);
                transitionRoutine = StartCoroutine(AnimateScreenIn(root));
            }
        }

        private void ApplyScreenBackdrop(AppScreen screen)
        {
            bool map = screen == AppScreen.Campaign || screen == AppScreen.Briefing;
            bool battleOverlay = screen == AppScreen.Result || screen == AppScreen.Battle;
            warTableRenderer?.SetVisible(map);
            if (!map && backdrop != null) backdrop.texture = theme.welcomeBackdrop;
            if (backdrop != null) backdrop.color = battleOverlay ? Color.clear : Color.white;
            if (backdropShade != null)
            {
                Color shade = backdropShade.color;
                shade.a = battleOverlay ? 0f : map ? 0.18f : 0.42f;
                backdropShade.color = shade;
            }
        }

        private IEnumerator AnimateScreenIn(GameObject root)
        {
            CanvasGroup group = root.GetComponent<CanvasGroup>();
            if (group == null) group = root.AddComponent<CanvasGroup>();
            RectTransform rect = root.GetComponent<RectTransform>();
            bool reducedMotion = FrontendPreferences.ReducedMotion;
            float fadeDuration = reducedMotion ? theme.reducedMotionDuration : theme.fadeDuration;
            float slideDuration = reducedMotion ? theme.reducedMotionDuration : theme.slideDuration;
            float duration = Mathf.Max(fadeDuration, slideDuration);
            Vector2 resting = Vector2.zero;
            Vector2 start = reducedMotion ? resting : new Vector2(32f, 0f);
            group.alpha = 0f;
            rect.anchoredPosition = start;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float fadeT = Mathf.Clamp01(fadeDuration <= 0f ? 1f : elapsed / fadeDuration);
                float slideT = Mathf.Clamp01(slideDuration <= 0f ? 1f : elapsed / slideDuration);
                fadeT = 1f - Mathf.Pow(1f - fadeT, 3f);
                slideT = 1f - Mathf.Pow(1f - slideT, 3f);
                group.alpha = fadeT;
                rect.anchoredPosition = Vector2.LerpUnclamped(start, resting, slideT);
                yield return null;
            }
            group.alpha = 1f;
            rect.anchoredPosition = resting;
            transitionRoutine = null;
        }

        private void CreateHeader(Transform root, string title, string subtitle, Action back)
        {
            Button backButton = CreateButton("Back", root, "‹", new Vector2(72f, 64f), theme.night, back);
            SetRect(backButton.GetComponent<RectTransform>(), new Vector2(80f, -112f), new Vector2(72f, 64f), TopLeft(), TopLeft());
            TextMeshProUGUI heading = CreateText("Heading", root, title, 48, catalog.titleFont, FontStyles.Bold,
                TextAlignmentOptions.Left, theme.textPrimary);
            SetRect(heading.rectTransform, new Vector2(178f, -112f), new Vector2(720f, 65f), TopLeft(), TopLeft());
            TextMeshProUGUI subheading = CreateText("Subheading", root, subtitle, 23, catalog.bodyFont, FontStyles.Normal,
                TextAlignmentOptions.Right, theme.textMuted);
            SetRect(subheading.rectTransform, new Vector2(-90f, -166f), new Vector2(520f, 46f), TopRight(), TopRight());
        }

        private Button CreateButton(string name, Transform parent, string label, Vector2 size, Color color, Action action)
        {
            GameObject root = CreateImage(name, parent, color);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            Image image = root.GetComponent<Image>();
            image.sprite = theme.buttonSprite != null ? theme.buttonSprite : catalog.panelSprite;
            image.type = image.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.raycastTarget = true;
            Button button = root.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.highlightedColor = Color.Lerp(color, Color.white, 0.14f);
            colors.pressedColor = Color.Lerp(color, Color.black, 0.18f);
            colors.disabledColor = new Color(0.12f, 0.14f, 0.16f, 0.75f);
            colors.selectedColor = Color.Lerp(color, theme.moonEdge, 0.20f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            button.onClick.AddListener(() =>
            {
                PlayFrontendCue(null, PresentationCue.UiClick);
                action?.Invoke();
            });
            root.AddComponent<FrontendButtonFeedback>();
            var outline = root.AddComponent<Outline>();
            outline.effectColor = new Color(0.55f, 0.72f, 0.94f, 0.22f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            TextMeshProUGUI text = CreateText("Label", root.transform, label, 23, catalog.bodyFont, FontStyles.Bold,
                TextAlignmentOptions.Center, RelativeLuminance(color) > 0.56f ? theme.nightDeep : theme.textPrimary);
            Stretch(text.rectTransform, new Vector2(12f, 8f), new Vector2(-12f, -8f));
            text.enableAutoSizing = true;
            text.fontSizeMin = 15f;
            text.fontSizeMax = 23f;
            return button;
        }

        private void CreateAmbientSigil(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
            Color color, Vector2 drift, float scalePulse, float rotationDrift, float speed, float phase)
        {
            GameObject sigil = CreateBrandImage(name, parent, theme.brandMark);
            Image image = sigil.GetComponent<Image>();
            image.color = color;
            SetAnchors(sigil.GetComponent<RectTransform>(), anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            sigil.AddComponent<FrontendAmbientMotion>().Configure(drift, scalePulse, rotationDrift, speed, phase);
        }

        private void AddCardAccent(Transform parent, Color color)
        {
            GameObject rail = CreateImage("Accent Rail", parent,
                new Color(color.r, color.g, color.b, 0.82f));
            SetAnchors(rail.GetComponent<RectTransform>(), new Vector2(0f, 1f), Vector2.one,
                new Vector2(24f, -7f), new Vector2(-24f, -3f));

            GameObject cap = CreateImage("Accent Cap", parent, color);
            SetRect(cap.GetComponent<RectTransform>(), new Vector2(24f, -8f), new Vector2(92f, 6f),
                TopLeft(), TopLeft());
        }

        private void AddCardWatermark(Transform parent, Color color)
        {
            GameObject watermark = CreateBrandImage("Card Watermark", parent, theme.brandMark);
            Image image = watermark.GetComponent<Image>();
            image.color = new Color(color.r, color.g, color.b, 0.045f);
            SetRect(watermark.GetComponent<RectTransform>(), new Vector2(-44f, 38f), new Vector2(250f, 250f),
                BottomRight(), BottomRight());
            watermark.transform.SetAsFirstSibling();
        }

        private void CreateLevelSelectionRing(Transform button)
        {
            GameObject ring = CreateImage("Selection Ring", button,
                new Color(theme.commandGold.r, theme.commandGold.g, theme.commandGold.b, 0.30f));
            Image image = ring.GetComponent<Image>();
            image.sprite = theme.buttonSprite != null ? theme.buttonSprite : catalog.panelSprite;
            image.type = image.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            Stretch(ring.GetComponent<RectTransform>(), new Vector2(-12f, -12f), new Vector2(12f, 12f));
            ring.transform.SetAsFirstSibling();
            ring.AddComponent<FrontendAmbientMotion>().Configure(Vector2.zero, 0.045f, 0f, 0.75f);
            ring.SetActive(false);
        }

        private static GameObject CreateImage(string name, Transform parent, Color color)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            root.transform.SetParent(parent, false);
            Image image = root.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return root;
        }

        private GameObject CreatePanel(string name, Transform parent, Color color)
        {
            GameObject root = CreateImage(name, parent, color);
            Image image = root.GetComponent<Image>();
            image.sprite = theme.panelSprite != null ? theme.panelSprite : catalog.panelSprite;
            image.type = image.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            var outline = root.AddComponent<Outline>();
            outline.effectColor = new Color(theme.moonEdge.r, theme.moonEdge.g, theme.moonEdge.b, 0.14f);
            outline.effectDistance = new Vector2(1f, -1f);
            return root;
        }

        private static GameObject CreateBrandImage(string name, Transform parent, Sprite sprite)
        {
            if (sprite == null)
                throw new InvalidOperationException($"Brand sprite is missing for {name}.");

            var root = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer),
                typeof(UnityEngine.UI.Image));
            root.transform.SetParent(parent, false);
            UnityEngine.UI.Image image = root.GetComponent<UnityEngine.UI.Image>();
            image.sprite = sprite;
            image.color = Color.white;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return root;
        }

        private static float RelativeLuminance(Color color) =>
            0.2126f * color.linear.r + 0.7152f * color.linear.g + 0.0722f * color.linear.b;

        private static void SetButtonEmphasis(Button button, Color color)
        {
            if (button?.targetGraphic != null) button.targetGraphic.color = color;
        }

        private IEnumerator AnimateStars(int stars)
        {
            if (FrontendPreferences.ReducedMotion)
            {
                resultStars.text = GameLocalization.Text($"{stars} / 3 YILDIZ", $"{stars} / 3 STARS");
                yield break;
            }
            resultStars.text = GameLocalization.Text("0 / 3 YILDIZ", "0 / 3 STARS");
            for (int index = 1; index <= stars; index++)
            {
                yield return new WaitForSecondsRealtime(0.35f);
                resultStars.text = GameLocalization.Text($"{index} / 3 YILDIZ", $"{index} / 3 STARS");
                PlayFrontendCue(theme.starReveal, PresentationCue.Victory);
            }
        }

        private void PlayFrontendCue(AudioClip preferred, PresentationCue fallback)
        {
            if (frontendUiSource == null) return;
            AudioClip clip = preferred != null ? preferred : catalog.Audio(fallback)?.clip;
            if (clip == null) return;
            frontendUiSource.Stop();
            frontendUiSource.clip = clip;
            frontendUiSource.outputAudioMixerGroup = catalog.uiMixer;
            frontendUiSource.Play();
        }

        private static void TriggerHaptic()
        {
#if UNITY_IOS || UNITY_ANDROID
            if (FrontendPreferences.Haptics) Handheld.Vibrate();
#endif
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, string value, float size,
            TMP_FontAsset font, FontStyles style, TextAlignmentOptions alignment, Color color)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            root.transform.SetParent(parent, false);
            TextMeshProUGUI text = root.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.font = font;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = color;
            text.enableWordWrapping = true;
            text.raycastTarget = false;
            if (GameLocalization.HasTranslation(value))
                root.AddComponent<LocalizedTmpText>().Configure(text, value);
            return text;
        }

        private void HandleLanguageChanged()
        {
            SetButtonEmphasis(turkishLanguageButton,
                FrontendPreferences.Language == GameLanguage.Turkish ? theme.commandGold : theme.night);
            SetButtonEmphasis(englishLanguageButton,
                FrontendPreferences.Language == GameLanguage.English ? theme.commandGold : theme.night);
            RefreshProfile();
            RefreshSelectedLevelVisuals();
            RefreshBriefing();
            RefreshLevelButtons();
            ShowOrientationCurtain(OrientationForScreen(CurrentScreen));
            HideOrientationCurtain();
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            var root = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            EventSystem.current = root.GetComponent<EventSystem>();
        }

        private static Vector2 Center() => new(0.5f, 0.5f);
        private static Vector2 TopCenter() => new(0.5f, 1f);
        private static Vector2 TopLeft() => new(0f, 1f);
        private static Vector2 TopRight() => new(1f, 1f);
        private static Vector2 BottomLeft() => new(0f, 0f);
        private static Vector2 BottomRight() => new(1f, 0f);
        private static Vector2 BottomCenter() => new(0.5f, 0f);

        private static void SetRect(RectTransform rect, Vector2 position, Vector2 size, Vector2 anchorMin, Vector2 anchorMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = anchorMin;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void SetAnchors(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void Stretch(RectTransform rect) => Stretch(rect, Vector2.zero, Vector2.zero);

        private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private void OnDestroy()
        {
            GameLocalization.LanguageChanged -= HandleLanguageChanged;
            if (session != null) session.PhaseChanged -= HandleSessionPhaseChanged;
            if (battlePaused) Time.timeScale = 1f;
        }
    }
}
