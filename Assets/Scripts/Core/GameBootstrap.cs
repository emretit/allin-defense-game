using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using TMPro;
using DefenderChronicles.Data;
using DefenderChronicles.Grid;
using DefenderChronicles.Heroes;
using DefenderChronicles.Towers;
using DefenderChronicles.UI;
using DefenderChronicles.Utils;

namespace DefenderChronicles.Core
{
    /// <summary>
    /// Builds EventSystem, HUD, Resources prefab wiring, and default path when the scene is not hand-authored.
    /// Runs before other gameplay scripts (<see cref="DefaultExecutionOrder"/>).
    /// </summary>
    [DefaultExecutionOrder(-300)]
    public class GameBootstrap : MonoBehaviour
    {
        public static bool RuntimeSceneReady { get; private set; }

        /// <summary>
        /// TMP_Settings.defaultFontAsset can throw if TMP resources are missing; runtime-created objects also lack serialized font refs.
        /// </summary>
        private static void AssignTmpFontIfPossible(TextMeshProUGUI tmp)
        {
            if (tmp == null) return;
            var fromResources = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (fromResources == null)
                fromResources = Resources.Load<TMP_FontAsset>("LiberationSans SDF");
            if (fromResources == null)
                fromResources = Resources.Load<TMP_FontAsset>("TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF");
            if (fromResources != null)
            {
                tmp.font = fromResources;
                return;
            }
            try
            {
                var def = TMP_Settings.defaultFontAsset;
                if (def != null)
                    tmp.font = def;
            }
            catch
            {
                // TMP essentials not configured; leave default (may be invisible)
            }
        }

        private void Awake()
        {
            LockMobileOrientation();
            EnsureCoreSystems();
            EnsureEventSystem();
            EnsureCameraTouch();
            EnsureCanvasAndHud();
            WirePrefabsFromResources();
            EnsureKnightHeroMvp();
            RuntimeSceneReady = true;
        }

        private static void LockMobileOrientation()
        {
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.orientation = ScreenOrientation.LandscapeLeft;
        }

        private static void EnsureCoreSystems()
        {
            FindOrCreate<GameManager>("GameManager");
            FindOrCreate<GridManager>("GridManager");
            GridManager.Instance?.EnsureRuntimeHighlightTemplate();
            FindOrCreate<OutpostSlotManager>("OutpostSlotManager");
            FindOrCreate<CommandPointManager>("CommandPointManager");

            var path = FindOrCreate<PathManager>("PathManager");
            if (path.GetWaypointTransformCount() < 2)
                path.ConfigureRuntimeWaypoints(MapPathPresets.Get(PlayerProgress.SelectedMapIndex));

            FindOrCreate<WaveManager>("WaveManager");
            FindOrCreate<TowerPlacer>("TowerPlacer");
            FindOrCreate<AudioManager>("AudioManager");
        }

        private static T FindOrCreate<T>(string objectName) where T : Component
        {
            var existing = Object.FindAnyObjectByType<T>();
            if (existing != null)
                return existing;
            var go = new GameObject(objectName);
            return go.AddComponent<T>();
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null)
                return;
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();
        }

        private static void EnsureCameraTouch()
        {
            var cam = Camera.main;
            if (cam == null)
                return;
            if (cam.GetComponent<TouchInputHandler>() == null)
                cam.gameObject.AddComponent<TouchInputHandler>();
            if (cam.GetComponent<TowerWorldTapHandler>() == null)
                cam.gameObject.AddComponent<TowerWorldTapHandler>();
            if (cam.GetComponent<HeroCommandController>() == null)
                cam.gameObject.AddComponent<HeroCommandController>();
        }

