using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DefenderChronicles.Data;
using DefenderChronicles.Presentation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
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
    public sealed class GameSessionController : MonoBehaviour
    {
        private const int StartingGold = 200;
        private const int StartingLives = 20;
        private const float IntermissionSeconds = 8f;

        public static GameSessionController Instance { get; private set; }

        public GamePhase Phase { get; private set; } = GamePhase.Boot;
        public int Gold { get; private set; } = StartingGold;
        public int Lives { get; private set; } = StartingLives;
        public IReadOnlyList<Vector3> Path => path;
        public IReadOnlyList<GreyboxEnemy> Enemies => enemies;
        public IReadOnlyList<GreyboxOutpost> Outposts => outposts;
        public GreyboxHero Hero => hero;
        public int WaveCount => waves?.Length ?? 0;
        public int CurrentWaveIndex => currentWaveIndex;
        public int SlotCount => slots.Count;
        public int CommandPointCount => commandPoints.Count;

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
        private readonly List<GreyboxCommandPoint> commandPoints = new();

        private WaveDefinition[] waves;
        private int currentWaveIndex;
        private int plannedThisWave;
        private int removedThisWave;
        private bool spawning;
        private bool initialized;
        private bool automatedSmoke;
        private bool automatedDefeatSmoke;
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
        private Button heroAbilityButton;
        private Button upgradeButton;
        private Button sellButton;
        private GameObject selectedPanel;
        private GameObject resultPanel;
        private Text resultTitle;
        private DefenderPresentationCatalog presentationCatalog;
        private DefenderPresentationDirector presentationDirector;
        private DefenderHudController artHud;

        private readonly Color groundColor = new(0.18f, 0.34f, 0.22f);
        private readonly Color roadColor = new(0.66f, 0.56f, 0.39f);
        private readonly Color roadEdgeColor = new(0.34f, 0.29f, 0.22f);
        private readonly Color stoneColor = new(0.47f, 0.52f, 0.50f);
        private readonly Color slotColor = new(0.16f, 0.57f, 0.72f);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void ConfigurePresentation(DefenderPresentationCatalog catalog)
        {
            if (initialized) throw new InvalidOperationException("Presentation must be configured before initialization.");
            presentationCatalog = catalog;
        }

        public void Initialize()
        {
            if (initialized) return;
            if (presentationCatalog == null && !Application.isEditor)
                throw new InvalidOperationException("Primitive presentation is restricted to automated editor tests.");
            initialized = true;
            Time.timeScale = 1f;
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;

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
            ShowMessage("Bir karakol platformuna dokun ve savunmanı kur.", 6f);
            RefreshUi();

            automatedSmoke = !Application.isEditor &&
                             (Environment.GetCommandLineArgs().Contains("-dc-smoke") ||
                              Environment.GetEnvironmentVariable("DC_SMOKE") == "1");
            automatedDefeatSmoke = !Application.isEditor &&
                                   Environment.GetEnvironmentVariable("DC_SMOKE_DEFEAT") == "1";
            if (automatedSmoke)
                StartCoroutine(RunAutomatedSmoke());
            else if (automatedDefeatSmoke)
                StartCoroutine(RunAutomatedDefeatAndRestartSmoke());
        }

        private void Update()
        {
            if (!initialized) return;

            enemies.RemoveAll(item => item == null);
            outposts.RemoveAll(item => item == null);
            friendlyUnits.RemoveAll(item => item == null);

            if (Phase == GamePhase.Intermission)
            {
                intermissionRemaining = Mathf.Max(0f, intermissionRemaining - Time.unscaledDeltaTime);
                if (nextWaveButtonLabel != null)
                    nextWaveButtonLabel.text = intermissionRemaining > 0f
                        ? $"DALGAYI ERKEN ÇAĞIR  {Mathf.CeilToInt(intermissionRemaining)}"
                        : "SONRAKİ DALGA";
                if (artHud != null)
                    artHud.SetNextWaveLabel(intermissionRemaining > 0f
                        ? $"{presentationCatalog.uiCopy.Get(UiTextKey.EarlyWave)}  {Mathf.CeilToInt(intermissionRemaining)}"
                        : presentationCatalog.uiCopy.Get(UiTextKey.NextWave));
                if (intermissionRemaining <= 0f)
                    StartNextWave();
            }

            HandleWorldPointer();
            RefreshUi();
        }

        private void BuildDefinitions()
        {
            path.Clear();
            path.AddRange(new[]
            {
                new Vector3(-2f, 0f, 2f),
                new Vector3(4f, 0f, 2f),
                new Vector3(4f, 0f, 8f),
                new Vector3(10f, 0f, 8f),
                new Vector3(10f, 0f, 3.5f),
                new Vector3(16f, 0f, 3.5f),
                new Vector3(16f, 0f, 9f),
                new Vector3(24f, 0f, 9f)
            });

            waves = new[]
            {
                new WaveDefinition("wave_01", "Hızlı yağmacılar", 25, false,
                    new SpawnGroupDefinition(EnemyArchetype.Raider, 10, 0f, 0.85f)),
                new WaveDefinition("wave_02", "Yağmacılar + zırhlı öncü", 35, false,
                    new SpawnGroupDefinition(EnemyArchetype.Raider, 9, 0f, 0.78f),
                    new SpawnGroupDefinition(EnemyArchetype.Vanguard, 4, 1.2f, 1.15f)),
                new WaveDefinition("wave_03", "İki koldan karma baskı", 45, false,
                    new SpawnGroupDefinition(EnemyArchetype.Vanguard, 6, 0f, 1.05f),
                    new SpawnGroupDefinition(EnemyArchetype.Raider, 12, 0.5f, 0.68f)),
                new WaveDefinition("wave_04", "Yenilenen muhafızlar", 55, false,
                    new SpawnGroupDefinition(EnemyArchetype.Regenerator, 5, 0f, 1.35f),
                    new SpawnGroupDefinition(EnemyArchetype.Raider, 12, 1f, 0.7f)),
                new WaveDefinition("wave_05", "Şifacı desteği", 70, false,
                    new SpawnGroupDefinition(EnemyArchetype.Vanguard, 8, 0f, 0.95f),
                    new SpawnGroupDefinition(EnemyArchetype.Support, 3, 0.5f, 2.1f),
                    new SpawnGroupDefinition(EnemyArchetype.Raider, 10, 0.5f, 0.65f)),
                new WaveDefinition("wave_06", "Kuşatma birliği", 90, false,
                    new SpawnGroupDefinition(EnemyArchetype.Siege, 3, 0f, 2.4f),
                    new SpawnGroupDefinition(EnemyArchetype.Regenerator, 5, 0.4f, 1.2f),
                    new SpawnGroupDefinition(EnemyArchetype.Vanguard, 9, 0.5f, 0.88f),
                    new SpawnGroupDefinition(EnemyArchetype.Raider, 10, 0.5f, 0.58f)),
                new WaveDefinition("wave_boss", "Kızıl Muhafız — üç safhalı boss", 150, true,
                    new SpawnGroupDefinition(EnemyArchetype.Raider, 6, 0f, 0.72f),
                    new SpawnGroupDefinition(EnemyArchetype.Vanguard, 4, 0.5f, 1.05f),
                    new SpawnGroupDefinition(EnemyArchetype.Support, 2, 0.5f, 2.2f),
                    new SpawnGroupDefinition(EnemyArchetype.Boss, 1, 2f, 1f))
            };
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
            gameCamera.orthographicSize = 11.2f;
            gameCamera.transform.position = new Vector3(11.4f, 18.5f, -11.8f);
            gameCamera.transform.rotation = Quaternion.Euler(50f, 0f, 0f);
            gameCamera.backgroundColor = new Color(0.055f, 0.11f, 0.17f);
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
        }

        private void BuildWorld()
        {
            if (presentationCatalog != null)
            {
                BuildStylizedWorld();
                return;
            }

            var world = new GameObject("Greybox World").transform;
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

            Vector3[] slotPositions =
            {
                new(2f, 0.15f, 0f), new(2f, 0.15f, 5f),
                new(6.5f, 0.15f, 6f), new(8f, 0.15f, 10.5f),
                new(12f, 0.15f, 1.2f), new(14f, 0.15f, 6f),
                new(18f, 0.15f, 6.8f), new(20.5f, 0.15f, 11.2f)
            };
            for (int i = 0; i < slotPositions.Length; i++)
            {
                var slotRoot = CreatePrimitive($"Outpost Slot {i + 1}", PrimitiveType.Cylinder, slotPositions[i],
                    new Vector3(1.18f, 0.16f, 1.18f), stoneColor, world, true);
                CreatePrimitive($"Outpost Slot {i + 1} Glow", PrimitiveType.Cylinder, slotPositions[i] + Vector3.up * 0.08f,
                    new Vector3(0.86f, 0.05f, 0.86f), slotColor, slotRoot.transform, false);
                CreatePrimitive($"Outpost Slot {i + 1} Peg", PrimitiveType.Cube, slotPositions[i] + new Vector3(0f, 0.22f, 0.42f),
                    new Vector3(0.12f, 0.28f, 0.12f), new Color(0.08f, 0.24f, 0.32f), slotRoot.transform, false);
                var slot = slotRoot.AddComponent<GreyboxOutpostSlot>();
                slot.Configure(this, i, slotColor);
                slots.Add(slot);
            }

            Vector3[] commandPositions =
            {
                new(3.2f, 0.12f, 3.4f), new(7.4f, 0.12f, 7.2f),
                new(13.2f, 0.12f, 3.8f), new(18.5f, 0.12f, 9f)
            };
            for (int i = 0; i < commandPositions.Length; i++)
            {
                var go = CreatePrimitive($"Command Point {i + 1}", PrimitiveType.Cylinder, commandPositions[i],
                    new Vector3(0.72f, 0.07f, 0.72f), new Color(0.22f, 0.70f, 0.42f), world, true);
                CreatePrimitive($"Command Point {i + 1} Banner Pole", PrimitiveType.Cylinder,
                    commandPositions[i] + new Vector3(-0.18f, 0.48f, 0f),
                    new Vector3(0.08f, 0.82f, 0.08f), new Color(0.12f, 0.18f, 0.15f), go.transform, false);
                CreatePrimitive($"Command Point {i + 1} Banner", PrimitiveType.Cube,
                    commandPositions[i] + new Vector3(0.12f, 0.82f, 0f),
                    new Vector3(0.45f, 0.28f, 0.06f), new Color(0.50f, 0.95f, 0.58f), go.transform, false);
                var point = go.AddComponent<GreyboxCommandPoint>();
                point.Configure(i);
                commandPoints.Add(point);
            }

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
            if (environment != null) environment.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            Vector3[] slotPositions =
            {
                new(2f, 0.15f, 0f), new(2f, 0.15f, 5f),
                new(6.5f, 0.15f, 6f), new(8f, 0.15f, 10.5f),
                new(12f, 0.15f, 1.2f), new(14f, 0.15f, 6f),
                new(18f, 0.15f, 6.8f), new(20.5f, 0.15f, 11.2f)
            };
            for (int i = 0; i < slotPositions.Length; i++)
            {
                GameObject root = StylizedPresentationFactory.CreateRoot($"Outpost Slot {i + 1}", slotPositions[i],
                    new Vector3(1.25f, 0.24f, 1.25f), presentationCatalog.outpostSlotPrefab,
                    presentationCatalog, PresentationCollider.Cylinder);
                root.transform.SetParent(world, true);
                var slot = root.AddComponent<GreyboxOutpostSlot>();
                slot.Configure(this, i, slotColor);
                slots.Add(slot);
            }

            Vector3[] commandPositions =
            {
                new(3.2f, 0.12f, 3.4f), new(7.4f, 0.12f, 7.2f),
                new(13.2f, 0.12f, 3.8f), new(18.5f, 0.12f, 9f)
            };
            for (int i = 0; i < commandPositions.Length; i++)
            {
                GameObject root = StylizedPresentationFactory.CreateRoot($"Command Point {i + 1}", commandPositions[i],
                    new Vector3(0.86f, 0.2f, 0.86f), presentationCatalog.commandPointPrefab,
                    presentationCatalog, PresentationCollider.Cylinder);
                root.transform.SetParent(world, true);
                var point = root.AddComponent<GreyboxCommandPoint>();
                point.Configure(i);
                commandPoints.Add(point);
            }
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
            GameObject go;
            if (presentationCatalog != null)
            {
                go = StylizedPresentationFactory.CreateRoot("Knight Commander",
                    commandPoints[0].transform.position, new Vector3(0.72f, 1.9f, 0.72f),
                    presentationCatalog.ActorPrefab(PresentationActorKind.Knight), presentationCatalog,
                    PresentationCollider.Capsule, presentationCatalog.ActorScale(PresentationActorKind.Knight));
                go.transform.SetParent(transform, true);
            }
            else
            {
                go = CreatePrimitive("Knight Commander", PrimitiveType.Capsule, commandPoints[0].transform.position + Vector3.up,
                    new Vector3(0.75f, 1f, 0.75f), new Color(0.20f, 0.52f, 1f), transform, true);
                CreatePrimitive("Knight Shield", PrimitiveType.Cube, go.transform.position + new Vector3(-0.42f, 0.1f, -0.1f),
                    new Vector3(0.16f, 0.58f, 0.42f), new Color(0.70f, 0.84f, 0.92f), go.transform, false);
                CreatePrimitive("Knight Crest", PrimitiveType.Sphere, go.transform.position + new Vector3(0f, 0.62f, 0f),
                    new Vector3(0.36f, 0.22f, 0.36f), new Color(0.95f, 0.86f, 0.34f), go.transform, false);
            }
            hero = go.AddComponent<GreyboxHero>();
            hero.Configure(this, commandPoints[0].transform.position);
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

            var safeRoot = CreateUiObject("Safe Area", canvasObject.transform);
            Stretch(safeRoot.GetComponent<RectTransform>());
            safeRoot.AddComponent<RuntimeSafeArea>();

            if (presentationCatalog != null)
            {
                GameObject hudObject = Instantiate(presentationCatalog.hudPrefab, safeRoot.transform);
                hudObject.name = "Defender HUD";
                Stretch(hudObject.GetComponent<RectTransform>());
                artHud = hudObject.GetComponent<DefenderHudController>();
                if (artHud == null)
                    throw new InvalidOperationException("Presentation HUD prefab has no DefenderHudController.");
                artHud.Initialize(presentationCatalog, TryBuildSelected, UseHeroAbility, CancelSelection,
                    StartNextWave, OnPausePressed, UpgradeSelected, SellSelected, RestartLevel,
                    () => EmitPresentation(PresentationCue.UiClick, Vector3.zero));
                return;
            }

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
            StartCoroutine(SpawnWave(waves[currentWaveIndex]));
        }

        private IEnumerator SpawnWave(WaveDefinition wave)
        {
            SetPhase(GamePhase.Wave);
            spawning = true;
            plannedThisWave = wave.groups?.Sum(group => Mathf.Max(0, group.count)) ?? 0;
            removedThisWave = 0;
            if (previewText != null) previewText.text = wave.preview;
            artHud?.SetPreview(wave.preview);
            EmitPresentation(PresentationCue.WaveStarted, path[0], value: currentWaveIndex);

            if (plannedThisWave <= 0)
            {
                Debug.LogError($"Wave '{wave.id}' contains no enemies.");
                spawning = false;
                CompleteWave();
                yield break;
            }

            foreach (var group in wave.groups)
            {
                if (group == null || group.count <= 0) continue;
                if (group.startDelay > 0f)
                    yield return new WaitForSeconds(group.startDelay);

                for (int i = 0; i < group.count; i++)
                {
                    SpawnEnemy(group.enemy);
                    yield return new WaitForSeconds(group.interval);
                }
            }

            spawning = false;
            TryCompleteWave();
        }

        private void SpawnEnemy(EnemyArchetype archetype)
        {
            if (path.Count < 2)
            {
                Debug.LogError("Cannot spawn enemy: level path is invalid.");
                removedThisWave++;
                return;
            }

            EnemyStats stats = GetStats(archetype);
            float size = archetype == EnemyArchetype.Boss ? 1.7f : archetype == EnemyArchetype.Siege ? 1.25f : 0.85f;
            GameObject go;
            if (presentationCatalog != null)
            {
                PresentationActorKind actorKind = ActorKind(archetype);
                go = StylizedPresentationFactory.CreateRoot(archetype.ToString(), path[0],
                    new Vector3(size, Mathf.Max(1.4f, size * 1.7f), size), presentationCatalog.ActorPrefab(actorKind),
                    presentationCatalog, PresentationCollider.Capsule, presentationCatalog.ActorScale(actorKind));
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
            string nextPreview = $"Sıradaki: {waves[currentWaveIndex].preview}";
            if (previewText != null) previewText.text = nextPreview;
            artHud?.SetPreview(nextPreview);
            if (currentWaveIndex == 1)
                ShowMessage("Kahramanı yeşil komuta noktalarına gönder.", 5f);
            else if (currentWaveIndex == 2)
                ShowMessage("Kalkan Darbesi kalabalıkları kısa süre durdurur.", 5f);
        }

        private void SetPhase(GamePhase next)
        {
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
                Time.timeScale = 1f;
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

            ShowMessage($"{OutpostDisplayName(kind)} kuruldu.", 2f);
        }

        public bool TryBuildOutpost(int slotIndex, OutpostKind kind)
        {
            if (!initialized || slotIndex < 0 || slotIndex >= slots.Count) return false;
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
                    presentationCatalog, PresentationCollider.Cylinder);
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
            outpost.Configure(this, targetSlot, kind, stats);
            targetSlot.SetOccupant(outpost);
            outposts.Add(outpost);
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
            if (!TryUpgradeOutpost(selectedOutpost.Slot.Index))
            {
                ShowMessage(selectedOutpost.CanUpgrade
                    ? "Yükseltme için yeterli altın yok."
                    : "Bu karakol en yüksek gri kutu seviyesinde.", 2f);
                return;
            }
            RefreshSelectedPanel();
        }

        public bool TryUpgradeOutpost(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= slots.Count) return false;
            GreyboxOutpost outpost = slots[slotIndex]?.Occupant;
            if (outpost == null || !outpost.CanUpgrade || !SpendGold(outpost.UpgradeCost)) return false;
            outpost.Upgrade();
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
            if (slotIndex < 0 || slotIndex >= slots.Count) return false;
            GreyboxOutpost outpost = slots[slotIndex]?.Occupant;
            if (outpost == null) return false;
            int refund = outpost.SellValue;
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
                ShowMessage($"Yetenek {Mathf.CeilToInt(hero.AbilityRemaining)} sn sonra hazır.", 2f);
        }

        private void CancelSelection()
        {
            if (selectedSlot != null) selectedSlot.SetSelected(false);
            selectedSlot = null;
            selectedOutpost = null;
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
                    yield break;
                }
                TryUpgradeOutpost(i);
            }

            hero.CommandTo(commandPoints[1].transform.position);
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
            if (Phase == GamePhase.Victory)
                Debug.Log("[DC_SMOKE_PASS] Build, upgrade, hero, pause and all seven waves completed with victory.");
            else
                Debug.LogError($"[DC_SMOKE_FAIL] Functional run ended in phase {Phase}.");
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
            if (!Physics.Raycast(ray, out RaycastHit hit, 100f)) return;

            var slot = hit.collider.GetComponent<GreyboxOutpostSlot>();
            if (slot != null)
            {
                SelectSlot(slot);
                return;
            }

            var outpost = hit.collider.GetComponent<GreyboxOutpost>();
            if (outpost != null)
            {
                SelectOutpost(outpost);
                return;
            }

            var command = hit.collider.GetComponent<GreyboxCommandPoint>();
            if (command != null && hero != null)
            {
                hero.CommandTo(command.transform.position);
                ShowMessage($"Şövalye komuta noktası {command.Index + 1}'e gidiyor.", 2f);
            }
        }

        private void SelectSlot(GreyboxOutpostSlot slot)
        {
            if (selectedSlot != null) selectedSlot.SetSelected(false);
            selectedSlot = slot;
            selectedSlot.SetSelected(true);
            selectedOutpost = slot.Occupant;
            RefreshSelectedPanel();
        }

        private void SelectOutpost(GreyboxOutpost outpost)
        {
            selectedOutpost = outpost;
            SelectSlot(outpost.Slot);
            selectedOutpost = outpost;
            RefreshSelectedPanel();
        }

        private void RefreshSelectedPanel()
        {
            if (artHud != null)
            {
                if (selectedOutpost != null)
                    artHud.SetSelected(selectedOutpost.Description, selectedOutpost.CanUpgrade, true, true);
                else if (selectedSlot != null)
                    artHud.SetSelected($"KARAKOL PLATFORMU {selectedSlot.Index + 1}\n\nAşağıdan bir karakol seç.", false, false, true);
                else
                    artHud.SetSelected(string.Empty, false, false, false);
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
                selectedText.text = $"KARAKOL PLATFORMU {selectedSlot.Index + 1}\n\nAşağıdan bir karakol seç.";
                upgradeButton.interactable = false;
                sellButton.interactable = false;
            }
            else
            {
                selectedText.text = "PLATFORM SEÇİLMEDİ";
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
                artHud.SetBuildInteractable(Gold >= GetOutpostStats(OutpostKind.Archer).cost,
                    Gold >= GetOutpostStats(OutpostKind.Barracks).cost,
                    Gold >= GetOutpostStats(OutpostKind.Mage).cost);
                if (Phase == GamePhase.Preparing)
                    artHud.SetNextWaveLabel(presentationCatalog.uiCopy.Get(UiTextKey.FirstWave));
                RefreshSelectedPanel();
                return;
            }
            if (goldText == null) return;
            goldText.text = $"ALTIN  {Gold}";
            livesText.text = $"KALE  {Lives}";
            livesText.color = Lives <= 5 ? new Color(1f, 0.35f, 0.3f) : Color.white;
            waveText.text = $"DALGA  {Mathf.Min(currentWaveIndex + (Phase == GamePhase.Wave ? 1 : 0), waves.Length)} / {waves.Length}";
            phaseText.text = Phase switch
            {
                GamePhase.Preparing => "HAZIRLIK",
                GamePhase.Wave => "SAVAŞ",
                GamePhase.Intermission => "MOLA",
                GamePhase.Paused => "DURAKLATILDI",
                GamePhase.Victory => "ZAFER",
                GamePhase.Defeat => "YENİLGİ",
                _ => "YÜKLENİYOR"
            };
            if (hero != null)
            {
                heroText.text = hero.IsAlive
                    ? $"ŞÖVALYE  {Mathf.CeilToInt(hero.HealthFraction * 100f)}%  •  YETENEK {(hero.AbilityRemaining <= 0f ? "HAZIR" : Mathf.CeilToInt(hero.AbilityRemaining) + " sn")}"
                    : $"ŞÖVALYE DÖNÜYOR  {Mathf.CeilToInt(hero.RespawnRemaining)} sn";
                if (heroAbilityButton != null)
                    heroAbilityButton.interactable = hero.IsAlive && hero.AbilityRemaining <= 0f && Phase != GamePhase.Paused;
            }
            if (Phase == GamePhase.Preparing && nextWaveButtonLabel != null)
                nextWaveButtonLabel.text = "İLK DALGAYI BAŞLAT";
            RefreshSelectedPanel();
        }

        private Coroutine messageRoutine;
        private void ShowMessage(string message, float duration)
        {
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

        internal void DamageEnemies(Vector3 position, float radius, int damage, float slowFactor = 1f, float slowDuration = 0f)
        {
            float sqr = radius * radius;
            foreach (var enemy in enemies.ToArray())
            {
                if (enemy == null || !enemy.IsAlive || (enemy.Position - position).sqrMagnitude > sqr) continue;
                enemy.TakeDamage(damage);
                if (slowFactor < 1f && enemy != null && enemy.IsAlive)
                    enemy.ApplySlow(slowFactor, slowDuration);
            }
        }

        internal void HealEnemies(Vector3 position, float radius, int amount, GreyboxEnemy source)
        {
            float sqr = radius * radius;
            foreach (var enemy in enemies)
            {
                if (enemy == null || enemy == source || !enemy.IsAlive || (enemy.Position - position).sqrMagnitude > sqr) continue;
                enemy.Heal(amount);
            }
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
                presentationCatalog, PresentationCollider.Capsule, presentationCatalog.ActorScale(PresentationActorKind.Guard));
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
                visual.transform.localScale = Vector3.one;
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

        internal Vector3 NearestCommandPoint(Vector3 position)
        {
            if (commandPoints.Count == 0) return position;
            return commandPoints.OrderBy(point => (point.transform.position - position).sqrMagnitude).First().transform.position;
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
            if (commandPoints.Count != 4) errors.Add("Greybox level needs exactly four command points.");
            if (waves == null || waves.Length != 7) errors.Add("Greybox level needs six waves and one boss wave.");
            if (waves != null && (waves.Length == 0 || !waves[^1].bossWave)) errors.Add("Final wave must be a boss wave.");
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

        internal static OutpostStats GetOutpostStats(OutpostKind kind) => kind switch
        {
            OutpostKind.Archer => new OutpostStats(75, 55, 22, 4.8f, 1.25f),
            OutpostKind.Barracks => new OutpostStats(100, 70, 14, 1.4f, 1f),
            OutpostKind.Mage => new OutpostStats(125, 85, 38, 4.2f, 0.65f),
            _ => new OutpostStats(75, 55, 20, 4f, 1f)
        };

        internal static string OutpostDisplayName(OutpostKind kind) => kind switch
        {
            OutpostKind.Archer => "OKÇU KARAKOLU",
            OutpostKind.Barracks => "MUHAFIZ KIŞLASI",
            OutpostKind.Mage => "BÜYÜ OCAĞI",
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

    public sealed class GreyboxCommandPoint : MonoBehaviour
    {
        public int Index { get; private set; }
        public void Configure(int index) => Index = index;
    }
}
