using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using AllinDefense.Core;
using AllinDefense.Presentation;
using AllinDefense.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace AllinDefense.Tests
{
    public sealed class FrontendExperienceTests
    {
        private AppFlowController flow;
        private GameSessionController session;
        private string savePath;

        [Serializable] public sealed class Sample
        {
            public string route;
            public bool cold;
            public double usableMs;
            public float longestFrameMs;
            public long allocatedBytes;
        }
        [Serializable] public sealed class Report
        {
            public string environment;
            public List<Sample> samples = new();
            public long warmMemory;
            public long finalMemory;
        }

        private static object Call(object target, string name, params object[] args) =>
            target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            savePath = Path.Combine(Path.GetTempPath(), $"dc-frontend-{Guid.NewGuid():N}.json");
            var catalog = Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog");
            session = new GameObject("Frontend Acceptance Session").AddComponent<GameSessionController>();
            flow = new GameObject("Frontend Acceptance Flow").AddComponent<AppFlowController>();
            flow.Initialize(catalog, session, false, savePath);
            yield return new WaitForSecondsRealtime(0.95f);
            Call(flow, "CompleteLocalGuestSignIn");
            yield return new WaitForSecondsRealtime(0.25f);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (flow != null) Object.Destroy(flow.gameObject);
            if (session != null) Object.Destroy(session.gameObject);
            Time.timeScale = 1f;
            yield return null;
            if (File.Exists(savePath)) File.Delete(savePath);
        }

        [UnityTest]
        public IEnumerator MenuTimingThirtyRoundTrips()
        {
            var report = new Report { environment = $"Unity Editor {Application.unityVersion}; {Screen.width}x{Screen.height}; not physical iOS" };
            for (int i = 0; i < 31; i++)
            {
                yield return Measure("menu-campaign", i == 0, () => Call(flow, "OpenCampaign"), report);
                yield return Measure("campaign-menu", i == 0, () => Call(flow, "ShowScreen", AppScreen.MainMenu), report);
                yield return Measure("menu-settings", i == 0, () => Call(flow, "OpenSettings"), report);
                Call(flow, "CloseSettings");
                if (i == 0) report.warmMemory = GC.GetTotalMemory(true);
            }
            report.finalMemory = GC.GetTotalMemory(true);
            string name = Environment.GetEnvironmentVariable("DC_FRONTEND_BENCHMARK_LABEL") ?? "current";
            Directory.CreateDirectory("Builds/FrontendExperience");
            File.WriteAllText($"Builds/FrontendExperience/{name}-timing.json", JsonUtility.ToJson(report, true));
            Assert.That(flow.CurrentScreen, Is.EqualTo(AppScreen.MainMenu));
        }

        [UnityTest]
        public IEnumerator ScreensAreLazyCachedAndRapidNavigationResetsInterruptedAnimations()
        {
            Assert.That(flow.GetComponentsInChildren<UnityEngine.UI.ScrollRect>(true).Length, Is.Zero);
            Call(flow, "OpenCampaign");
            yield return null;
            yield return null; // The first region is intentionally built after the map shell paints.
            var map = flow.GetComponentsInChildren<UnityEngine.UI.ScrollRect>(true).Single(r => r.name == "Campaign Scroll");
            int count = flow.GetComponentsInChildren<Transform>(true).Length;
            for (int i = 0; i < 8; i++)
            {
                Call(flow, "ShowScreen", AppScreen.MainMenu);
                Call(flow, "OpenCampaign");
                Call(flow, "OpenCampaign");
            }
            yield return new WaitForSecondsRealtime(.25f);
            Assert.That(flow.CurrentScreen, Is.EqualTo(AppScreen.Campaign));
            Assert.That(flow.GetComponentsInChildren<Transform>(true).Length, Is.EqualTo(count));
            Assert.That(map, Is.SameAs(flow.GetComponentsInChildren<UnityEngine.UI.ScrollRect>().Single(r => r.name == "Campaign Scroll")));
            foreach (var group in flow.GetComponentsInChildren<CanvasGroup>())
            {
                Assert.That(group.alpha, Is.EqualTo(1));
                Assert.That(group.interactable, Is.True);
                Assert.That(((RectTransform)group.transform).anchoredPosition, Is.EqualTo(Vector2.zero));
            }
            Assert.That(flow.GetComponentsInChildren<Camera>(true).Length, Is.Zero, "Frontend artwork needs no extra camera");
        }

        [UnityTest]
        public IEnumerator AccountAndSettingsReturnToMapWithoutLosingScroll()
        {
            Call(flow, "OpenCampaign");
            yield return new WaitForSecondsRealtime(.25f);
            var map = flow.GetComponentsInChildren<UnityEngine.UI.ScrollRect>().Single(r => r.name == "Campaign Scroll");
            // The closed test shows a single region page, so there is no horizontal offset to restore.
            bool multiplePages = map.content.rect.width > map.viewport.rect.width + 1f;
            if (multiplePages) map.horizontalNormalizedPosition = .5f;
            float expectedHorizontal = map.horizontalNormalizedPosition;
            var route = flow.GetComponentsInChildren<FrontendRegionScrollRect>().First();
            route.verticalNormalizedPosition = .3f;
            Call(flow, "OpenSettings");
            var account = flow.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b => b.name == "Settings Account");
            account.onClick.Invoke();
            Call(flow, "ReturnFromAccount");
            yield return new WaitForSecondsRealtime(.25f);
            Assert.That(flow.CurrentScreen, Is.EqualTo(AppScreen.Campaign));
            Assert.That(map.horizontalNormalizedPosition, Is.EqualTo(expectedHorizontal).Within(.01));
            Assert.That(route.verticalNormalizedPosition, Is.EqualTo(.3f).Within(.02));
            Call(flow, "OpenBriefing");
            Assert.That(flow.CurrentScreen, Is.EqualTo(AppScreen.Campaign));
        }

        [UnityTest]
        public IEnumerator ContinueStartsOnceAndReturnsToMenu()
        {
            Call(flow, "ContinueCampaign");
            Call(flow, "ContinueCampaign");
            flow.LaunchCampaignLevel(2);
            Assert.That(flow.IsLaunchingBattle, Is.True);
            double deadline = Time.realtimeSinceStartupAsDouble + 15;
            while (flow.IsLaunchingBattle && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
            Assert.That(flow.CurrentScreen, Is.EqualTo(AppScreen.Battle));
            Assert.That(session.CampaignLevelNumber, Is.EqualTo(1));
            Assert.That(session.IsInitialized, Is.True);
            Assert.That(session.Phase, Is.EqualTo(GamePhase.Preparing));
            flow.ReturnToMainMenu();
            yield return new WaitForSecondsRealtime(.25f);
            Assert.That(flow.CurrentScreen, Is.EqualTo(AppScreen.MainMenu));
            Assert.That(Time.timeScale, Is.Zero);
        }

        [UnityTest]
        public IEnumerator ContinueUsesLastMapSelectionAndLateAuthenticationKeepsBattleOpen()
        {
            flow.Progress.CompleteLevel(1, 3, 20);
            Call(flow, "OpenCampaign");
            Call(flow, "SelectLevel", 2);
            Assert.That(flow.Progress.Data.lastSelectedLevel, Is.EqualTo(2));
            Call(flow, "ShowScreen", AppScreen.MainMenu);
            Call(flow, "ContinueCampaign");
            double deadline = Time.realtimeSinceStartupAsDouble + 15;
            while (flow.IsLaunchingBattle && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
            Assert.That(session.CampaignLevelNumber, Is.EqualTo(2));
            Assert.That(flow.CurrentScreen, Is.EqualTo(AppScreen.Battle));
            flow.CompleteGuestFallback();
            flow.CompleteServiceSignIn(PlayerAuthProvider.Guest, "test-guest", "Test Defender");
            Assert.That(flow.CurrentScreen, Is.EqualTo(AppScreen.Battle));
        }

        private IEnumerator Measure(string route, bool cold, Action action, Report report)
        {
            long allocated = GC.GetAllocatedBytesForCurrentThread();
            double start = Time.realtimeSinceStartupAsDouble;
            action();
            float maxFrame = 0;
            yield return null;
            while (flow.GetComponent<FrontendNavigation>().IsTransitioning)
            {
                maxFrame = Mathf.Max(maxFrame, Time.unscaledDeltaTime * 1000f);
                Assert.That(Time.realtimeSinceStartupAsDouble - start, Is.LessThan(5), "Menu transition stuck");
                yield return null;
            }
            report.samples.Add(new Sample { route = route, cold = cold,
                usableMs = (Time.realtimeSinceStartupAsDouble - start) * 1000,
                longestFrameMs = Mathf.Max(maxFrame, Time.unscaledDeltaTime * 1000f),
                allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocated });
        }

        [UnityTest]
        public IEnumerator SafeAreaRecoversOverwrittenBoundsWithoutAScreenChange()
        {
            var fitter = flow.GetComponentInChildren<SafeAreaFitter>();
            var rect = (RectTransform)fitter.transform;
            Rect safeArea = Screen.safeArea;
            var screenSize = new Vector2(Screen.width, Screen.height);
            var expectedMin = safeArea.position / screenSize;
            var expectedMax = safeArea.max / screenSize;

            // Reproduce layout setup overwriting the bounds after the fitter's Awake.
            rect.anchorMin = expectedMin + new Vector2(.01f, .01f);
            rect.anchorMax = expectedMax - new Vector2(.01f, .01f);
            rect.offsetMin = new Vector2(10, 20);
            rect.offsetMax = new Vector2(-10, -20);

            // A normal frame must recover without a capture or forced canvas refresh.
            yield return null;
            yield return null;

            Assert.That(Screen.safeArea, Is.EqualTo(safeArea));
            Assert.That(new Vector2(Screen.width, Screen.height), Is.EqualTo(screenSize));
            Assert.That(Vector2.Distance(rect.anchorMin, expectedMin), Is.LessThan(.0001f));
            Assert.That(Vector2.Distance(rect.anchorMax, expectedMax), Is.LessThan(.0001f));
            Assert.That(rect.offsetMin.sqrMagnitude, Is.LessThan(.0001f));
            Assert.That(rect.offsetMax.sqrMagnitude, Is.LessThan(.0001f));
        }

        [UnityTest]
        public IEnumerator MapHeadingAndSettingsSelectionFollowLanguage()
        {
            var original = FrontendPreferences.Language;
            try
            {
                Call(flow, "OpenCampaign");
                yield return null;
                yield return null;
                foreach (var language in new[] { GameLanguage.Turkish, GameLanguage.English })
                {
                    FrontendPreferences.Language = language;
                    var heading = flow.GetComponentsInChildren<TMPro.TMP_Text>(true).Single(t => t.name == "Region Name");
                    Assert.That(heading.text, Is.EqualTo(language == GameLanguage.Turkish ? "1 · YEŞİL NÖBET" : "1 · GREENWATCH"));
                    Call(flow, "OpenSettings");
                    var buttons = flow.GetComponentsInChildren<UnityEngine.UI.Button>();
                    var selected = buttons.Single(b => b.name == (language == GameLanguage.Turkish ? "Turkish Language" : "English Language"));
                    var other = buttons.Single(b => b.name == (language == GameLanguage.Turkish ? "English Language" : "Turkish Language"));
                    Assert.That(selected.GetComponent<UnityEngine.UI.Image>().color, Is.Not.EqualTo(other.GetComponent<UnityEngine.UI.Image>().color));
                    Call(flow, "CloseSettings");
                }
            }
            finally { FrontendPreferences.Language = original; }
        }

        [UnityTest]
        public IEnumerator BossSignalUsesCampaignWaveData()
        {
            foreach (int level in new[] { 1, 10 })
            {
                var waves = AllinDefense.Data.CampaignLevelFactory.Create(level).waves;
                session.SetCampaignLevel(level);
                typeof(GameSessionController).GetField("waves", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(session, waves);
                typeof(GameSessionController).GetField("currentWaveIndex", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(session, waves.Length - 1);
                bool expectedBoss = level == 10;
                Assert.That(session.NextWaveHasBoss, Is.EqualTo(expectedBoss));
                foreach (var phase in new[] { GamePhase.Intermission, GamePhase.Wave })
                {
                    var spec = DefenderHudController.TopBarSpec("GOLD", "KEEP", "WAVE", 100, 20,
                        waves.Length - 1, waves.Length, phase, session.NextWaveHasBoss);
                    Assert.That(spec.bossSignal, Is.EqualTo(expectedBoss));
                }
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator SettingsHandlesFitTheirRowsAndLastLevelHasNoNextAction()
        {
            Call(flow, "OpenSettings");
            yield return null;
            Canvas.ForceUpdateCanvases();
            foreach (var slider in flow.GetComponentsInChildren<UnityEngine.UI.Slider>())
            {
                Assert.That(slider.handleRect.rect.height, Is.LessThanOrEqualTo(((RectTransform)slider.transform).rect.height));
                Assert.That(slider.handleRect.rect.height, Is.GreaterThan(40));
                Assert.That(slider.fillRect, Is.Not.Null);
                float originalValue = slider.value;
                slider.value = .75f;
                Assert.That(slider.normalizedValue, Is.EqualTo(.75f).Within(.001));
                slider.value = originalValue;
            }
            Call(flow, "CloseSettings");
            session.SetCampaignLevel(50);
            Call(flow, "ShowResult", true, 3, new CampaignCompletionReward(0, 0));
            yield return null;
            var next = flow.GetComponentsInChildren<UnityEngine.UI.Button>(true).Single(b => b.name == "Next Level");
            Assert.That(next.gameObject.activeSelf, Is.False);
            Call(flow, "ShowResult", false, 0, new CampaignCompletionReward(0, 0));
            Assert.That(next.gameObject.activeSelf, Is.False);
            Assert.That(flow.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b => b.name == "Retry Level").interactable, Is.True);
        }
    }
}
