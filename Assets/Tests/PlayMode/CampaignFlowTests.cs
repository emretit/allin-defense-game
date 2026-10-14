using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using AllinDefense.Core;
using AllinDefense.Combat;
using AllinDefense.Data;
using AllinDefense.Presentation;
using AllinDefense.UI;
using AllinDefense.UI.Kit;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace AllinDefense.Tests
{
    public sealed class CampaignFlowTests
    {
        private AppFlowController flow;
        private GameSessionController session;
        private string folder;
        private bool reducedMotion;
        private GameLanguage language;
        private static object Call(object target, string name, params object[] args) =>
            target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);
        private static void VisibleRegions(int? count) => typeof(AppFlowController)
            .GetMethod("SetVisibleRegionCountForTests", BindingFlags.Static | BindingFlags.NonPublic)
            .Invoke(null, new object[] { count });
        private T Find<T>(string id) where T : Component => UiQuery.FindById<T>(flow, id);
        private string Text(string id) => Find<TMP_Text>(id).text;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            folder = Path.Combine(Path.GetTempPath(), "campaign-flow-" + Guid.NewGuid().ToString("N"));
            reducedMotion = FrontendPreferences.ReducedMotion;
            language = GameLocalization.CurrentLanguage;
            FrontendPreferences.ReducedMotion = true;
            VisibleRegions(5);
            session = new GameObject("Campaign Flow Session").AddComponent<GameSessionController>();
            flow = new GameObject("Campaign Flow").AddComponent<AppFlowController>();
            flow.Initialize(Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog"),
                session, false, Path.Combine(folder, "save.json"));
            yield return new WaitForSecondsRealtime(.95f);
            Call(flow, "ShowScreen", AppScreen.MainMenu);
            GameLocalization.SetLanguage(GameLanguage.Turkish);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            FrontendLoadingOverlay.Hide();
            Object.Destroy(flow.gameObject);
            Object.Destroy(session.gameObject);
            yield return null;
            VisibleRegions(null);
            FrontendPreferences.ReducedMotion = reducedMotion;
            GameLocalization.SetLanguage(language);
            Time.timeScale = 1;
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }

        private void Win(int level, int lives = 1)
        {
            session.SetCampaignLevel(level);
            var reward = flow.Progress.CompleteLevel(level, 1, lives);
            Call(flow, "ShowResult", true, BattleRules.StarsForLives(lives, CampaignCatalog.Level(level).startingLives), reward);
        }

        [UnityTest]
        public IEnumerator RegionVictoriesOpenNextPageAndReplayWithoutStarAnimation()
        {
            for (int region = 1; region < 5; region++)
            {
                int level = region * CampaignCatalog.LevelsPerRegion;
                Win(level);
                Assert.That(Find<RectTransform>("Campaign Completion").gameObject.activeSelf, Is.True);
                Assert.That(Text("Completion Heading"), Is.EqualTo(FrontendCopy.RegionCompleted.Format(
                    CampaignCatalog.Regions[region - 1].localizedName, 1)));
                Assert.That(Text("Completion Next"), Is.EqualTo(FrontendCopy.NextRegion.Format(CampaignCatalog.Regions[region].localizedName)));
                Assert.That(Find<Button>("Next Level").gameObject.activeSelf, Is.False);
                Assert.That(Find<UiStarRow>("Result Stars").VisibleStars, Is.EqualTo(1));
                Find<Button>("Completion Continue").onClick.Invoke();
                yield return new WaitForSecondsRealtime(.25f);
                Assert.That(flow.CurrentScreen, Is.EqualTo(AppScreen.Campaign));
                var pager = flow.GetComponentInChildren<FrontendCampaignPager>(true);
                Assert.That(pager.horizontalNormalizedPosition, Is.EqualTo(region / 4f).Within(.02f));
                Assert.That(Find<Button>($"Level {level + 1}").interactable, Is.True);
                Assert.That(flow.Progress.Data.lastSelectedLevel, Is.EqualTo(level + 1));
                Win(level);
                Assert.That(Find<UiStarRow>("Result Stars").VisibleStars, Is.EqualTo(1));
                Assert.That(Text("Completion Heading"), Does.Contain("1/30"));
            }
            Call(flow, "ShowResult", false, 0, default(CampaignCompletionReward));
            Assert.That(Find<RectTransform>("Campaign Completion").gameObject.activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator ProductionLimitShowsComingSoonAndNinetyStars()
        {
            Object.Destroy(flow.gameObject);
            Object.Destroy(session.gameObject);
            yield return null;
            VisibleRegions(null);
            session = new GameObject("Production Campaign Session").AddComponent<GameSessionController>();
            flow = new GameObject("Production Campaign Flow").AddComponent<AppFlowController>();
            flow.Initialize(Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog"),
                session, false, Path.Combine(folder, "production.json"));
            yield return new WaitForSecondsRealtime(.95f);
            Call(flow, "ShowScreen", AppScreen.MainMenu);
            flow.Progress.Data.levels.Add(new CampaignLevelProgress { level = 50, stars = 3 });
            Win(30);
            Assert.That(Text("Completion Next"), Is.EqualTo(FrontendCopy.RegionsComingSoon.Value));
            Assert.That(Find<Button>("Completion Continue").gameObject.activeSelf, Is.False);
            Assert.That(Find<RectTransform>("Unlocked Chapter").gameObject.activeSelf, Is.False);
            Assert.That(Text("Profile Star Count"), Is.EqualTo("1/90"));
            Call(flow, "OpenCampaign");
            yield return new WaitForSecondsRealtime(.25f);
            Assert.That(Find<Button>("Level 31"), Is.Null);
            Assert.That(flow.GetComponentInChildren<FrontendCampaignPager>(true).PageCount, Is.EqualTo(3));
        }

        [UnityTest]
        public IEnumerator FinalShowsFiveRegionTotalsAndReturnsToLowestStarsInBothLanguages()
        {
            for (int level = 1; level <= 49; level++)
                flow.Progress.Data.levels.Add(new CampaignLevelProgress { level = level, stars = level == 35 ? 1 : 3 });
            Win(50, CampaignCatalog.Level(50).startingLives);
            foreach (var current in new[] { GameLanguage.Turkish, GameLanguage.English })
            {
                GameLocalization.SetLanguage(current);
                Canvas.ForceUpdateCanvases();
                Assert.That(Text("Result Title"), Is.EqualTo(FrontendCopy.KingdomSafe.Value));
                Assert.That(Text("Result Reward"), Is.EqualTo(FrontendCopy.ProfileStars.Format(148, 150)));
                Assert.That(Text("Profile Star Count"), Is.EqualTo("148/150"));
                for (int region = 1; region <= 5; region++)
                {
                    var row = Find<TMP_Text>($"Completion Region {region}");
                    Assert.That(row.gameObject.activeSelf, Is.True);
                    Assert.That(row.text, Is.EqualTo(FrontendCopy.RegionStars.Format(CampaignCatalog.Regions[region - 1].localizedName,
                        region == 4 ? 28 : 30)));
                    row.ForceMeshUpdate();
                    Assert.That(row.isTextOverflowing, Is.False);
                }
                Assert.That(Find<Button>("Next Level").gameObject.activeSelf, Is.False);
                Assert.That(flow.Progress.Data.unlockedLevel, Is.EqualTo(50));
                Assert.That(Text("Completion Continue.Label"), Is.EqualTo(FrontendCopy.ReturnToMissingStars.Value));
                Capture($"final-{current}");
                yield return null;
            }
            Find<Button>("Completion Continue").onClick.Invoke();
            yield return new WaitForSecondsRealtime(.25f);
            Assert.That(flow.Progress.Data.lastSelectedLevel, Is.EqualTo(35));
            Assert.That(flow.GetComponentInChildren<FrontendCampaignPager>(true).horizontalNormalizedPosition,
                Is.EqualTo(.75f).Within(.02f));
            Assert.That(flow.Progress.NextUnfinishedLevel(), Is.EqualTo(35));
            Call(flow, "ShowScreen", AppScreen.MainMenu);
            Call(flow, "ContinueCampaign");
            Assert.That(flow.Progress.Data.lastSelectedLevel, Is.EqualTo(35));
        }

        [UnityTest]
        public IEnumerator LockedPageExplainsBossRequirementAndUpdatesAfterVictory()
        {
            Call(flow, "OpenCampaign");
            Call(flow, "FocusRegion", 1);
            yield return new WaitForSecondsRealtime(.25f);
            var locked = Find<RectTransform>("Region Locked 2");
            Assert.That(locked.gameObject.activeSelf, Is.True);
            Assert.That(locked.GetComponentInChildren<TMP_Text>().text, Is.EqualTo(FrontendCopy.RegionLocked.Format(10)));
            Assert.That(Find<Image>("Region Tab Lock 2").gameObject.activeSelf, Is.True);
            Assert.That(Find<Button>("Level 11").interactable, Is.False);
            GameLocalization.SetLanguage(GameLanguage.English);
            Assert.That(locked.GetComponentInChildren<TMP_Text>().text, Is.EqualTo("Complete chapter 10 first"));
            Win(10);
            Find<Button>("Completion Continue").onClick.Invoke();
            yield return new WaitForSecondsRealtime(.25f);
            Assert.That(locked.gameObject.activeSelf, Is.False);
            Assert.That(Find<Image>("Region Tab Lock 2").gameObject.activeSelf, Is.False);
            Assert.That(Find<Button>("Level 11").interactable, Is.True);
        }

        [UnityTest]
        public IEnumerator LateObjectivesUseRegionSubtitleAndNamedBossWithoutTextOverflow()
        {
            flow.Progress.Data.unlockedLevel = 50;
            for (int region = 1; region < 5; region++)
                flow.Progress.Data.levels.Add(new CampaignLevelProgress { level = region * 10, stars = 1 });
            Call(flow, "OpenCampaign");
            yield return null;
            foreach (var current in new[] { GameLanguage.Turkish, GameLanguage.English })
            {
                GameLocalization.SetLanguage(current);
                Call(flow, "SelectLevel", 31);
                Assert.That(Text("Briefing Reward"), Does.Contain(GameLocalization.Translate(CampaignCatalog.Regions[3].subtitle)));
                Call(flow, "SelectLevel", 40);
                Assert.That(Text("Briefing Reward"), Does.Contain(CampaignCatalog.Level(40).localizedName));
                Win(40);
                var heading = Find<TMP_Text>("Completion Heading");
                Canvas.ForceUpdateCanvases(); heading.ForceMeshUpdate();
                Assert.That(heading.text, Does.Contain(CampaignCatalog.Regions[3].localizedName));
                Assert.That(heading.isTextOverflowing, Is.False);
                var chapter = Find<TMP_Text>("Result Chapter"); chapter.ForceMeshUpdate();
                Assert.That(chapter.isTextOverflowing, Is.False);
                Capture($"region-{current}");
                yield return null;
            }
        }

        private void Capture(string name)
        {
            var canvas = flow.GetComponentInChildren<Canvas>();
            var cameraRoot = new GameObject("Campaign Flow Evidence Camera");
            var camera = cameraRoot.AddComponent<Camera>();
            var target = new RenderTexture(1920, 1080, 24);
            var texture = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            var previousTarget = RenderTexture.active;
            var previousMode = canvas.renderMode;
            var previousCamera = canvas.worldCamera;
            float previousDistance = canvas.planeDistance;
            try
            {
                camera.targetTexture = target;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1;
                Canvas.ForceUpdateCanvases();
                foreach (var layout in flow.GetComponentsInChildren<FrontendResponsiveLayout>()) layout.Refresh();
                Canvas.ForceUpdateCanvases();
                foreach (string id in new[] { "Result Title", "Completion Continue.Label", "Completion Map.Label" })
                {
                    var text = Find<TMP_Text>(id);
                    if (!text.gameObject.activeInHierarchy) continue;
                    text.ForceMeshUpdate();
                    Assert.That(text.isTextOverflowing, Is.False, id + ": " + text.text);
                }
                camera.Render();
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
                texture.Apply();
                string output = Path.Combine(Application.dataPath, "../Builds/flow-0b");
                Directory.CreateDirectory(output);
                File.WriteAllBytes(Path.Combine(output, name + ".png"), texture.EncodeToPNG());
            }
            finally
            {
                canvas.renderMode = previousMode;
                canvas.worldCamera = previousCamera;
                canvas.planeDistance = previousDistance;
                RenderTexture.active = previousTarget;
                camera.targetTexture = null;
                Object.Destroy(cameraRoot);
                Object.Destroy(texture);
                Object.Destroy(target);
            }
        }

        [Test]
        public void NextUnfinishedUsesAllFiftyLevelsAndStableLowestStarOrder()
        {
            var store = flow.Progress;
            store.Data.unlockedLevel = 50;
            for (int level = 1; level <= 49; level++) store.Data.levels.Add(new CampaignLevelProgress { level = level, stars = 3 });
            Assert.That(store.NextUnfinishedLevel(), Is.EqualTo(50));
            Assert.That(store.NextUnfinishedLevel(10), Is.EqualTo(1));
            store.Data.levels.Add(new CampaignLevelProgress { level = 50, stars = 2 });
            Assert.That(store.NextUnfinishedLevel(), Is.EqualTo(50));
            store.ProgressFor(24).stars = 2;
            Assert.That(store.NextUnfinishedLevel(), Is.EqualTo(24));
            Assert.That(store.VisibleStars(50), Is.EqualTo(148));
            Assert.That(store.VisibleStars(10), Is.EqualTo(30));
        }
    }
}
