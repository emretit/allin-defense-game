using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using AllinDefense.Core;
using AllinDefense.Data;
using AllinDefense.Presentation;
using AllinDefense.Utils;
using AllinDefense.UI.Kit;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AllinDefense.UI
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
        private const float FrontendUiNominalVolume = 0.80f;
        private readonly Dictionary<AppScreen, GameObject> screens = new();
        private readonly List<Button> regionTabs = new();
        private readonly List<Button> levelButtons = new();
        private readonly List<int> levelNumbers = new();
        private readonly List<Action> regionBuilders = new();
        private readonly HashSet<int> builtRegions = new();
        private readonly List<UiStarRow> levelStars = new();
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
        private TextMeshProUGUI resultReward;
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
        private bool hasShownScreen;
        private Coroutine orientationRoutine;
        private int selectedLevel = 1;
        private bool battlePaused;
        private static int pendingLevel;

        public bool IsLaunchingBattle => launchingBattle;
        public AppScreen CurrentScreen { get; private set; }
        public CampaignProgressStore Progress => progress;
        public string GoogleIosClientId => theme.googleIosClientId?.Trim();
        public Func<bool> AppleSignInAvailable { get; set; } = () => Application.isEditor || Application.platform == RuntimePlatform.IPhonePlayer;
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
            session.ConfigureBattleNavigation(() => ReloadCampaignLevel(session.CampaignLevelNumber), OpenCampaign);
            if ((AcceptanceRequested || (Application.isEditor && Environment.GetEnvironmentVariable("DC_FRONTEND_EDITOR") == "1")) && string.IsNullOrEmpty(savePath)) savePath = Path.Combine(Application.temporaryCachePath, "frontend-acceptance-save.json");
            if (AcceptanceRequested)
            {
                if (acceptanceStage == 0 || string.IsNullOrEmpty(acceptanceSavePath))
                    acceptanceSavePath = Path.Combine(Application.temporaryCachePath, $"frontend-acceptance-{Guid.NewGuid():N}.json");
                savePath = acceptanceSavePath;
            }
            progress = savePath == null ? CampaignApplication.GetOrCreate().Progress : new CampaignProgressStore(savePath);
            knownUnlockedLevel = progress.Data.unlockedLevel;
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
            UiSounds.Playback = PlayUiSound;
            frontendUiSource.playOnAwake = false;
            frontendUiSource.spatialBlend = 0f;
            frontendUiSource.outputAudioMixerGroup = catalog.uiMixer;
            ApplyStoredAudioPreferences();
            battlePaused = pauseBattle;
            session.PhaseChanged += HandleSessionPhaseChanged;
            session.BeforeWaveRequested = ShowNewEnemy;
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
            if (welcomeLoginStatus != null) welcomeLoginStatus.text = GameLocalization.Translate(status ?? string.Empty);
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

            GameObject shade = CreateImage("Cinematic Shade", canvasObject.transform, UiTokens.CinematicShade);
            backdropShade = shade.GetComponent<Image>();
            Stretch(shade.GetComponent<RectTransform>());

            GameObject topShade = CreateImage("Top Shade", canvasObject.transform, UiTokens.TopShade);
            if (theme.designSystem != null) topShade.GetComponent<Image>().sprite = theme.designSystem.verticalGradient;
            SetAnchors(topShade.GetComponent<RectTransform>(), new Vector2(0f, 0.82f), Vector2.one, Vector2.zero, Vector2.zero);
            GameObject bottomShade = CreateImage("Bottom Shade", canvasObject.transform, UiTokens.BottomShade);
            SetAnchors(bottomShade.GetComponent<RectTransform>(), Vector2.zero, new Vector2(1f, 0.16f), Vector2.zero, Vector2.zero);

            CreateAmbientSigil("Moonlit Sigil", canvasObject.transform,
                new Vector2(0.66f, 0.61f), new Vector2(1.08f, 1.05f),
                UiTokens.WithAlpha(theme.moonEdge, 0.055f),
                new Vector2(10f, 7f), 0.012f, 1.4f, 0.022f, 0.4f);
            CreateAmbientSigil("Ember Sigil", canvasObject.transform,
                new Vector2(-0.10f, -0.05f), new Vector2(0.34f, 0.37f),
                UiTokens.WithAlpha(theme.commandGold, 0.040f),
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

            TextMeshProUGUI chapter = CreateText("Boot Chapter", root, FrontendCopy.CampaignEyebrow, 18, catalog.bodyFont,
                FontStyles.Bold, TextAlignmentOptions.Center, theme.moonEdge);
            chapter.characterSpacing = 6f;
            SetRect(chapter.rectTransform, new Vector2(0f, -274f), new Vector2(520f, 40f), Center(), Center());
        }

        private IEnumerator FinishBoot(AppScreen target)
        {
            Transform crest = screens[AppScreen.Boot].transform.Find("Boot Brand Logo");
            if (crest != null)
            {
                crest.localScale = Vector3.one * UiTokens.PressScale;
                var reveal = (crest.GetComponent<UiMotion>() ?? crest.gameObject.AddComponent<UiMotion>()).Scale(crest, Vector3.one);
                while (!reveal.IsComplete && !reveal.IsCancelled) yield return null;
            }
            if (target == AppScreen.Welcome && progress.Data.playerId != "local-guest") target = AppScreen.MainMenu;
            ShowScreen(target);
            ApplyDevelopmentCaptureScreen();
            StartFrontendAcceptance();
        }

        private UiStarRow resultStarRow;
        private TMP_Text resultChapter;
        private RectTransform resultRewardRow, resultAdvice;
        private TMP_Text resultAdviceText;
        private Image resultLeakIcon, resultSavedIcon;
        private RectTransform resultUnlock;
        private Button resultRetryIcon, resultMapIcon, resultMapText;
        private int knownUnlockedLevel;
        private RectTransform resultAccountSuggestion;
        private UiMotion.Handle resultAccountDelay;

        private void BuildResultScreen()
        {
            menuKit ??= new UiKit(theme.designSystem);
            Transform root = screens[AppScreen.Result].transform;
            var scrim = CreateImage("Result Scrim", root, UiTokens.ResultScrim);
            Stretch((RectTransform)scrim.transform);
            var card = menuKit.Card(root, "Result Chronicle");
            card.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            root.gameObject.AddComponent<FrontendResponsiveLayout>().Configure(size =>
                SetRect(card, Vector2.zero, new Vector2(Mathf.Min(size.x - 88, 1320), card.sizeDelta.y), Center(), Center()));
            card.GetComponent<VerticalLayoutGroup>().spacing = 18;
            resultTitle = MenuText(card, "Result Title", "", 64, true);
            resultTitle.font = catalog.titleFont; resultTitle.alignment = TextAlignmentOptions.Center;
            resultTitle.enableAutoSizing = true; resultTitle.fontSizeMin = 32; resultTitle.fontSizeMax = 64;
            resultTitle.textWrappingMode = TextWrappingModes.Normal;
            UiKit.Size(resultTitle.rectTransform, height: 100);
            resultChapter = MenuText(card, "Result Chapter", "", 30);
            resultChapter.alignment = TextAlignmentOptions.Center;
            resultStarRow = menuKit.StarRow(card, "Result Stars", 0);
            foreach (var image in resultStarRow.GetComponentsInChildren<Image>()) UiKit.Size(image.rectTransform, 96, 96);
            resultRewardRow = MenuStack(card, "Result Reward Row", false);
            resultLeakIcon = menuKit.Icon(resultRewardRow, "Result Reward Icon", catalog.goldIcon, 52);
            resultReward = MenuText(resultRewardRow, "Result Reward", "", 32, true);
            resultUnlock = menuKit.Chip(resultRewardRow, "Unlocked Chapter", "", 28);
            menuKit.Icon(resultUnlock, "Unlocked Icon", menuKit.Assets.iconCheck, 40).transform.SetAsFirstSibling();
            resultSavedIcon = menuKit.Icon(resultRewardRow, "Result Saved", menuKit.Assets.iconCheck, 32);
            resultAdvice = menuKit.Card(card, "Defence Advice");
            resultAdvice.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.MiddleLeft;
            var adviceRow = MenuStack(resultAdvice, "Advice Row", false);
            menuKit.Icon(adviceRow, "Advice Tower", catalog.archerIcon, 64);
            resultAdviceText = MenuText(adviceRow, "Advice Text", "", 30);
            resultAdviceText.textWrappingMode = TextWrappingModes.Normal;
            UiKit.Size(resultAdviceText.rectTransform, height: 90, flex: 1);
            BuildCampaignCompletionCard(card);
            var actions = MenuStack(card, "Result Actions", false);
            resultRetryIcon = menuKit.IconButton(actions, "Victory Retry", menuKit.Assets.iconRestart,
                () => MenuClick(() => ReloadCampaignLevel(session.CampaignLevelNumber)));
            resultRetryIcon.GetComponent<UiAccessibleButton>().Configure(FrontendCopy.TryAgain);
            retryButton = MenuButton(actions, "Retry Level", FrontendCopy.TryAgain, null,
                () => ReloadCampaignLevel(session.CampaignLevelNumber), true);
            nextLevelButton = MenuButton(actions, "Next Level", FrontendCopy.NextLevel, menuKit.Assets.iconPlay,
                () => ReloadCampaignLevel(session.CampaignLevelNumber + 1), true);
            resultMapIcon = menuKit.IconButton(actions, "Victory Map", menuKit.Assets.iconMap, () => MenuClick(OpenCampaign));
            resultMapIcon.GetComponent<UiAccessibleButton>().Configure(FrontendCopy.ReturnToMap);
            foreach (var button in new[] { resultRetryIcon, resultMapIcon }) UiKit.Size((RectTransform)button.transform, 160, 160);
            resultMapText = PlainTextButton(card, "Return To Map", FrontendCopy.ReturnToMap, OpenCampaign);
            resultAccountSuggestion = menuKit.Surface(card, "Result Account Suggestion", UiTokens.RadiusMedium, false);
            resultAccountSuggestion.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(16, 16, 0, 0);
            menuKit.Icon(resultAccountSuggestion, "Result Account Shield", menuKit.Assets.iconShield, 44);
            PlainTextButton(resultAccountSuggestion, "Result Link Account", FrontendCopy.LinkAccount, () => ShowFrontendAfterPortrait(AppScreen.Login));
            resultAccountSuggestion.gameObject.SetActive(false);
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
            progress.SignIn(PlayerAuthProvider.Guest, id, FrontendCopy.GuestDefender.Value);
            RefreshProfile();
            ShowScreen(CurrentScreen == AppScreen.Login && navigationState.AccountReturn != AppScreen.Welcome
                ? navigationState.AccountReturn : AppScreen.MainMenu);
        }

        private void RequestExternalSignIn(PlayerAuthProvider provider)
        {
            if (provider == PlayerAuthProvider.PlayerAccount) ShowScreen(AppScreen.Login);
            SetLoginStatus(FrontendCopy.OpeningTheSecureAccountWindow.Value);
            if (AuthenticationRequested == null)
            {
                SetLoginStatus(FrontendCopy.AccountSignInUnavailable.Value);
                return;
            }
            AuthenticationRequested.Invoke(provider);
        }

        private void ContinueCampaign()
        {
            if (GameSessionController.IsValidCheckpoint(progress, progress.Data.checkpoint) && progress.Data.checkpoint.level <= MaxPlayableLevel)
            {
                LaunchCampaignLevel(progress.Data.checkpoint.level);
                return;
            }
            if (IsFirstDefence())
            {
                OpenCampaign();
                return;
            }
            LaunchCampaignLevel(progress.NextUnfinishedLevel(PlayableLevelCount));
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
            selectedLevelTitle.text = $"{definition.number}. {definition.localizedName}";
            selectedLevelDetails.text = FrontendCopy.MenuWaves.Format(definition.waveCount);
            briefingTime.text = FrontendCopy.MenuMinutes.Format(definition.targetMinutes);
            briefingReward.text = LearningObjective(definition.number);
            foreach (Transform chip in enemyChips) { chip.gameObject.SetActive(false); Destroy(chip.gameObject); }
            foreach (var group in CampaignLevelFactory.Create(definition.number).waves.SelectMany(w => w.groups).GroupBy(g => g.enemy))
            {
                var enemy = group.Key;
                var chip = menuKit.Surface(enemyChips, "Enemy Chip " + enemy, UiTokens.RadiusPill, false);
                var button = chip.gameObject.AddComponent<Button>();
                button.targetGraphic = chip.GetComponent<Image>();
                button.onClick.AddListener(() => ShowBriefingEnemy(enemy));
                chip.gameObject.AddComponent<UiAccessibleButton>().Configure(new LocalizedCopy(
                    EnemyNameCopy(enemy).Turkish, EnemyNameCopy(enemy).English));
                chip.gameObject.AddComponent<UiPointSize>().Configure(UiMetrics.MinTouchPt);
                chip.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(12, 12, 6, 6);
                menuKit.Icon(chip, "Enemy Icon " + group.Key, EnemyIcon(group.Key), 44);
                MenuText(chip, "Enemy Count " + group.Key, $"{FirstRegionCampaign.EnemyName(group.Key)} ×{group.Sum(g => g.count)}", 24);
            }
            playSelectedButton.interactable = selectedLevel <= progress.Data.unlockedLevel;
        }

        private void HandleProgressChanged()
        {
            if (CurrentScreen is not (AppScreen.Battle or AppScreen.Result)) knownUnlockedLevel = progress.Data.unlockedLevel;
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
            OpenCampaign();
            ShowBriefingSheet();
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
                bool unlocked = level <= MaxPlayableLevel && IsRegionUnlocked(CampaignCatalog.Level(level).region);
                bool selected = level == selectedLevel;
                CampaignLevelProgress levelProgress = progress.ProgressFor(level);
                var state = button.GetComponent<UiVisualState>();
                state.SetState(!unlocked ? UiState.Disabled : selected ? UiState.Selected : UiState.Normal);
                var label = button.GetComponentInChildren<TMP_Text>(true);
                label.text = level.ToString();
                label.gameObject.SetActive(unlocked);
                levelLocks[index].gameObject.SetActive(!unlocked);
                int stars = levelProgress?.stars ?? 0;
                levelStars[index].SetStars(stars, false);
                var details = CampaignCatalog.Level(level);
                var copy = unlocked ? FrontendCopy.AccessibleLevel : FrontendCopy.AccessibleLockedLevel;
                button.GetComponent<UiAccessibleButton>().Configure(new LocalizedCopy(
                    string.Format(copy.Turkish, level, details.displayName, stars),
                    string.Format(copy.English, level, GameLocalization.EnglishText(details.displayName), stars)));
                Transform selectionRing = button.transform.Find("Selection Ring");
                if (selectionRing != null) selectionRing.gameObject.SetActive(unlocked && selected);
            }
        }

        private static string ThreatLabel(int level) => level switch
        {
            <= 3 => FrontendCopy.ThreatLow.Value,
            <= 6 => FrontendCopy.ThreatRising.Value,
            < 10 => FrontendCopy.ThreatHigh.Value,
            _ => FrontendCopy.ThreatLord.Value
        };

        private void UpdateRegionTabs()
        {
            RefreshRegionLocks();
            for (int i = 0; i < regionTabs.Count; i++)
            {
                SetButtonEmphasis(regionTabs[i], i == navigationState.Region ? theme.commandGold : theme.defenderBlue);
                regionTabLocks[i].gameObject.SetActive(!IsRegionUnlocked(i + 1));
                var region = CampaignCatalog.Regions[i];
                regionTabs[i].GetComponent<UiAccessibleButton>().Configure(new LocalizedCopy(
                    string.Format(FrontendCopy.AccessibleRegion.Turkish, region.number, region.displayName),
                    string.Format(FrontendCopy.AccessibleRegion.English, region.number, GameLocalization.EnglishText(region.displayName))));
            }
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
            string displayName = progress.Data.authProvider == PlayerAuthProvider.Guest
                ? FrontendCopy.GuestDefender.Value
                : string.IsNullOrWhiteSpace(progress.Data.displayName) ? FrontendCopy.GreenwatchDefender.Value : progress.Data.displayName;
            profileLabel.text = displayName;
            profileStars.text = FrontendCopy.StarCount.Format(progress.VisibleStars(PlayableLevelCount), PlayableLevelCount * 3);
            bool saveError = progress.HasWriteError || progress.SyncStatus == CampaignSyncStatus.Failed;
            saveErrorIcon.sprite = saveError ? menuKit.Assets.iconClose : menuKit.Assets.iconCheck;
            saveErrorIcon.color = saveError ? UiTokens.Danger : UiTokens.Moon;
            int target = progress.NextUnfinishedLevel(PlayableLevelCount);
            CampaignLevelDefinition level = CampaignCatalog.Level(target);
            chapterEyebrow.text = GameLocalization.Uppercase(FrontendCopy.MenuChapter.Format(level.region, CampaignCatalog.Regions[level.region - 1].localizedName, level.number));
            chapterTitle.text = GameLocalization.Uppercase(level.localizedName);
            chapterWaves.text = FrontendCopy.MenuWaves.Format(level.waveCount);
            chapterTime.text = FrontendCopy.MenuMinutes.Format(level.targetMinutes);
            chapterGold.text = FrontendCopy.GoldCost.Format(level.startingGold);
            chapterStars.SetStars(progress.ProgressFor(target)?.stars ?? 0, false);
            int completed = progress.Data.levels.Count(x => x.level >= (level.region - 1) * CampaignCatalog.LevelsPerRegion + 1 && x.level <= level.region * CampaignCatalog.LevelsPerRegion && x.stars > 0);
            regionProgress.SetProgress(completed);
            chapterProgress.text = FrontendCopy.MenuRegionProgress.Format(completed);
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
                ? Mathf.Min(PlayableLevelCount, session.CampaignLevelNumber + 1)
                : session.CampaignLevelNumber;
            stars = Mathf.Clamp(stars, 0, 3);
            resultTitle.text = victory ? FrontendCopy.VictoryHeading.Value : FrontendCopy.DefeatHeading.Value;
            resultTitle.color = victory ? UiTokens.Gold : UiTokens.Danger;
            resultChapter.gameObject.SetActive(true);
            resultChapter.text = CampaignCatalog.Level(session.CampaignLevelNumber).localizedName;
            resultStarRow.gameObject.SetActive(victory);
            resultAdvice.gameObject.SetActive(!victory);
            resultAdviceText.text = DefenceAdvice(session.MostDamagingLeak);
            resultLeakIcon.gameObject.SetActive(!victory);
            resultLeakIcon.sprite = victory ? null : EnemyIcon(session.MostDamagingLeak);
            resultReward.text = victory ? FrontendCopy.ResultStars.Format(stars)
                : FrontendCopy.ResultWaveLeak.Format(session.CurrentWaveIndex + 1, session.WaveCount,
                    FirstRegionCampaign.EnemyName(session.MostDamagingLeak));
            bool newlyUnlocked = victory && progress.Data.unlockedLevel <= MaxPlayableLevel && (progress.Data.unlockedLevel > knownUnlockedLevel ||
                CurrentScreen == AppScreen.Result && resultUnlock.gameObject.activeSelf);
            resultUnlock.gameObject.SetActive(newlyUnlocked);
            resultUnlock.GetComponentInChildren<TMP_Text>().text = FrontendCopy.UnlockedChapter.Format(progress.Data.unlockedLevel);
            knownUnlockedLevel = progress.Data.unlockedLevel;
            resultSavedIcon.gameObject.SetActive(victory && !progress.HasWriteError);
            bool milestone = victory && stars > 0 && session.CampaignLevelNumber % CampaignCatalog.LevelsPerRegion == 0;
            bool hasNextLevel = victory && !milestone && session.CampaignLevelNumber < MaxPlayableLevel;
            nextLevelButton.gameObject.SetActive(hasNextLevel);
            nextLevelButton.interactable = hasNextLevel;
            retryButton.gameObject.SetActive(!milestone && (!victory || !hasNextLevel));
            resultRetryIcon.gameObject.SetActive(victory && hasNextLevel);
            resultMapIcon.gameObject.SetActive(victory && !milestone);
            resultMapText.gameObject.SetActive(!victory);
            RefreshCampaignCompletionCard(victory, stars);
            resultAccountDelay?.Cancel();
            resultAccountSuggestion.gameObject.SetActive(false);
            session.SetBattleHudVisible(false);
            canvas.gameObject.SetActive(true);
            battlePaused = true;
            ShowScreen(AppScreen.Result);
            if (victory)
            {
                resultStarRow.SetStars(stars, !milestone);
                if (session.CampaignLevelNumber == 1 && progress.Data.authProvider == PlayerAuthProvider.Guest)
                    resultAccountDelay = (resultRewardRow.GetComponent<UiMotion>() ?? resultRewardRow.gameObject.AddComponent<UiMotion>())
                        .Animate(_ => { }, 1.2f, () => resultAccountSuggestion.gameObject.SetActive(CurrentScreen == AppScreen.Result));
            }
            TriggerHaptic();
        }

        private void RefreshLevelButtons()
        {
            RefreshSelectedLevelVisuals();
            progressDirty = false;
            RefreshProfile();
            RefreshRegionLocks();
            UpdateRegionTabs();
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
            orientationRetry = CreateButton("Retry Orientation", orientationCurtain.transform, FrontendCopy.TryAgain,
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
                    orientationCurtainLabel.text = FrontendCopy.BattlePreparationFailedPleaseTryAgain.Value;
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
                orientationCurtainLabel.text = FrontendCopy.OrientationFailed.Value;
                orientationRetry.gameObject.SetActive(true);
            }
        }

        private void ShowOrientationCurtain(MobileOrientationMode mode)
        {
            if (orientationCurtain == null) return;
            orientationCurtainLabel.text = mode == MobileOrientationMode.LandscapeBattle
                ? FrontendCopy.PreparingTheBattlefieldRotatingToLandscape.Value
                : FrontendCopy.PreparingTheRealmMapRotatingToPortrait.Value;
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
            canvasScaler.matchWidthOrHeight = mode == MobileOrientationMode.LandscapeBattle ? .5f : 0f;
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
            if (launchingBattle || orientationRoutine != null || settingsOpen) return;
            performance.Begin("menu-settings");
            if (settingsModal == null) BuildSettingsModal();
            UiId.AssignHierarchy(settingsModal.transform);
            RefreshLanguageButtons();
            if (settingsAccount != null)
            {
                var copy = progress.Data.authProvider is PlayerAuthProvider.Guest or PlayerAuthProvider.None
                    ? FrontendCopy.GuestLinkAccount : new LocalizedCopy(
                        string.Format(FrontendCopy.LinkedAccountRow.Turkish, progress.Data.displayName),
                        string.Format(FrontendCopy.LinkedAccountRow.English, progress.Data.displayName));
                settingsAccount.GetComponentInChildren<UiKitText>().Configure(settingsAccount.GetComponentInChildren<TMP_Text>(), copy);
                settingsAccount.GetComponent<UiAccessibleButton>().Configure(copy);
            }
            navigation.Cancel();
            settingsFade?.Cancel();
            PlayFrontendCue(null, PresentationCue.UiClick);
            settingsOpen = true;
            settingsModal.SetActive(true);
            settingsGroup.alpha = 0;
            settingsGroup.interactable = false; settingsGroup.blocksRaycasts = true;
            settingsFade = (settingsModal.GetComponent<UiMotion>() ?? settingsModal.AddComponent<UiMotion>()).Animate(t =>
            {
                settingsGroup.alpha = t;
                settingsModal.transform.Find("Settings Chronicle").localScale = Vector3.one * Mathf.Lerp(UiTokens.PressScale, 1, t);
            }, UiTokens.FadeDuration, () => settingsGroup.interactable = true);
            settingsModal.transform.SetAsLastSibling();
            screens[CurrentScreen].GetComponent<CanvasGroup>().interactable = false;
            screens[CurrentScreen].GetComponent<CanvasGroup>().alpha = 0;
            performance.Complete();
        }

        private void CloseSettings()
        {
            if (settingsOpen) PlayFrontendCue(null, PresentationCue.UiClick);
            settingsOpen = false;
            if (settingsModal != null && settingsModal.activeSelf)
            {
                settingsFade?.Cancel();
                settingsGroup.interactable = settingsGroup.blocksRaycasts = false;
                float from = settingsGroup.alpha;
                settingsFade = settingsModal.GetComponent<UiMotion>().Animate(t =>
                {
                    settingsGroup.alpha = from * (1 - t);
                    settingsModal.transform.Find("Settings Chronicle").localScale = Vector3.one * Mathf.Lerp(1, UiTokens.PressScale, t);
                }, UiTokens.FadeDuration, () => settingsModal.SetActive(false));
            }
            if (screens.TryGetValue(CurrentScreen, out var screen))
            {
                screen.GetComponent<CanvasGroup>().interactable = true;
                screen.GetComponent<CanvasGroup>().alpha = 1;
            }
        }

        private void ApplyStoredAudioPreferences()
        {
            if (frontendUiSource != null) frontendUiSource.volume = FrontendUiNominalVolume * FrontendPreferences.SfxVolume;
        }

        private void ReturnFromAccount() => ShowScreen(navigationState.AccountReturn);

        private void OpenLegalUrl(string url, string label)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                SetLoginStatus(FrontendCopy.LegalUrlUnavailable.Format(label));
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
            pendingLevel = Mathf.Clamp(level, 1, MaxPlayableLevel);
            FrontendLoadingOverlay.Show(catalog.bodyFont);
            FrontendLoadingOverlay.Reload(SceneManager.GetActiveScene().buildIndex, error =>
            {
                pendingLevel = 0; pendingNavigation = null; launchingBattle = false;
                Debug.LogWarning("Battle reload failed: " + error);
                ShowOrientationCurtain(MobileOrientationMode.LandscapeBattle);
                orientationCurtainLabel.text = FrontendCopy.LevelLoadFailed.Value;
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
                CurrentScreen == AppScreen.Welcome ? AppScreen.Welcome : CurrentScreen == AppScreen.Result ? AppScreen.Campaign : CurrentScreen;
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
            if (screen == AppScreen.Login) RefreshAccount();
            if (loginStatus != null && screen != AppScreen.Login) loginStatus.text = string.Empty;
            ApplyScreenBackdrop(screen);
            navigation.Show(screens[screen], screen is not (AppScreen.Battle or AppScreen.Result),
                measure ? performance.Complete : null);
            if (screen == AppScreen.Campaign)
            {
                ShowBriefingSheet();
                if (campaignDestinationLevel.HasValue)
                {
                    int destination = campaignDestinationLevel.Value;
                    campaignDestinationLevel = null;
                    StartCoroutine(FocusCampaignDestination(destination));
                }
            }
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
            UiId.AssignHierarchy(screens[screen].transform);
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

        private void CreateHeader(Transform root, LocalizedCopy title, LocalizedCopy subtitle, Action back)
        {
            Button backButton = CreateButton("Back", root, "‹", new Vector2(140f, 140f), theme.night, back);
            backButton.GetComponent<UiAccessibleButton>().Configure(FrontendCopy.AccessibleBack);
            SetRect(backButton.GetComponent<RectTransform>(), new Vector2(44f, -12f), new Vector2(140f, 140f), TopLeft(), TopLeft());
            TextMeshProUGUI heading = CreateText("Heading", root, title, UiTokens.TypeHeading, catalog.titleFont, FontStyles.Bold,
                TextAlignmentOptions.Left, theme.textPrimary);
            SetRect(heading.rectTransform, new Vector2(210f, -44f), new Vector2(780f, 78f), TopLeft(), TopLeft());
            TextMeshProUGUI subheading = CreateText("Subheading", root, subtitle, 23, catalog.bodyFont, FontStyles.Normal,
                TextAlignmentOptions.Right, theme.textMuted);
            SetRect(subheading.rectTransform, new Vector2(-90f, -166f), new Vector2(520f, 46f), TopRight(), TopRight());
        }

        private Button CreateButton(string name, Transform parent, LocalizedCopy label, Vector2 size, Color color, Action action)
        {
            GameObject root = CreateImage(name, parent, color);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            Image image = root.GetComponent<Image>();
            image.sprite = theme.buttonSprite != null ? theme.buttonSprite : catalog.panelSprite;
            image.type = image.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.raycastTarget = true;
            Button button = root.AddComponent<Button>();
            root.AddComponent<UiAccessibleButton>().Configure(label);
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.highlightedColor = Color.Lerp(color, Color.white, 0.14f);
            colors.pressedColor = Color.Lerp(color, Color.black, 0.18f);
            colors.disabledColor = UiTokens.ButtonDisabled;
            colors.selectedColor = Color.Lerp(color, theme.moonEdge, 0.20f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = UiTokens.ButtonColorDuration;
            button.colors = colors;
            button.onClick.AddListener(() => MenuClick(action));
            root.AddComponent<FrontendButtonFeedback>();
            var outline = root.AddComponent<Outline>();
            outline.effectColor = UiTokens.ButtonOutline;
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            TextMeshProUGUI text = CreateText("Label", root.transform, label, 23, catalog.bodyFont, FontStyles.Bold,
                TextAlignmentOptions.Center, RelativeLuminance(color) > 0.4f ? UiTokens.GoldText : theme.textPrimary);
            Stretch(text.rectTransform, new Vector2(12f, 8f), new Vector2(-12f, -8f));
            text.enableAutoSizing = true;
            text.fontSizeMin = UiTokens.TypeLabel;
            text.fontSizeMax = 34f;
            MobileTouchTarget.Attach(button);
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
                UiTokens.WithAlpha(color, 0.82f));
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
            image.color = UiTokens.WithAlpha(color, 0.045f);
            SetRect(watermark.GetComponent<RectTransform>(), new Vector2(-44f, 38f), new Vector2(250f, 250f),
                BottomRight(), BottomRight());
            watermark.transform.SetAsFirstSibling();
        }

        private static GameObject CreateImage(string name, Transform parent, Color color)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            root.transform.SetParent(parent, false);
            UiId.Assign(root.transform, name);
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
            outline.effectColor = UiTokens.WithAlpha(theme.moonEdge, 0.14f);
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
            UiId.Assign(root.transform, name);
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

        private void PlayUiSound(PresentationCue cue)
        {
            if (CurrentScreen == AppScreen.Battle)
                session.GetComponentInChildren<DefenderAudioController>()?.Play(cue);
            else PlayFrontendCue(null, cue);
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

        private static void TriggerHaptic() => UiHaptics.Light();

        private static TextMeshProUGUI CreateText(string name, Transform parent, LocalizedCopy value, float size,
            TMP_FontAsset font, FontStyles style, TextAlignmentOptions alignment, Color color)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            root.transform.SetParent(parent, false);
            UiId.Assign(root.transform, name);
            TextMeshProUGUI text = root.GetComponent<TextMeshProUGUI>();
            text.text = value.Value;
            text.font = font;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = color;
            text.enableWordWrapping = true;
            text.raycastTarget = false;
            if (value.IsLocalized)
                root.AddComponent<LocalizedTmpText>().Configure(text, value);
            return text;
        }

        private void RefreshLanguageButtons()
        {
            languageSegments?.Select(FrontendPreferences.Language == GameLanguage.Turkish ? 0 : 1, false);
        }

        private void HandleLanguageChanged()
        {
            RefreshLanguageButtons();
            progressDirty = true;
            RefreshProfile();
            if (CurrentScreen == AppScreen.Result) RefreshCampaignCompletionCard(resultWasVictory, resultEarnedStars);
            if (builtScreens.Contains(AppScreen.Campaign))
            {
                for (int i = 0; i < regionScrolls.Count; i++)
                {
                    Transform heading = regionScrolls[i].content.Find("Region Name");
                    if (heading != null) heading.GetComponent<TextMeshProUGUI>().text = $"{CampaignCatalog.Regions[i].number} · {GameLocalization.Uppercase(CampaignCatalog.Regions[i].localizedName)}";
                }
                RefreshLevelButtons();
                UpdateSelectedDetails();
            }
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

        private void RefreshDeletionAccessibilityScope()
        {
            if (deleteAccountModal != null && deleteAccountModal.GetComponent<UiAccessibleButton>() == null)
                deleteAccountModal.AddComponent<UiAccessibleButton>().ConfigureModalScope();
        }

        private void OnDestroy()
        {
            if (UiSounds.Playback == PlayUiSound) UiSounds.Playback = null;
            if (session != null) session.BeforeWaveRequested = null;
            GameLocalization.LanguageChanged -= HandleLanguageChanged;
            if (progress != null) progress.Changed -= HandleProgressChanged;
            if (session != null) session.PhaseChanged -= HandleSessionPhaseChanged;
            if (progress != null) progress.SyncStatusChanged -= OnSaveStatusChanged;
            if (progress != null) progress.ProfileChanged -= CloseHeroName;
            if (battlePaused) Time.timeScale = 1f;
        }
    }
}
