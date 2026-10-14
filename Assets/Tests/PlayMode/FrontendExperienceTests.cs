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

        [Test] public void ReducedMotionFinishesEveryScreenAndModalBeforeReturning()
        {
            bool reduced = FrontendPreferences.ReducedMotion;
            try
            {
                FrontendPreferences.ReducedMotion = true;
                foreach (AppScreen screen in Enum.GetValues(typeof(AppScreen)))
                {
                    Call(flow, "ShowScreen", screen);
                    var navigation = flow.GetComponent<FrontendNavigation>();
                    Assert.That(navigation.IsTransitioning, Is.False, screen.ToString());
                    var group = flow.GetComponentsInChildren<CanvasGroup>(true).First(g => g.name == flow.CurrentScreen.ToString());
                    Assert.That(group.alpha, Is.EqualTo(1), screen.ToString());
                    Assert.That(((RectTransform)group.transform).anchoredPosition, Is.EqualTo(Vector2.zero));
                    Assert.That(group.interactable, Is.True);
                }
                var retry = UiQuery.FindById<UnityEngine.UI.Button>(flow, "Victory Retry");
                Assert.That(retry.GetComponent<AllinDefense.UI.Kit.UiAccessibleButton>().Label, Is.EqualTo(FrontendCopy.TryAgain.Value));
                Assert.That(retry.GetComponent<AllinDefense.UI.Kit.UiVisualState>().Icon.sprite,
                    Is.EqualTo(Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog").frontendTheme.designSystem.iconRestart));
                Call(flow, "ShowScreen", AppScreen.MainMenu);
                Call(flow, "OpenSettings");
                var settings = flow.GetComponentsInChildren<CanvasGroup>(true).First(g => g.name == "Settings Modal");
                Assert.That(settings.alpha, Is.EqualTo(1));
                Call(flow, "CloseSettings"); Assert.That(settings.gameObject.activeSelf, Is.False);
            }
            finally { FrontendPreferences.ReducedMotion = reduced; }
        }
        [Test] public void SecondTapDuringTransitionCannotProduceASecondAction()
        {
            bool reduced = FrontendPreferences.ReducedMotion;
            try
            {
                FrontendPreferences.ReducedMotion = false;
                int actions = 0;
                Action navigate = () => { actions++; Call(flow, "ShowScreen", AppScreen.Login); };
                Call(flow, "MenuClick", navigate); Call(flow, "MenuClick", navigate);
                Assert.That(actions, Is.EqualTo(1));
            }
            finally { FrontendPreferences.ReducedMotion = reduced; }
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
            var map = UiQuery.FindById<UnityEngine.UI.ScrollRect>(flow, "Campaign Scroll");
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
            Assert.That(map, Is.SameAs(UiQuery.FindById<UnityEngine.UI.ScrollRect>(flow, "Campaign Scroll")));
            foreach (var group in flow.GetComponentsInChildren<CanvasGroup>())
            {
                Assert.That(group.alpha, Is.EqualTo(1));
                Assert.That(group.interactable, Is.True);
                if (group.GetComponent<AllinDefense.UI.Kit.UiOverlay>() == null)
                    Assert.That(((RectTransform)group.transform).anchoredPosition, Is.EqualTo(Vector2.zero));
            }
            Assert.That(flow.GetComponentsInChildren<Camera>(true).Length, Is.Zero, "Frontend artwork needs no extra camera");
        }

        [UnityTest]
        public IEnumerator AccountAndSettingsReturnToMapWithoutLosingScroll()
        {
            Call(flow, "OpenCampaign");
            yield return new WaitForSecondsRealtime(.25f);
            var map = UiQuery.FindById<UnityEngine.UI.ScrollRect>(flow, "Campaign Scroll");
            var pages = flow.GetComponentsInChildren<FrontendRegionScrollRect>(true);
            for (int i = 1; i < pages.Length; i++)
                flow.Progress.CompleteLevel(i * AllinDefense.Data.CampaignCatalog.LevelsPerRegion, 3, 1);
            for (int i = 0; i < pages.Length; i++)
            {
                UiQuery.FindById<UnityEngine.UI.Button>(flow, $"Region Tab {i + 1}").onClick.Invoke();
                yield return new WaitForSecondsRealtime(.25f);
                float expectedHorizontal = pages.Length <= 1 ? 0f : i / (pages.Length - 1f);
                Assert.That(map.horizontalNormalizedPosition, Is.EqualTo(expectedHorizontal).Within(.01));
                var route = pages[i];
                Assert.That(route.content.rect.height, Is.GreaterThan(route.viewport.rect.height), "The visible unlocked route must be scrollable");
                float expectedVertical = .3f + i * .1f;
                route.verticalNormalizedPosition = expectedVertical;
                yield return null;
                Assert.That(route.verticalNormalizedPosition, Is.EqualTo(expectedVertical).Within(.02), "The player must see the requested scroll position before leaving");
                Vector2 expectedRoutePosition = route.content.anchoredPosition;
                Vector2 expectedMapPosition = map.content.anchoredPosition;
                Call(flow, "OpenSettings");
                yield return new WaitForSecondsRealtime(UiTokens.FadeDuration + .05f);
                Call(flow, "CloseSettings");
                yield return new WaitForSecondsRealtime(UiTokens.FadeDuration + .05f);
                Assert.That(route.content.anchoredPosition.y, Is.EqualTo(expectedRoutePosition.y).Within(1f));
                Assert.That(map.content.anchoredPosition.x, Is.EqualTo(expectedMapPosition.x).Within(1f));
                Call(flow, "OpenSettings");
                yield return new WaitForSecondsRealtime(UiTokens.FadeDuration + .05f);
                UiQuery.FindById<UnityEngine.UI.Button>(flow, "Settings Account").onClick.Invoke();
                yield return new WaitForSecondsRealtime(UiTokens.FadeDuration + .05f);
                Assert.That(flow.CurrentScreen, Is.EqualTo(AppScreen.Login));
                Assert.That(route.gameObject.activeInHierarchy, Is.False, "The account transition must finish before returning");
                UiQuery.FindById<UnityEngine.UI.Button>(UiQuery.FindById(flow, "Account Header"), "Back").onClick.Invoke();
                yield return new WaitForSecondsRealtime(.25f);
                Assert.That(flow.CurrentScreen, Is.EqualTo(AppScreen.Campaign));
                Assert.That(map.horizontalNormalizedPosition, Is.EqualTo(expectedHorizontal).Within(.01));
                Assert.That(route.verticalNormalizedPosition, Is.EqualTo(expectedVertical).Within(.02));
                Assert.That(route.content.anchoredPosition.y, Is.EqualTo(expectedRoutePosition.y).Within(1f));
                Assert.That(map.content.anchoredPosition.x, Is.EqualTo(expectedMapPosition.x).Within(1f));
            }
            for (int i = 0; i < pages.Length; i++)
                Assert.That(pages[i].verticalNormalizedPosition, Is.EqualTo(.3f + i * .1f).Within(.02), $"Region {i + 1} keeps its own route position");
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
        public IEnumerator MenuAndMapUseLayoutCardsAndGraphicalProgressInBothLanguages()
        {
            var original = FrontendPreferences.Language;
            try
            {
                foreach (var language in new[] { GameLanguage.Turkish, GameLanguage.English })
                {
                    FrontendPreferences.Language = language;
                    Call(flow, "ShowScreen", AppScreen.MainMenu);
                    yield return new WaitForSecondsRealtime(UiTokens.FadeDuration + .05f);
                    Assert.That(UiQuery.FindById(flow, "Main Menu Layout").GetComponent<UnityEngine.UI.VerticalLayoutGroup>(), Is.Not.Null);
                    Assert.That(UiQuery.FindById(flow, "Settings").GetComponent<AllinDefense.UI.Kit.UiVisualState>().Icon.sprite,
                        Is.EqualTo(Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog").frontendTheme.designSystem.iconSettings));
                    Assert.That(UiQuery.FindById(flow, "Chapter Stars").GetComponent<AllinDefense.UI.Kit.UiStarRow>(), Is.Not.Null);
                    Assert.That(UiQuery.FindById<UnityEngine.UI.Image>(flow, "Save Error").sprite,
                        Is.EqualTo(Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog").frontendTheme.designSystem.iconCheck));
                    UiQuery.FindById<UnityEngine.UI.Button>(flow, "Menu Hero").onClick.Invoke();
                    Assert.That(UiQuery.FindById(flow, "Hero Name Title").gameObject.activeInHierarchy, Is.True);
                    Call(flow, "CloseHeroName");
                    Call(flow, "OpenCampaign");
                    yield return null;
                    yield return null;
                    Canvas.ForceUpdateCanvases();
                    Assert.That(UiQuery.FindById(flow, "Selection Bar").GetComponent<AllinDefense.UI.Kit.UiOverlay>(), Is.Not.Null);
                    yield return new WaitForSecondsRealtime(.25f);
                    var overlay = UiQuery.FindById(flow, "Selection Bar").GetComponent<AllinDefense.UI.Kit.UiOverlay>();
                    var card = (RectTransform)overlay.Body.parent;
                    var bodyCorners = new Vector3[4]; overlay.Body.GetWorldCorners(bodyCorners);
                    foreach (Vector3 corner in bodyCorners)
                        Assert.That(card.rect.Contains(card.InverseTransformPoint(corner)), Is.True, "Animated briefing stays inside its card.");
                    Assert.That(UiQuery.FindById(flow, "Region Tabs").gameObject.activeSelf, Is.True);
                    Assert.That(UiQuery.FindById(flow, "Level 2/Lock").gameObject.activeSelf, Is.True);
                    Assert.That(UiQuery.FindById(flow, "Level 1/Selection Ring").gameObject.activeSelf, Is.True);
                    Assert.That(UiQuery.FindById(flow, "Level Stars 1").GetComponent<AllinDefense.UI.Kit.UiStarRow>(), Is.Not.Null);
                    Assert.That(UiQuery.FindById<TMPro.TMP_Text>(flow, "Briefing Reward").text.Length, Is.LessThanOrEqualTo(40));
                    for (int level = 1; level <= 10; level++)
                    {
                        string objective = (string)typeof(AppFlowController).GetMethod("LearningObjective", BindingFlags.Static | BindingFlags.NonPublic)
                            .Invoke(null, new object[] { level });
                        Assert.That(objective.Length, Is.LessThanOrEqualTo(level == 10 ? 64 : 40), $"{language} level {level} objective");
                    }
                    Assert.That(UiQuery.FindById(flow, "Enemy Intel").GetComponentsInChildren<UnityEngine.UI.Image>().Length, Is.GreaterThan(0));
                    var map = UiQuery.FindById<UnityEngine.UI.ScrollRect>(flow, "Campaign Scroll");
                    var sheet = (RectTransform)UiQuery.FindById(flow, "Selection Bar");
                    var mapCorners = new Vector3[4]; var sheetCorners = new Vector3[4];
                    ((RectTransform)map.transform).GetWorldCorners(mapCorners); sheet.GetWorldCorners(sheetCorners);
                    Assert.That(mapCorners[0].y, Is.GreaterThanOrEqualTo(sheetCorners[1].y - .1f), "Sheet must reserve map space.");
                }
            }
            finally { FrontendPreferences.Language = original; }
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
                    var heading = UiQuery.FindById<TMPro.TMP_Text>(flow, "Region Name");
                    Assert.That(heading.text, Is.EqualTo(language == GameLanguage.Turkish ? "1 · YEŞİL NÖBET" : "1 · GREENWATCH"));
                    Call(flow, "OpenSettings");
                    var buttons = flow.GetComponentsInChildren<UnityEngine.UI.Button>();
                    var selected = UiQuery.FindById<UnityEngine.UI.Button>(flow, language == GameLanguage.Turkish ? "Turkish Language" : "English Language");
                    var other = UiQuery.FindById<UnityEngine.UI.Button>(flow, language == GameLanguage.Turkish ? "English Language" : "Turkish Language");
                    Assert.That(selected.GetComponent<AllinDefense.UI.Kit.UiVisualState>().State, Is.Not.EqualTo(other.GetComponent<AllinDefense.UI.Kit.UiVisualState>().State));
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
            var next = UiQuery.FindById<UnityEngine.UI.Button>(flow, "Next Level");
            Assert.That(next.gameObject.activeSelf, Is.False);
            Call(flow, "ShowResult", false, 0, new CampaignCompletionReward(0, 0));
            Assert.That(next.gameObject.activeSelf, Is.False);
            Assert.That(UiQuery.FindById<UnityEngine.UI.Button>(flow, "Retry Level").interactable, Is.True);
        }
        [UnityTest]
        public IEnumerator SettingsControlsPersistPreferences()
        {
            float music = FrontendPreferences.MusicVolume, effects = FrontendPreferences.SfxVolume;
            bool motion = FrontendPreferences.ReducedMotion, shake = FrontendPreferences.CameraShake, haptics = FrontendPreferences.Haptics;
            try
            {
                Call(flow, "OpenSettings");
                UiQuery.FindById(flow, "MÜZİK Row").GetComponentInChildren<UnityEngine.UI.Slider>().value = .23f;
                UiQuery.FindById(flow, "EFEKTLER Row").GetComponentInChildren<UnityEngine.UI.Slider>().value = .41f;
                UiQuery.FindById(flow, "AZALTILMIŞ HAREKET Row").GetComponentInChildren<AllinDefense.UI.Kit.UiSwitch>().SetValue(!motion);
                UiQuery.FindById(flow, "KAMERA SARSINTISI Row").GetComponentInChildren<AllinDefense.UI.Kit.UiSwitch>().SetValue(!shake);
                UiQuery.FindById(flow, "TİTREŞİM Row").GetComponentInChildren<AllinDefense.UI.Kit.UiSwitch>().SetValue(!haptics);
                Assert.That(FrontendPreferences.MusicVolume, Is.EqualTo(.23f));
                Assert.That(FrontendPreferences.SfxVolume, Is.EqualTo(.41f));
                Assert.That(FrontendPreferences.ReducedMotion, Is.EqualTo(!motion));
                Assert.That(FrontendPreferences.CameraShake, Is.EqualTo(!shake));
                Assert.That(FrontendPreferences.Haptics, Is.EqualTo(!haptics));
                Assert.That(flow.GetComponentsInChildren<UiId>(true).Any(id => id.Id == "Settings Hero"), Is.False);
                yield return null;
            }
            finally
            {
                FrontendPreferences.MusicVolume = music; FrontendPreferences.SfxVolume = effects;
                FrontendPreferences.ReducedMotion = motion; FrontendPreferences.CameraShake = shake; FrontendPreferences.Haptics = haptics;
            }
        }

        [UnityTest]
        public IEnumerator ResultStarsMatchRewardAndDefeatHidesCelebration()
        {
            foreach (int earned in new[] { 1, 2, 3 })
            {
                Call(flow, "ShowResult", true, earned, new CampaignCompletionReward(27, 68));
                yield return new WaitForSecondsRealtime(1.5f);
                var row = UiQuery.FindById(flow, "Result Stars").GetComponent<AllinDefense.UI.Kit.UiStarRow>();
                Assert.That(UiQuery.FindById<TMPro.TMP_Text>(flow, "Result Reward").text, Is.EqualTo(FrontendCopy.ResultStars.Format(earned)));
                var rewardIcon = UiQuery.FindById<UnityEngine.UI.Image>(flow, "Result Reward Icon");
                Assert.That(rewardIcon.gameObject.activeSelf, Is.False);
                Assert.That(rewardIcon.sprite, Is.Null);
                Assert.That(row.Earned, Is.EqualTo(earned));
                Assert.That(row.VisibleStars, Is.EqualTo(earned));
                var assets = Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog").frontendTheme.designSystem;
                Assert.That(row.GetComponentsInChildren<UnityEngine.UI.Image>().Count(image => image.sprite == assets.iconStarFilled), Is.EqualTo(earned));
            }
            Call(flow, "ShowResult", false, 0, default(CampaignCompletionReward));
            Assert.That(UiQuery.FindById(flow, "Result Stars").gameObject.activeSelf, Is.False);
            Assert.That(UiQuery.FindById(flow, "Next Level").gameObject.activeSelf, Is.False);
            Assert.That(UiQuery.FindById(flow, "Defence Advice").gameObject.activeSelf, Is.True);
            Assert.That(UiQuery.FindById(flow, "Result Reward Icon").gameObject.activeSelf, Is.True);
        }

        [UnityTest]
        public IEnumerator TenthLevelCompletionDoesNotAnnounceUnplayableChapter()
        {
            flow.Progress.Data.unlockedLevel = 10;
            flow.Progress.Save();
            flow.GetComponent<AudioListener>().enabled = false;
            session.SetCampaignLevel(10);
            session.ConfigureCampaignStore(flow.Progress);
            session.Initialize();
            Call(session, "SetPhase", GamePhase.Victory);
            Assert.That(flow.Progress.Data.unlockedLevel, Is.EqualTo(11));
            Assert.That(flow.CurrentScreen, Is.EqualTo(AppScreen.Result));
            Assert.That(UiQuery.FindById(flow, "Unlocked Chapter").gameObject.activeSelf, Is.False);
            Assert.That(UiQuery.FindById(flow, "Next Level").gameObject.activeSelf, Is.False);
            yield return null;
        }

        [Test]
        public void ContinueCheckpointValidationRejectsWrongVersionOwnerAndLockedLevel()
        {
            flow.Progress.Data.unlockedLevel = 8;
            var checkpoint = new BattleCheckpoint
            {
                owner = flow.Progress.ProfileKey, level = 3, gold = 123, lives = 12,
                hero = new ActorCheckpoint { health = 170 }
            };
            Assert.That(GameSessionController.IsValidCheckpoint(flow.Progress, checkpoint), Is.True);
            checkpoint.ruleVersion = "first-region-v1";
            Assert.That(GameSessionController.IsValidCheckpoint(flow.Progress, checkpoint), Is.False);
            checkpoint.ruleVersion = AllinDefense.Data.FirstRegionCampaign.Version;
            checkpoint.owner = "another-profile";
            Assert.That(GameSessionController.IsValidCheckpoint(flow.Progress, checkpoint), Is.False);
            checkpoint.owner = flow.Progress.ProfileKey;
            checkpoint.level = 9;
            Assert.That(GameSessionController.IsValidCheckpoint(flow.Progress, checkpoint), Is.False);
            checkpoint.level = 11;
            flow.Progress.Data.unlockedLevel = 50;
            Assert.That(GameSessionController.IsValidCheckpoint(flow.Progress, checkpoint), Is.False);
        }

        [UnityTest]
        public IEnumerator ContinuePrioritizesValidLocalCheckpointOverUnlockedProgress()
        {
            flow.Progress.Data.unlockedLevel = 8;
            flow.Progress.Data.checkpoint = new BattleCheckpoint
            {
                owner = flow.Progress.ProfileKey, level = 3, gold = 123, lives = 12,
                hero = new ActorCheckpoint { health = 170 }
            };
            flow.Progress.Save();
            UiQuery.FindById<UnityEngine.UI.Button>(flow, "Continue").onClick.Invoke();
            double deadline = Time.realtimeSinceStartupAsDouble + 15;
            while (flow.IsLaunchingBattle && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
            Assert.That(flow.CurrentScreen, Is.EqualTo(AppScreen.Battle));
            Assert.That(session.CampaignLevelNumber, Is.EqualTo(3));
            Assert.That(session.CheckpointRestored, Is.True);
            Assert.That(session.Gold, Is.EqualTo(123));
            Assert.That(session.Lives, Is.EqualTo(12));
        }

        [UnityTest]
        public IEnumerator VictoryAccountSuggestionOpensPortraitAccountAndReturnsToMap()
        {
            session.SetCampaignLevel(1);
            Call(flow, "ShowResult", true, 3, new CampaignCompletionReward(27, 68));
            yield return new WaitForSecondsRealtime(1.4f);
            var link = UiQuery.FindById<UnityEngine.UI.Button>(flow, "Result Link Account");
            Assert.That(link.gameObject.activeInHierarchy, Is.True);
            link.onClick.Invoke();
            yield return new WaitForSecondsRealtime(.3f);
            Assert.That(flow.CurrentScreen, Is.EqualTo(AppScreen.Login));
            Assert.That(flow.GetComponentInChildren<UnityEngine.UI.CanvasScaler>().referenceResolution, Is.EqualTo(new Vector2(1080, 1920)));
            Assert.That(UiQuery.FindById(flow, "Guest Sign In").gameObject.activeSelf, Is.False);
            Call(flow, "ReturnFromAccount");
            Assert.That(flow.CurrentScreen, Is.EqualTo(AppScreen.Campaign));
        }

        [UnityTest]
        public IEnumerator ResultUnlockChipOnlyShowsBattleProgressAfterAccountSwitch()
        {
            flow.CompleteServiceSignIn(PlayerAuthProvider.PlayerAccount, "returning-player", "Defender");
            flow.Progress.Data.unlockedLevel = 5;
            flow.Progress.Save();
            session.SetCampaignLevel(4);
            Call(flow, "ShowResult", true, 3, new CampaignCompletionReward(27, 68));
            Assert.That(UiQuery.FindById(flow, "Unlocked Chapter").gameObject.activeSelf, Is.False);
            Call(flow, "ShowScreen", AppScreen.Battle);
            session.SetCampaignLevel(5);
            flow.Progress.Data.unlockedLevel = 6;
            flow.Progress.Save();
            Call(flow, "ShowResult", true, 3, new CampaignCompletionReward(27, 68));
            Assert.That(UiQuery.FindById(flow, "Unlocked Chapter").gameObject.activeSelf, Is.True);
            yield return null;
        }

        [UnityTest]
        public IEnumerator NewEnemyAppearsOnceAndRestoresPhaseAndSpeed()
        {
            const string key = "AllinDefense.Frontend.EnemySeen.Raider";
            bool existed = PlayerPrefs.HasKey(key); int previous = PlayerPrefs.GetInt(key);
            try
            {
                PlayerPrefs.DeleteKey(key);
                flow.GetComponent<AudioListener>().enabled = false;
                session.Initialize();
                Call(flow, "ShowScreen", AppScreen.Battle);
                Call(session, "SetPhase", GamePhase.Preparing);
                Time.timeScale = 2f;
                var wave = new AllinDefense.Data.WaveDefinition("intro", "", 0, false,
                    new AllinDefense.Data.SpawnGroupDefinition(AllinDefense.Data.EnemyArchetype.Raider, 1, 0, 1));
                typeof(GameSessionController).GetField("waves", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(session, new[] { wave });
                session.StartNextWave();
                Assert.That(session.Phase, Is.EqualTo(GamePhase.Paused));
                Assert.That(Time.timeScale, Is.Zero);
                Assert.That(FrontendPreferences.HasSeenEnemy(AllinDefense.Data.EnemyArchetype.Raider), Is.False);
                Assert.That(UiQuery.FindById(flow, "New Enemy").gameObject.activeInHierarchy, Is.True);
                Call(flow, "AcknowledgeEnemy");
                Assert.That(FrontendPreferences.HasSeenEnemy(AllinDefense.Data.EnemyArchetype.Raider), Is.True);
                Assert.That(session.Phase, Is.EqualTo(GamePhase.Wave));
                Assert.That(Time.timeScale, Is.EqualTo(2f));
                Call(session, "SetPhase", GamePhase.Preparing);
                Assert.That((bool)Call(flow, "ShowNewEnemy", wave), Is.False);
                yield return null;
            }
            finally
            {
                if (existed) PlayerPrefs.SetInt(key, previous); else PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save(); Time.timeScale = 1;
            }
        }

    }
}