        private static void EnsureCanvasAndHud()
        {
            if (Object.FindObjectsByType<UIManager>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length > 0)
                return;

            var canvasGo = new GameObject("GameUICanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();
            canvasGo.AddComponent<SafeAreaFitter>();

            var ui = canvasGo.AddComponent<UIManager>();

            var gold = CreateTmpText(canvasGo.transform, "GoldText", new Vector2(140, -36), TextAlignmentOptions.Left);
            var lives = CreateTmpText(canvasGo.transform, "LivesText", new Vector2(140, -76), TextAlignmentOptions.Left);
            var wave = CreateTmpText(canvasGo.transform, "WaveText", new Vector2(140, -116), TextAlignmentOptions.Left);
            var waveCount = CreateTmpText(canvasGo.transform, "WaveCountText", new Vector2(140, -156), TextAlignmentOptions.Left);

            var nextWave = CreateButton(canvasGo.transform, "NextWaveButton", new Vector2(-160, -500), new Vector2(220, 56), "Next wave");
            var pause = CreateButton(canvasGo.transform, "PauseButton", new Vector2(140, -500), new Vector2(160, 56), "Pause");
            var pauseLabel = pause.GetComponentInChildren<TextMeshProUGUI>();
            var cancel = CreateButton(canvasGo.transform, "CancelPlacementButton", new Vector2(380, -500), new Vector2(200, 56), "Cancel build");

            var towerBtnY = -420f;
            var btnArcher = CreateButton(canvasGo.transform, "BtnArcher", new Vector2(-420, towerBtnY), new Vector2(140, 48), "Archer");
            var btnMage = CreateButton(canvasGo.transform, "BtnMage", new Vector2(-270, towerBtnY), new Vector2(140, 48), "Mage");
            var btnWar = CreateButton(canvasGo.transform, "BtnWarrior", new Vector2(-120, towerBtnY), new Vector2(140, 48), "Warrior");
            var btnCat = CreateButton(canvasGo.transform, "BtnCatapult", new Vector2(30, towerBtnY), new Vector2(140, 48), "Catapult");

            var gameOver = CreatePanel(canvasGo.transform, "GameOverPanel", "Game Over", true);
            var victory = CreatePanel(canvasGo.transform, "VictoryPanel", "Victory!", true);

            var waveSlider = CreateHudSlider(canvasGo.transform, "WaveProgressSlider", new Vector2(0, -188), new Vector2(420, 20), 0f, 1f, 0f);
            var musicSlider = CreateHudSlider(canvasGo.transform, "MusicVolumeSlider", new Vector2(-220, -228), new Vector2(200, 18), 0f, 1f, 0.4f);
            var sfxSlider = CreateHudSlider(canvasGo.transform, "SfxVolumeSlider", new Vector2(220, -228), new Vector2(200, 18), 0f, 1f, 0.8f);
            var ml = CreateTmpText(canvasGo.transform, "MusicLabel", new Vector2(-220, -208), TextAlignmentOptions.Center);
            ml.text = "Music";
            ml.fontSize = 22;
            var sl = CreateTmpText(canvasGo.transform, "SfxLabel", new Vector2(220, -208), TextAlignmentOptions.Center);
            sl.text = "SFX";
            sl.fontSize = 22;

            var btnHero = CreateButton(canvasGo.transform, "HeroSpecialButton", new Vector2(-520, -500), new Vector2(200, 48), "Shield Bash");
            var heroCd = CreateTmpText(canvasGo.transform, "HeroCooldownText", new Vector2(-520, -448), TextAlignmentOptions.Center);
            heroCd.text = "";
            heroCd.fontSize = 22;

            EnsureRuntimeTowerInfoPanel(canvasGo.transform);

            ui.ApplyRuntimeBindings(
                gold, lives, wave, nextWave, pause, pauseLabel, cancel, waveCount,
                gameOver, victory,
                btnArcher, btnMage, btnWar, btnCat,
                waveSlider, musicSlider, sfxSlider, btnHero, heroCd);
        }

        private static Slider CreateHudSlider(Transform parent, string name, Vector2 anchored, Vector2 size, float min, float max, float value)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = anchored;
            rt.sizeDelta = size;
            go.AddComponent<Image>().color = new Color(0.12f, 0.14f, 0.2f, 0.92f);
            var slider = go.AddComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = value;
            slider.targetGraphic = go.GetComponent<Image>();

            var fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(go.transform, false);
            var far = fillArea.AddComponent<RectTransform>();
            far.anchorMin = Vector2.zero;
            far.anchorMax = Vector2.one;
            far.offsetMin = new Vector2(6, 4);
            far.offsetMax = new Vector2(-6, -4);
            var fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            var fr = fill.AddComponent<RectTransform>();
            fr.anchorMin = Vector2.zero;
            fr.anchorMax = Vector2.one;
            fr.offsetMin = Vector2.zero;
            fr.offsetMax = Vector2.zero;
            fill.AddComponent<Image>().color = new Color(0.25f, 0.55f, 0.85f, 1f);
            slider.fillRect = fr;
            return slider;
        }

