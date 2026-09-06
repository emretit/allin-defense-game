using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DefenderChronicles.Data;
using DefenderChronicles.Combat;
using DefenderChronicles.Presentation;
using DefenderChronicles.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DefenderChronicles.Core
{
    public enum GamePhase
    {
        Boot,
        Preparing,
        Wave,
        Intermission,
        Paused,
        Victory,
        Defeat
    }

    public interface IDamageable
    {
        bool IsAlive { get; }
        Vector3 Position { get; }
        void TakeDamage(int amount);
    }

    public interface ICombatTarget : IDamageable { }

    [DefaultExecutionOrder(-400)]
    public sealed partial class GameSessionController : MonoBehaviour
    {
        private const int StartingGold = 200;
        private const int StartingLives = 20;
        private const string SelectableLayerName = "Selectable";
        private const float IntermissionSeconds = 8f;
        public const float BaselineGameplayPace = 1f;
        private static readonly float[] SpeedSteps = { 1f, 2f, 4f, 8f };
        private static readonly string[] SpeedLabels = { "1x", "2x", "4x", "8x" };
        private static readonly Vector3[] OutpostSlotPositions =
        {
            new(2f, 0.15f, 0f), new(2f, 0.15f, 5f),
            new(6.5f, 0.15f, 6f), new(8f, 0.15f, 10.5f),
            new(12f, 0.15f, 1.2f), new(14f, 0.15f, 6f),
            new(18f, 0.15f, 6.8f), new(20.5f, 0.15f, 11.2f)
        };

        public static GameSessionController Instance { get; private set; }

        public GamePhase Phase { get; private set; } = GamePhase.Boot;
        public GamePhase UnpausedPhase => Phase == GamePhase.Paused ? phaseBeforePause : Phase;
        public int Gold { get; private set; } = StartingGold;
        public int Lives { get; private set; } = StartingLives;
        public IReadOnlyList<Vector3> Path => path;
        public IReadOnlyList<GreyboxEnemy> Enemies => enemies;
        public IReadOnlyList<GreyboxOutpost> Outposts => outposts;
        public GreyboxHero Hero => hero;
        public int WaveCount => waves?.Length ?? 0;
        public int CurrentWaveIndex => currentWaveIndex;
        public int SlotCount => slots.Count;
        public float CurrentGameSpeed => currentGameSpeed;
        public float GameplayDeltaTime => Time.deltaTime * BaselineGameplayPace;
        public int CampaignLevelNumber { get; private set; } = 10;
        public int StartingLivesForLevel { get; private set; } = StartingLives;
        public bool IsInitialized => initialized;

        public void SetCampaignLevel(int levelNumber)
        {
            CampaignLevelNumber = Mathf.Clamp(levelNumber, 1, CampaignCatalog.LevelCount);
        }

        public static float SpeedForStep(int index) => SpeedSteps[Mathf.Clamp(index, 0, SpeedSteps.Length - 1)];
        public static string SpeedLabelForStep(int index) => SpeedLabels[Mathf.Clamp(index, 0, SpeedLabels.Length - 1)];

        public static string SpeedLabelForScale(float scale)
        {
            int closestIndex = 0;
            float closestDistance = float.MaxValue;
            for (int index = 0; index < SpeedSteps.Length; index++)
            {
                float distance = Mathf.Abs(SpeedSteps[index] - scale);
                if (distance >= closestDistance) continue;
                closestDistance = distance;
                closestIndex = index;
            }
            return SpeedLabels[closestIndex];
        }

        public static float GameplayDelay(float authoredSeconds) =>
            Mathf.Max(0f, authoredSeconds) / BaselineGameplayPace;

        public event Action<GamePhase> PhaseChanged;
        public event Action<int> GoldChanged;
        public event Action<int> LivesChanged;
        public event Action<PresentationSignal> PresentationSignaled;

        public DefenderPresentationCatalog PresentationCatalog => presentationCatalog;
        public bool HasProductionPresentation => presentationCatalog != null;

        private readonly List<Vector3> path = new();
        private readonly List<GreyboxEnemy> enemies = new();
        private readonly List<GreyboxOutpost> outposts = new();
        private readonly List<GreyboxFriendlyUnit> friendlyUnits = new();
        private readonly List<GreyboxOutpostSlot> slots = new();

        private WaveDefinition[] waves;
        private CampaignLevelDefinition activeLevelDefinition;
        private Vector3[] activeOutpostSlotPositions = (Vector3[])OutpostSlotPositions.Clone();
        private Vector3 activeGuardianPosition = new(21.7f, 0.12f, 8.15f);
        private int currentWaveIndex;
        private int plannedThisWave;
        private int removedThisWave;
        private bool spawning;
        private bool initialized;
        private float nextHudRefresh;
        public double LastSimulationMilliseconds { get; private set; }
        public double LastHudMilliseconds { get; private set; }
        private BattleTelemetry telemetry;
        private bool firstTowerRecorded;
        public int PeakEnemyCount { get; private set; }
        private bool automatedSmoke;
        private bool automatedDefeatSmoke;
        private bool automatedSlotRegressionSmoke;
        private bool automatedAbilityCapture;
        private bool automatedPauseCapture;
        private bool automatedActorCapture;
        private bool automatedPerformanceAcceptance;
        private bool automatedSoakAcceptance;
        private bool automatedAudioVisualAcceptance;
        private bool automatedVisualAcceptance;
        private bool automatedVisualAcceptanceDefeat;
        private static bool defeatRestartRequested;
        private float intermissionRemaining;
        private GamePhase phaseBeforePause;

        private Camera gameCamera;
        private GreyboxHero hero;
        private GreyboxOutpostSlot selectedSlot;
        private GreyboxOutpost selectedOutpost;

        private Font uiFont;
        private Text goldText;
        private Text livesText;
        private Text waveText;
        private Text phaseText;
        private Text previewText;
        private Text heroText;
        private Text messageText;
        private Text selectedText;
        private Button nextWaveButton;
        private Text nextWaveButtonLabel;
        private Button pauseButton;
        private Text pauseButtonLabel;
        private Button speedButton;
        private Text speedButtonLabel;
        private Button heroAbilityButton;
        private Button upgradeButton;
        private Button sellButton;
        private GameObject selectedPanel;
        private GameObject resultPanel;
        private Text resultTitle;
        private DefenderPresentationCatalog presentationCatalog;
        private DefenderPresentationDirector presentationDirector;
        private DefenderHudController artHud;
        private Action returnToMainMenu;
        private float currentGameSpeed = 1f;
        private int speedStepIndex;

        private readonly Color groundColor = new(0.18f, 0.34f, 0.22f);
        private readonly Color roadColor = new(0.66f, 0.56f, 0.39f);
        private readonly Color roadEdgeColor = new(0.34f, 0.29f, 0.22f);
        private readonly Color stoneColor = new(0.47f, 0.52f, 0.50f);
        private readonly Color slotColor = new(0.16f, 0.57f, 0.72f);

        private void Awake()
        {
            // Telemetry queries Unity application state, which is unavailable during player construction.
            telemetry = new BattleTelemetry();
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            GameLocalization.LanguageChanged += HandleLanguageChanged;
        }

        private void OnDestroy()
        {
            telemetry?.Record(this,"exit",Phase.ToString()); telemetry?.Flush();
            GameLocalization.LanguageChanged -= HandleLanguageChanged;
            if (Instance == this) Instance = null;
        }

        private void HandleLanguageChanged()
        {
            if (CampaignLevelNumber <= 10 && waves != null) waves = FirstRegionCampaign.CreateWaves(CampaignLevelNumber);
            RefreshThreatPreviewForPhase();
            RefreshUi();
        }

        public void ConfigurePresentation(DefenderPresentationCatalog catalog)
        {
            if (initialized) throw new InvalidOperationException("Presentation must be configured before initialization.");
            presentationCatalog = catalog;
        }

        public void ConfigureMainMenuAction(Action action)
        {
            if (initialized) throw new InvalidOperationException("Main menu action must be configured before initialization.");
            returnToMainMenu = action;
        }

        public void Initialize()
        {
            if (initialized) return;
            if (presentationCatalog == null && !Application.isEditor)
                throw new InvalidOperationException("Primitive presentation is restricted to automated editor tests.");
            initialized = true;
            speedStepIndex = 0;
            currentGameSpeed = SpeedSteps[speedStepIndex];
            Time.timeScale = currentGameSpeed;
            BuildDefinitions();
            ConfigureCameraAndLight();
            BuildWorld();
            BuildInterface();
            BuildHero();
            if (presentationCatalog != null)
            {
                var presentationObject = new GameObject("Presentation Director");
                presentationObject.transform.SetParent(transform, false);
                presentationDirector = presentationObject.AddComponent<DefenderPresentationDirector>();
                presentationDirector.Initialize(this, presentationCatalog);
            }
            ValidateContent();
            SetPhase(GamePhase.Preparing);
            RestoreWaveCheckpoint();
            bool explicitAcceptanceProbe = Environment.GetEnvironmentVariable("DC_PERFORMANCE_ACCEPTANCE") == "1" ||
                                           Environment.GetEnvironmentVariable("DC_SOAK_ACCEPTANCE") == "1" ||
                                           Environment.GetEnvironmentVariable("DC_AUDIO_ACCEPTANCE") == "1";
            if (explicitAcceptanceProbe) Application.runInBackground = true;
            if (!Application.isEditor &&
                ((Debug.isDebugBuild && Application.platform == RuntimePlatform.IPhonePlayer) || explicitAcceptanceProbe))
                gameObject.AddComponent<DeviceAcceptanceProbe>();
            if (CampaignLevelNumber == 1)
                ShowMessage("Bir karakol platformuna dokun ve savunmanı kur.", 6f);
            RefreshUi();
            RefreshThreatPreviewForPhase();

            automatedSmoke = !Application.isEditor && Debug.isDebugBuild &&
                             (Environment.GetCommandLineArgs().Contains("-dc-smoke") ||
                              Environment.GetEnvironmentVariable("DC_SMOKE") == "1");
            automatedDefeatSmoke = !Application.isEditor && Debug.isDebugBuild &&
                                   Environment.GetEnvironmentVariable("DC_SMOKE_DEFEAT") == "1";
            automatedSlotRegressionSmoke = !Application.isEditor && Debug.isDebugBuild &&
                                           Environment.GetEnvironmentVariable("DC_SLOT_REGRESSION") == "1";
            automatedAbilityCapture = !Application.isEditor && Debug.isDebugBuild &&
                                      Environment.GetEnvironmentVariable("DC_ABILITY_CAPTURE") == "1";
            automatedPauseCapture = !Application.isEditor && Debug.isDebugBuild &&
                                    Environment.GetEnvironmentVariable("DC_PAUSE_CAPTURE") == "1";
            automatedActorCapture = !Application.isEditor && Debug.isDebugBuild &&
                                    Environment.GetEnvironmentVariable("DC_ACTOR_CAPTURE") == "1";
            automatedPerformanceAcceptance = !Application.isEditor && Debug.isDebugBuild &&
                                             Environment.GetEnvironmentVariable("DC_PERFORMANCE_ACCEPTANCE") == "1";
            automatedSoakAcceptance = !Application.isEditor && Debug.isDebugBuild &&
                                      Environment.GetEnvironmentVariable("DC_SOAK_ACCEPTANCE") == "1";
            automatedAudioVisualAcceptance = !Application.isEditor && Debug.isDebugBuild &&
                                              Environment.GetEnvironmentVariable("DC_AUDIO_ACCEPTANCE") == "1";
            automatedVisualAcceptance = !Application.isEditor && Debug.isDebugBuild &&
                                        Environment.GetEnvironmentVariable("DC_VIS812_CAPTURE") == "1";
            automatedVisualAcceptanceDefeat = !Application.isEditor && Debug.isDebugBuild &&
                                              Environment.GetEnvironmentVariable("DC_VIS812_CAPTURE_DEFEAT") == "1";
            if (automatedVisualAcceptance)
                StartCoroutine(RunAutomatedVis812VisualAcceptance());
            else if (automatedVisualAcceptanceDefeat)
                StartCoroutine(RunAutomatedVis812DefeatAcceptance());
            else if (automatedAudioVisualAcceptance)
                StartCoroutine(RunAutomatedAudioVisualAcceptance());
            else if (automatedPerformanceAcceptance)
                StartCoroutine(RunAutomatedPerformanceAcceptance());
            else if (automatedSoakAcceptance)
                StartCoroutine(RunAutomatedSoakAcceptance());
            else if (automatedSlotRegressionSmoke)
                StartCoroutine(RunAutomatedSlotRegressionSmoke());
            else if (automatedSmoke)
                StartCoroutine(RunAutomatedSmoke());
            else if (automatedDefeatSmoke)
                StartCoroutine(RunAutomatedDefeatAndRestartSmoke());
            else if (automatedAbilityCapture)
                StartCoroutine(RunAutomatedAbilityCapture());
            else if (automatedPauseCapture)
                StartCoroutine(RunAutomatedPauseCapture());
            else if (automatedActorCapture)
                StartCoroutine(RunAutomatedActorCapture());
        }

        private void Update()
        {
            if (!initialized) return;

            enemies.RemoveAll(item => item == null);
            outposts.RemoveAll(item => item == null);
            friendlyUnits.RemoveAll(item => item == null);

            long simulationStart = System.Diagnostics.Stopwatch.GetTimestamp();
            Profiler.BeginSample("DC.BattleSimulation");
            AdvanceBattle(Time.deltaTime);
            Profiler.EndSample();
            LastSimulationMilliseconds = (System.Diagnostics.Stopwatch.GetTimestamp() - simulationStart) * 1000d / System.Diagnostics.Stopwatch.Frequency;
            if (Phase == GamePhase.Intermission)
            {
                if (nextWaveButtonLabel != null)
                    nextWaveButtonLabel.text = intermissionRemaining > 0f
                        ? GameLocalization.Text($"DALGAYI ERKEN ÇAĞIR  {Mathf.CeilToInt(intermissionRemaining)}",
                            $"CALL WAVE EARLY  {Mathf.CeilToInt(intermissionRemaining)}")
                        : GameLocalization.Text("SONRAKİ DALGA", "NEXT WAVE");
                if (artHud != null)
                    artHud.SetNextWaveLabel(intermissionRemaining > 0f
                        ? $"{presentationCatalog.uiCopy.Get(UiTextKey.EarlyWave)}  {Mathf.CeilToInt(intermissionRemaining)}"
                        : presentationCatalog.uiCopy.Get(UiTextKey.NextWave));
                if (intermissionRemaining <= 0f)
                    StartNextWave();
            }

            HandleWorldPointer();
            if (Time.unscaledTime >= nextHudRefresh)
            {
                long uiStart = System.Diagnostics.Stopwatch.GetTimestamp(); Profiler.BeginSample("DC.BattleHud");
                RefreshUi(); Profiler.EndSample();
                LastHudMilliseconds = (System.Diagnostics.Stopwatch.GetTimestamp() - uiStart) * 1000d / System.Diagnostics.Stopwatch.Frequency;
                nextHudRefresh = Time.unscaledTime + .1f;
                RefreshFirstBattleGuide();
            }
        }

        private void RefreshThreatPreviewForPhase()
        {
            if (waves == null || waves.Length == 0) return;
            string preview = Phase switch
            {
                GamePhase.Preparing when currentWaveIndex < waves.Length =>
                    GameLocalization.Text($"Sıradaki: {waves[currentWaveIndex].preview}", $"Next: {waves[currentWaveIndex].preview}"),
                GamePhase.Intermission when currentWaveIndex < waves.Length =>
                    GameLocalization.Text($"Sıradaki: {waves[currentWaveIndex].preview}", $"Next: {waves[currentWaveIndex].preview}"),
                GamePhase.Wave when currentWaveIndex < waves.Length => waves[currentWaveIndex].preview,
                _ => previewText != null ? previewText.text : string.Empty
            };

            if (previewText != null) previewText.text = preview;
            artHud?.SetPreview(preview);
        }

        private void BuildDefinitions()
        {
            RuntimeCampaignLevel level = CampaignLevelFactory.Create(CampaignLevelNumber);
            activeLevelDefinition = level.campaign;
            path.Clear();
            path.AddRange(level.path);
            waves = level.waves;
            activeOutpostSlotPositions = level.outpostSlots;
            activeGuardianPosition = level.guardianPosition;
            Gold = activeLevelDefinition.startingGold;
            Lives = activeLevelDefinition.startingLives;
            StartingLivesForLevel = activeLevelDefinition.startingLives;
        }

        private void ConfigureCameraAndLight()
        {
            gameCamera = Camera.main;
            if (gameCamera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                gameCamera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            gameCamera.orthographic = true;
            gameCamera.orthographicSize = 8.9f;
            Vector3 focus = new Vector3(11f, 0.25f, 5.5f);
            gameCamera.transform.position = focus + new Vector3(0f, 15.8f, -16.9f);
            gameCamera.transform.rotation = Quaternion.LookRotation(focus - gameCamera.transform.position, Vector3.up);
            gameCamera.backgroundColor = new Color(0.035f, 0.085f, 0.145f);
            gameCamera.clearFlags = CameraClearFlags.SolidColor;
            gameCamera.nearClipPlane = 0.1f;
            gameCamera.farClipPlane = 100f;

            if (FindAnyObjectByType<Light>() == null)
            {
                var lightObject = new GameObject("Greybox Sun");
                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.45f;
                light.color = new Color(1f, 0.92f, 0.78f);
                lightObject.transform.rotation = Quaternion.Euler(52f, -32f, 0f);
            }

            if (presentationCatalog != null)
            {
                DefenderSceneLook.Apply(gameCamera);
                var responsiveCamera = gameCamera.GetComponent<AdaptiveBattlefieldCamera>();
                if (responsiveCamera == null)
                    responsiveCamera = gameCamera.gameObject.AddComponent<AdaptiveBattlefieldCamera>();
                responsiveCamera.Configure(gameCamera);
            }
        }

        private void BuildWorld()
        {
            if (presentationCatalog != null)
            {
                BuildStylizedWorld();
                return;
            }

            var world = new GameObject("Greybox World").transform;
            world.SetParent(transform,false);
            CreatePrimitive("Deep Blue Backdrop", PrimitiveType.Cube, new Vector3(11f, -0.72f, 5.5f),
                new Vector3(33f, 0.24f, 19f), new Color(0.06f, 0.16f, 0.24f), world, false);
            CreatePrimitive("Valley Ground", PrimitiveType.Cube, new Vector3(11f, -0.35f, 5.5f),
                new Vector3(27.5f, 0.5f, 14f), groundColor, world, true);
            CreatePrimitive("North Ridge", PrimitiveType.Cube, new Vector3(11f, 0.02f, 12.78f),
                new Vector3(28f, 0.62f, 0.75f), new Color(0.23f, 0.31f, 0.27f), world, false);
            CreatePrimitive("South Ridge", PrimitiveType.Cube, new Vector3(11f, 0.02f, -1.78f),
                new Vector3(28f, 0.62f, 0.75f), new Color(0.23f, 0.31f, 0.27f), world, false);

            for (int i = 0; i < path.Count - 1; i++)
                CreateRoadSegment(path[i], path[i + 1], world);

            BuildEnemyGate(world);
            BuildDefenderKeep(world);

            CreateGreyboxOutpostSlots(world);

            // Low-detail original scenery made only from Unity primitives.
            for (int i = 0; i < 24; i++)
            {
                float x = -1.2f + (i * 1.18f) % 27f;
                float z = i % 2 == 0 ? -1.15f : 12.15f;
                CreatePrimitive($"Boundary Rock {i + 1}", PrimitiveType.Sphere, new Vector3(x, 0.2f, z),
                    new Vector3(0.42f + (i % 3) * 0.13f, 0.34f, 0.42f + (i % 4) * 0.08f),
                    new Color(0.31f, 0.36f, 0.33f), world, false);
            }

            for (int i = 0; i < 16; i++)
            {
                float x = 0.5f + (i * 1.7f) % 22f;
                float z = i % 3 == 0 ? 0.6f : 11.1f;
                CreatePrimitive($"Pine Trunk {i + 1}", PrimitiveType.Cylinder, new Vector3(x, 0.28f, z),
                    new Vector3(0.12f, 0.55f, 0.12f), new Color(0.25f, 0.17f, 0.10f), world, false);
                CreatePrimitive($"Pine Crown {i + 1}", PrimitiveType.Sphere, new Vector3(x, 0.82f, z),
                    new Vector3(0.62f, 0.82f, 0.62f), new Color(0.11f, 0.30f, 0.18f), world, false);
            }
        }

        private void BuildStylizedWorld()
        {
            var world = new GameObject("Greenwatch Valley").transform;
            world.SetParent(transform, false);
            GameObject environment = StylizedPresentationFactory.SpawnModel(presentationCatalog.environmentPrefab, world,
                presentationCatalog, "Greenwatch Environment");
            // Blender's FBX handedness mirrors authored world-space X/Z positions; a 180° Y rotation
            // restores the gameplay coordinate system while leaving individual centered models unchanged.
            if (environment != null)
            {
                environment.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                foreach (Renderer renderer in environment.GetComponentsInChildren<Renderer>(true))
                {
                    bool platformBase = renderer.name.StartsWith("DeepBackdrop", StringComparison.OrdinalIgnoreCase) ||
                                        renderer.name.StartsWith("ValleyGround", StringComparison.OrdinalIgnoreCase);
                    bool rigidBoundary = renderer.name.StartsWith("NorthRidge", StringComparison.OrdinalIgnoreCase) ||
                                         renderer.name.StartsWith("SouthRidge", StringComparison.OrdinalIgnoreCase);
                    if (renderer.name.StartsWith("Road", StringComparison.OrdinalIgnoreCase) || rigidBoundary || platformBase ||
                        renderer.sharedMaterial == presentationCatalog.navyMaterial)
                        renderer.enabled = false;
                }
            }
            DefenderBattlefieldDressing.Install(world, presentationCatalog, gameCamera, path);

            CreateStylizedOutpostSlots(world);
        }

        private void CreateGreyboxOutpostSlots(Transform world)
        {
            for (int i = 0; i < activeOutpostSlotPositions.Length; i++)
            {
                Vector3 position = activeOutpostSlotPositions[i];
                var slotRoot = CreatePrimitive($"Outpost Slot {i + 1}", PrimitiveType.Cylinder, position,
                    new Vector3(1.18f, 0.16f, 1.18f), stoneColor, world, true);
                CreatePrimitive($"Outpost Slot {i + 1} Glow", PrimitiveType.Cylinder, position + Vector3.up * 0.08f,
                    new Vector3(0.86f, 0.05f, 0.86f), slotColor, slotRoot.transform, false);
                CreatePrimitive($"Outpost Slot {i + 1} Peg", PrimitiveType.Cube,
                    position + new Vector3(0f, 0.22f, 0.42f), new Vector3(0.12f, 0.28f, 0.12f),
                    new Color(0.08f, 0.24f, 0.32f), slotRoot.transform, false);
                AddOutpostSlot(slotRoot);
            }
        }

        private void CreateStylizedOutpostSlots(Transform world)
        {
            for (int i = 0; i < activeOutpostSlotPositions.Length; i++)
            {
                GameObject root = StylizedPresentationFactory.CreateRoot($"Outpost Slot {i + 1}",
                    activeOutpostSlotPositions[i], new Vector3(1.25f, 0.24f, 1.25f),
                    presentationCatalog.outpostSlotPrefab, presentationCatalog, PresentationCollider.Cylinder);
                root.transform.SetParent(world, true);
                AddOutpostSlot(root);
            }
        }

        private void AddOutpostSlot(GameObject root)
        {
            SetLayerRecursively(root, LayerMask.NameToLayer(SelectableLayerName));
            var slot = root.AddComponent<GreyboxOutpostSlot>();
            slot.Configure(this, slots.Count, slotColor);
            slots.Add(slot);
        }

        private void CreateRoadSegment(Vector3 from, Vector3 to, Transform parent)
        {
            Vector3 delta = to - from;
            Vector3 center = (from + to) * 0.5f + new Vector3(0f, -0.04f, 0f);
            var edge = CreatePrimitive("Road Edge", PrimitiveType.Cube, center + Vector3.down * 0.02f,
                new Vector3(1.92f, 0.16f, delta.magnitude + 1.8f), roadEdgeColor, parent, false);
            edge.transform.rotation = Quaternion.LookRotation(delta.normalized, Vector3.up);
            var road = CreatePrimitive("Road Surface", PrimitiveType.Cube, center + Vector3.up * 0.04f,
                new Vector3(1.42f, 0.12f, delta.magnitude + 1.22f), roadColor, parent, false);
            road.transform.rotation = Quaternion.LookRotation(delta.normalized, Vector3.up);
        }

        private void BuildEnemyGate(Transform world)
        {
            Vector3 origin = path[0];
            CreatePrimitive("Enemy Gate Base", PrimitiveType.Cube, origin + new Vector3(-0.25f, 0.22f, 0f),
                new Vector3(1.6f, 0.44f, 2.6f), new Color(0.24f, 0.10f, 0.12f), world, false);
            CreatePrimitive("Enemy Gate Left Pillar", PrimitiveType.Cube, origin + new Vector3(-0.55f, 1.05f, -0.82f),
                new Vector3(0.52f, 1.9f, 0.52f), new Color(0.50f, 0.12f, 0.16f), world, false);
            CreatePrimitive("Enemy Gate Right Pillar", PrimitiveType.Cube, origin + new Vector3(-0.55f, 1.05f, 0.82f),
                new Vector3(0.52f, 1.9f, 0.52f), new Color(0.50f, 0.12f, 0.16f), world, false);
            CreatePrimitive("Enemy Gate Beam", PrimitiveType.Cube, origin + new Vector3(-0.55f, 2.02f, 0f),
                new Vector3(0.62f, 0.42f, 2.26f), new Color(0.63f, 0.18f, 0.20f), world, false);
        }

        private void BuildDefenderKeep(Transform world)
        {
            Vector3 origin = path[^1];
            CreatePrimitive("Defender Keep Main", PrimitiveType.Cube, origin + new Vector3(0.15f, 1.12f, 0f),
                new Vector3(2.4f, 2.25f, 3.15f), new Color(0.28f, 0.42f, 0.57f), world, false);
            CreatePrimitive("Defender Keep Tower North", PrimitiveType.Cylinder, origin + new Vector3(-0.55f, 1.55f, 1.35f),
                new Vector3(0.72f, 2.6f, 0.72f), new Color(0.34f, 0.50f, 0.66f), world, false);
            CreatePrimitive("Defender Keep Tower South", PrimitiveType.Cylinder, origin + new Vector3(-0.55f, 1.55f, -1.35f),
                new Vector3(0.72f, 2.6f, 0.72f), new Color(0.34f, 0.50f, 0.66f), world, false);
            CreatePrimitive("Defender Keep Banner", PrimitiveType.Cube, origin + new Vector3(-0.95f, 2.95f, 0f),
                new Vector3(0.12f, 0.7f, 1.05f), new Color(0.25f, 0.75f, 1f), world, false);
        }

        private void BuildHero()
        {
            Vector3 guardianPosition = activeGuardianPosition;
            GameObject go;
            if (presentationCatalog != null)
            {
                go = StylizedPresentationFactory.CreateRoot("Knight Commander",
                    guardianPosition, new Vector3(0.72f, 1.9f, 0.72f),
                    presentationCatalog.ActorPrefab(PresentationActorKind.Knight), presentationCatalog,
                    PresentationCollider.Capsule, presentationCatalog.ActorScale(PresentationActorKind.Knight),
                    presentationCatalog.ActorAnimator(PresentationActorKind.Knight), PresentationActorKind.Knight);
                go.transform.SetParent(transform, true);
            }
            else
            {
                go = CreatePrimitive("Knight Commander", PrimitiveType.Capsule, guardianPosition + Vector3.up,
                    new Vector3(0.75f, 1f, 0.75f), new Color(0.20f, 0.52f, 1f), transform, true);
                CreatePrimitive("Knight Shield", PrimitiveType.Cube, go.transform.position + new Vector3(-0.42f, 0.1f, -0.1f),
                    new Vector3(0.16f, 0.58f, 0.42f), new Color(0.70f, 0.84f, 0.92f), go.transform, false);
                CreatePrimitive("Knight Crest", PrimitiveType.Sphere, go.transform.position + new Vector3(0f, 0.62f, 0f),
                    new Vector3(0.36f, 0.22f, 0.36f), new Color(0.95f, 0.86f, 0.34f), go.transform, false);
            }
            hero = go.AddComponent<GreyboxHero>();
            hero.Configure(this, guardianPosition);
        }

        private void BuildInterface()
        {
            EnsureEventSystem();
            uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var canvasObject = new GameObject("Game HUD");
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();

            if (presentationCatalog != null)
            {
                // Decorative rails render edge-to-edge. DefenderHudResponsiveLayout keeps
                // their controls inside Screen.safeArea without shrinking the whole HUD.
                GameObject hudObject = Instantiate(presentationCatalog.hudPrefab, canvasObject.transform);
                hudObject.name = "Defender HUD";
                Stretch(hudObject.GetComponent<RectTransform>());
                artHud = hudObject.GetComponent<DefenderHudController>();
                if (artHud == null)
                    throw new InvalidOperationException("Presentation HUD prefab has no DefenderHudController.");
                artHud.Initialize(presentationCatalog, TryBuildSelected, UseHeroAbility, CancelSelection,
                    StartNextWave, OnPausePressed, ReturnToMainMenu, ToggleGameSpeed, UpgradeSelected, SellSelected, RestartLevel,
                    () => EmitPresentation(PresentationCue.UiClick, Vector3.zero));
                return;
            }

            var safeRoot = CreateUiObject("Safe Area", canvasObject.transform);
            Stretch(safeRoot.GetComponent<RectTransform>());
            safeRoot.AddComponent<RuntimeSafeArea>();

            var topBar = CreatePanel("Top Bar", safeRoot.transform, new Color(0.035f, 0.055f, 0.075f, 0.94f));
            SetAnchors(topBar.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -76f), new Vector2(0f, 0f));
            goldText = CreateText("Gold", topBar.transform, "ALTIN  200", 28, TextAnchor.MiddleLeft);
            SetRect(goldText.rectTransform, new Vector2(20f, 8f), new Vector2(300f, 60f), new Vector2(0f, 0f), new Vector2(0f, 0f));
            livesText = CreateText("Lives", topBar.transform, "KALE  20", 28, TextAnchor.MiddleLeft);
            SetRect(livesText.rectTransform, new Vector2(320f, 8f), new Vector2(260f, 60f), Vector2.zero, Vector2.zero);
            waveText = CreateText("Wave", topBar.transform, "DALGA  0 / 7", 27, TextAnchor.MiddleCenter);
            SetRect(waveText.rectTransform, new Vector2(-190f, 8f), new Vector2(380f, 60f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
            phaseText = CreateText("Phase", topBar.transform, "HAZIRLIK", 22, TextAnchor.MiddleCenter);
            SetRect(phaseText.rectTransform, new Vector2(205f, 8f), new Vector2(260f, 60f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
            pauseButton = CreateButton("Pause", topBar.transform, "DURAKLAT", new Vector2(190f, 52f), OnPausePressed);
            SetRect(pauseButton.GetComponent<RectTransform>(), new Vector2(-210f, 12f), new Vector2(190f, 52f), new Vector2(1f, 0f), new Vector2(1f, 0f));
            pauseButtonLabel = pauseButton.GetComponentInChildren<Text>();
            speedButton = CreateButton("Speed", topBar.transform, "1x", new Vector2(92f, 52f), ToggleGameSpeed);
            SetRect(speedButton.GetComponent<RectTransform>(), new Vector2(-314f, 12f), new Vector2(92f, 52f), new Vector2(1f, 0f), new Vector2(1f, 0f));
            speedButtonLabel = speedButton.GetComponentInChildren<Text>();

            previewText = CreateText("Preview", safeRoot.transform, "", 26, TextAnchor.MiddleCenter);
            previewText.color = new Color(1f, 0.86f, 0.42f);
            SetRect(previewText.rectTransform, new Vector2(-370f, -156f), new Vector2(740f, 48f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));

            messageText = CreateText("Message", safeRoot.transform, "", 30, TextAnchor.MiddleCenter);
            messageText.color = Color.white;
            SetRect(messageText.rectTransform, new Vector2(-500f, -220f), new Vector2(1000f, 58f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));

            var bottomBar = CreatePanel("Bottom Bar", safeRoot.transform, new Color(0.035f, 0.055f, 0.075f, 0.95f));
            SetAnchors(bottomBar.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 0f),
                Vector2.zero, new Vector2(0f, 122f));

            var archer = CreateButton("Archer", bottomBar.transform, "OKÇU  75", new Vector2(190f, 62f), () => TryBuildSelected(OutpostKind.Archer));
            SetRect(archer.GetComponent<RectTransform>(), new Vector2(18f, 34f), new Vector2(190f, 62f), Vector2.zero, Vector2.zero);
            var barracks = CreateButton("Barracks", bottomBar.transform, "KIŞLA  100", new Vector2(190f, 62f), () => TryBuildSelected(OutpostKind.Barracks));
            SetRect(barracks.GetComponent<RectTransform>(), new Vector2(222f, 34f), new Vector2(190f, 62f), Vector2.zero, Vector2.zero);
            var mage = CreateButton("Mage", bottomBar.transform, "BÜYÜ  125", new Vector2(190f, 62f), () => TryBuildSelected(OutpostKind.Mage));
            SetRect(mage.GetComponent<RectTransform>(), new Vector2(426f, 34f), new Vector2(190f, 62f), Vector2.zero, Vector2.zero);
            heroAbilityButton = CreateButton("Special", bottomBar.transform, "KALKAN DARBESİ", new Vector2(238f, 62f), UseHeroAbility);
            SetRect(heroAbilityButton.GetComponent<RectTransform>(), new Vector2(638f, 34f), new Vector2(238f, 62f), Vector2.zero, Vector2.zero);
            var cancel = CreateButton("Cancel", bottomBar.transform, "İPTAL", new Vector2(150f, 62f), CancelSelection);
            SetRect(cancel.GetComponent<RectTransform>(), new Vector2(894f, 34f), new Vector2(150f, 62f), Vector2.zero, Vector2.zero);

            heroText = CreateText("Hero Status", bottomBar.transform, "ŞÖVALYE", 21, TextAnchor.MiddleLeft);
            SetRect(heroText.rectTransform, new Vector2(638f, 4f), new Vector2(300f, 28f), Vector2.zero, Vector2.zero);

            nextWaveButton = CreateButton("Next Wave", bottomBar.transform, "İLK DALGAYI BAŞLAT", new Vector2(310f, 62f), StartNextWave);
            SetRect(nextWaveButton.GetComponent<RectTransform>(), new Vector2(-328f, 34f), new Vector2(310f, 62f), new Vector2(1f, 0f), new Vector2(1f, 0f));
            nextWaveButtonLabel = nextWaveButton.GetComponentInChildren<Text>();

            selectedPanel = CreatePanel("Selected", safeRoot.transform, new Color(0.035f, 0.055f, 0.075f, 0.92f));
            SetAnchors(selectedPanel.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-320f, -110f), new Vector2(-18f, 168f));
            selectedText = CreateText("Selected Text", selectedPanel.transform, "PLATFORM SEÇİLMEDİ", 24, TextAnchor.UpperLeft);
            Stretch(selectedText.rectTransform, new Vector2(16f, 92f), new Vector2(-16f, -14f));
            upgradeButton = CreateButton("Upgrade", selectedPanel.transform, "YÜKSELT", new Vector2(130f, 58f), UpgradeSelected);
            SetRect(upgradeButton.GetComponent<RectTransform>(), new Vector2(14f, 18f), new Vector2(130f, 58f), Vector2.zero, Vector2.zero);
            sellButton = CreateButton("Sell", selectedPanel.transform, "SAT", new Vector2(130f, 58f), SellSelected);
            SetRect(sellButton.GetComponent<RectTransform>(), new Vector2(168f, 18f), new Vector2(130f, 58f), Vector2.zero, Vector2.zero);
            selectedPanel.SetActive(false);

            resultPanel = CreatePanel("Result", safeRoot.transform, new Color(0f, 0f, 0f, 0.82f));
            Stretch(resultPanel.GetComponent<RectTransform>());
            resultTitle = CreateText("Result Title", resultPanel.transform, "", 68, TextAnchor.MiddleCenter);
            SetRect(resultTitle.rectTransform, new Vector2(-500f, -60f), new Vector2(1000f, 160f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            var restart = CreateButton("Restart", resultPanel.transform, "YENİDEN DENE", new Vector2(320f, 82f), RestartLevel);
            SetRect(restart.GetComponent<RectTransform>(), new Vector2(-160f, -165f), new Vector2(320f, 82f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            resultPanel.SetActive(false);
        }

        private void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();
        }

        public void StartNextWave()
        {
            if (Phase != GamePhase.Preparing && Phase != GamePhase.Intermission) return;
            if (currentWaveIndex >= waves.Length || spawning) return;
            SaveWaveCheckpoint();
            StartCoroutine(SpawnWave(waves[currentWaveIndex]));
        }

        private IEnumerator SpawnWave(WaveDefinition wave)
        {
            SetPhase(GamePhase.Wave);
            spawning = true;
            plannedThisWave = wave.groups?.Sum(group => Mathf.Max(0, group.count)) ?? 0;
            removedThisWave = 0;
            RefreshThreatPreviewForPhase();
            EmitPresentation(PresentationCue.WaveStarted, path[0], value: currentWaveIndex);

            if (plannedThisWave <= 0)
            {
                Debug.LogError($"Wave '{wave.id}' contains no enemies.");
                spawning = false;
                CompleteWave();
                yield break;
            }

            ScheduleWave(wave);
            yield break;
        }

        private void SpawnEnemy(EnemyArchetype archetype)
        {
            if (path.Count < 2)
            {
                Debug.LogError("Cannot spawn enemy: level path is invalid.");
                removedThisWave++;
                return;
            }

            EnemyStats stats = GetScaledStats(archetype);
            float size = archetype == EnemyArchetype.Boss ? 1.7f : archetype == EnemyArchetype.Siege ? 1.25f : 0.85f;
            GameObject go;
            if (presentationCatalog != null)
            {
                PresentationActorKind actorKind = ActorKind(archetype);
                go = StylizedPresentationFactory.CreateRoot(archetype.ToString(), path[0],
                    new Vector3(size, Mathf.Max(1.4f, size * 1.7f), size), presentationCatalog.ActorPrefab(actorKind),
                    presentationCatalog, PresentationCollider.Capsule, presentationCatalog.ActorScale(actorKind),
                    presentationCatalog.ActorAnimator(actorKind), actorKind);
                go.transform.SetParent(transform, true);
            }
            else
            {
                var type = archetype == EnemyArchetype.Boss ? PrimitiveType.Capsule : PrimitiveType.Sphere;
                go = CreatePrimitive(archetype.ToString(), type, path[0] + Vector3.up * (size * 0.6f),
                    new Vector3(size, size, size), stats.color, transform, true);
            }
            var enemy = go.AddComponent<GreyboxEnemy>();
            enemy.Configure(this, archetype, stats, path);
            enemies.Add(enemy);
            PeakEnemyCount = Mathf.Max(PeakEnemyCount, enemies.Count);
            EmitPresentation(PresentationCue.UnitSpawned, go.transform.position, go, ActorKind(archetype));
        }

        internal void EnemyRemoved(GreyboxEnemy enemy, bool defeated)
        {
            if (enemy == null) return;
            enemies.Remove(enemy);
            removedThisWave++;
            if (defeated)
            {
                EarnGold(enemy.Reward);
                EmitPresentation(PresentationCue.EnemyDefeated, enemy.Position, enemy.gameObject,
                    ActorKind(enemy.Archetype));
            }
            TryCompleteWave();
        }

        internal void EnemyReachedKeep(GreyboxEnemy enemy, int damage)
        {
            telemetry.Record(this,"leak",enemy.Archetype.ToString(),damage);
            ShowMessage(GameLocalization.Text("Kaleye sızma: ","Keep breached: ") + FirstRegionCampaign.EnemyName(enemy.Archetype),2);
            EmitPresentation(PresentationCue.UiError,path[^1],value:damage);
            leakedLives[(int)enemy.Archetype] += Mathf.Max(1, damage);
            LoseLives(Mathf.Max(1, damage));
            EnemyRemoved(enemy, false);
        }

        private void TryCompleteWave()
        {
            enemies.RemoveAll(item => item == null);
            if (spawning || enemies.Count > 0 || Phase != GamePhase.Wave) return;
            CompleteWave();
        }

        private void CompleteWave()
        {
            if (currentWaveIndex >= waves.Length) return;
            WaveDefinition completed = waves[currentWaveIndex];
            telemetry.Record(this,"wave_result",completed.id); telemetry.Flush();
            if (waveElapsed > 0) Debug.Log($"[WAVE_BALANCE] level={CampaignLevelNumber} wave={currentWaveIndex+1} seconds={waveElapsed:F2} gold={Gold} lives={Lives} peak={PeakEnemyCount}");
            EarnGold(completed.clearBonus);
            currentWaveIndex++;

            if (currentWaveIndex >= waves.Length)
            {
                SetPhase(GamePhase.Victory);
                if (resultPanel != null)
                {
                    resultPanel.SetActive(true);
                    resultTitle.text = "ZAFER\nGREENWATCH KORUNDU";
                    resultTitle.color = new Color(0.45f, 1f, 0.58f);
                }
                artHud?.ShowResult(true);
                EmitPresentation(PresentationCue.Victory, path[^1]);
                Time.timeScale = 0f;
                return;
            }

            intermissionRemaining = IntermissionSeconds;
            SetPhase(GamePhase.Intermission);
            RefreshThreatPreviewForPhase();
            if (currentWaveIndex == 1)
                ShowMessage("Kale muhafızı son savunma hattını otomatik korur.", 5f);
            else if (currentWaveIndex == 2)
                ShowMessage("Kalkan Darbesi kalabalıkları kısa süre durdurur.", 5f);
        }

        private void SetPhase(GamePhase next)
        {
            if (next == GamePhase.Victory && campaignStore != null)
                CompletionReward = campaignStore.CompleteLevel(CampaignLevelNumber, BattleRules.StarsForLives(Lives,StartingLivesForLevel), Lives);
            else if (next is GamePhase.Victory or GamePhase.Defeat) ClearCheckpoint();
            Phase = next;
            PhaseChanged?.Invoke(next);
            if (nextWaveButton != null)
                nextWaveButton.interactable = next == GamePhase.Preparing || next == GamePhase.Intermission;
        }

        private void OnPausePressed()
        {
            if (Phase == GamePhase.Victory || Phase == GamePhase.Defeat) return;
            if (Phase == GamePhase.Paused)
            {
                Time.timeScale = currentGameSpeed;
                SetPhase(phaseBeforePause);
                if (pauseButtonLabel != null) pauseButtonLabel.text = "DURAKLAT";
            }
            else
            {
                phaseBeforePause = Phase;
                SetPhase(GamePhase.Paused);
                Time.timeScale = 0f;
                if (pauseButtonLabel != null) pauseButtonLabel.text = "DEVAM";
            }
        }

        public void TogglePause() => OnPausePressed();

        private void ReturnToMainMenu()
        {
            if (Phase != GamePhase.Paused || returnToMainMenu == null) return;
            Time.timeScale = 0f;
            returnToMainMenu.Invoke();
        }

        private void ToggleGameSpeed()
        {
            speedStepIndex = (speedStepIndex + 1) % SpeedSteps.Length;
            currentGameSpeed = SpeedForStep(speedStepIndex);
            if (Phase != GamePhase.Paused && Phase != GamePhase.Victory && Phase != GamePhase.Defeat)
                Time.timeScale = currentGameSpeed;
            RefreshUi();
        }

        private void RestartLevel()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void TryBuildSelected(OutpostKind kind)
        {
            if (selectedSlot == null)
            {
                ShowMessage("Önce mavi bir karakol platformu seç.", 2.5f);
                EmitPresentation(PresentationCue.UiError, Vector3.zero);
                return;
            }
            if (selectedSlot.Occupant != null)
            {
                SelectOutpost(selectedSlot.Occupant);
                return;
            }

            if (!TryBuildOutpost(selectedSlot.Index, kind))
            {
                ShowMessage("Yeterli altın yok.", 2f);
                EmitPresentation(PresentationCue.UiError, Vector3.zero);
                return;
            }

            ShowMessage(GameLocalization.Text($"{OutpostDisplayName(kind)} kuruldu.", $"{OutpostDisplayName(kind)} built."), 2f);
        }

        public bool TryBuildOutpost(int slotIndex, OutpostKind kind)
        {
            if (!CanChangeDefence || !CanBuild(kind) || slotIndex < 0 || slotIndex >= slots.Count) return false;
            GreyboxOutpostSlot targetSlot = slots[slotIndex];
            if (targetSlot == null || targetSlot.Occupant != null) return false;

            OutpostStats stats = GetOutpostStats(kind);
            if (!SpendGold(stats.cost)) return false;

            SelectSlot(targetSlot);

            Color color = kind switch
            {
                OutpostKind.Archer => new Color(0.92f, 0.65f, 0.22f),
                OutpostKind.Barracks => new Color(0.64f, 0.70f, 0.76f),
                _ => new Color(0.62f, 0.28f, 0.95f)
            };
            GameObject go;
            if (presentationCatalog != null)
            {
                go = StylizedPresentationFactory.CreateRoot($"{kind} Outpost", targetSlot.transform.position,
                    new Vector3(1.15f, 1.55f, 1.15f), presentationCatalog.OutpostPrefab(kind, 1),
                    presentationCatalog, PresentationCollider.Cylinder, presentationCatalog.OutpostScale(kind, 1));
                go.transform.SetParent(transform, true);
            }
            else
            {
                var primitive = kind == OutpostKind.Barracks ? PrimitiveType.Cube : PrimitiveType.Cylinder;
                go = CreatePrimitive($"{kind} Outpost", primitive, targetSlot.transform.position + Vector3.up * 0.75f,
                    new Vector3(1.1f, 1.35f, 1.1f), color, transform, true);
                BuildOutpostSilhouette(kind, go.transform);
            }
            var outpost = go.AddComponent<GreyboxOutpost>();
            SetLayerRecursively(go, LayerMask.NameToLayer(SelectableLayerName));
            outpost.Configure(this, targetSlot, kind, stats);
            targetSlot.SetOccupant(outpost);
            outposts.Add(outpost);
            telemetry.Record(this, firstTowerRecorded ? "build" : "first_tower", kind.ToString(), stats.cost);
            firstTowerRecorded = true;
            SelectOutpost(outpost);
            EmitPresentation(PresentationCue.OutpostBuilt, go.transform.position, go, outpost: kind);
            return true;
        }

        private void BuildOutpostSilhouette(OutpostKind kind, Transform root)
        {
            switch (kind)
            {
                case OutpostKind.Archer:
                    CreatePrimitiveLocal("Archer Roof", PrimitiveType.Cube, root,
                        new Vector3(0f, 0.62f, 0f), new Vector3(0.95f, 0.18f, 0.95f),
                        new Color(0.50f, 0.28f, 0.12f));
                    CreatePrimitiveLocal("Archer Bow", PrimitiveType.Cube, root,
                        new Vector3(0.45f, 0.12f, 0f), new Vector3(0.10f, 0.52f, 0.12f),
                        new Color(0.95f, 0.85f, 0.42f));
                    break;
                case OutpostKind.Barracks:
                    CreatePrimitiveLocal("Barracks Door", PrimitiveType.Cube, root,
                        new Vector3(0f, -0.14f, -0.51f), new Vector3(0.34f, 0.42f, 0.08f),
                        new Color(0.20f, 0.20f, 0.22f));
                    CreatePrimitiveLocal("Barracks Flag", PrimitiveType.Cube, root,
                        new Vector3(0.42f, 0.52f, 0f), new Vector3(0.10f, 0.42f, 0.46f),
                        new Color(0.72f, 0.20f, 0.22f));
                    break;
                case OutpostKind.Mage:
                    CreatePrimitiveLocal("Mage Crystal", PrimitiveType.Sphere, root,
                        new Vector3(0f, 0.62f, 0f), new Vector3(0.46f, 0.46f, 0.46f),
                        new Color(0.35f, 0.80f, 1f));
                    CreatePrimitiveLocal("Mage Base Ring", PrimitiveType.Cylinder, root,
                        new Vector3(0f, -0.4f, 0f), new Vector3(0.82f, 0.10f, 0.82f),
                        new Color(0.28f, 0.18f, 0.38f));
                    break;
            }
        }

        private void UpgradeSelected()
        {
            if (selectedOutpost == null) return;
            if (selectedOutpost.CanSpecialize && artHud != null)
            { artHud.ShowSpecializationChoices(selectedOutpost, this); return; }
            if (!TryUpgradeOutpost(selectedOutpost.Slot.Index))
            {
                ShowMessage(selectedOutpost.CanUpgrade
                    ? GameLocalization.Text("Yükseltme için yeterli altın yok.", "Not enough gold to upgrade.")
                    : GameLocalization.Text("En yüksek seviyeye ulaşıldı.", "Maximum level reached."), 2f);
                return;
            }
            RefreshSelectedPanel();
        }

        public bool TryUpgradeOutpost(int slotIndex)
        {
            if (!CanChangeDefence || slotIndex < 0 || slotIndex >= slots.Count) return false;
            GreyboxOutpost outpost = slots[slotIndex]?.Occupant;
            if (outpost == null || !outpost.CanUpgrade || !SpendGold(outpost.UpgradeCost)) return false;
            outpost.Upgrade();
            telemetry.Record(this,"upgrade",outpost.Kind.ToString(),outpost.UpgradeCost);
            ReplaceOutpostVisual(outpost);
            EmitPresentation(PresentationCue.OutpostUpgraded, outpost.Position, outpost.gameObject, outpost: outpost.Kind);
            return true;
        }

        private void SellSelected()
        {
            if (selectedOutpost == null) return;
            var slot = selectedOutpost.Slot;
            if (!TrySellOutpost(slot.Index)) return;
            selectedOutpost = null;
            selectedSlot = slot;
            RefreshSelectedPanel();
        }

        public bool TrySellOutpost(int slotIndex)
        {
            if (!CanChangeDefence || slotIndex < 0 || slotIndex >= slots.Count) return false;
            GreyboxOutpost outpost = slots[slotIndex]?.Occupant;
            if (outpost == null) return false;
            int refund = outpost.SellValue;
            telemetry.Record(this,"sell",outpost.Kind.ToString(),refund);
            outposts.Remove(outpost);
            EmitPresentation(PresentationCue.OutpostSold, outpost.Position, outpost.gameObject, outpost: outpost.Kind);
            outpost.Sell();
            EarnGold(refund);
            if (selectedOutpost == outpost) selectedOutpost = null;
            return true;
        }

        private void UseHeroAbility()
        {
            if (hero == null || !hero.IsAlive)
            {
                ShowMessage("Şövalye henüz savaşa dönemedi.", 2f);
                return;
            }
            if (!hero.TryUseShieldBash())
                ShowMessage(hero.AbilityRemaining > 0 ? GameLocalization.Text($"Yetenek {Mathf.CeilToInt(hero.AbilityRemaining)} sn sonra hazır.", $"Ability ready in {Mathf.CeilToInt(hero.AbilityRemaining)} sec.") : GameLocalization.Text("Menzilde düşman yok · Yetenek harcanmadı.", "No enemy in range · Ability not spent."), 2f);
        }

        private void CancelSelection()
        {
            if (selectedSlot != null) selectedSlot.SetSelected(false);
            selectedSlot = null;
            selectedOutpost = null;
            if (selectionRange != null) selectionRange.Show(Vector3.zero, 1, false);
            RefreshSelectedPanel();
        }

        private IEnumerator RunAutomatedSmoke()
        {
            Debug.Log("[DC_SMOKE] Functional iOS smoke run started.");
            yield return null;

            EarnGold(2200);
            OutpostKind[] layout =
            {
                OutpostKind.Archer, OutpostKind.Barracks, OutpostKind.Mage, OutpostKind.Archer,
                OutpostKind.Mage, OutpostKind.Barracks, OutpostKind.Archer, OutpostKind.Mage
            };

            for (int i = 0; i < layout.Length; i++)
            {
                if (!TryBuildOutpost(i, layout[i]))
                {
                    Debug.LogError($"[DC_SMOKE_FAIL] Could not build outpost at slot {i}.");
                    yield return new WaitForSecondsRealtime(0.5f);
                    Application.Quit(2);
                    yield break;
                }
                TryUpgradeOutpost(i);
            }

            hero.TryUseShieldBash();
            TogglePause();
            yield return null;
            TogglePause();
            Time.timeScale = 12f;
            StartNextWave();

            float deadline = Time.realtimeSinceStartup + 150f;
            while (Phase != GamePhase.Victory && Phase != GamePhase.Defeat && Time.realtimeSinceStartup < deadline)
            {
                if (Phase == GamePhase.Intermission) StartNextWave();
                if (hero != null && hero.IsAlive && hero.AbilityRemaining <= 0f) hero.TryUseShieldBash();
                yield return null;
            }

            Time.timeScale = 1f;
            bool passed = Phase == GamePhase.Victory;
            if (passed)
                Debug.Log("[DC_SMOKE_PASS] Build, upgrade, hero, pause and all seven waves completed with victory.");
            else
                Debug.LogError($"[DC_SMOKE_FAIL] Functional run ended in phase {Phase}.");
            yield return new WaitForSecondsRealtime(0.5f);
            Application.Quit(passed ? 0 : 2);
        }

        private IEnumerator RunAutomatedPerformanceAcceptance()
        {
            Debug.Log("[DC_PERFORMANCE_ACCEPTANCE] VIS-807 scenario run started.");
            yield return new WaitForSecondsRealtime(0.5f);

            var probe = GetComponent<DeviceAcceptanceProbe>();
            if (probe == null)
            {
                Debug.LogError("[DC_PERFORMANCE_ACCEPTANCE_FAIL] device_probe_missing=true");
                yield break;
            }

            probe.MarkScenario("preparation");
            yield return new WaitForSecondsRealtime(6f);
            probe.LogCheckpoint("preparation");

            EarnGold(2600);
            OutpostKind[] layout =
            {
                OutpostKind.Archer, OutpostKind.Barracks, OutpostKind.Mage, OutpostKind.Archer,
                OutpostKind.Mage, OutpostKind.Barracks, OutpostKind.Archer, OutpostKind.Mage
            };

            for (int i = 0; i < layout.Length; i++)
            {
                if (!TryBuildOutpost(i, layout[i]) || !TryUpgradeOutpost(i))
                {
                    Debug.LogError($"[DC_PERFORMANCE_ACCEPTANCE_FAIL] setup_slot={i + 1} kind={layout[i]}");
                    yield break;
                }
            }

            Time.timeScale = 4f;
            probe.MarkScenario("normal");
            SetPhase(GamePhase.Wave);
            SpawnPerformanceMix(10, 3, 0, 0);
            yield return RunPerformanceSample(8f);
            probe.LogCheckpoint("normal");

            probe.MarkScenario("dense");
            ClearPerformanceEnemies();
            SpawnPerformanceMix(24, 10, 5, 3);
            yield return RunPerformanceSample(10f);
            probe.LogCheckpoint("dense");

            probe.MarkScenario("boss");
            ClearPerformanceEnemies();
            SpawnPerformanceMix(8, 5, 2, 2);
            SpawnEnemy(EnemyArchetype.Boss);
            EmitPresentation(PresentationCue.BossPhase, path[0], actor: PresentationActorKind.Boss, value: 1);
            yield return RunPerformanceSample(12f);
            probe.LogCheckpoint("boss");

            Time.timeScale = 1f;
            if (probe.MeetsPerformanceGate)
            {
                Debug.Log($"[DC_PERFORMANCE_ACCEPTANCE_PASS] scenarios=preparation/normal/dense/boss " +
                          $"median_fps={probe.MedianFps:F1} p95_frame_ms={probe.P95FrameMilliseconds:F1}");
            }
            else
            {
                Debug.LogError($"[DC_PERFORMANCE_ACCEPTANCE_FAIL] scenarios=preparation/normal/dense/boss " +
                               $"median_fps={probe.MedianFps:F1} p95_frame_ms={probe.P95FrameMilliseconds:F1} " +
                               $"low_memory_warning={probe.HasLowMemoryWarning}");
            }
            yield return new WaitForSecondsRealtime(1f);
            Application.Quit(probe.MeetsPerformanceGate ? 0 : 2);
        }

        private IEnumerator RunAutomatedSoakAcceptance()
        {
            Debug.Log("[DC_SOAK_ACCEPTANCE] 90-second dense combat run started.");
            yield return new WaitForSecondsRealtime(0.5f);

            var probe = GetComponent<DeviceAcceptanceProbe>();
            if (probe == null)
            {
                Debug.LogError("[DC_SOAK_ACCEPTANCE_FAIL] device_probe_missing=true");
                yield break;
            }

            BuildVis812DefenseLayout();
            Time.timeScale = 8f;
            SetPhase(GamePhase.Wave);
            float startedAt = Time.realtimeSinceStartup;
            float warmupEndsAt = startedAt + 30f;
            float deadline = startedAt + 90f;
            long startingMemory = 0;
            float nextCycle = Time.realtimeSinceStartup;
            int cycles = 0;

            while (Time.realtimeSinceStartup < deadline)
            {
                if (Time.realtimeSinceStartup >= nextCycle)
                {
                    ClearPerformanceEnemies();
                    SpawnPerformanceMix(24, 10, 5, 3);
                    if (cycles % 3 == 2) SpawnEnemy(EnemyArchetype.Boss);
                    cycles++;
                    probe.MarkScenario($"soak-{cycles:00}");
                    probe.LogCheckpoint($"soak-{cycles:00}");
                    nextCycle += 10f;
                }
                if (startingMemory == 0 && Time.realtimeSinceStartup >= warmupEndsAt)
                {
                    startingMemory = Profiler.GetTotalAllocatedMemoryLong();
                    Debug.Log($"[DC_SOAK_WARMUP_COMPLETE] memory_mb={startingMemory / (1024f * 1024f):F1}");
                }
                if (hero != null && hero.IsAlive && hero.AbilityRemaining <= 0f)
                    hero.TryUseShieldBash();
                yield return null;
            }

            long endingMemory = Profiler.GetTotalAllocatedMemoryLong();
            if (startingMemory == 0) startingMemory = endingMemory;
            float growthPercent = startingMemory <= 0
                ? 0f
                : (endingMemory - startingMemory) * 100f / startingMemory;
            probe.LogCheckpoint("soak-final");
            Time.timeScale = 1f;

            bool passed = growthPercent <= 20f && !probe.HasLowMemoryWarning && Phase != GamePhase.Defeat;
            if (passed)
            {
                Debug.Log($"[DC_SOAK_ACCEPTANCE_PASS] cycles={cycles} duration_seconds=90 " +
                          $"memory_growth_pct={growthPercent:F1}");
            }
            else
            {
                Debug.LogError($"[DC_SOAK_ACCEPTANCE_FAIL] cycles={cycles} memory_growth_pct={growthPercent:F1} " +
                               $"low_memory_warning={probe.HasLowMemoryWarning} phase={Phase}");
            }
            yield return new WaitForSecondsRealtime(1f);
            Application.Quit();
        }

        private IEnumerator RunAutomatedAudioVisualAcceptance()
        {
            int durationSeconds = 480;
            if (int.TryParse(Environment.GetEnvironmentVariable("DC_AUDIO_ACCEPTANCE_SECONDS"), out int requestedDuration))
                durationSeconds = Mathf.Clamp(requestedDuration, 30, 720);

            Debug.Log($"[DC_AUDIO_VISUAL_ACCEPTANCE] duration_seconds={durationSeconds} " +
                      "levels=Silent/Low/Normal cues=22 music_states=6");
            yield return new WaitForSecondsRealtime(0.5f);

            DefenderAudioController audio = presentationDirector != null ? presentationDirector.Audio : null;
            if (audio == null)
            {
                Debug.LogError("[DC_AUDIO_VISUAL_ACCEPTANCE_FAIL] audio_controller_missing=true");
                Application.Quit(2);
                yield break;
            }

            BuildVis812DefenseLayout();
            PresentationCue[] cues = (PresentationCue[])Enum.GetValues(typeof(PresentationCue));
            MusicState[] musicStates = (MusicState[])Enum.GetValues(typeof(MusicState));
            PresentationAudioReviewLevel[] reviewLevels =
            {
                PresentationAudioReviewLevel.Silent,
                PresentationAudioReviewLevel.Low,
                PresentationAudioReviewLevel.Normal
            };
            var seenCues = new HashSet<PresentationCue>();
            var seenMusicStates = new HashSet<MusicState>();
            var seenReviewLevels = new HashSet<PresentationAudioReviewLevel>();
            float startedAt = Time.realtimeSinceStartup;
            float nextCombatCycle = startedAt;
            float levelWindow = durationSeconds / (float)reviewLevels.Length;
            int activeLevelIndex = -1;
            int cueIndex = 0;
            int musicIndex = 0;
            int combatCycles = 0;
            int maxActiveVoices = 0;
            bool pauseSnapshotChecked = false;
            Time.timeScale = 1f;
            SetPhase(GamePhase.Wave);

            while (Time.realtimeSinceStartup - startedAt < durationSeconds)
            {
                float elapsed = Time.realtimeSinceStartup - startedAt;
                if (Time.realtimeSinceStartup >= nextCombatCycle)
                {
                    ClearPerformanceEnemies();
                    SpawnPerformanceMix(10, 4, 1, 1);
                    if (combatCycles % 4 == 3) SpawnEnemy(EnemyArchetype.Boss);
                    combatCycles++;
                    nextCombatCycle += 12f;
                    Debug.Log($"[DC_AUDIO_COMBAT_CYCLE] cycle={combatCycles} enemies={enemies.Count} " +
                              $"boss={(combatCycles % 4 == 0)} elapsed_seconds={elapsed:F1}");
                }
                int desiredLevelIndex = Mathf.Min(reviewLevels.Length - 1,
                    Mathf.FloorToInt(elapsed / Mathf.Max(1f, levelWindow)));
                if (desiredLevelIndex != activeLevelIndex)
                {
                    activeLevelIndex = desiredLevelIndex;
                    PresentationAudioReviewLevel level = reviewLevels[activeLevelIndex];
                    audio.SetReviewLevel(level);
                    seenReviewLevels.Add(level);
                    Debug.Log($"[DC_AUDIO_REVIEW_LEVEL] level={level} elapsed_seconds={elapsed:F1}");
                }

                if (cueIndex % 5 == 0)
                {
                    MusicState state = musicStates[musicIndex % musicStates.Length];
                    musicIndex++;
                    audio.SetMusic(state);
                    seenMusicStates.Add(state);
                    Debug.Log($"[DC_AUDIO_MUSIC_STATE] state={state} elapsed_seconds={elapsed:F1}");
                }

                PresentationCue cue = cues[cueIndex % cues.Length];
                cueIndex++;
                PresentationActorKind actor = AudioAcceptanceActor(cue);
                OutpostKind outpost = AudioAcceptanceOutpost(cue);
                Vector3 position = AudioAcceptancePosition(cue);
                GameObject source = cue is PresentationCue.HeroMoved or PresentationCue.HeroAttacked or
                    PresentationCue.HeroAbility
                    ? hero != null ? hero.gameObject : null
                    : null;
                EmitPresentation(cue, position, source, actor, outpost, (cueIndex % 3) + 1);
                seenCues.Add(cue);
                PresentationAudioCueSpec spec = PresentationAudioPolicy.Describe(
                    new PresentationSignal(cue, position, source, actor, outpost));
                Debug.Log($"[DC_AUDIO_CUE] cue={cue} group={spec.concurrencyGroup} " +
                          $"priority={spec.priority} delay={spec.delaySeconds:F2} review={audio.ReviewLevel}");

                if (!pauseSnapshotChecked && elapsed >= durationSeconds * 0.45f)
                {
                    audio.SetPaused(true);
                    yield return new WaitForSecondsRealtime(0.25f);
                    audio.SetPaused(false);
                    pauseSnapshotChecked = true;
                    Debug.Log("[DC_AUDIO_SNAPSHOT_PASS] normal_pause_normal=true");
                }

                yield return new WaitForSecondsRealtime(0.65f);
                maxActiveVoices = Mathf.Max(maxActiveVoices, audio.ActiveVoiceCount);
            }

            audio.SetReviewLevel(PresentationAudioReviewLevel.Normal);
            ClearPerformanceEnemies();
            Time.timeScale = 1f;
            int expectedCombatCycles = Mathf.Max(1, Mathf.FloorToInt((durationSeconds - 0.01f) / 12f) + 1);
            bool ambientEnabled = audio.Ambient != null && audio.Ambient.IsEnabled;
            int ambientDetails = audio.Ambient != null ? audio.Ambient.PlayedDetailCount : 0;
            int expectedAmbientDetails = ambientEnabled ? Mathf.Max(1, durationSeconds / 60) : 0;
            bool passed = seenCues.Count == cues.Length &&
                          seenMusicStates.Count == musicStates.Length &&
                          seenReviewLevels.Count == reviewLevels.Length &&
                          pauseSnapshotChecked && maxActiveVoices <= 10 &&
                          combatCycles >= expectedCombatCycles && ambientDetails >= expectedAmbientDetails;
            string result = $"duration_seconds={durationSeconds} cues={seenCues.Count}/{cues.Length} " +
                            $"music_states={seenMusicStates.Count}/{musicStates.Length} " +
                            $"levels={seenReviewLevels.Count}/{reviewLevels.Length} " +
                            $"combat_cycles={combatCycles}/{expectedCombatCycles} " +
                            $"ambient_enabled={ambientEnabled} ambient_details={ambientDetails}/{expectedAmbientDetails} " +
                            $"pause_snapshot={pauseSnapshotChecked} max_active_voices={maxActiveVoices}/10";
            if (passed)
                Debug.Log($"[DC_AUDIO_VISUAL_ACCEPTANCE_PASS] {result}");
            else
                Debug.LogError($"[DC_AUDIO_VISUAL_ACCEPTANCE_FAIL] {result}");

            yield return new WaitForSecondsRealtime(1f);
            Application.Quit(passed ? 0 : 2);
        }

        private PresentationActorKind AudioAcceptanceActor(PresentationCue cue) => cue switch
        {
            PresentationCue.HeroMoved or PresentationCue.HeroAttacked or PresentationCue.HeroAbility =>
                PresentationActorKind.Knight,
            PresentationCue.HealPulse => PresentationActorKind.Support,
            PresentationCue.OutpostDisrupted => PresentationActorKind.Siege,
            PresentationCue.BossPhase or PresentationCue.BossAttack => PresentationActorKind.Boss,
            PresentationCue.UnitSpawned or PresentationCue.UnitDefeated => PresentationActorKind.Guard,
            PresentationCue.EnemyHit or PresentationCue.EnemyDefeated or PresentationCue.SlowApplied =>
                PresentationActorKind.Raider,
            _ => PresentationActorKind.Vanguard
        };

        private OutpostKind AudioAcceptanceOutpost(PresentationCue cue) => cue switch
        {
            PresentationCue.MageImpact => OutpostKind.Mage,
            PresentationCue.OutpostAttacked => OutpostKind.Archer,
            _ => OutpostKind.Barracks
        };

        private Vector3 AudioAcceptancePosition(PresentationCue cue)
        {
            if (cue is PresentationCue.HeroMoved or PresentationCue.HeroAttacked or PresentationCue.HeroAbility)
                return hero != null ? hero.Position : path[^1];
            if (cue is PresentationCue.Victory or PresentationCue.Defeat)
                return path[^1];
            int index = Mathf.Abs((int)cue) % path.Count;
            return path[index];
        }

        private IEnumerator RunAutomatedVis812VisualAcceptance()
        {
            Debug.Log("[DC_VIS812_CAPTURE] final visual evidence run started.");
            yield return new WaitForSecondsRealtime(0.5f);

            yield return HoldVis812State("preparation", 3f);

            BuildVis812DefenseLayout();
            if (hero != null && hero.IsAlive)
                hero.TryUseShieldBash();
            EmitPresentation(PresentationCue.HeroAbility, hero != null ? hero.Position : path[^1],
                hero != null ? hero.gameObject : null, PresentationActorKind.Knight);
            yield return HoldVis812State("ability", 3f);

            Time.timeScale = 1f;
            SetPhase(GamePhase.Wave);
            EmitPresentation(PresentationCue.WaveStarted, path[0], value: currentWaveIndex);
            SpawnPerformanceMix(6, 2, 0, 0);
            yield return HoldVis812State("normal", 4f);

            ClearPerformanceEnemies();
            SetPhase(GamePhase.Wave);
            SpawnPerformanceMix(22, 8, 4, 3);
            EmitPresentation(PresentationCue.SlowApplied, path[Mathf.Min(2, path.Count - 1)]);
            yield return HoldVis812State("dense", 4f);

            ClearPerformanceEnemies();
            SetPhase(GamePhase.Wave);
            SpawnPerformanceMix(6, 3, 1, 1);
            SpawnEnemy(EnemyArchetype.Boss);
            for (int phase = 1; phase <= 3; phase++)
            {
                EmitPresentation(PresentationCue.BossPhase, path[0], actor: PresentationActorKind.Boss, value: phase);
                EmitPresentation(PresentationCue.BossAttack, path[Mathf.Min(1, path.Count - 1)], actor: PresentationActorKind.Boss, value: phase);
                yield return HoldVis812State($"boss{phase}", 3f);
            }

            ClearPerformanceEnemies();
            while (Phase != GamePhase.Victory && currentWaveIndex < WaveCount)
                CompleteWave();
            yield return HoldVis812State("victory", 12f);

            Time.timeScale = 1f;
            Debug.Log("[DC_VIS812_CAPTURE_PASS] states=preparation/ability/normal/dense/boss1/boss2/boss3/victory");
            yield return new WaitForSecondsRealtime(6f);
            Application.Quit();
        }

        private IEnumerator RunAutomatedVis812DefeatAcceptance()
        {
            Debug.Log("[DC_VIS812_CAPTURE_DEFEAT] final visual evidence defeat run started.");
            yield return new WaitForSecondsRealtime(0.5f);

            BuildVis812DefenseLayout();
            LoseLives(StartingLives);
            yield return HoldVis812State("defeat", 4f);

            Time.timeScale = 1f;
            Debug.Log("[DC_VIS812_CAPTURE_DEFEAT_PASS] state=defeat");
            yield return new WaitForSecondsRealtime(3f);
            Application.Quit();
        }

        private void BuildVis812DefenseLayout()
        {
            EarnGold(2600);
            OutpostKind[] layout =
            {
                OutpostKind.Archer, OutpostKind.Barracks, OutpostKind.Mage, OutpostKind.Archer,
                OutpostKind.Mage, OutpostKind.Barracks, OutpostKind.Archer, OutpostKind.Mage
            };

            for (int i = 0; i < layout.Length; i++)
            {
                if (slots.ElementAtOrDefault(i)?.Occupant == null)
                    TryBuildOutpost(i, layout[i]);
                TryUpgradeOutpost(i);
            }
        }

        private IEnumerator HoldVis812State(string state, float seconds)
        {
            RefreshUi();
            yield return null;
            RefreshUi();
            Canvas.ForceUpdateCanvases();
            yield return null;
            yield return null;
            Canvas.ForceUpdateCanvases();
            Debug.Log($"[DC_VIS812_STATE] state={state} phase={Phase} wave={currentWaveIndex}/{WaveCount} " +
                      $"gold={Gold} lives={Lives} enemies={enemies.Count}");

            float deadline = Time.realtimeSinceStartup + seconds;
            float heartbeat = Time.realtimeSinceStartup + 0.75f;
            while (Time.realtimeSinceStartup < deadline)
            {
                if (Time.realtimeSinceStartup >= heartbeat)
                {
                    Debug.Log($"[DC_VIS812_HEARTBEAT] state={state} phase={Phase}");
                    heartbeat = Time.realtimeSinceStartup + 0.75f;
                }
                yield return null;
            }
        }

        private IEnumerator RunPerformanceSample(float seconds)
        {
            float deadline = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < deadline)
            {
                if (hero != null && hero.IsAlive && hero.AbilityRemaining <= 0f) hero.TryUseShieldBash();
                yield return null;
            }
        }

        private void SpawnPerformanceMix(int raiders, int vanguards, int regenerators, int supports)
        {
            for (int i = 0; i < raiders; i++) SpawnEnemy(EnemyArchetype.Raider);
            for (int i = 0; i < vanguards; i++) SpawnEnemy(EnemyArchetype.Vanguard);
            for (int i = 0; i < regenerators; i++) SpawnEnemy(EnemyArchetype.Regenerator);
            for (int i = 0; i < supports; i++) SpawnEnemy(EnemyArchetype.Support);
        }

        private void ClearPerformanceEnemies()
        {
            foreach (GreyboxEnemy enemy in enemies.ToArray())
                if (enemy != null)
                    Destroy(enemy.gameObject);
            enemies.Clear();
            spawning = false;
        }

        private IEnumerator RunAutomatedSlotRegressionSmoke()
        {
            Debug.Log("[DC_SLOT_REGRESSION] Slot regression smoke started.");
            yield return null;

            EarnGold(2200);
            OutpostKind[] layout =
            {
                OutpostKind.Archer, OutpostKind.Barracks, OutpostKind.Mage, OutpostKind.Archer,
                OutpostKind.Mage, OutpostKind.Barracks, OutpostKind.Archer, OutpostKind.Mage
            };

            for (int i = 0; i < layout.Length; i++)
            {
                OutpostKind kind = layout[i];
                if (!TryBuildOutpost(i, kind))
                {
                    Debug.LogError($"[DC_SLOT_REGRESSION_FAIL] slot={i + 1} action=build kind={kind}");
                    Application.Quit();
                    yield break;
                }

                GreyboxOutpost built = slots[i]?.Occupant;
                if (built == null || built.Kind != kind || built.Level != 1)
                {
                    Debug.LogError($"[DC_SLOT_REGRESSION_FAIL] slot={i + 1} action=build-verify expectedKind={kind} actualKind={built?.Kind} actualLevel={built?.Level.ToString() ?? "null"}");
                    Application.Quit();
                    yield break;
                }

                if (!TryUpgradeOutpost(i))
                {
                    Debug.LogError($"[DC_SLOT_REGRESSION_FAIL] slot={i + 1} action=upgrade kind={kind}");
                    Application.Quit();
                    yield break;
                }

                GreyboxOutpost upgraded = slots[i]?.Occupant;
                if (upgraded == null || upgraded.Kind != kind || upgraded.Level != 2 || upgraded.CanUpgrade)
                {
                    Debug.LogError($"[DC_SLOT_REGRESSION_FAIL] slot={i + 1} action=upgrade-verify expectedKind={kind} actualKind={upgraded?.Kind} actualLevel={upgraded?.Level.ToString() ?? "null"} canUpgrade={upgraded?.CanUpgrade.ToString() ?? "null"}");
                    Application.Quit();
                    yield break;
                }

                if (!TrySellOutpost(i) || slots[i]?.Occupant != null)
                {
                    Debug.LogError($"[DC_SLOT_REGRESSION_FAIL] slot={i + 1} action=sell kind={kind}");
                    Application.Quit();
                    yield break;
                }

                if (!TryBuildOutpost(i, OutpostKind.Archer))
                {
                    Debug.LogError($"[DC_SLOT_REGRESSION_FAIL] slot={i + 1} action=rebuild kind=Archer");
                    Application.Quit();
                    yield break;
                }

                GreyboxOutpost rebuilt = slots[i]?.Occupant;
                if (rebuilt == null || rebuilt.Kind != OutpostKind.Archer || rebuilt.Level != 1)
                {
                    Debug.LogError($"[DC_SLOT_REGRESSION_FAIL] slot={i + 1} action=rebuild-verify actualKind={rebuilt?.Kind} actualLevel={rebuilt?.Level.ToString() ?? "null"}");
                    Application.Quit();
                    yield break;
                }

                if (!TrySellOutpost(i) || slots[i]?.Occupant != null)
                {
                    Debug.LogError($"[DC_SLOT_REGRESSION_FAIL] slot={i + 1} action=resell kind=Archer");
                    Application.Quit();
                    yield break;
                }

                Debug.Log($"[DC_SLOT_REGRESSION_PASS] slot={i + 1} primary={kind} upgradedLevel=2 reuse=Archer slotCount={SlotCount}");
                yield return null;
            }

            Debug.Log("[DC_SLOT_REGRESSION_COMPLETE] all_slots_passed=true");
            yield return null;
            Application.Quit();
        }

        private IEnumerator RunAutomatedAbilityCapture()
        {
            Debug.Log("[DC_ABILITY_CAPTURE] startup");
            yield return new WaitForSeconds(0.45f);

            if (hero == null || !hero.IsAlive)
            {
                Debug.LogError("[DC_ABILITY_CAPTURE_FAIL] hero_missing_or_downed");
                yield break;
            }

            bool triggered = hero.TryUseShieldBash();
            RefreshUi();
            yield return null;
            RefreshUi();
            Debug.Log(triggered
                ? $"[DC_ABILITY_CAPTURE_PASS] cooldown_remaining={hero.AbilityRemaining:0.0}"
                : $"[DC_ABILITY_CAPTURE_FAIL] cooldown_remaining={hero.AbilityRemaining:0.0}");
        }

        private IEnumerator RunAutomatedPauseCapture()
        {
            Debug.Log("[DC_PAUSE_CAPTURE] startup");
            yield return new WaitForSeconds(0.45f);

            if (Phase == GamePhase.Victory || Phase == GamePhase.Defeat)
            {
                Debug.LogError($"[DC_PAUSE_CAPTURE_FAIL] invalid_phase={Phase}");
                yield break;
            }

            OnPausePressed();
            RefreshUi();
            yield return null;
            RefreshUi();
            Debug.Log(Phase == GamePhase.Paused
                ? "[DC_PAUSE_CAPTURE_PASS] overlay_visible=true"
                : $"[DC_PAUSE_CAPTURE_FAIL] phase={Phase}");
        }

        private IEnumerator RunAutomatedActorCapture()
        {
            Debug.Log("[DC_ACTOR_CAPTURE] startup");
            yield return new WaitForSeconds(0.45f);

            bool sawKnightAttack = false;
            bool sawKnightAbility = false;
            bool sawGuardSpawn = false;
            bool sawGuardThrust = false;
            bool sawGuardBlock = false;
            bool sawRaiderSpawn = false;
            bool sawRaiderHit = false;
            bool sawRaiderDeath = false;
            bool sawVanguardSpawn = false;
            bool sawVanguardHeavy = false;
            bool sawRegeneratorSpawn = false;
            bool sawRegeneratorPulse = false;
            bool sawSupportSpawn = false;
            bool sawSupportHeal = false;
            bool sawSiegeSpawn = false;
            bool sawSiegeHeavy = false;
            void TrackActorSignal(PresentationSignal signal)
            {
                if (signal.actor == PresentationActorKind.Knight && signal.cue == PresentationCue.HeroAttacked)
                    sawKnightAttack = true;
                if (signal.actor == PresentationActorKind.Knight && signal.cue == PresentationCue.HeroAbility)
                    sawKnightAbility = true;
                if (signal.actor == PresentationActorKind.Guard && signal.cue == PresentationCue.UnitSpawned)
                    sawGuardSpawn = true;
                if (signal.actor == PresentationActorKind.Guard && signal.cue == PresentationCue.OutpostAttacked)
                    sawGuardThrust = true;
                if (signal.actor == PresentationActorKind.Guard && signal.cue == PresentationCue.HeroAbility)
                    sawGuardBlock = true;
                if (signal.actor == PresentationActorKind.Raider && signal.cue == PresentationCue.UnitSpawned)
                    sawRaiderSpawn = true;
                if (signal.actor == PresentationActorKind.Raider && signal.cue == PresentationCue.EnemyHit)
                    sawRaiderHit = true;
                if (signal.actor == PresentationActorKind.Raider && signal.cue == PresentationCue.EnemyDefeated)
                    sawRaiderDeath = true;
                if (signal.actor == PresentationActorKind.Vanguard && signal.cue == PresentationCue.UnitSpawned)
                    sawVanguardSpawn = true;
                if (signal.actor == PresentationActorKind.Vanguard && signal.cue == PresentationCue.OutpostDisrupted)
                    sawVanguardHeavy = true;
                if (signal.actor == PresentationActorKind.Regenerator && signal.cue == PresentationCue.UnitSpawned)
                    sawRegeneratorSpawn = true;
                if (signal.actor == PresentationActorKind.Regenerator && signal.cue == PresentationCue.HealPulse)
                    sawRegeneratorPulse = true;
                if (signal.actor == PresentationActorKind.Support && signal.cue == PresentationCue.UnitSpawned)
                    sawSupportSpawn = true;
                if (signal.actor == PresentationActorKind.Support && signal.cue == PresentationCue.HealPulse)
                    sawSupportHeal = true;
                if (signal.actor == PresentationActorKind.Siege && signal.cue == PresentationCue.UnitSpawned)
                    sawSiegeSpawn = true;
                if (signal.actor == PresentationActorKind.Siege && signal.cue == PresentationCue.OutpostDisrupted)
                    sawSiegeHeavy = true;

                bool actorCaptureCue = signal.cue is PresentationCue.HeroAttacked or PresentationCue.HeroAbility
                    or PresentationCue.UnitSpawned or PresentationCue.OutpostAttacked
                    or PresentationCue.EnemyHit or PresentationCue.EnemyDefeated or PresentationCue.OutpostDisrupted
                    or PresentationCue.HealPulse;
                if (actorCaptureCue && (signal.actor is PresentationActorKind.Knight or PresentationActorKind.Guard
                        or PresentationActorKind.Raider or PresentationActorKind.Vanguard
                        or PresentationActorKind.Regenerator or PresentationActorKind.Support
                        or PresentationActorKind.Siege))
                {
                    DefenderVfxSpec vfx = DefenderVfxPool.Describe(signal);
                    string animation = DefenderPresentationDirector.AnimationTriggerFor(signal) ?? "none";
                    Debug.Log($"[DC_ACTOR_CAPTURE_SIGNAL] actor={signal.actor} cue={signal.cue} " +
                              $"animation={animation} vfx={vfx.materialRole}/{vfx.shape} projectile={vfx.projectile}");
                }
            }

            PresentationSignaled += TrackActorSignal;
            StartNextWave();

            float deadline = Time.realtimeSinceStartup + 3f;
            GreyboxEnemy raider = null;
            while (Time.realtimeSinceStartup < deadline)
            {
                raider = enemies.FirstOrDefault(enemy => enemy != null && enemy.Archetype == EnemyArchetype.Raider);
                if (raider != null) break;
                yield return null;
            }

            if (raider == null || hero == null || !hero.IsAlive)
            {
                PresentationSignaled -= TrackActorSignal;
                Debug.LogError($"[DC_ACTOR_CAPTURE_FAIL] raider_found={raider != null} hero_ready={hero != null && hero.IsAlive}");
                yield break;
            }

            raider.transform.position = hero.Position + new Vector3(-1.15f, 0f, -0.35f);
            raider.transform.rotation = Quaternion.LookRotation((hero.Position - raider.Position).normalized, Vector3.up);
            yield return null;

            deadline = Time.realtimeSinceStartup + 2f;
            while (!sawKnightAttack && Time.realtimeSinceStartup < deadline)
                yield return null;

            if (!sawRaiderHit && raider != null && raider.IsAlive)
                raider.TakeDamage(5);
            yield return null;

            if (!sawKnightAbility)
                hero.TryUseShieldBash();
            yield return new WaitForSeconds(0.15f);

            if (!sawRaiderDeath && raider != null && raider.IsAlive)
                raider.TakeDamage(999);
            yield return new WaitForSeconds(0.15f);

            Vector3 guardPreviewPosition = hero.Position + new Vector3(-3.15f, 0f, -0.95f);
            Vector3 raiderPreviewPosition = hero.Position + new Vector3(-4.25f, 0f, -0.95f);
            Vector3 vanguardPreviewPosition = hero.Position + new Vector3(-2.05f, 0f, -0.95f);
            Vector3 regeneratorPreviewPosition = hero.Position + new Vector3(-0.95f, 0f, -0.95f);
            Vector3 supportPreviewPosition = hero.Position + new Vector3(0.18f, 0f, -0.95f);
            Vector3 siegePreviewPosition = hero.Position + new Vector3(1.72f, 0f, -0.98f);
            GameObject guardPreview = null;
            GameObject raiderPreview = null;
            GameObject vanguardPreview = null;
            GameObject regeneratorPreview = null;
            GameObject supportPreview = null;
            GameObject siegePreview = null;
            if (presentationCatalog != null)
            {
                guardPreview = StylizedPresentationFactory.CreateRoot("Guard Capture Preview", guardPreviewPosition,
                    new Vector3(0.72f, 1.28f, 0.72f), presentationCatalog.ActorPrefab(PresentationActorKind.Guard),
                    presentationCatalog, PresentationCollider.Capsule, presentationCatalog.ActorScale(PresentationActorKind.Guard),
                    presentationCatalog.ActorAnimator(PresentationActorKind.Guard), PresentationActorKind.Guard);
                guardPreview.transform.SetParent(transform, true);

                raiderPreview = StylizedPresentationFactory.CreateRoot("Raider Capture Preview", raiderPreviewPosition,
                    new Vector3(0.85f, 1.45f, 0.85f), presentationCatalog.ActorPrefab(PresentationActorKind.Raider),
                    presentationCatalog, PresentationCollider.Capsule, presentationCatalog.ActorScale(PresentationActorKind.Raider),
                    presentationCatalog.ActorAnimator(PresentationActorKind.Raider), PresentationActorKind.Raider);
                raiderPreview.transform.SetParent(transform, true);

                vanguardPreview = StylizedPresentationFactory.CreateRoot("Vanguard Capture Preview", vanguardPreviewPosition,
                    new Vector3(1.0f, 1.72f, 1.0f), presentationCatalog.ActorPrefab(PresentationActorKind.Vanguard),
                    presentationCatalog, PresentationCollider.Capsule, presentationCatalog.ActorScale(PresentationActorKind.Vanguard),
                    presentationCatalog.ActorAnimator(PresentationActorKind.Vanguard), PresentationActorKind.Vanguard);
                vanguardPreview.transform.SetParent(transform, true);

                regeneratorPreview = StylizedPresentationFactory.CreateRoot("Regenerator Capture Preview", regeneratorPreviewPosition,
                    new Vector3(0.9f, 1.58f, 0.9f), presentationCatalog.ActorPrefab(PresentationActorKind.Regenerator),
                    presentationCatalog, PresentationCollider.Capsule, presentationCatalog.ActorScale(PresentationActorKind.Regenerator),
                    presentationCatalog.ActorAnimator(PresentationActorKind.Regenerator), PresentationActorKind.Regenerator);
                regeneratorPreview.transform.SetParent(transform, true);

                supportPreview = StylizedPresentationFactory.CreateRoot("Support Capture Preview", supportPreviewPosition,
                    new Vector3(0.9f, 1.58f, 0.9f), presentationCatalog.ActorPrefab(PresentationActorKind.Support),
                    presentationCatalog, PresentationCollider.Capsule, presentationCatalog.ActorScale(PresentationActorKind.Support),
                    presentationCatalog.ActorAnimator(PresentationActorKind.Support), PresentationActorKind.Support);
                supportPreview.transform.SetParent(transform, true);

                siegePreview = StylizedPresentationFactory.CreateRoot("Siege Capture Preview", siegePreviewPosition,
                    new Vector3(1.34f, 1.08f, 1.34f), presentationCatalog.ActorPrefab(PresentationActorKind.Siege),
                    presentationCatalog, PresentationCollider.Capsule, presentationCatalog.ActorScale(PresentationActorKind.Siege),
                    presentationCatalog.ActorAnimator(PresentationActorKind.Siege), PresentationActorKind.Siege);
                siegePreview.transform.SetParent(transform, true);
            }

            float reviewUntil = Time.realtimeSinceStartup + 12f;
            bool reportedCapturePass = false;
            while (Time.realtimeSinceStartup < reviewUntil)
            {
                GameObject guardSource = guardPreview != null ? guardPreview : null;
                GameObject raiderSource = raiderPreview != null ? raiderPreview : null;
                GameObject vanguardSource = vanguardPreview != null ? vanguardPreview : null;
                GameObject regeneratorSource = regeneratorPreview != null ? regeneratorPreview : null;
                GameObject supportSource = supportPreview != null ? supportPreview : null;
                GameObject siegeSource = siegePreview != null ? siegePreview : null;
                EmitPresentation(PresentationCue.UnitSpawned, guardPreviewPosition, guardSource, PresentationActorKind.Guard);
                EmitPresentation(PresentationCue.OutpostAttacked, guardPreviewPosition, guardSource,
                    PresentationActorKind.Guard, OutpostKind.Barracks);
                EmitPresentation(PresentationCue.HeroAbility, guardPreviewPosition, guardSource, PresentationActorKind.Guard);
                EmitPresentation(PresentationCue.UnitSpawned, raiderPreviewPosition, raiderSource, PresentationActorKind.Raider);
                EmitPresentation(PresentationCue.HeroAttacked, raiderPreviewPosition, hero.gameObject, PresentationActorKind.Knight);
                EmitPresentation(PresentationCue.HeroAbility, hero.Position, hero.gameObject, PresentationActorKind.Knight);
                EmitPresentation(PresentationCue.EnemyHit, raiderPreviewPosition, raiderSource, PresentationActorKind.Raider);
                EmitPresentation(PresentationCue.EnemyDefeated, raiderPreviewPosition, raiderSource, PresentationActorKind.Raider);
                EmitPresentation(PresentationCue.UnitSpawned, vanguardPreviewPosition, vanguardSource,
                    PresentationActorKind.Vanguard);
                EmitPresentation(PresentationCue.OutpostDisrupted, vanguardPreviewPosition, vanguardSource,
                    PresentationActorKind.Vanguard);
                EmitPresentation(PresentationCue.UnitSpawned, regeneratorPreviewPosition, regeneratorSource,
                    PresentationActorKind.Regenerator);
                EmitPresentation(PresentationCue.HealPulse, regeneratorPreviewPosition, regeneratorSource,
                    PresentationActorKind.Regenerator);
                EmitPresentation(PresentationCue.UnitSpawned, supportPreviewPosition, supportSource,
                    PresentationActorKind.Support);
                EmitPresentation(PresentationCue.HealPulse, supportPreviewPosition + new Vector3(0.95f, 0f, 0.24f),
                    supportSource, PresentationActorKind.Support);
                EmitPresentation(PresentationCue.UnitSpawned, siegePreviewPosition, siegeSource,
                    PresentationActorKind.Siege);
                EmitPresentation(PresentationCue.OutpostDisrupted, siegePreviewPosition + new Vector3(0.95f, 0f, 0.26f),
                    siegeSource, PresentationActorKind.Siege);
                if (!reportedCapturePass && sawKnightAttack && sawKnightAbility && sawGuardSpawn &&
                    sawGuardThrust && sawGuardBlock && sawRaiderSpawn && sawRaiderHit && sawRaiderDeath &&
                    sawVanguardSpawn && sawVanguardHeavy && sawRegeneratorSpawn && sawRegeneratorPulse &&
                    sawSupportSpawn && sawSupportHeal && sawSiegeSpawn && sawSiegeHeavy)
                {
                    Debug.Log("[DC_ACTOR_CAPTURE_PASS] knight_attack=true knight_ability=true " +
                              "guard_spawn=true guard_thrust=true guard_block=true " +
                              "raider_spawn=true raider_hit=true raider_death=true " +
                              "vanguard_spawn=true vanguard_heavy=true " +
                              "regenerator_spawn=true regenerator_pulse=true " +
                              "support_spawn=true support_heal=true " +
                              "siege_spawn=true siege_heavy=true");
                    reportedCapturePass = true;
                }
                yield return new WaitForSeconds(0.9f);
            }

            PresentationSignaled -= TrackActorSignal;
            bool passed = sawKnightAttack && sawKnightAbility && sawGuardSpawn && sawGuardThrust && sawGuardBlock &&
                          sawRaiderSpawn && sawRaiderHit && sawRaiderDeath && sawVanguardSpawn && sawVanguardHeavy &&
                          sawRegeneratorSpawn && sawRegeneratorPulse && sawSupportSpawn && sawSupportHeal &&
                          sawSiegeSpawn && sawSiegeHeavy;
            if (!reportedCapturePass)
            {
                Debug.Log(passed
                    ? "[DC_ACTOR_CAPTURE_PASS] knight_attack=true knight_ability=true guard_spawn=true guard_thrust=true guard_block=true raider_spawn=true raider_hit=true raider_death=true vanguard_spawn=true vanguard_heavy=true regenerator_spawn=true regenerator_pulse=true support_spawn=true support_heal=true siege_spawn=true siege_heavy=true"
                    : $"[DC_ACTOR_CAPTURE_FAIL] knight_attack={sawKnightAttack} knight_ability={sawKnightAbility} " +
                      $"guard_spawn={sawGuardSpawn} guard_thrust={sawGuardThrust} guard_block={sawGuardBlock} " +
                      $"raider_spawn={sawRaiderSpawn} raider_hit={sawRaiderHit} raider_death={sawRaiderDeath} " +
                      $"vanguard_spawn={sawVanguardSpawn} vanguard_heavy={sawVanguardHeavy} " +
                      $"regenerator_spawn={sawRegeneratorSpawn} regenerator_pulse={sawRegeneratorPulse} " +
                      $"support_spawn={sawSupportSpawn} support_heal={sawSupportHeal} " +
                      $"siege_spawn={sawSiegeSpawn} siege_heavy={sawSiegeHeavy}");
            }
        }

        private IEnumerator RunAutomatedDefeatAndRestartSmoke()
        {
            yield return null;
            if (defeatRestartRequested)
            {
                bool restartedCleanly = Phase == GamePhase.Preparing && Gold == StartingGold && Lives == StartingLives;
                defeatRestartRequested = false;
                if (restartedCleanly)
                    Debug.Log("[DC_DEFEAT_RESTART_PASS] Defeat and clean scene restart completed.");
                else
                    Debug.LogError($"[DC_DEFEAT_RESTART_FAIL] Restart state was {Phase}, gold {Gold}, lives {Lives}.");
                yield break;
            }

            LoseLives(StartingLives);
            if (Phase != GamePhase.Defeat)
            {
                Debug.LogError($"[DC_DEFEAT_RESTART_FAIL] Expected Defeat, got {Phase}.");
                yield break;
            }

            defeatRestartRequested = true;
            yield return null;
            RestartLevel();
        }

        private void HandleWorldPointer()
        {
            if (Phase == GamePhase.Paused || Phase == GamePhase.Victory || Phase == GamePhase.Defeat) return;

            Vector2 screen;
            int pointerId = -1;
            bool pressed = false;
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                screen = Touchscreen.current.primaryTouch.position.ReadValue();
                pointerId = Touchscreen.current.primaryTouch.touchId.ReadValue();
                pressed = true;
            }
            else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                screen = Mouse.current.position.ReadValue();
                pressed = true;
            }
            else
            {
                return;
            }

            if (!pressed || gameCamera == null) return;
            if (EventSystem.current != null && (pointerId >= 0
                    ? EventSystem.current.IsPointerOverGameObject(pointerId)
                    : EventSystem.current.IsPointerOverGameObject()))
                return;

            Ray ray = gameCamera.ScreenPointToRay(screen);
            int selectableMask = LayerMask.GetMask(SelectableLayerName);
            if (selectableMask == 0 || !Physics.Raycast(ray, out RaycastHit hit, 100f,
                    selectableMask, QueryTriggerInteraction.Ignore)) return;

            var slot = hit.collider.GetComponentInParent<GreyboxOutpostSlot>();
            if (slot != null)
            {
                SelectSlot(slot);
                return;
            }

            var outpost = hit.collider.GetComponentInParent<GreyboxOutpost>();
            if (outpost != null)
            {
                SelectOutpost(outpost);
                return;
            }

        }

        private CombatRangeVisual selectionRange;
        private void SelectSlot(GreyboxOutpostSlot slot)
        {
            if (selectedSlot != null) selectedSlot.SetSelected(false);
            selectedSlot = slot;
            selectedSlot.SetSelected(true);
            selectedOutpost = slot.Occupant;
            if (selectionRange == null) selectionRange = CombatRangeVisual.Create(transform,"Selected tower range",new Color(.35f,.8f,1));
            selectionRange.Show(slot.transform.position, selectedOutpost != null ? selectedOutpost.RangeValue : TowerProgression.Stats(OutpostKind.Archer,1).range,true);
            RefreshSelectedPanel();
        }

        private void SelectOutpost(GreyboxOutpost outpost)
        {
            selectedOutpost = outpost;
            SelectSlot(outpost.Slot);
            selectedOutpost = outpost;
            RefreshSelectedPanel();
        }

        private static void SetLayerRecursively(GameObject root, int layer)
        {
            if (root == null || layer < 0) return;
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                child.gameObject.layer = layer;
        }

        private void RefreshSelectedPanel()
        {
            if (artHud != null)
            {
                if (selectedOutpost != null)
                    artHud.SetSelected(selectedOutpost, null, true);
                else if (selectedSlot != null)
                    artHud.SetSelected(null, selectedSlot, true);
                else
                    artHud.SetSelected(null, null, false);
                return;
            }
            if (selectedText == null) return;
            if (selectedOutpost != null)
            {
                selectedText.text = selectedOutpost.Description;
                upgradeButton.interactable = selectedOutpost.CanUpgrade;
                sellButton.interactable = true;
            }
            else if (selectedSlot != null)
            {
                selectedText.text = GameLocalization.Text(
                    $"KARAKOL PLATFORMU {selectedSlot.Index + 1}\n\nAşağıdan bir karakol seç.",
                    $"OUTPOST PLATFORM {selectedSlot.Index + 1}\n\nChoose an outpost below.");
                upgradeButton.interactable = false;
                sellButton.interactable = false;
            }
            else
            {
                selectedText.text = GameLocalization.Text("PLATFORM SEÇİLMEDİ", "NO PLATFORM SELECTED");
                upgradeButton.interactable = false;
                sellButton.interactable = false;
            }
            selectedPanel.SetActive(selectedSlot != null || selectedOutpost != null);
        }

        private void RefreshUi()
        {
            if (artHud != null)
            {
                artHud.Refresh(this);
                bool canBuildOnSelection = Phase != GamePhase.Paused && selectedSlot != null && selectedSlot.Occupant == null;
                artHud.SetBuildAvailability(canBuildOnSelection, Gold);
                if (Phase == GamePhase.Preparing)
                    artHud.SetNextWaveLabel(presentationCatalog.uiCopy.Get(UiTextKey.FirstWave));
                RefreshSelectedPanel();
                return;
            }
            if (goldText == null) return;
            goldText.text = GameLocalization.Text($"ALTIN  {Gold}", $"GOLD  {Gold}");
            livesText.text = GameLocalization.Text($"KALE  {Lives}", $"KEEP  {Lives}");
            livesText.color = Lives <= 5 ? new Color(1f, 0.35f, 0.3f) : Color.white;
            int visibleWave = Mathf.Min(currentWaveIndex + (UnpausedPhase == GamePhase.Wave ? 1 : 0), waves.Length);
            waveText.text = GameLocalization.Text($"DALGA  {visibleWave} / {waves.Length}", $"WAVE  {visibleWave} / {waves.Length}");
            phaseText.text = Phase switch
            {
                GamePhase.Preparing => GameLocalization.Text("HAZIRLIK", "PREPARING"),
                GamePhase.Wave => GameLocalization.Text("SAVAŞ", "COMBAT"),
                GamePhase.Intermission => GameLocalization.Text("MOLA", "INTERMISSION"),
                GamePhase.Paused => GameLocalization.Text("DURAKLATILDI", "PAUSED"),
                GamePhase.Victory => GameLocalization.Text("ZAFER", "VICTORY"),
                GamePhase.Defeat => GameLocalization.Text("YENİLGİ", "DEFEAT"),
                _ => GameLocalization.Text("YÜKLENİYOR", "LOADING")
            };
            if (hero != null)
            {
                heroText.text = hero.IsAlive
                    ? GameLocalization.Text(
                        $"ŞÖVALYE  {Mathf.CeilToInt(hero.HealthFraction * 100f)}%  •  YETENEK {(hero.AbilityRemaining <= 0f ? "HAZIR" : Mathf.CeilToInt(hero.AbilityRemaining) + " sn")}",
                        $"KNIGHT  {Mathf.CeilToInt(hero.HealthFraction * 100f)}%  •  ABILITY {(hero.AbilityRemaining <= 0f ? "READY" : Mathf.CeilToInt(hero.AbilityRemaining) + " sec")}")
                    : GameLocalization.Text($"ŞÖVALYE DÖNÜYOR  {Mathf.CeilToInt(hero.RespawnRemaining)} sn",
                        $"KNIGHT RETURNS IN  {Mathf.CeilToInt(hero.RespawnRemaining)} sec");
                if (heroAbilityButton != null)
                    heroAbilityButton.interactable = hero.IsAlive && hero.AbilityRemaining <= 0f && Phase != GamePhase.Paused;
            }
            if (Phase == GamePhase.Preparing && nextWaveButtonLabel != null)
                nextWaveButtonLabel.text = GameLocalization.Text("İLK DALGAYI BAŞLAT", "START FIRST WAVE");
            if (speedButtonLabel != null)
                speedButtonLabel.text = SpeedLabelForStep(speedStepIndex);
            RefreshSelectedPanel();
        }

        private Coroutine messageRoutine;
        private void ShowMessage(string message, float duration)
        {
            message = GameLocalization.Translate(message);
            if (messageText == null && artHud == null) return;
            if (messageRoutine != null) StopCoroutine(messageRoutine);
            messageRoutine = StartCoroutine(MessageRoutine(message, duration));
        }

        private IEnumerator MessageRoutine(string message, float duration)
        {
            if (messageText != null) messageText.text = message;
            artHud?.SetMessage(message);
            yield return new WaitForSecondsRealtime(duration);
            if (messageText != null) messageText.text = string.Empty;
            artHud?.SetMessage(string.Empty);
            messageRoutine = null;
        }

        private bool SpendGold(int amount)
        {
            if (amount < 0 || Gold < amount) return false;
            Gold -= amount;
            GoldChanged?.Invoke(Gold);
            return true;
        }

        private void EarnGold(int amount)
        {
            if (amount <= 0) return;
            Gold += amount;
            GoldChanged?.Invoke(Gold);
        }

        private void LoseLives(int amount)
        {
            if (Phase == GamePhase.Defeat || Phase == GamePhase.Victory) return;
            if (automatedPerformanceAcceptance || automatedSoakAcceptance) return;
            Lives = Mathf.Max(0, Lives - Mathf.Max(0, amount));
            LivesChanged?.Invoke(Lives);
            if (Lives > 0) return;
            SetPhase(GamePhase.Defeat);
            if (resultPanel != null)
            {
                resultPanel.SetActive(true);
                resultTitle.text = "YENİLGİ\nGREENWATCH DÜŞTÜ";
                resultTitle.color = new Color(1f, 0.35f, 0.28f);
            }
            artHud?.ShowResult(false);
            EmitPresentation(PresentationCue.Defeat, path[^1]);
            Time.timeScale = 0f;
        }

        internal GreyboxEnemy FindFirstEnemy(Vector3 position, float range)
        {
            GreyboxEnemy best = null;
            float progress = float.MinValue;
            float sqrRange = range * range;
            foreach (var enemy in enemies)
            {
                if (enemy == null || !enemy.IsAlive || (enemy.Position - position).sqrMagnitude > sqrRange) continue;
                if (enemy.RouteProgress <= progress) continue;
                best = enemy;
                progress = enemy.RouteProgress;
            }
            return best;
        }

        internal IDamageable FindNearestBlocker(Vector3 position, float range)
        {
            IDamageable best = null;
            float bestDistance = range * range;
            if (hero != null && hero.IsAlive)
            {
                float d = (hero.Position - position).sqrMagnitude;
                if (d <= bestDistance) { best = hero; bestDistance = d; }
            }
            foreach (var unit in friendlyUnits)
            {
                if (unit == null || !unit.IsAlive) continue;
                float d = (unit.Position - position).sqrMagnitude;
                if (d > bestDistance) continue;
                best = unit;
                bestDistance = d;
            }
            return best;
        }

        internal GreyboxOutpost FindNearestOutpost(Vector3 position, float range)
        {
            GreyboxOutpost best = null;
            float bestDistance = range * range;
            foreach (var outpost in outposts)
            {
                if (outpost == null) continue;
                float d = (outpost.Position - position).sqrMagnitude;
                if (d > bestDistance) continue;
                best = outpost;
                bestDistance = d;
            }
            return best;
        }

        internal void DamageEnemies(Vector3 position, float radius, int damage, float slowFactor = 1f, float slowDuration = 0f, DamageType damageType = DamageType.Magic)
        {
            float sqr = radius * radius;
            foreach (var enemy in enemies.ToArray())
            {
                if (enemy == null || !enemy.IsAlive || (enemy.Position - position).sqrMagnitude > sqr) continue;
                enemy.TakeDamage(damage, damageType);
                if (slowDuration > 0f) enemy.ApplySlow(slowFactor, slowDuration);
            }
        }

        internal bool HealEnemies(Vector3 position, float radius, int amount, GreyboxEnemy source, out Vector3 primaryTargetPosition)
        {
            float sqr = radius * radius;
            float bestTargetSqr = float.MaxValue;
            primaryTargetPosition = position;
            bool healedAny = false;
            foreach (var enemy in enemies)
            {
                if (enemy == null || enemy == source || !enemy.IsAlive || (enemy.Position - position).sqrMagnitude > sqr) continue;
                enemy.Heal(amount);
                healedAny = true;
                float targetSqr = source != null ? (enemy.Position - source.Position).sqrMagnitude : (enemy.Position - position).sqrMagnitude;
                if (targetSqr >= bestTargetSqr) continue;
                bestTargetSqr = targetSqr;
                primaryTargetPosition = enemy.Position;
            }
            return healedAny;
        }

        internal void RegisterFriendly(GreyboxFriendlyUnit unit)
        {
            if (unit != null && !friendlyUnits.Contains(unit)) friendlyUnits.Add(unit);
        }

        internal GameObject CreateFriendlyVisual(string objectName, Vector3 position)
        {
            if (presentationCatalog == null) return null;
            GameObject root = StylizedPresentationFactory.CreateRoot(objectName, position,
                new Vector3(0.52f, 1.25f, 0.52f), presentationCatalog.ActorPrefab(PresentationActorKind.Guard),
                presentationCatalog, PresentationCollider.Capsule, presentationCatalog.ActorScale(PresentationActorKind.Guard),
                presentationCatalog.ActorAnimator(PresentationActorKind.Guard), PresentationActorKind.Guard);
            root.transform.SetParent(transform, true);
            EmitPresentation(PresentationCue.UnitSpawned, position, root, PresentationActorKind.Guard);
            return root;
        }

        internal void ReplaceOutpostVisual(GreyboxOutpost outpost)
        {
            if (presentationCatalog == null || outpost == null) return;
            Transform motion = outpost.transform.Find("Motion");
            Transform art = motion != null ? motion.Find("Art") : outpost.transform.Find("Art");
            if (art != null) Destroy(art.gameObject);
            GameObject visual = StylizedPresentationFactory.SpawnModel(
                presentationCatalog.OutpostPrefab(outpost.Kind, 2), motion != null ? motion : outpost.transform,
                presentationCatalog, "Art", true);
            if (visual != null)
            {
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
                visual.transform.localScale = Vector3.one * presentationCatalog.OutpostScale(outpost.Kind, 2);
                outpost.RefreshPresentationRenderer();
            }
        }

        internal void EmitPresentation(PresentationCue cue, Vector3 position, GameObject source = null,
            PresentationActorKind actor = PresentationActorKind.Raider,
            OutpostKind outpost = OutpostKind.Archer, int value = 0)
        {
            if (presentationCatalog == null) return;
            PresentationSignaled?.Invoke(new PresentationSignal(cue, position, source, actor, outpost, value));
        }

        public float ComputeRouteProgress(Vector3 position, int targetIndex)
        {
            if (path.Count < 2) return 0f;
            int index = Mathf.Clamp(targetIndex - 1, 0, path.Count - 2);
            float completed = 0f;
            for (int i = 0; i < index; i++) completed += Vector3.Distance(path[i], path[i + 1]);
            Vector3 from = path[index];
            Vector3 to = path[index + 1];
            return completed + CombatMath.RouteProgress(from, to, position);
        }

        private void ValidateContent()
        {
            var errors = new List<string>();
            if (path.Count < 2) errors.Add("Level path needs at least two points.");
            if (slots.Count != 8) errors.Add("Greybox level needs exactly eight outpost slots.");
            if (waves == null || activeLevelDefinition == null || waves.Length != activeLevelDefinition.waveCount)
                errors.Add("Campaign wave count does not match its level definition.");
            if (waves != null && waves.Length > 0 && waves[^1].bossWave != activeLevelDefinition.isBoss)
                errors.Add("Boss wave does not match the campaign milestone.");
            if (uiFont == null) errors.Add("Unity LegacyRuntime font is unavailable.");
            if (presentationCatalog != null)
                errors.AddRange(presentationCatalog.ValidationErrors());

            if (errors.Count > 0)
                throw new InvalidOperationException("Defender Chronicles content validation failed:\n- " + string.Join("\n- ", errors));
        }

        internal static EnemyStats GetStats(EnemyArchetype archetype) => archetype switch
        {
            EnemyArchetype.Raider => new EnemyStats(75, 2.25f, 10, 1, 9, new Color(0.80f, 0.25f, 0.22f)),
            EnemyArchetype.Vanguard => new EnemyStats(230, 1.35f, 22, 1, 13, new Color(0.60f, 0.18f, 0.14f)),
            EnemyArchetype.Regenerator => new EnemyStats(420, 0.95f, 38, 2, 15, new Color(0.25f, 0.62f, 0.28f)),
            EnemyArchetype.Support => new EnemyStats(180, 1.25f, 30, 1, 8, new Color(0.94f, 0.45f, 0.78f)),
            EnemyArchetype.Siege => new EnemyStats(620, 0.72f, 55, 3, 22, new Color(0.42f, 0.30f, 0.18f)),
            EnemyArchetype.Boss => new EnemyStats(2600, 0.62f, 220, 5, 30, new Color(0.72f, 0.08f, 0.08f)),
            _ => new EnemyStats(100, 1f, 10, 1, 10, Color.red)
        };

        private EnemyStats GetScaledStats(EnemyArchetype archetype)
        {
            EnemyStats baseStats = GetStats(archetype);
            if (CampaignLevelNumber <= FirstRegionCampaign.LevelCount)
            {
                var balance = FirstRegionCampaign.Level(CampaignLevelNumber);
                return new EnemyStats(Mathf.RoundToInt(baseStats.maxHealth * balance.healthMultiplier),
                    baseStats.speed * BattleRules.LegacyPace,
                    balance.killRewards[(int)archetype], baseStats.baseDamage, baseStats.attackDamage, baseStats.color);
            }
            float progress = Mathf.Clamp01((CampaignLevelNumber - 1f) / (CampaignCatalog.LevelCount - 1f));
            float healthMultiplier = Mathf.Lerp(1f, 2.7f, progress);
            float speedMultiplier = Mathf.Lerp(1f, 1.16f, progress);
            float attackMultiplier = Mathf.Lerp(1f, 1.85f, progress);
            return new EnemyStats(
                Mathf.RoundToInt(baseStats.maxHealth * healthMultiplier),
                baseStats.speed * speedMultiplier * BattleRules.LegacyPace,
                Mathf.RoundToInt(baseStats.reward * Mathf.Lerp(1f, 1.4f, progress)),
                Mathf.Max(1, Mathf.RoundToInt(baseStats.baseDamage * Mathf.Lerp(1f, 1.6f, progress))),
                Mathf.Max(1, Mathf.RoundToInt(baseStats.attackDamage * attackMultiplier)),
                baseStats.color);
        }

        internal static PresentationActorKind ActorKind(EnemyArchetype archetype) => archetype switch
        {
            EnemyArchetype.Raider => PresentationActorKind.Raider,
            EnemyArchetype.Vanguard => PresentationActorKind.Vanguard,
            EnemyArchetype.Regenerator => PresentationActorKind.Regenerator,
            EnemyArchetype.Support => PresentationActorKind.Support,
            EnemyArchetype.Siege => PresentationActorKind.Siege,
            EnemyArchetype.Boss => PresentationActorKind.Boss,
            _ => PresentationActorKind.Raider
        };

        internal static OutpostStats GetOutpostStats(OutpostKind kind) => TowerProgression.Base(kind);

        internal static string OutpostDisplayName(OutpostKind kind) => kind switch
        {
            OutpostKind.Archer => GameLocalization.Text("OKÇU KARAKOLU", "ARCHER OUTPOST"),
            OutpostKind.Barracks => GameLocalization.Text("MUHAFIZ KIŞLASI", "GUARD BARRACKS"),
            OutpostKind.Mage => GameLocalization.Text("BÜYÜ OCAĞI", "MAGE OUTPOST"),
            _ => kind.ToString().ToUpperInvariant()
        };

        internal static GameObject CreatePrimitive(string objectName, PrimitiveType primitive, Vector3 position,
            Vector3 scale, Color color, Transform parent, bool keepCollider)
        {
            var go = GameObject.CreatePrimitive(primitive);
            go.name = objectName;
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = scale;
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = CreateMaterial(color);
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
            if (!keepCollider)
            {
                var collider = go.GetComponent<Collider>();
                if (collider != null) Destroy(collider);
            }
            return go;
        }

        internal static GameObject CreatePrimitiveLocal(string objectName, PrimitiveType primitive, Transform parent,
            Vector3 localPosition, Vector3 localScale, Color color, bool keepCollider = false)
        {
            var go = GameObject.CreatePrimitive(primitive);
            go.name = objectName;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = CreateMaterial(color);
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
            if (!keepCollider)
            {
                var collider = go.GetComponent<Collider>();
                if (collider != null) Destroy(collider);
            }
            return go;
        }

        internal static Material CreateMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                            ?? Shader.Find("Universal Render Pipeline/Unlit")
                            ?? Shader.Find("Sprites/Default")
                            ?? Shader.Find("Unlit/Color");
            var material = new Material(shader) { color = color };
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            return material;
        }

        private GameObject CreateUiObject(string objectName, Transform parent)
        {
            var go = new GameObject(objectName, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private GameObject CreatePanel(string objectName, Transform parent, Color color)
        {
            var go = CreateUiObject(objectName, parent);
            go.AddComponent<Image>().color = color;
            return go;
        }

        private Text CreateText(string objectName, Transform parent, string value, int size, TextAnchor alignment)
        {
            var go = CreateUiObject(objectName, parent);
            var text = go.AddComponent<Text>();
            text.font = uiFont;
            text.text = value;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = Color.white;
            text.supportRichText = true;
            text.resizeTextForBestFit = false;
            if (GameLocalization.HasTranslation(value))
                go.AddComponent<LocalizedLegacyText>().Configure(text, value);
            return text;
        }

        private Button CreateButton(string objectName, Transform parent, string label, Vector2 size, UnityEngine.Events.UnityAction action)
        {
            var go = CreateUiObject(objectName, parent);
            var image = go.AddComponent<Image>();
            image.color = new Color(0.16f, 0.29f, 0.40f, 1f);
            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);
            var colors = button.colors;
            colors.highlightedColor = new Color(0.24f, 0.46f, 0.62f, 1f);
            colors.pressedColor = new Color(0.10f, 0.20f, 0.28f, 1f);
            colors.disabledColor = new Color(0.12f, 0.14f, 0.16f, 0.75f);
            button.colors = colors;

            var text = CreateText("Label", go.transform, label, 24, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform, new Vector2(8f, 4f), new Vector2(-8f, -4f));
            go.GetComponent<RectTransform>().sizeDelta = size;
            return button;
        }

        private static void Stretch(RectTransform rect, Vector2? minOffset = null, Vector2? maxOffset = null)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = minOffset ?? Vector2.zero;
            rect.offsetMax = maxOffset ?? Vector2.zero;
        }

        private static void SetAnchors(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void SetRect(RectTransform rect, Vector2 anchoredPosition, Vector2 size, Vector2 anchorMin, Vector2 anchorMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }
    }

    public readonly struct EnemyStats
    {
        public readonly int maxHealth;
        public readonly float speed;
        public readonly int reward;
        public readonly int baseDamage;
        public readonly int attackDamage;
        public readonly Color color;

        public EnemyStats(int maxHealth, float speed, int reward, int baseDamage, int attackDamage, Color color)
        {
            this.maxHealth = maxHealth;
            this.speed = speed;
            this.reward = reward;
            this.baseDamage = baseDamage;
            this.attackDamage = attackDamage;
            this.color = color;
        }
    }

    public readonly struct OutpostStats
    {
        public readonly int cost;
        public readonly int upgradeCost;
        public readonly int damage;
        public readonly float range;
        public readonly float attacksPerSecond;

        public OutpostStats(int cost, int upgradeCost, int damage, float range, float attacksPerSecond)
        {
            this.cost = cost;
            this.upgradeCost = upgradeCost;
            this.damage = damage;
            this.range = range;
            this.attacksPerSecond = attacksPerSecond;
        }
    }

    public sealed class RuntimeSafeArea : MonoBehaviour
    {
        private RectTransform rect;
        private Rect previous;

        private void Awake()
        {
            rect = GetComponent<RectTransform>();
            Apply();
        }

        private void Update()
        {
            if (previous != Screen.safeArea) Apply();
        }

        private void Apply()
        {
            if (rect == null || Screen.width <= 0 || Screen.height <= 0) return;
            previous = Screen.safeArea;
            Vector2 min = previous.position;
            Vector2 max = previous.position + previous.size;
            min.x /= Screen.width;
            min.y /= Screen.height;
            max.x /= Screen.width;
            max.y /= Screen.height;
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }

    public sealed class GreyboxOutpostSlot : MonoBehaviour
    {
        private GameSessionController session;
        private Renderer cachedRenderer;
        private Color normalColor;
        public int Index { get; private set; }
        public GreyboxOutpost Occupant { get; private set; }

        public void Configure(GameSessionController owner, int index, Color color)
        {
            session = owner;
            Index = index;
            normalColor = color;
            cachedRenderer = GetComponent<Renderer>();
        }

        public void SetOccupant(GreyboxOutpost value)
        {
            Occupant = value;
            if (cachedRenderer != null) cachedRenderer.sharedMaterial.color = value == null ? normalColor : new Color(0.18f, 0.22f, 0.25f);
        }

        public void SetSelected(bool selected)
        {
            if (cachedRenderer == null) return;
            cachedRenderer.sharedMaterial.color = selected
                ? new Color(0.25f, 0.95f, 1f)
                : Occupant == null ? normalColor : new Color(0.18f, 0.22f, 0.25f);
        }
    }

}
