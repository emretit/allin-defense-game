using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    public sealed partial class AppFlowController : MonoBehaviour
    {
        private readonly Dictionary<AppScreen, GameObject> screens = new();
        private readonly List<Button> regionTabs = new();
        private readonly List<Button> levelButtons = new();
        private readonly List<int> levelNumbers = new();
        private readonly List<Action> regionBuilders = new();
        private readonly HashSet<int> builtRegions = new();
        private readonly List<TextMeshProUGUI> levelStars = new();
        private DefenderPresentationCatalog catalog;
        private GameSessionController session;
        private CampaignProgressStore progress;
        private FrontendPresentationTheme theme;
        private AudioSource frontendUiSource;
        private AudioListener frontendListener;
        private Canvas canvas;
        private CanvasScaler canvasScaler;
        private RawImage backdrop;
        private Image backdropShade;
        private FrontendWarTableRenderer warTableRenderer;
        private ScrollRect campaignScroll;
        private TextMeshProUGUI profileLabel;
        private TextMeshProUGUI continueDetails;
        private Button continueButton;
        private TextMeshProUGUI loginStatus;
        private TextMeshProUGUI selectedLevelTitle;
        private TextMeshProUGUI selectedLevelDetails;
        private Button playSelectedButton;
        private TextMeshProUGUI resultTitle;
        private TextMeshProUGUI resultStars;
        private TextMeshProUGUI resultReward;
        private TextMeshProUGUI briefingEnemies;
        private TextMeshProUGUI briefingReward;
        private FrontendTacticalMapPreview tacticalMapPreview;
        private Button nextLevelButton;
        private Button retryButton;
        private Button turkishLanguageButton;
        private Button englishLanguageButton;
        private GameObject settingsModal;
        private GameObject orientationCurtain;
        private TextMeshProUGUI orientationCurtainLabel;
        private FrontendNavigation navigation;
        private FrontendPerformanceProbe performance;
        private FrontendNavigationState navigationState = new();
        private static FrontendNavigationState pendingNavigation;
        private readonly HashSet<AppScreen> builtScreens = new();
        private readonly List<ScrollRect> regionScrolls = new();
        private Coroutine campaignRegionBuild;
        private bool launchingBattle;
        private bool progressDirty = true;
        private Button orientationRetry;
        private bool orientationReady;
        private Action retryOrientation;
        private Coroutine starsRoutine;
        private bool hasShownScreen;
        private Coroutine orientationRoutine;
        private int selectedLevel = 1;
        private bool battlePaused;
        private static int pendingLevel;

        public bool IsLaunchingBattle => launchingBattle;
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
            if ((AcceptanceRequested || (Application.isEditor && Environment.GetEnvironmentVariable("DC_FRONTEND_EDITOR") == "1")) && string.IsNullOrEmpty(savePath)) savePath = Path.Combine(Application.temporaryCachePath, "frontend-acceptance-save.json");
            progress = savePath == null ? CampaignApplication.GetOrCreate().Progress : new CampaignProgressStore(savePath);
            progress.Changed += HandleProgressChanged;
            if (pendingNavigation != null) { navigationState = pendingNavigation; pendingNavigation = null; }
            selectedLevel = Mathf.Clamp(navigationState.HasMapPosition ? navigationState.SelectedLevel : progress.Data.lastSelectedLevel, 1, MaxPlayableLevel);
            if (!navigationState.HasMapPosition)
            {
                navigationState.SelectedLevel = selectedLevel;
                navigationState.Region = (selectedLevel - 1) / CampaignCatalog.LevelsPerRegion;
            }
            navigation = gameObject.AddComponent<FrontendNavigation>();
            performance = gameObject.AddComponent<FrontendPerformanceProbe>();
            theme = catalog.frontendTheme != null
                ? catalog.frontendTheme
                : FrontendPresentationTheme.CreateRuntimeFallback();
            if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DC_FRONTEND_EVIDENCE_PATH")) ||
                !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DC_FRONTEND_EVIDENCE_SUITE")))
                Application.runInBackground = true;
            if (FindAnyObjectByType<AudioListener>() == null) frontendListener = gameObject.AddComponent<AudioListener>();
            frontendUiSource = gameObject.AddComponent<AudioSource>();
            frontendUiSource.playOnAwake = false;
            frontendUiSource.spatialBlend = 0f;
            frontendUiSource.outputAudioMixerGroup = catalog.uiMixer;
            frontendUiSource.volume = FrontendPreferences.SfxVolume;
            battlePaused = pauseBattle;
            session.PhaseChanged += HandleSessionPhaseChanged;
            progress.SyncStatusChanged += OnSaveStatusChanged;
            progress.ProfileChanged += CloseHeroName;
            MobileOrientationController.Request(HasPendingLevel
                ? MobileOrientationMode.LandscapeBattle
                : MobileOrientationMode.PortraitFrontend);
            GameLocalization.LanguageChanged += HandleLanguageChanged;
            GameLocalization.ApplyStoredUnityLocale();

            EnsureEventSystem();
            BuildCanvas();
            BuildHeroNameModal();
            BuildOrientationCurtain();
            ApplyStoredAudioPreferences();

            if (battlePaused) Time.timeScale = 0f;
            if (HasPendingLevel) { StartFrontendAcceptance(); return; }
            ShowScreen(AppScreen.Boot);
            if (string.Equals(Environment.GetEnvironmentVariable("DC_FRONTEND_CAPTURE"), "boot",
                    StringComparison.OrdinalIgnoreCase))
            {
                ApplyDevelopmentCaptureScreen();
                return;
            }
            StartCoroutine(FinishBoot(progress.Data.playerId == "local-guest" && progress.Data.TotalStars == 0 && progress.Data.checkpoint == null
                ? AppScreen.Welcome
                : AppScreen.MainMenu));
        }

        public void CompleteServiceSignIn(PlayerAuthProvider provider, string playerId, string displayName)
        {
            if (provider == PlayerAuthProvider.None)
                throw new ArgumentOutOfRangeException(nameof(provider));
            progress.SignIn(provider, playerId, displayName);
            RefreshProfile();
            if (CurrentScreen is AppScreen.Boot or AppScreen.Login or AppScreen.Welcome)
                ShowScreen(CurrentScreen == AppScreen.Login && navigationState.AccountReturn != AppScreen.Welcome
                    ? navigationState.AccountReturn : AppScreen.MainMenu);
        }

        public void CompleteGuestFallback()
        {
            // A late network failure must not pull an active battle back into menus.
            if (CurrentScreen is AppScreen.Boot or AppScreen.Welcome or AppScreen.Login)
                CompleteLocalGuestSignIn();
        }

        public void SetLoginStatus(string status)
        {
            if (loginStatus != null) loginStatus.text = GameLocalization.Translate(status ?? string.Empty);
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
            if (launchingBattle || orientationRoutine != null) return;
            launchingBattle = true;
            SaveMapPosition();
            performance.Begin("battle-entry");
            selectedLevel = Mathf.Clamp(level, 1, MaxPlayableLevel);
            progress.SelectLevel(selectedLevel);
            if (session.IsInitialized) { ReloadCampaignLevel(selectedLevel); return; }
            FrontendLoadingOverlay.Show(catalog.bodyFont);
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
            canvasScaler.matchWidthOrHeight = 0f;

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

            GameObject safeRoot = new("Safe Area", typeof(RectTransform));
            safeRoot.transform.SetParent(canvasObject.transform, false);
            Stretch(safeRoot.GetComponent<RectTransform>());
            // Awake applies the insets immediately, so attach after layout setup.
            safeRoot.AddComponent<SafeAreaFitter>();

            foreach (AppScreen screen in Enum.GetValues(typeof(AppScreen)))
            {
                GameObject root = new(screen.ToString(), typeof(RectTransform));
                root.transform.SetParent(safeRoot.transform, false);
                Stretch(root.GetComponent<RectTransform>());
                root.AddComponent<CanvasGroup>();
                root.SetActive(false);
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
            if (target == AppScreen.Welcome && progress.Data.playerId != "local-guest") target = AppScreen.MainMenu;
            ShowScreen(target);
            ApplyDevelopmentCaptureScreen();
            StartFrontendAcceptance();
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

            retryButton = CreateButton("Retry Level", card.transform, "YENİDEN DENE", new Vector2(310f, 140f),
                theme.defenderBlue, () => ReloadCampaignLevel(session.CampaignLevelNumber));
            SetRect(retryButton.GetComponent<RectTransform>(), new Vector2(-350f, -180f), new Vector2(310f, 140f), Center(), Center());

            nextLevelButton = CreateButton("Next Level", card.transform, "SONRAKİ BÖLÜM  ›", new Vector2(340f, 140f),
                theme.commandGold, () => ReloadCampaignLevel(session.CampaignLevelNumber + 1));
            SetRect(nextLevelButton.GetComponent<RectTransform>(), new Vector2(0f, -180f), new Vector2(340f, 140f), Center(), Center());

            Button map = CreateButton("Return To Map", card.transform, "HARİTAYA DÖN", new Vector2(310f, 140f),
                theme.night, OpenCampaign);
            SetRect(map.GetComponent<RectTransform>(), new Vector2(350f, -180f), new Vector2(310f, 140f), Center(), Center());
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
            ShowScreen(CurrentScreen == AppScreen.Login && navigationState.AccountReturn != AppScreen.Welcome
                ? navigationState.AccountReturn : AppScreen.MainMenu);
        }

        private void RequestExternalSignIn(PlayerAuthProvider provider)
        {
            ShowScreen(AppScreen.Login);
            SetLoginStatus(GameLocalization.Text("Güvenli hesap penceresi açılıyor...",
                "Opening the secure account window..."));
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
            if (IsFirstDefence())
            {
                OpenCampaign();
                return;
            }
            LaunchCampaignLevel(progress.NextUnfinishedLevel());
        }

        private bool IsFirstDefence()
        {
            return progress != null && ShouldOpenCampaignBeforeFirstBattle(progress.Data.TotalStars,
                progress.Data.unlockedLevel, navigationState.HasMapPosition);
        }

        public static bool ShouldOpenCampaignBeforeFirstBattle(int totalStars, int unlockedLevel,
            bool hasVisitedCampaignMap)
        {
            return totalStars == 0 && unlockedLevel <= 1 && !hasVisitedCampaignMap;
        }

        private void OpenCampaign()
        {
            if (launchingBattle || orientationRoutine != null) return;
            bool hadMapPosition = navigationState.HasMapPosition;
            bool returningFromBattle = CurrentScreen is AppScreen.Battle or AppScreen.Result ||
                                       MobileOrientationController.CurrentMode == MobileOrientationMode.LandscapeBattle;
            if (returningFromBattle) ShowFrontendAfterPortrait(AppScreen.Campaign);
            else ShowScreen(AppScreen.Campaign);
            SelectLevel(hadMapPosition ? navigationState.SelectedLevel : progress.Data.lastSelectedLevel);
            if (!hadMapPosition && navigationState.Region != 0) FocusRegion(navigationState.Region);
            navigationState.HasMapPosition = true;
            navigationState.SelectedLevel = selectedLevel;
            PlayFrontendCue(theme.mapOpen, PresentationCue.UiClick);
        }

        private void SelectLevel(int level)
        {
            int old = selectedLevel;
            selectedLevel = Mathf.Clamp(level, 1, MaxPlayableLevel);
            navigationState.SelectedLevel = selectedLevel;
            if (progress.Data.lastSelectedLevel != selectedLevel) progress.SelectLevel(selectedLevel);
            if (selectedLevelTitle == null) return;
            UpdateSelectedDetails();
            if (old != selectedLevel || progressDirty) RefreshSelectedLevelVisuals();
        }

        private void UpdateSelectedDetails()
        {
            var definition = CampaignCatalog.Level(selectedLevel);
            var map = CampaignLevelFactory.Map(definition.mapId);
            if (tacticalMapPreview != null)
            {
                BiomeDefinition biome = CampaignLevelFactory.Biome(map.biomeIndex);
                tacticalMapPreview.Configure(map, biome.accentColor, catalog.bodyFont);
            }
            string routeLabel = GameLocalization.IsEnglish
                ? $"{map.entranceCount} entrance{(map.entranceCount == 1 ? "" : "s")}"
                : $"{map.entranceCount} giriş";
            selectedLevelTitle.text = $"{definition.number}. {definition.localizedName}";
            selectedLevelDetails.text = GameLocalization.IsEnglish
                ? $"{definition.waveCount} waves · ~{definition.targetMinutes:0.#} min · {definition.startingGold} gold · {routeLabel}"
                : $"{definition.waveCount} dalga · ~{definition.targetMinutes:0.#} dk · {definition.startingGold} altın · {routeLabel}";
            briefingEnemies.text = GameLocalization.Text("DÜŞMANLAR", "ENEMIES") + "\n" + EnemyPreviewForLevel(definition.number);
            briefingReward.text = GameLocalization.Text("HEDEF", "OBJECTIVE") + "\n" + LearningObjective(definition.number);
            playSelectedButton.interactable = selectedLevel <= progress.Data.unlockedLevel;
        }

        private void HandleProgressChanged()
        {
            progressDirty = true;
            if (CurrentScreen == AppScreen.MainMenu) RefreshProfile();
            if (CurrentScreen == AppScreen.Campaign)
            {
                RefreshLevelButtons();
                SelectLevel(selectedLevel);
            }
        }

        private void SaveMapPosition()
        {
            if (campaignScroll == null || CurrentScreen != AppScreen.Campaign) return;
            navigationState.HasMapPosition = true;
            navigationState.SelectedLevel = selectedLevel;
            navigationState.Horizontal = campaignScroll.horizontalNormalizedPosition;
            for (int i = 0; i < regionScrolls.Count; i++) navigationState.Vertical[i] = regionScrolls[i].verticalNormalizedPosition;
        }

        private void OpenBriefing()
        {
            OpenCampaign(); // Compatibility for old callers and evidence commands.
        }

        private void StartSelectedLevel()
        {
            TriggerHaptic();
            LaunchCampaignLevel(selectedLevel);
        }

        private void RefreshBriefing()
        {
            if (selectedLevelDetails != null) UpdateSelectedDetails();
        }

        private void RefreshSelectedLevelVisuals()
        {
            for (int index = 0; index < levelButtons.Count; index++)
            {
                Button button = levelButtons[index];
                int level = levelNumbers[index];
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

        private static string EnemyPreviewForLevel(int level) => string.Join(" · ",
            CampaignLevelFactory.Create(level).waves.SelectMany(w => w.groups).Select(g => g.enemy).Distinct().Select(FirstRegionCampaign.EnemyName));

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

        private void UpdateRegionTabs()
        {
            for (int i = 0; i < regionTabs.Count; i++)
                SetButtonEmphasis(regionTabs[i], i == navigationState.Region ? theme.commandGold : theme.defenderBlue);
        }

        private void FocusRegion(int regionIndex)
        {
            if (campaignScroll == null) return;
            Canvas.ForceUpdateCanvases();
            campaignScroll.StopMovement();
            navigationState.Region = Mathf.Clamp(regionIndex, 0, VisibleRegionCount - 1);
            UpdateRegionTabs();
            campaignScroll.horizontalNormalizedPosition = VisibleRegionCount <= 1
                ? 0f
                : Mathf.Clamp01(regionIndex / (VisibleRegionCount - 1f));
            QueueRegionBuild(navigationState.Region);
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
            int visibleStars = progress.Data.levels.Where(x => x.level <= 10).Sum(x => x.stars);
            profileLabel.text = $"{displayName}  ·  {provider}\n" +
                GameLocalization.Text($"{visibleStars}/30 yıldız", $"{visibleStars}/30 stars") + "  ·  " + SaveStatus();
            if (continueDetails != null)
            {
                int target = progress.NextUnfinishedLevel();
                CampaignLevelDefinition last = CampaignCatalog.Level(target);
                CampaignLevelProgress result = progress.ProgressFor(target);
                string stars = result == null || result.stars <= 0
                    ? GameLocalization.Text("0/3 YILDIZ", "0/3 STARS")
                    : GameLocalization.Text($"{result.stars}/3 YILDIZ", $"{result.stars}/3 STARS");
                continueDetails.text = GameLocalization.IsEnglish
                    ? $"REGION {last.region}  ·  {last.localizedName.ToUpperInvariant()}  ·  {stars}\n" +
                      $"{last.waveCount} waves  ·  ~{last.targetMinutes:0.#} min  ·  {last.startingGold} starting gold"
                    : $"BÖLGE {last.region}  ·  {last.localizedName.ToUpperInvariant()}  ·  {stars}\n" +
                      $"{last.waveCount} dalga  ·  ~{last.targetMinutes:0.#} dk  ·  {last.startingGold} başlangıç altını";
            }
            if (continueButton != null)
            {
                TextMeshProUGUI label = continueButton.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label != null)
                    label.text = IsFirstDefence()
                        ? GameLocalization.Text("İLK SAVUNMANA HAZIRLAN  ›", "PREPARE YOUR FIRST DEFENCE  ›")
                        : GameLocalization.Text("DEVAM ET  ›", "CONTINUE  ›");
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
                reward = session.CompletionReward;
            }
            ShowResult(victory, stars, reward);
        }

        private void ShowResult(bool victory, int stars, CampaignCompletionReward reward)
        {
            EnsureScreen(AppScreen.Result);
            MobileOrientationController.Request(MobileOrientationMode.LandscapeBattle);
            SetCanvasOrientation(MobileOrientationMode.LandscapeBattle);
            RefreshLevelButtons();
            selectedLevel = victory
                ? Mathf.Min(FirstRegionCampaign.LevelCount, session.CampaignLevelNumber + 1)
                : session.CampaignLevelNumber;
            resultTitle.text = victory
                ? GameLocalization.Text(
                    $"ZAFER\n{CampaignCatalog.Level(session.CampaignLevelNumber).localizedName.ToUpperInvariant()} TAMAMLANDI",
                    $"VICTORY\n{CampaignCatalog.Level(session.CampaignLevelNumber).localizedName.ToUpperInvariant()} COMPLETE")
                : GameLocalization.Text("SAVUNMA HATTI DÜŞTÜ", "THE DEFENCE LINE HAS FALLEN");
            resultTitle.color = victory ? theme.textPrimary : theme.enemyRed;
            resultStars.text = victory
                ? GameLocalization.Text($"{stars} / 3 YILDIZ", $"{stars} / 3 STARS")
                : GameLocalization.Text("0 / 3 YILDIZ", "0 / 3 STARS");
            resultReward.text = victory ? SaveStatus() + "\n" +
                (session.CampaignLevelNumber == 1 && progress.Data.authProvider == PlayerAuthProvider.Guest
                    ? GameLocalization.Text("İlerlemeni korumak için menüden hesabını bağlayabilirsin.", "You can link an account from the menu to protect your progress.")
                    : LearningObjective(Mathf.Min(10, selectedLevel)))
                : GameLocalization.Text($"Dalga {session.CurrentWaveIndex + 1} · ", $"Wave {session.CurrentWaveIndex + 1} · ") +
                    FirstRegionCampaign.EnemyName(session.MostDamagingLeak) + "\n" + DefenceAdvice(session.MostDamagingLeak);
            nextLevelButton.interactable = victory && session.CampaignLevelNumber < FirstRegionCampaign.LevelCount;
            SetButtonEmphasis(nextLevelButton, victory ? theme.commandGold : theme.night);
            SetButtonEmphasis(retryButton, victory ? theme.defenderBlue : theme.commandGold);
            canvas.gameObject.SetActive(true);
            battlePaused = true;
            ShowScreen(AppScreen.Result);
            if (starsRoutine != null) StopCoroutine(starsRoutine);
            if (victory) starsRoutine = StartCoroutine(AnimateStars(stars));
            TriggerHaptic();
        }

        private void RefreshLevelButtons()
        {
            for (int index = 0; index < levelButtons.Count; index++)
            {
                int level = levelNumbers[index];
                bool unlocked = level <= progress.Data.unlockedLevel;
                Button button = levelButtons[index];
                button.interactable = unlocked;
                TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null) label.text = unlocked ? level.ToString() : "×";
                CampaignLevelProgress levelProgress = progress.ProgressFor(level);
                if (levelProgress != null && levelProgress.stars > 0)
                    levelStars[index].text = $"{levelProgress.stars}/3";
            }
            RefreshSelectedLevelVisuals();
            progressDirty = false;
            RefreshProfile();
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
            orientationCurtain.GetComponent<Image>().raycastTarget = true;
            orientationRetry = CreateButton("Retry Orientation", orientationCurtain.transform, "YENİDEN DENE",
                new Vector2(580, 140), theme.commandGold, () => retryOrientation?.Invoke());
            SetRect((RectTransform)orientationRetry.transform, new Vector2(0, -260), new Vector2(580, 140), Center(), Center());
            orientationRetry.gameObject.SetActive(false);
            orientationCurtain.transform.SetAsLastSibling();
            orientationCurtain.SetActive(false);
        }

        private IEnumerator EnterBattleAfterLandscape(int level)
        {
            // Give the persistent loading surface one frame to reach the display before
            // iOS performs the more expensive orientation and battle setup work.
            yield return null;
            SetCanvasOrientation(MobileOrientationMode.LandscapeBattle);
            canvas.gameObject.SetActive(true);
            ShowOrientationCurtain(MobileOrientationMode.LandscapeBattle);
            retryOrientation = () => { launchingBattle = false; LaunchCampaignLevel(level); };
            MobileOrientationController.Request(MobileOrientationMode.LandscapeBattle);
            yield return WaitForOrientation(MobileOrientationMode.LandscapeBattle);
            if (!orientationReady)
            {
                FrontendLoadingOverlay.Hide();
                orientationRoutine = null;
                yield break;
            }
            session.SetCampaignLevel(level);
            if (frontendListener != null) frontendListener.enabled = false;
            session.ConfigureCampaignStore(progress);
            var setup = session.InitializeIncrementally();
            while (true)
            {
                bool next = false;
                string failure = null;
                try { next = setup.MoveNext(); }
                catch (Exception exception) { failure = exception.ToString(); }
                if (failure != null)
                {
                    Debug.LogWarning("Battle preparation failed: " + failure);
                    FrontendLoadingOverlay.Hide();
                    orientationCurtainLabel.text = GameLocalization.Text("Savaş hazırlanamadı. Yeniden deneyebilirsin.", "Battle preparation failed. Please try again.");
                    retryOrientation = () => ReloadCampaignLevel(level);
                    orientationRetry.gameObject.SetActive(true);
                    orientationRoutine = null;
                    yield break;
                }
                if (!next) break;
                yield return setup.Current;
            }
            ShowScreen(AppScreen.Battle);
            warTableRenderer?.SetVisible(false);
            HideOrientationCurtain();
            FrontendLoadingOverlay.Hide();
            canvas.gameObject.SetActive(false);
            battlePaused = false;
            Time.timeScale = session.CurrentGameSpeed;
            launchingBattle = false;
            orientationRoutine = null;
            performance.Complete();
        }

        private void ShowFrontendAfterPortrait(AppScreen screen)
        {
            if (orientationRoutine != null || launchingBattle) return;
            performance.Begin("battle-" + screen);
            canvas.gameObject.SetActive(true);
            ShowOrientationCurtain(MobileOrientationMode.PortraitFrontend);
            retryOrientation = () => ShowFrontendAfterPortrait(screen);
            orientationRoutine = StartCoroutine(RevealFrontendAfterPortrait(screen));
        }

        private IEnumerator RevealFrontendAfterPortrait(AppScreen screen)
        {
            MobileOrientationController.Request(MobileOrientationMode.PortraitFrontend);
            yield return WaitForOrientation(MobileOrientationMode.PortraitFrontend);
            if (!orientationReady) { orientationRoutine = null; yield break; }
            session.SuspendForFrontend();
            if (frontendListener == null) frontendListener = gameObject.AddComponent<AudioListener>();
            frontendListener.enabled = true;
            Time.timeScale = 0f;
            SetCanvasOrientation(MobileOrientationMode.PortraitFrontend);
            ShowScreen(screen);
            if (screen == AppScreen.Campaign) SelectLevel(navigationState.SelectedLevel);
            HideOrientationCurtain();
            orientationRoutine = null;
            performance.Complete();
        }

        private IEnumerator WaitForOrientation(MobileOrientationMode mode)
        {
            orientationReady = !MobileOrientationController.UsesRuntimeRotation;
            if (orientationReady) yield break;
            var wait = new FrontendOrientationWait(Time.realtimeSinceStartupAsDouble);
            while (!wait.Tick(Time.realtimeSinceStartupAsDouble, MobileOrientationController.IsSettled(mode),
                       new Vector2Int(Screen.width, Screen.height), Screen.safeArea)) yield return null;
            orientationReady = wait.Ready;
            if (!orientationReady)
            {
                FrontendLoadingOverlay.Hide();
                orientationCurtainLabel.text = GameLocalization.Text(
                    "Ekran yönü değiştirilemedi.\nCihazı çevirip yeniden deneyebilirsin.",
                    "The screen could not rotate.\nTurn your device and try again.");
                orientationRetry.gameObject.SetActive(true);
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
            orientationRetry.gameObject.SetActive(false);
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

        private void OpenSettings()
        {
            if (launchingBattle || orientationRoutine != null || (settingsModal != null && settingsModal.activeSelf)) return;
            performance.Begin("menu-settings");
            if (settingsModal == null) BuildSettingsModal();
            navigation.Cancel();
            settingsModal.SetActive(true);
            settingsModal.transform.SetAsLastSibling();
            screens[CurrentScreen].GetComponent<CanvasGroup>().interactable = false;
            screens[CurrentScreen].GetComponent<CanvasGroup>().alpha = 0;
            performance.Complete();
        }

        private void CloseSettings()
        {
            if (settingsModal != null) settingsModal.SetActive(false);
            if (screens.TryGetValue(CurrentScreen, out var screen))
            {
                screen.GetComponent<CanvasGroup>().interactable = true;
                screen.GetComponent<CanvasGroup>().alpha = 1;
            }
        }

        private void ApplyStoredAudioPreferences()
        {
            AudioManager.Instance?.SetMusicVolume(FrontendPreferences.MusicVolume);
            AudioManager.Instance?.SetSFXVolume(FrontendPreferences.SfxVolume);
            if (frontendUiSource != null) frontendUiSource.volume = FrontendPreferences.SfxVolume;
        }

        private void ReturnFromAccount() => ShowScreen(navigationState.AccountReturn);

        private void OpenLegalUrl(string url, string label)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                SetLoginStatus($"{label} bağlantısı yayın yapılandırmasında eklenmelidir.");
                return;
            }
            Application.OpenURL(url);
        }

        private void ReloadCampaignLevel(int level)
        {
            if (FrontendLoadingOverlay.Active) return;
            launchingBattle = true;
            SaveMapPosition();
            pendingNavigation = navigationState;
            pendingLevel = Mathf.Clamp(level, 1, FirstRegionCampaign.LevelCount);
            FrontendLoadingOverlay.Show(catalog.bodyFont);
            FrontendLoadingOverlay.Reload(SceneManager.GetActiveScene().buildIndex, error =>
            {
                pendingLevel = 0; pendingNavigation = null; launchingBattle = false;
                Debug.LogWarning("Battle reload failed: " + error);
                ShowOrientationCurtain(MobileOrientationMode.LandscapeBattle);
                orientationCurtainLabel.text = GameLocalization.Text("Bölüm yüklenemedi. Yeniden deneyebilirsin.", "The level could not load. Please try again.");
                retryOrientation = () => ReloadCampaignLevel(level);
                orientationRetry.gameObject.SetActive(true);
            }, () => MobileOrientationController.Request(MobileOrientationMode.LandscapeBattle));
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
                OpenBriefing();
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
                    OpenBriefing();
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
            CloseHeroName();
            if (screen == AppScreen.Briefing) screen = AppScreen.Campaign;
            if (hasShownScreen && CurrentScreen == screen) return;
            if (screen != AppScreen.Campaign && campaignRegionBuild != null)
            {
                StopCoroutine(campaignRegionBuild);
                campaignRegionBuild = null;
            }
            SaveMapPosition();
            if (screen == AppScreen.Login) navigationState.AccountReturn =
                CurrentScreen == AppScreen.Welcome ? AppScreen.Welcome : CurrentScreen;
            if (screen != AppScreen.Result && starsRoutine != null) { StopCoroutine(starsRoutine); starsRoutine = null; }
            bool measure = !launchingBattle && orientationRoutine == null;
            if (measure) performance.Begin(CurrentScreen + "-" + screen);
            EnsureScreen(screen);
            CurrentScreen = screen;
            hasShownScreen = true;
            CloseSettings();
            if (screen == AppScreen.MainMenu) RefreshProfile();
            if (screen == AppScreen.Campaign)
            {
                if (progressDirty) RefreshLevelButtons();
                SelectLevel(selectedLevel);
                QueueRegionBuild(navigationState.Region);
                if (navigationState.HasMapPosition)
                {
                    Canvas.ForceUpdateCanvases();
                    campaignScroll.horizontalNormalizedPosition = navigationState.Horizontal;
                    for (int i = 0; i < regionScrolls.Count; i++) regionScrolls[i].verticalNormalizedPosition = navigationState.Vertical[i];
                }
            }
            if (loginStatus != null && screen != AppScreen.Login) loginStatus.text = string.Empty;
            ApplyScreenBackdrop(screen);
            navigation.Show(screens[screen], screen is not (AppScreen.Battle or AppScreen.Result),
                measure ? performance.Complete : null);
        }

        private void EnsureScreen(AppScreen screen)
        {
            if (builtScreens.Contains(screen)) return;
            switch (screen)
            {
                case AppScreen.Boot: BuildBootScreen(); break;
                case AppScreen.Welcome: BuildWelcomeScreen(); break;
                case AppScreen.Login: BuildLoginScreen(); break;
                case AppScreen.MainMenu: BuildMainMenuScreen(); break;
                case AppScreen.Campaign: BuildCampaignScreen(); break;
                case AppScreen.Result: BuildResultScreen(); break;
            }
            builtScreens.Add(screen);
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

        private void CreateHeader(Transform root, string title, string subtitle, Action back)
        {
            Button backButton = CreateButton("Back", root, "‹", new Vector2(140f, 140f), theme.night, back);
            SetRect(backButton.GetComponent<RectTransform>(), new Vector2(44f, -12f), new Vector2(140f, 140f), TopLeft(), TopLeft());
            TextMeshProUGUI heading = CreateText("Heading", root, title, 48, catalog.titleFont, FontStyles.Bold,
                TextAlignmentOptions.Left, theme.textPrimary);
            SetRect(heading.rectTransform, new Vector2(210f, -44f), new Vector2(780f, 78f), TopLeft(), TopLeft());
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
            text.fontSizeMin = 24f;
            text.fontSizeMax = 34f;
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
            SetButtonEmphasis(turkishLanguageButton, FrontendPreferences.Language == GameLanguage.Turkish ? theme.commandGold : theme.night);
            SetButtonEmphasis(englishLanguageButton, FrontendPreferences.Language == GameLanguage.English ? theme.commandGold : theme.night);
            progressDirty = true;
            RefreshProfile();
            if (builtScreens.Contains(AppScreen.Campaign)) { RefreshLevelButtons(); UpdateSelectedDetails(); }
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
            if (progress != null) progress.Changed -= HandleProgressChanged;
            if (session != null) session.PhaseChanged -= HandleSessionPhaseChanged;
            if (progress != null) progress.SyncStatusChanged -= OnSaveStatusChanged;
            if (progress != null) progress.ProfileChanged -= CloseHeroName;
            if (battlePaused) Time.timeScale = 1f;
        }
    }
}
