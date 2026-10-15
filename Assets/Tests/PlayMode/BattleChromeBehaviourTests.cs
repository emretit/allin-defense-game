using System.Collections;
using System.Reflection;
using AllinDefense.Core;
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
    public sealed class BattleChromeBehaviourTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private GameSessionController session;
        private DefenderHudController hud;
        private GameLanguage language;
        private bool motion;

        [SetUp] public void Setup()
        {
            language = GameLocalization.CurrentLanguage;
            motion = FrontendPreferences.ReducedMotion;
            FrontendPreferences.ReducedMotion = true;
            session = new GameObject("Battle chrome test").AddComponent<GameSessionController>();
            session.ConfigurePresentation(Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog"));
            session.Initialize();
            hud = session.GetComponentInChildren<DefenderHudController>();
        }

        [UnityTearDown] public IEnumerator Cleanup()
        {
            Object.Destroy(session.gameObject);
            yield return null;
            Time.timeScale = 1;
            GameLocalization.SetLanguage(language);
            FrontendPreferences.ReducedMotion = motion;
        }

        [UnityTest] public IEnumerator IntroSuppressesPauseAndRestoresOnlyItsOwnPause()
        {
            Assert.That(session.BeginIntroModal(), Is.True);
            hud.Refresh(session);
            Assert.That(session.Phase, Is.EqualTo(GamePhase.Paused));
            Assert.That(UiQuery.FindById<UiOverlay>(hud, "PauseOverlay").gameObject.activeSelf, Is.False);
            session.EndIntroModal();
            Assert.That(session.Phase, Is.EqualTo(GamePhase.Preparing));
            Assert.That(Time.timeScale, Is.EqualTo(session.CurrentGameSpeed));
            session.TogglePause();
            session.BeginIntroModal(); session.EndIntroModal();
            Assert.That(session.Phase, Is.EqualTo(GamePhase.Paused));
            yield return null;
        }

        [UnityTest] public IEnumerator SpecializationKeepsWaveActionVisibleButDisabled()
        {
            foreach (var phase in new[] { GamePhase.Preparing, GamePhase.Intermission })
            {
                typeof(GameSessionController).GetMethod("SetPhase", Private).Invoke(session, new object[] { phase });
                Assert.That(session.BeginSpecializationModal(), Is.True);
                hud.Refresh(session);
                var button = UiQuery.FindById<Button>(hud, "NextWave");
                Assert.That(button.gameObject.activeInHierarchy, Is.True);
                Assert.That(button.interactable, Is.False);
                session.EndSpecializationModal(); hud.Refresh(session);
                Assert.That(button.interactable, Is.True);
            }
            yield return null;
        }

        [UnityTest] public IEnumerator RewardAndDamageCanvasHideOnPauseAndStayBelowHud()
        {
            var pool = session.GetComponentInChildren<DefenderRewardPopupPool>();
            Assert.That(pool, Is.Not.Null);
            pool.Play(new PresentationSignal(PresentationCue.EnemyDefeated, Vector3.zero, value: 2));
            pool.Play(new PresentationSignal(PresentationCue.UiError, Vector3.zero, value: 1));
            Assert.That(pool.ActiveCount, Is.EqualTo(2));
            var canvas = pool.GetComponentInChildren<Canvas>();
            Assert.That(canvas.sortingOrder, Is.LessThan(hud.GetComponentInParent<Canvas>().sortingOrder));
            session.TogglePause(); yield return null;
            Assert.That(canvas.enabled, Is.False);
            session.TogglePause(); yield return null;
            Assert.That(canvas.enabled, Is.True);
        }

        [UnityTest] public IEnumerator WaveBannerUsesForecastPointSlotWithoutCoveringWavePanel()
        {
            FrontendPreferences.ReducedMotion = false;
            hud.ShowWaveBanner(2, 5);
            Canvas.ForceUpdateCanvases();
            var banner = (RectTransform)hud.transform.Find("TopBar/Wave Start Banner");
            var forecast = UiQuery.FindById<RectTransform>(hud, "WaveForecast");
            Assert.That(banner.parent, Is.EqualTo(forecast.parent));
            Assert.That(banner.anchoredPosition, Is.EqualTo(forecast.anchoredPosition));
            Assert.That(Bounds(banner).Overlaps(Bounds(UiQuery.FindById<RectTransform>(hud, "WavePanel"))), Is.False);
            var label = banner.GetComponentInChildren<TMP_Text>();
            Assert.That(label.fontSize, Is.EqualTo(16));
            Assert.That(label.GetComponent<UiAccessibleText>().Label, Does.Contain("2/5"));
            yield return null;
        }

        [UnityTest] public IEnumerator ReturningHeroFitsBothLanguagesAndNamesCanAutosize()
        {
            typeof(GreyboxHero).GetField("downed", Private).SetValue(session.Hero, true);
            typeof(GreyboxHero).GetProperty("RespawnRemaining").SetValue(session.Hero, 12f);
            foreach (var selected in new[] { GameLanguage.Turkish, GameLanguage.English })
            {
                GameLocalization.SetLanguage(selected); hud.Refresh(session);
                var label = UiQuery.FindById<TMP_Text>(hud, "Hero");
                label.ForceMeshUpdate();
                Assert.That(label.text, Is.EqualTo(FrontendCopy.SweepHeroReturning.Format(12)));
                Assert.That(label.isTextOverflowing, Is.False);
                Assert.That(label.enableAutoSizing, Is.True);
                Assert.That(label.fontSizeMin, Is.EqualTo(10));
                Assert.That(label.fontSizeMax, Is.EqualTo(12));
            }
            yield return null;
        }

        [UnityTest] public IEnumerator EveryTutorialStepAvoidsForecastAndHeroControls()
        {
            Time.timeScale = 0;
            var target = session.GetComponentInChildren<GreyboxOutpostSlot>().transform;
            for (int step = 0; step <= 6; step++)
            {
                hud.SetTutorialGuide(step, target, Camera.main);
                Canvas.ForceUpdateCanvases();
                var bubble = UiQuery.FindById<RectTransform>(hud, "TutorialGuide");
                Assert.That(bubble.gameObject.activeInHierarchy, Is.True);
                foreach (string id in new[] { "WaveForecast", "Hero", "AbilityCooldown", "NextWave" })
                    Assert.That(Bounds(bubble).Overlaps(Bounds(UiQuery.FindById<RectTransform>(hud, id))), Is.False, $"step {step}: {id}");
            }
            foreach (int step in new[] { 0, 1, 4 })
            {
                var slot = session.GetComponentInChildren<GreyboxOutpostSlot>();
                typeof(GameSessionController).GetMethod("SelectSlot", Private).Invoke(session, new object[] { slot });
                Assert.That(hud.BattleRadial.IsOpen, Is.True);
                hud.SetTutorialGuide(step, target, Camera.main);
                Canvas.ForceUpdateCanvases();
                var bubble = UiQuery.FindById<RectTransform>(hud, "TutorialGuide");
                Assert.That(bubble.gameObject.activeInHierarchy, Is.True);
                foreach (string id in new[] { "WaveForecast", "Hero", "AbilityCooldown", "NextWave" })
                    Assert.That(Bounds(bubble).Overlaps(Bounds(UiQuery.FindById<RectTransform>(hud, id))), Is.False, $"build step {step}: {id}");
                hud.BattleRadial.Close();
            }
            yield return null;
        }

        private static Rect Bounds(RectTransform rect)
        {
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }
    }
}