        private static Button CreateTopAnchoredButton(Transform parent, string name, Vector2 anchored, Vector2 size, string label)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = anchored;
            rt.sizeDelta = size;
            go.AddComponent<Image>().color = new Color(0.2f, 0.25f, 0.35f, 0.95f);
            var btn = go.AddComponent<Button>();

            var textGo = new GameObject("Label");
            textGo.transform.SetParent(go.transform, false);
            var trt = textGo.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 24;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.text = label;
            AssignTmpFontIfPossible(tmp);

            return btn;
        }

        private static TextMeshProUGUI CreateTopAnchoredTmp(Transform parent, string name, Vector2 anchored, Vector2 size, TextAlignmentOptions align)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = anchored;
            rt.sizeDelta = size;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 26;
            tmp.alignment = align;
            tmp.text = "";
            AssignTmpFontIfPossible(tmp);
            return tmp;
        }

        private static void EnsureRuntimeTowerInfoPanel(Transform canvasRoot)
        {
            if (Object.FindObjectsByType<TowerInfoPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length > 0)
                return;

            var root = new GameObject("TowerInfoRoot");
            root.transform.SetParent(canvasRoot, false);
            var rrt = root.AddComponent<RectTransform>();
            rrt.anchorMin = new Vector2(1f, 0.5f);
            rrt.anchorMax = new Vector2(1f, 0.5f);
            rrt.pivot = new Vector2(1f, 0.5f);
            rrt.anchoredPosition = new Vector2(-16, 40);
            rrt.sizeDelta = new Vector2(340, 380);

            var panelGo = new GameObject("Panel");
            panelGo.transform.SetParent(root.transform, false);
            var prt = panelGo.AddComponent<RectTransform>();
            prt.anchorMin = Vector2.zero;
            prt.anchorMax = Vector2.one;
            prt.offsetMin = Vector2.zero;
            prt.offsetMax = Vector2.zero;
            panelGo.AddComponent<Image>().color = new Color(0.08f, 0.1f, 0.14f, 0.94f);

            var tip = root.AddComponent<TowerInfoPanel>();
            float ty = -20f;
            const float line = 34f;
            var nameT = CreateTopAnchoredTmp(panelGo.transform, "TowerName", new Vector2(16, ty), new Vector2(300, 30), TextAlignmentOptions.Left);
            ty -= line;
            var lvlT = CreateTopAnchoredTmp(panelGo.transform, "TowerLevel", new Vector2(16, ty), new Vector2(300, 30), TextAlignmentOptions.Left);
            ty -= line;
            var dmgT = CreateTopAnchoredTmp(panelGo.transform, "TowerDmg", new Vector2(16, ty), new Vector2(300, 30), TextAlignmentOptions.Left);
            ty -= line;
            var rngT = CreateTopAnchoredTmp(panelGo.transform, "TowerRange", new Vector2(16, ty), new Vector2(300, 30), TextAlignmentOptions.Left);
            ty -= line;
            var spdT = CreateTopAnchoredTmp(panelGo.transform, "TowerSpeed", new Vector2(16, ty), new Vector2(300, 30), TextAlignmentOptions.Left);
            ty -= line + 8f;
            var btnUp = CreateTopAnchoredButton(panelGo.transform, "UpgradeBtn", new Vector2(20, ty), new Vector2(300, 40), "Upgrade");
            var upLabel = btnUp.GetComponentInChildren<TextMeshProUGUI>();
            ty -= 48f;
            var btnSell = CreateTopAnchoredButton(panelGo.transform, "SellBtn", new Vector2(20, ty), new Vector2(300, 40), "Sell");
            var sellLabel = btnSell.GetComponentInChildren<TextMeshProUGUI>();
            ty -= 48f;
            var btnClose = CreateTopAnchoredButton(panelGo.transform, "CloseBtn", new Vector2(20, ty), new Vector2(300, 36), "Close");

            tip.BindRuntime(panelGo, nameT, lvlT, dmgT, rngT, spdT, btnUp, upLabel, btnSell, sellLabel, btnClose);
            panelGo.SetActive(false);
        }

        private static void EnsureKnightHeroMvp()
        {
            if (UnityEngine.Object.FindAnyObjectByType<KnightHero>() != null)
                return;

            var go = new GameObject("KnightHero");
            go.transform.position = new Vector3(9f, 3f, 0f);
            go.AddComponent<CircleCollider2D>().radius = 0.42f;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.color = new Color(0.35f, 0.55f, 1f);
            var tex = Texture2D.whiteTexture;
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 6f);

            var kh = go.AddComponent<KnightHero>();
            var hd = new HeroData
            {
                heroName = "Knight",
                heroType = HeroType.Knight,
                cost = 0,
                attackRange = 2.8f,
                attackSpeed = 1.1f,
                damage = 14,
                maxHealth = 220,
                level = 1,
                experienceToNextLevel = 60,
                specialAbilityName = "Shield Bash",
                specialAbilityDescription = "Damages and stuns nearby enemies."
            };
            kh.ApplyRuntimeHeroData(hd);
            kh.ConfigureCombatLayers(~0);
        }

        private static TextMeshProUGUI CreateTmpText(Transform parent, string name, Vector2 anchored, TextAlignmentOptions align)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = anchored;
            rt.sizeDelta = new Vector2(400, 44);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 32;
            tmp.alignment = align;
            tmp.text = "0";
            AssignTmpFontIfPossible(tmp);
            return tmp;
        }

        private static Button CreateButton(Transform parent, string name, Vector2 anchored, Vector2 size, string label)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0);
            rt.anchorMax = new Vector2(0.5f, 0);
            rt.pivot = new Vector2(0.5f, 0);
            rt.anchoredPosition = anchored;
            rt.sizeDelta = size;
            var img = go.AddComponent<Image>();
            img.color = new Color(0.2f, 0.25f, 0.35f, 0.95f);
            var btn = go.AddComponent<Button>();

            var textGo = new GameObject("Label");
            textGo.transform.SetParent(go.transform, false);
            var trt = textGo.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 26;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.text = label;
            AssignTmpFontIfPossible(tmp);

            return btn;
        }

        private static GameObject CreatePanel(Transform parent, string name, string title, bool addRestart)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0.75f);

            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(go.transform, false);
            var trt = titleGo.AddComponent<RectTransform>();
            trt.anchorMin = new Vector2(0.5f, 0.5f);
            trt.anchorMax = new Vector2(0.5f, 0.5f);
            trt.sizeDelta = new Vector2(600, 120);
            trt.anchoredPosition = new Vector2(0, 40);
            var tmp = titleGo.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 48;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.text = title;
            AssignTmpFontIfPossible(tmp);

            if (addRestart)
            {
                var rbGo = new GameObject("RestartButton");
                rbGo.transform.SetParent(go.transform, false);
                var rrt = rbGo.AddComponent<RectTransform>();
                rrt.anchorMin = new Vector2(0.5f, 0.5f);
                rrt.anchorMax = new Vector2(0.5f, 0.5f);
                rrt.sizeDelta = new Vector2(240, 52);
                rrt.anchoredPosition = new Vector2(0, -60);
                rbGo.AddComponent<Image>().color = new Color(0.25f, 0.45f, 0.35f, 1f);
                var rb = rbGo.AddComponent<Button>();
                rb.onClick.AddListener(() =>
                {
                    if (UIManager.Instance != null)
                        UIManager.Instance.OnRestartButton();
                });
                var lbl = new GameObject("Label");
                lbl.transform.SetParent(rbGo.transform, false);
                var lrt = lbl.AddComponent<RectTransform>();
                lrt.anchorMin = Vector2.zero;
                lrt.anchorMax = Vector2.one;
                lrt.offsetMin = Vector2.zero;
                lrt.offsetMax = Vector2.zero;
                var lt = lbl.AddComponent<TextMeshProUGUI>();
                lt.fontSize = 28;
                lt.alignment = TextAlignmentOptions.Center;
                lt.text = "Restart";
                AssignTmpFontIfPossible(lt);
            }

            go.SetActive(false);
            return go;
        }

        private static void WirePrefabsFromResources()
        {
            if (WaveManager.Instance != null)
                WaveManager.Instance.ApplyRuntimeDefaultsFromResources();
            if (TowerPlacer.Instance != null)
                TowerPlacer.Instance.ApplyRuntimeDefaultsFromResources();
        }
    }
}
