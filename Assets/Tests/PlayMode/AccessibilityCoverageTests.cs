using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using AllinDefense.Core;
using AllinDefense.Presentation;
using AllinDefense.UI;
using AllinDefense.UI.Kit;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace AllinDefense.Tests
{
    public sealed class AccessibilityCoverageTests
    {
        [Serializable] private sealed class ScreenCoverage
        {
            public string screen;
            public int interactive;
            public int labelled;
            public int languageChanged;
            public int activationVerified;
            public float coveragePercent;
            public List<string> missing = new();
        }
        [Serializable] private sealed class CoverageReport
        {
            public string status;
            public string environment;
            public string simulator = "coordinator";
            public List<ScreenCoverage> screens = new();
        }
        private AppFlowController flow;
        private GameSessionController session;
        private string savePath;
        private GameLanguage originalLanguage;
        private bool originalMotion;
        private CoverageReport report;
        private readonly List<string> errors = new();
        private static object Call(object target, string method, params object[] args) =>
            target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            originalLanguage = FrontendPreferences.Language;
            originalMotion = FrontendPreferences.ReducedMotion;
            FrontendPreferences.Language = GameLanguage.Turkish;
            FrontendPreferences.ReducedMotion = true;
            savePath = Path.Combine(Path.GetTempPath(), $"dc-a11y-{Guid.NewGuid():N}.json");
            var catalog = Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog");
            session = new GameObject("Accessibility Session").AddComponent<GameSessionController>();
            flow = new GameObject("Accessibility Flow").AddComponent<AppFlowController>();
            flow.Initialize(catalog, session, false, savePath);
            yield return new WaitForSecondsRealtime(.95f);
            Call(flow, "CompleteLocalGuestSignIn");
            yield return null;
            report = new CoverageReport { environment = $"Unity Editor {Application.unityVersion}; native iOS callbacks share the tested Invoke/Adjust methods" };
            errors.Clear();
        }
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (flow != null) Object.Destroy(flow.gameObject);
            if (session != null) Object.Destroy(session.gameObject);
            yield return null;
            FrontendPreferences.Language = originalLanguage;
            FrontendPreferences.ReducedMotion = originalMotion;
            Time.timeScale = 1;
            if (File.Exists(savePath)) File.Delete(savePath);
        }
        private Transform Screen(AppScreen screen) => flow.GetComponentsInChildren<CanvasGroup>(true).First(group => group.name == screen.ToString()).transform;
        private static Component[] Controls(Transform root) => root.GetComponentsInChildren<Component>()
            .Where(component => component is Button || component is Slider || component is Toggle || component is UiSegmentedControl).ToArray();
        private static string PathFor(Transform root, Transform target)
        {
            string path = target.name;
            while (target != root && target.parent != null) { target = target.parent; path = target.name + "/" + path; }
            return path;
        }
        private IEnumerator Audit(string name, Transform root)
        {
            FrontendPreferences.Language = GameLanguage.Turkish;
            yield return null;
            var sample = new ScreenCoverage { screen = name };
            report.screens.Add(sample);
            var labels = new Dictionary<string, string>();
            foreach (var control in Controls(root))
            {
                sample.interactive++;
                string path = PathFor(root, control.transform);
                var accessible = control.GetComponent<UiAccessibleButton>();
                if (accessible == null || string.IsNullOrWhiteSpace(accessible.Label)) { sample.missing.Add(path + ": Turkish label/component missing"); continue; }
                labels[path] = accessible.Label;
                if (!accessible.IsAccessible) sample.missing.Add(path + ": excluded from active scope");
                if (control is Button button)
                {
                    var original = button.onClick;
                    int invoked = 0;
                    button.onClick = new Button.ButtonClickedEvent();
                    button.onClick.AddListener(() => invoked++);
                    bool expected = button.IsInteractable();
                    try
                    {
                        if (accessible.Invoke() != expected || invoked != (expected ? 1 : 0)) sample.missing.Add(path + ": activation mismatch");
                        else sample.activationVerified++;
                    }
                    finally { button.onClick = original; }
                }
                else if (control is Slider slider)
                {
                    float originalValue = slider.value;
                    var original = slider.onValueChanged;
                    int changed = 0;
                    slider.onValueChanged = new Slider.SliderEvent();
                    slider.onValueChanged.AddListener(_ => changed++);
                    bool expected = slider.IsInteractable();
                    try
                    {
                        bool adjusted = accessible.Adjust(slider.normalizedValue >= .5f ? -1 : 1);
                        if (adjusted != expected || changed != (expected ? 1 : 0)) sample.missing.Add(path + ": adjustment mismatch");
                        else sample.activationVerified++;
                    }
                    finally { slider.SetValueWithoutNotify(originalValue); slider.onValueChanged = original; }
                }
                else if (control is Toggle toggle)
                {
                    bool originalValue = toggle.isOn;
                    bool expected = toggle.IsInteractable();
                    if (accessible.Invoke() != expected || toggle.isOn != (expected ? !originalValue : originalValue)) sample.missing.Add(path + ": toggle activation mismatch");
                    else sample.activationVerified++;
                    toggle.SetIsOnWithoutNotify(originalValue);
                }
            }
            FrontendPreferences.Language = GameLanguage.English;
            yield return null;
            foreach (var control in Controls(root))
            {
                string path = PathFor(root, control.transform);
                var accessible = control.GetComponent<UiAccessibleButton>();
                if (accessible == null || string.IsNullOrWhiteSpace(accessible.Label)) { sample.missing.Add(path + ": English label/component missing"); continue; }
                if (!labels.TryGetValue(path, out string turkish)) { sample.missing.Add(path + ": Turkish counterpart missing"); continue; }
                sample.labelled++;
                if (accessible.Label == turkish) sample.missing.Add(path + ": label did not change language");
                else sample.languageChanged++;
            }
            foreach (var segments in Controls(root).OfType<UiSegmentedControl>())
            {
                var language = FrontendPreferences.Language;
                int originalIndex = segments.Selected;
                if (!segments.GetComponent<UiAccessibleButton>().Invoke() || segments.Selected == originalIndex)
                    sample.missing.Add(PathFor(root, segments.transform) + ": segment activation mismatch");
                else sample.activationVerified++;
                segments.Select(originalIndex, false);
                FrontendPreferences.Language = language;
            }
            sample.coveragePercent = sample.interactive == 0 ? 0 : 100f * sample.labelled / sample.interactive;
            if (sample.interactive == 0) sample.missing.Add(name + ": no controls audited");
            errors.AddRange(sample.missing.Select(missing => name + ": " + missing));
            FrontendPreferences.Language = GameLanguage.Turkish;
            yield return null;
        }
        private void ModalOnly(Transform owner, Transform modal)
        {
            foreach (var accessible in owner.GetComponentsInChildren<UiAccessibleButton>())
            {
                if (string.IsNullOrWhiteSpace(accessible.Label)) continue;
                bool expected = accessible.transform.IsChildOf(modal);
                Assert.That(accessible.IsAccessible, Is.EqualTo(expected), PathFor(owner, accessible.transform));
                if (!expected) Assert.That(accessible.Invoke(), Is.False, "Background activation must fail.");
            }
        }
        private void CaptureSettings()
        {
            var canvas = flow.GetComponentInChildren<Canvas>();
            var originalMode = canvas.renderMode;
            var originalCamera = canvas.worldCamera;
            float originalDistance = canvas.planeDistance;
            var camera = new GameObject("Accessibility Capture Camera").AddComponent<Camera>();
            var target = new RenderTexture(1080, 1920, 24);
            var texture = new Texture2D(1080, 1920, TextureFormat.RGB24, false);
            var originalTarget = RenderTexture.active;
            try
            {
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = UiTokens.Night;
                camera.orthographic = true;
                camera.targetTexture = target;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1;
                Canvas.ForceUpdateCanvases();
                camera.Render();
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                texture.Apply();
                File.WriteAllBytes("Builds/a11y/settings.png", texture.EncodeToPNG());
            }
            finally
            {
                canvas.renderMode = originalMode;
                canvas.worldCamera = originalCamera;
                canvas.planeDistance = originalDistance;
                RenderTexture.active = originalTarget;
                camera.targetTexture = null;
                Object.Destroy(camera.gameObject); Object.Destroy(texture); Object.Destroy(target);
                Canvas.ForceUpdateCanvases();
            }
        }
        [UnityTest]
        public IEnumerator FrontendRouteHasBilingualAccessibleControlsAndModalIsolation()
        {
            try
            {
                yield return Audit("main-menu", Screen(AppScreen.MainMenu));
                Call(flow, "OpenCampaign");
                yield return null; yield return null;
                Call(flow, "EnsureRegion", 0);
                yield return Audit("map-region-1", Screen(AppScreen.Campaign));
                Call(flow, "FocusRegion", 1);
                yield return null; yield return null;
                Call(flow, "EnsureRegion", 1);
                var level12 = UiQuery.FindById<Button>(flow, "Level 12").GetComponent<UiAccessibleButton>();
                Assert.That(level12.Label, Is.EqualTo("Bölüm 12, Maden Yolu, kilitli"));
                flow.Progress.Data.unlockedLevel = 20;
                flow.Progress.Data.levels.Add(new CampaignLevelProgress { level = 10, stars = 1 });
                flow.Progress.Data.levels.Add(new CampaignLevelProgress { level = 12, stars = 2 });
                Call(flow, "RefreshLevelButtons");
                Assert.That(level12.Label, Is.EqualTo("Bölüm 12, Maden Yolu, 2 yıldız"));
                Assert.That(level12.IsAccessible, Is.True);
                var secondRegion = flow.GetComponentsInChildren<RectTransform>().First(rect => rect.name == "Region 2");
                Assert.That(Controls(secondRegion).OfType<Button>().Count(), Is.EqualTo(10));
                yield return Audit("map-region-2", Screen(AppScreen.Campaign));
                Call(flow, "FocusRegion", 0);
                Call(flow, "SelectLevel", 1);
                Call(flow, "ShowBriefingSheet");
                yield return Audit("briefing", UiQuery.FindById(flow, "Selection Bar"));
                Call(flow, "ShowBriefingEnemy", AllinDefense.Data.EnemyArchetype.Raider);
                yield return null;
                var enemyModal = UiQuery.FindById(flow, "New Enemy");
                ModalOnly(flow.transform, enemyModal);
                yield return Audit("briefing-enemy-modal", enemyModal);
                Call(flow, "AcknowledgeEnemy");
                yield return null;
                Call(flow, "OpenSettings");
                yield return null;
                var settings = UiQuery.FindById(flow, "Settings Modal");
                ModalOnly(flow.transform, settings);
                yield return Audit("settings", settings);
                Directory.CreateDirectory("Builds/a11y");
                CaptureSettings();
                Call(flow, "CloseSettings");
                Assert.That(UiQuery.FindById<Button>(flow, "Play Selected").GetComponent<UiAccessibleButton>().IsAccessible, Is.True);
                Call(flow, "ShowScreen", AppScreen.Login);
                yield return Audit("account", Screen(AppScreen.Login));
                Call(flow, "ConfirmAccountDeletion");
                yield return null;
                var deleteModal = UiQuery.FindById(flow, "Confirm Account Deletion");
                ModalOnly(flow.transform, deleteModal);
                yield return Audit("account-deletion-modal", deleteModal);
                Call(flow, "CloseAccountDeletion");
                Call(flow, "ShowScreen", AppScreen.MainMenu);
                Call(flow, "OpenHeroName");
                yield return null;
                var heroModal = flow.GetComponentsInChildren<RectTransform>().First(rect => rect.name == "Hero Name Modal");
                ModalOnly(flow.transform, heroModal);
                yield return Audit("hero-modal", heroModal);
                Call(flow, "CloseHeroName");
                session.SetCampaignLevel(1);
                Call(flow, "ShowResult", true, 3, new CampaignCompletionReward(27, 68));
                yield return new WaitForSecondsRealtime(1.5f);
                yield return Audit("victory", Screen(AppScreen.Result));
                Call(flow, "ShowResult", false, 0, default(CampaignCompletionReward));
                yield return Audit("defeat", Screen(AppScreen.Result));
                Call(flow, "ShowScreen", AppScreen.Battle);
                flow.GetComponent<AudioListener>().enabled = false;
                session.ConfigurePresentation(Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog"));
                session.Initialize();
                var hud = session.GetComponentInChildren<DefenderHudController>();
                session.TogglePause(); hud.Refresh(session);
                yield return null;
                var pause = UiQuery.FindById<UiOverlay>(hud, "PauseOverlay");
                ModalOnly(hud.transform, pause.transform);
                yield return Audit("pause", pause.transform);
                var confirmation = UiQuery.FindById<UiOverlay>(hud, "MapConfirmation");
                confirmation.transform.SetAsLastSibling(); confirmation.Show();
                ModalOnly(hud.transform, confirmation.transform);
                yield return Audit("pause-map-confirmation", confirmation.transform);
                confirmation.Hide(); pause.Hide();
                Assert.That(errors, Is.Empty, string.Join("\n", errors));
            }
            finally
            {
                report.status = errors.Count == 0 && report.screens.Count == 13 ? "passed" : "failed";
                Directory.CreateDirectory("Builds/a11y");
                File.WriteAllText("Builds/a11y/report.json", JsonUtility.ToJson(report, true));
            }
        }
        [UnityTest]
        public IEnumerator AdjustableControlsExposeRolesValuesAndPerformTheirActions()
        {
            float music = FrontendPreferences.MusicVolume;
            bool haptics = FrontendPreferences.Haptics;
            try
            {
                Call(flow, "OpenSettings");
                yield return null;
                var slider = UiQuery.FindById(flow, "MÜZİK Row").GetComponentInChildren<Slider>();
                var accessible = slider.GetComponent<UiAccessibleButton>();
                slider.value = .7f;
                Assert.That(accessible.Role, Is.EqualTo(AccessibilityRole.Slider));
                Assert.That(accessible.Value, Is.EqualTo("yüzde 70"));
                Assert.That(accessible.Adjust(1), Is.True);
                Assert.That(FrontendPreferences.MusicVolume, Is.EqualTo(.8f).Within(.001f));
                Assert.That(accessible.Adjust(-1), Is.True);
                Assert.That(FrontendPreferences.MusicVolume, Is.EqualTo(.7f).Within(.001f));
                slider.value = 1;
                Assert.That(accessible.Adjust(1), Is.False);
                var control = UiQuery.FindById(flow, "TİTREŞİM Row").GetComponentInChildren<UiSwitch>();
                accessible = control.GetComponent<UiAccessibleButton>();
                Assert.That(accessible.Role, Is.EqualTo(AccessibilityRole.Toggle));
                Assert.That(accessible.Invoke(), Is.True);
                Assert.That(control.Value, Is.EqualTo(!haptics));
                Assert.That(accessible.IsSelected, Is.EqualTo(control.Value));
                Assert.That(accessible.Value, Is.EqualTo((control.Value ? FrontendCopy.SwitchOn : FrontendCopy.SwitchOff).Value));
                var segments = UiQuery.FindById<UiSegmentedControl>(flow, "Language Segments");
                accessible = segments.GetComponent<UiAccessibleButton>();
                Assert.That(accessible.Role, Is.EqualTo(AccessibilityRole.TabBar));
                Assert.That(accessible.Adjust(1), Is.True);
                Assert.That(FrontendPreferences.Language, Is.EqualTo(GameLanguage.English));
                Assert.That(accessible.Value, Is.EqualTo("English"));
                var english = segments.SelectedButton.GetComponent<UiAccessibleButton>();
                Assert.That(english.Role, Is.EqualTo(AccessibilityRole.TabButton));
                Assert.That(english.IsSelected, Is.True);
                Assert.That(accessible.Invoke(), Is.True);
                Assert.That(FrontendPreferences.Language, Is.EqualTo(GameLanguage.Turkish));
                var toggleRoot = UiKit.Node(segments.transform.parent, "Test Toggle");
                var toggle = toggleRoot.gameObject.AddComponent<Toggle>();
                accessible = toggleRoot.gameObject.AddComponent<UiAccessibleButton>();
                accessible.Configure(FrontendCopy.Haptics);
                Assert.That(accessible.Role, Is.EqualTo(AccessibilityRole.Toggle));
                Assert.That(accessible.Invoke(), Is.True);
                Assert.That(toggle.isOn, Is.True);
                toggle.interactable = false;
                Assert.That(accessible.Invoke(), Is.False);
                Object.Destroy(toggleRoot.gameObject);
            }
            finally { FrontendPreferences.MusicVolume = music; FrontendPreferences.Haptics = haptics; }
        }
    }
}
