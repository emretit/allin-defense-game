using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using AllinDefense.Core;
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
    public sealed class RelearnExperienceTests
    {
        private AppFlowController flow;
        private GameSessionController session;
        private string folder;
        private bool reduced, replay;
        private GameLanguage language;
        private string[] enemyKeys;
        private int[] enemyValues;
        private static object Call(object target, string method, params object[] args) =>
            target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);
        private T Find<T>(string id) where T : Component => UiQuery.FindById<T>(flow, id);

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            folder = Path.Combine(Path.GetTempPath(), "relearn-" + Guid.NewGuid().ToString("N"));
            reduced = FrontendPreferences.ReducedMotion;
            replay = FrontendPreferences.ReplayFirstBattleGuide;
            language = GameLocalization.CurrentLanguage;
            enemyKeys = Enum.GetValues(typeof(EnemyArchetype)).Cast<EnemyArchetype>()
                .Select(enemy => "AllinDefense.Frontend.EnemySeen." + enemy).ToArray();
            enemyValues = enemyKeys.Select(key => PlayerPrefs.GetInt(key, -1)).ToArray();
            FrontendPreferences.ReducedMotion = true;
            session = new GameObject("Relearn Session").AddComponent<GameSessionController>();
            flow = new GameObject("Relearn Flow").AddComponent<AppFlowController>();
            flow.Initialize(Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog"),
                session, false, Path.Combine(folder, "save.json"));
            yield return new WaitForSecondsRealtime(.95f);
            Call(flow, "ShowScreen", AppScreen.MainMenu);
        }

        [UnityTest]
        public IEnumerator BriefingOpensEverySeenEnemyAndReturnsWithoutStartingBattle()
        {
            flow.Progress.Data.unlockedLevel = 10;
            Call(flow, "OpenCampaign");
            yield return null;
            Call(flow, "SelectLevel", 10);
            foreach (var enemy in CampaignLevelFactory.Create(10).waves.SelectMany(w => w.groups).Select(g => g.enemy).Distinct())
            {
                FrontendPreferences.MarkEnemySeen(enemy);
                var button = Find<Button>("Enemy Chip " + enemy);
                Assert.That(button.GetComponent<UiAccessibleButton>(), Is.Not.Null);
                Assert.That(button.GetComponent<UiPointSize>().Points, Is.GreaterThanOrEqualTo(44));
                button.onClick.Invoke();
                Assert.That(Find<TMP_Text>("New Enemy Name").text, Is.EqualTo(FirstRegionCampaign.EnemyName(enemy)));
                Assert.That(flow.CurrentScreen, Is.EqualTo(AppScreen.Campaign));
                Find<Button>("Acknowledge Enemy").onClick.Invoke();
                Assert.That(flow.GetComponentsInChildren<UiOverlay>().Any(o => o.name == "New Enemy"), Is.False);
                Assert.That(flow.CurrentScreen, Is.EqualTo(AppScreen.Campaign));
                Assert.That(FrontendPreferences.HasSeenEnemy(enemy), Is.True);
                yield return null;
            }
            foreach (var value in new[] { GameLanguage.Turkish, GameLanguage.English })
            {
                GameLocalization.SetLanguage(value);
                Find<Button>("Enemy Chip Boss").onClick.Invoke();
                Capture("briefing-card-" + value, "New Enemy");
                Find<Button>("Acknowledge Enemy").onClick.Invoke();
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator SettingsResetPreservesSaveAndReplaysGuideAndEnemyCard()
        {
            flow.Progress.CompleteLevel(1, 3, 20);
            FrontendPreferences.ReplayFirstBattleGuide = false;
            foreach (EnemyArchetype enemy in Enum.GetValues(typeof(EnemyArchetype))) FrontendPreferences.MarkEnemySeen(enemy);
            string before = JsonUtility.ToJson(flow.Progress.Data);
            byte[] saved = File.ReadAllBytes(flow.Progress.SavePath);
            Call(flow, "OpenSettings");
            foreach (var value in new[] { GameLanguage.Turkish, GameLanguage.English })
            {
                GameLocalization.SetLanguage(value);
                Find<Button>("Replay Guide").onClick.Invoke();
                Assert.That(Find<TMP_Text>("Replay Guide Status").text, Is.EqualTo(FrontendCopy.ReplayGuideReady.Value));
                Capture("settings-reset-" + value, "Settings Modal");
            }
            Assert.That(JsonUtility.ToJson(flow.Progress.Data), Is.EqualTo(before));
            Assert.That(File.ReadAllBytes(flow.Progress.SavePath), Is.EqualTo(saved));
            Assert.That(FrontendPreferences.ReplayFirstBattleGuide, Is.True);
            foreach (EnemyArchetype enemy in Enum.GetValues(typeof(EnemyArchetype))) Assert.That(FrontendPreferences.HasSeenEnemy(enemy), Is.False);
            Call(flow, "CloseSettings");
            flow.LaunchCampaignLevel(1);
            for (int i = 0; i < 180 && flow.CurrentScreen != AppScreen.Battle; i++) yield return null;
            Assert.That(flow.CurrentScreen, Is.EqualTo(AppScreen.Battle));
            Call(session, "RefreshFirstBattleGuide");
            Assert.That(typeof(GameSessionController).GetField("tutorialStep", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session), Is.EqualTo(0));
            Assert.That((bool)Call(flow, "ShowNewEnemy", CampaignLevelFactory.Create(1).waves[0]), Is.True);
            Assert.That(session.Phase, Is.EqualTo(GamePhase.Paused));
            Find<Button>("Acknowledge Enemy").onClick.Invoke();
            Assert.That(FrontendPreferences.HasSeenEnemy(EnemyArchetype.Raider), Is.True);
            Assert.That(session.Phase, Is.EqualTo(GamePhase.Wave));
            Assert.That(flow.Progress.ProgressFor(1).stars, Is.EqualTo(3));
            Assert.That(flow.Progress.Data.unlockedLevel, Is.EqualTo(2));
        }

        private void Capture(string name, string rootId)
        {
            var canvas = flow.GetComponentInChildren<Canvas>();
            var cameraRoot = new GameObject("Relearn Evidence Camera");
            var camera = cameraRoot.AddComponent<Camera>();
            var target = new RenderTexture(1080, 1920, 24);
            var texture = new Texture2D(1080, 1920, TextureFormat.RGB24, false);
            var active = RenderTexture.active;
            var mode = canvas.renderMode;
            var previousCamera = canvas.worldCamera;
            float distance = canvas.planeDistance;
            try
            {
                camera.targetTexture = target;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1;
                Canvas.ForceUpdateCanvases();
                foreach (var layout in flow.GetComponentsInChildren<FrontendResponsiveLayout>()) layout.Refresh();
                Canvas.ForceUpdateCanvases();
                foreach (var text in Find<RectTransform>(rootId).GetComponentsInChildren<TMP_Text>())
                {
                    text.ForceMeshUpdate();
                    Assert.That(text.isTextOverflowing, Is.False, text.name + ": " + text.text);
                }
                camera.Render(); RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, 1080, 1920), 0, 0); texture.Apply();
                Directory.CreateDirectory("Builds/relearn");
                File.WriteAllBytes("Builds/relearn/" + name + ".png", texture.EncodeToPNG());
            }
            finally
            {
                canvas.renderMode = mode; canvas.worldCamera = previousCamera; canvas.planeDistance = distance;
                RenderTexture.active = active; camera.targetTexture = null;
                Object.Destroy(cameraRoot); Object.Destroy(texture); Object.Destroy(target);
            }
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(flow.gameObject); Object.Destroy(session.gameObject);
            yield return null;
            Time.timeScale = 1;
            FrontendPreferences.ReducedMotion = reduced;
            FrontendPreferences.ReplayFirstBattleGuide = replay;
            GameLocalization.SetLanguage(language);
            for (int i = 0; i < enemyKeys.Length; i++)
                if (enemyValues[i] < 0) PlayerPrefs.DeleteKey(enemyKeys[i]); else PlayerPrefs.SetInt(enemyKeys[i], enemyValues[i]);
            PlayerPrefs.Save();
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }
    }
}
