using System;
using System.Collections;
using System.IO;
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
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace AllinDefense.Tests
{
    public sealed class BattleWaveFlowTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private GameSessionController session;
        private DefenderHudController hud;
        private string path;
        private GameLanguage language;
        private bool reducedMotion;
        [SetUp] public void Setup()
        {
            language = GameLocalization.CurrentLanguage;
            reducedMotion = FrontendPreferences.ReducedMotion;
            GameLocalization.SetLanguage(GameLanguage.Turkish);
            FrontendPreferences.ReducedMotion = true;
        }
        private void Create(CampaignProgressStore store = null)
        {
            session = new GameObject("Wave flow test").AddComponent<GameSessionController>();
            if (store != null) session.ConfigureCampaignStore(store);
            session.ConfigurePresentation(Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog"));
            session.Initialize();
            hud = session.GetComponentInChildren<DefenderHudController>();
            Time.timeScale = 0;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (session != null) Object.Destroy(session.gameObject);
            yield return null;
            Time.timeScale = 1;
            GameLocalization.SetLanguage(language);
            FrontendPreferences.ReducedMotion = reducedMotion;
            if (path != null && File.Exists(path)) File.Delete(path);
        }
        [UnityTest] public IEnumerator IntermissionCountsDownAndForecastSurvivesFrameRefresh()
        {
            Create();
            var wave = new WaveDefinition("flow", "Yağmacı ×50", 0, false,
                new[] { new SpawnGroupDefinition { enemy = EnemyArchetype.Raider, count = 50, interval = 1 } });
            typeof(GameSessionController).GetField("waves", Private).SetValue(session, new[] { wave, wave });
            typeof(GameSessionController).GetMethod("RefreshThreatPreviewForPhase", Private).Invoke(session, null);
            hud.Refresh(session);
            var forecast = UiQuery.FindById<TMP_Text>(hud, "WaveForecast.Label");
            Assert.That(forecast.text, Is.EqualTo("Sıradaki: Yağmacı ×50"));
            Assert.That(forecast.gameObject.activeInHierarchy, Is.True);
            typeof(GameSessionController).GetField("currentWaveIndex", Private).SetValue(session, 1);
            typeof(GameSessionController).GetField("intermissionRemaining", Private).SetValue(session, 8f);
            typeof(GameSessionController).GetMethod("SetPhase", Private).Invoke(session, new object[] { GamePhase.Intermission });
            typeof(GameSessionController).GetMethod("RefreshThreatPreviewForPhase", Private).Invoke(session, null);
            hud.Refresh(session);
            var label = UiQuery.FindById<Button>(hud, "NextWave").GetComponentInChildren<TMP_Text>();
            Assert.That(label.text, Is.EqualTo("SONRAKİ DALGA · 8"));
            session.AdvanceBattle(1f); hud.Refresh(session);
            Assert.That(label.text, Is.EqualTo("SONRAKİ DALGA · 7"));
            Assert.That(forecast.text, Is.EqualTo("Sıradaki: Yağmacı ×50"));
            Assert.That(UiQuery.FindById<Image>(hud, "WaveCountdownRing").fillAmount, Is.EqualTo(.125f).Within(.001));
            Assert.That(UiQuery.FindById<Image>(hud, "WaveCountdownRing").transform.localScale, Is.EqualTo(Vector3.one));
            yield return null;
        }
        [UnityTest] public IEnumerator RestartRequiresConfirmationAndSafeReturnPreservesCheckpoint()
        {
            path = Path.Combine(Path.GetTempPath(), "wave-confirm-" + Guid.NewGuid().ToString("N") + ".json");
            var store = new CampaignProgressStore(path);
            Create(store);
            typeof(GameSessionController).GetMethod("SaveWaveCheckpoint", Private).Invoke(session, null);
            Assert.That(store.Data.checkpoint, Is.Not.Null);
            int restarts = 0;
            session.ConfigureBattleNavigation(() => restarts++, () => { });
            session.TogglePause(); hud.Refresh(session);
            UiQuery.FindById<Button>(hud, "PauseRestart").onClick.Invoke();
            Assert.That(hud.RestartConfirmationOpen, Is.True);
            yield return null;
            Assert.That(EventSystem.current, Is.Not.Null);
            Assert.That(EventSystem.current.currentSelectedGameObject,
                Is.SameAs(UiQuery.FindById<Button>(hud, "ReturnToGame").gameObject));
            Assert.That(restarts, Is.Zero);
            Assert.That(store.Data.checkpoint, Is.Not.Null);
            UiQuery.FindById<Button>(hud, "ReturnToGame").onClick.Invoke();
            hud.Refresh(session);
            Assert.That(session.Phase, Is.EqualTo(GamePhase.Preparing));
            Assert.That(store.Data.checkpoint, Is.Not.Null);
            Assert.That(new CampaignProgressStore(path).Data.checkpoint, Is.Not.Null);
            session.TogglePause(); hud.Refresh(session);
            UiQuery.FindById<Button>(hud, "PauseRestart").onClick.Invoke();
            UiQuery.FindById<Button>(hud, "ConfirmRestart").onClick.Invoke();
            Assert.That(restarts, Is.EqualTo(1));
            Assert.That(store.Data.checkpoint, Is.Null);
            yield return null;
        }
        [UnityTest] public IEnumerator HeroReturnUsesStatusInsteadOfAbilityCooldown()
        {
            Create();
            typeof(GreyboxHero).GetField("downed", Private).SetValue(session.Hero, true);
            typeof(GreyboxHero).GetProperty("RespawnRemaining").SetValue(session.Hero, 8f);
            hud.Refresh(session);
            Assert.That(hud.HeroDisplayLabel, Is.EqualTo(FrontendCopy.SweepHeroReturning.Format(8)));
            Assert.That(UiQuery.FindById<Button>(hud, "Ability").interactable, Is.False);
            yield return null;
        }
    }
}
