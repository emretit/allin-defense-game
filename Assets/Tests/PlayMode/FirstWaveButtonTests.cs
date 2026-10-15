using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AllinDefense.Core;
using AllinDefense.Data;
using AllinDefense.Presentation;
using AllinDefense.UI;
using AllinDefense.UI.Kit;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace AllinDefense.Tests
{
    public sealed class FirstWaveButtonTests
    {
        private GameSessionController session;
        private AppFlowController flow;
        private string savePath;
        private bool reducedMotion;
        private readonly Dictionary<string, int?> enemyPreferences = new();

        [UnitySetUp]
        public IEnumerator Setup()
        {
            reducedMotion = FrontendPreferences.ReducedMotion;
            foreach (EnemyArchetype enemy in Enum.GetValues(typeof(EnemyArchetype)))
            {
                string key = "AllinDefense.Frontend.EnemySeen." + enemy;
                enemyPreferences[key] = PlayerPrefs.HasKey(key) ? PlayerPrefs.GetInt(key) : null;
                FrontendPreferences.MarkEnemySeen(enemy);
            }
#if UNITY_EDITOR
            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                var review = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("AllinDefense.Editor.FrontendExperienceReview")).First(t => t != null);
                review.GetMethod("SetSize").Invoke(null, new object[] { 844, 390 });
            }
#endif
            Screen.SetResolution(844, 390, FullScreenMode.Windowed);
            yield return null;
            savePath = Path.Combine(Path.GetTempPath(), $"first-wave-{Guid.NewGuid():N}.json");
            var catalog = Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog");
            session = new GameObject("First wave session").AddComponent<GameSessionController>();
            session.ConfigurePresentation(catalog);
            flow = new GameObject("First wave flow").AddComponent<AppFlowController>();
            flow.Initialize(catalog, session, false, savePath);
            yield return new WaitForSecondsRealtime(1);
            flow.CompleteGuestFallback();
            yield return new WaitForSecondsRealtime(.3f);
        }

        [UnityTest] public IEnumerator GuidedFirstBattleStartsFromWaveButton() => StartFromButton(1, false);
        [UnityTest] public IEnumerator UnguidedBattleStartsFromWaveButton() => StartFromButton(7, false);
        [UnityTest] public IEnumerator GuidedFirstBattleStartsAfterVisibleEnemyAcknowledgement() => StartFromButton(1, true);
        [UnityTest] public IEnumerator UnguidedBattleStartsAfterVisibleEnemyAcknowledgement() => StartFromButton(7, true);

        private IEnumerator StartFromButton(int level, bool newEnemy)
        {
            FrontendPreferences.ReducedMotion = false;
            if (newEnemy) PlayerPrefs.DeleteKey("AllinDefense.Frontend.EnemySeen.Raider");
            flow.Progress.Data.unlockedLevel = level;
            flow.LaunchCampaignLevel(level);
            double deadline = Time.realtimeSinceStartupAsDouble + 20;
            while (flow.IsLaunchingBattle && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
            Assert.That(flow.IsLaunchingBattle, Is.False);
            Assert.That(flow.CurrentScreen, Is.EqualTo(AppScreen.Battle));
            Assert.That(session.Phase, Is.EqualTo(GamePhase.Preparing));
            Assert.That(session.TryBuildOutpost(0, OutpostKind.Archer), Is.True);
            Assert.That(session.TryBuildOutpost(1, OutpostKind.Archer), Is.True);
            var hud = session.GetComponentInChildren<DefenderHudController>();
            hud.BattleRadial.Close();
            yield return null;
            yield return null;
            Debug.Log($"[FIRST_WAVE_UI] level={level}, newEnemy={newEnemy}, screen={Screen.width}x{Screen.height}");
            var waveButton = UiQuery.FindById<Button>(hud, "NextWave");
            if (level == 1)
            {
                Assert.That(hud.TutorialTarget, Is.EqualTo(waveButton.transform));
                Assert.That(hud.TutorialArrow.gameObject.activeInHierarchy, Is.True);
            }
            else Assert.That(hud.TutorialTarget, Is.Null);
            ClickAtScreenPosition(waveButton);
            if (newEnemy)
            {
                Assert.That(session.Phase, Is.EqualTo(GamePhase.Paused));
                var modal = UiQuery.FindById<UiOverlay>(flow, "New Enemy");
                Assert.That(modal, Is.Not.Null);
                Assert.That(modal.gameObject.activeInHierarchy, Is.True,
                    "The first wave was intercepted, but its enemy introduction is hidden under the disabled frontend canvas.");
                yield return new WaitForSecondsRealtime(.3f);
                Assert.That(session.IntroModalOpen, Is.True);
                Assert.That(UiQuery.FindById<UiOverlay>(hud, "PauseOverlay").gameObject.activeSelf, Is.False);
                foreach (string id in new[] { "New Enemy Trait", "New Enemy Tactic" })
                    Assert.That(UiQuery.FindById<TMPro.TMP_Text>(flow, id).fontSize, Is.EqualTo(32));
                var tactic = UiQuery.FindById<TMPro.TMP_Text>(flow, "New Enemy Tactic");
                Assert.That(tactic.overflowMode, Is.EqualTo(TMPro.TextOverflowModes.Overflow));
                Assert.That(tactic.rectTransform.rect.height, Is.GreaterThanOrEqualTo(tactic.preferredHeight - 1));
                ClickAtScreenPosition(UiQuery.FindById<Button>(flow, "Acknowledge Enemy"));
                yield return new WaitForSecondsRealtime(.3f);
                Assert.That(FrontendPreferences.HasSeenEnemy(EnemyArchetype.Raider), Is.True);
                Assert.That(UiQuery.FindById<UiOverlay>(flow, "New Enemy"), Is.Null);
            }
            Assert.That(session.IntroModalOpen, Is.False);
            Assert.That(session.Phase, Is.EqualTo(GamePhase.Wave));
            Assert.That(Time.timeScale, Is.EqualTo(session.CurrentGameSpeed));
            yield return null;
            ClickAtScreenPosition(UiQuery.FindById<Button>(hud, "Pause"));
            Assert.That(session.Phase, Is.EqualTo(GamePhase.Paused), "Closing the introduction must leave the battle HUD reachable.");
        }

        private static void ClickAtScreenPosition(Button button)
        {
            Assert.That(button, Is.Not.Null);
            Assert.That(button.gameObject.activeInHierarchy && button.IsInteractable(), Is.True, button.name);
            Canvas.ForceUpdateCanvases();
            var rect = (RectTransform)button.transform;
            Vector2 point = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
            Assert.That(Screen.safeArea.Contains(point), Is.True, $"{button.name} is outside the safe area at {point}");
            var pointer = new PointerEventData(EventSystem.current) { position = point, button = PointerEventData.InputButton.Left };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            Assert.That(hits, Is.Not.Empty, button.name);
            var target = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject);
            Assert.That(target, Is.EqualTo(button.gameObject),
                $"{button.name} at {point} is blocked by {hits[0].gameObject.name}; hits: {string.Join(", ", hits.Select(h => h.gameObject.name))}");
            pointer.pointerCurrentRaycast = hits[0];
            ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerClickHandler);
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (flow != null) Object.Destroy(flow.gameObject);
            if (session != null) Object.Destroy(session.gameObject);
            yield return null;
            foreach (var entry in enemyPreferences)
                if (entry.Value.HasValue) PlayerPrefs.SetInt(entry.Key, entry.Value.Value); else PlayerPrefs.DeleteKey(entry.Key);
            enemyPreferences.Clear();
            FrontendPreferences.ReducedMotion = reducedMotion;
            PlayerPrefs.Save();
            Time.timeScale = 1;
            if (File.Exists(savePath)) File.Delete(savePath);
#if UNITY_EDITOR
            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                var review = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("AllinDefense.Editor.FrontendExperienceReview")).First(t => t != null);
                review.GetMethod("RestoreSize").Invoke(null, null);
            }
#endif
        }
    }
}
