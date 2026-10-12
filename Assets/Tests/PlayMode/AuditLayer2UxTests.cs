using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using AllinDefense.Core;
using AllinDefense.Data;
using AllinDefense.Presentation;
using AllinDefense.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace AllinDefense.Tests
{
    public sealed class AuditLayer2UxTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private GameSessionController session;
        private DefenderHudController hud;
        private AppFlowController frontend;
        private string savePath;
        private GameLanguage savedLanguage;
        private bool hadLanguage;
        private const string LanguageKey = "AllinDefense.Frontend.Language";
        private static object Call(object target, string method, params object[] args) =>
            target.GetType().GetMethod(method, Private).Invoke(target, args);
        private static T Field<T>(object target, string name) => (T)target.GetType().GetField(name, Private).GetValue(target);
        private Button Button(string name) => UiQuery.FindById<Button>(hud, name);
        private static string Label(Button button) => button.GetComponentInChildren<TMP_Text>(true).text;

        [SetUp] public void SetUp()
        {
            savedLanguage = GameLocalization.CurrentLanguage;
            hadLanguage = PlayerPrefs.HasKey(LanguageKey);
        }

        private void Create(int level = 10)
        {
            session = new GameObject("Audit mobile UX").AddComponent<GameSessionController>();
            session.ConfigurePresentation(Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog"));
            session.SetCampaignLevel(level);
            session.Initialize();
            hud = session.GetComponentInChildren<DefenderHudController>();
        }

        private GreyboxOutpost SelectTower()
        {
            Call(session, "EarnGold", 1000);
            Assert.That(session.TryBuildOutpost(0, OutpostKind.Archer), Is.True);
            var tower = session.Outposts[0];
            Call(session, "SelectOutpost", tower);
            return tower;
        }

        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (frontend != null) Object.Destroy(frontend.gameObject);
            if (session != null) Object.Destroy(session.gameObject);
            yield return null;
            Time.timeScale = 1;
            GameLocalization.SetLanguage(savedLanguage);
            if (!hadLanguage) PlayerPrefs.DeleteKey(LanguageKey);
            if (savePath != null && File.Exists(savePath)) File.Delete(savePath);
        }

        [UnityTest] public IEnumerator SellRequiresSecondTapAndShowsLocalizedRefund()
        {
            GameLocalization.SetLanguage(GameLanguage.Turkish);
            Create(); Time.timeScale = 0;
            var tower = SelectTower();
            int gold = session.Gold, refund = tower.SellValue;
            var sell = Button("Sell"); sell.onClick.Invoke();
            Assert.That(session.Outposts, Does.Contain(tower), "First tap must not sell");
            Assert.That(session.Gold, Is.EqualTo(gold));
            Assert.That(Label(sell), Does.Contain($"ONAYLA +{refund}"));
            GameLocalization.SetLanguage(GameLanguage.English);
            Assert.That(Label(sell), Does.Contain($"CONFIRM +{refund}"));
            sell.onClick.Invoke();
            Assert.That(session.Outposts.Count, Is.Zero);
            Assert.That(session.Gold, Is.EqualTo(gold + refund));
            yield return null;
        }

        [UnityTest] public IEnumerator EveryPhaseTransitionCancelsEvenWhenTheOriginalPhaseReturnsImmediately()
        {
            Create(); SelectTower(); Time.timeScale = 0;
            var sell = Button("Sell"); sell.onClick.Invoke();
            Call(session, "SetPhase", GamePhase.Intermission);
            Call(session, "SetPhase", GamePhase.Preparing);
            sell.onClick.Invoke();
            Assert.That(session.Outposts.Count, Is.EqualTo(1));
            yield return null;
        }

        [UnityTest] public IEnumerator LegacyEditorHudAlsoRequiresSaleConfirmation()
        {
            session = new GameObject("Audit legacy mobile UX").AddComponent<GameSessionController>();
            session.SetCampaignLevel(10); session.Initialize(); Time.timeScale = 0;
            var tower = SelectTower();
            var sell = UiQuery.FindById<Button>(session, "Sell");
            sell.onClick.Invoke(); Assert.That(session.Outposts, Does.Contain(tower));
            sell.onClick.Invoke(); Assert.That(session.Outposts.Count, Is.Zero);
            yield return null;
        }

        [UnityTest] public IEnumerator SellConfirmationExpiresUsingUnscaledTime()
        {
            Create(); SelectTower(); Time.timeScale = 0;
            var sell = Button("Sell"); sell.onClick.Invoke();
            yield return new WaitForSecondsRealtime(3.15f);
            sell.onClick.Invoke();
            Assert.That(session.Outposts.Count, Is.EqualTo(1));
            sell.onClick.Invoke();
            Assert.That(session.Outposts.Count, Is.Zero);
        }

        [UnityTest] public IEnumerator SelectionPanelAndPhaseChangesCancelSale()
        {
            Create(); var tower = SelectTower(); Time.timeScale = 0;
            var sell = Button("Sell");
            sell.onClick.Invoke();
            Call(session, "CancelSelection"); Call(session, "SelectOutpost", tower);
            sell.onClick.Invoke(); Assert.That(session.Outposts.Count, Is.EqualTo(1));
            Call(session, "SelectSlot", tower.Slot); // Same selection is deliberately preserved.
            Call(session, "SetPhase", GamePhase.Intermission);
            sell.onClick.Invoke(); Assert.That(session.Outposts.Count, Is.EqualTo(1));
            hud.SetSelected(null, null, false); Call(session, "SelectOutpost", tower);
            sell.onClick.Invoke(); Assert.That(session.Outposts.Count, Is.EqualTo(1));
            Assert.That(session.TryBuildOutpost(1, OutpostKind.Archer), Is.True);
            var other = session.Outposts[1]; Call(session, "SelectOutpost", other);
            sell.onClick.Invoke(); Assert.That(session.Outposts.Count, Is.EqualTo(2));
            yield return null;
        }

        [UnityTest] public IEnumerator ProgrammaticSaleStillExecutesImmediately()
        {
            Create(); var tower = SelectTower();
            Assert.That(session.TrySellOutpost(tower.Slot.Index), Is.True);
            Assert.That(session.Outposts.Count, Is.Zero);
            yield return null;
        }

        [UnityTest] public IEnumerator HudButtonsHaveAtLeast44PointHitRects()
        {
            Create(); Time.timeScale = 0;
            SelectTower(); Canvas.ForceUpdateCanvases(); yield return null;
            AssertHitRects(hud.GetComponentsInChildren<Button>(true));
        }

        [UnityTest] public IEnumerator KitResumeReceivesClicksAtItsPhysicalTouchTargetEdge()
        {
            Create(); Time.timeScale = 0;
            session.TogglePause(); Call(session, "RefreshUi");
            yield return new WaitForSecondsRealtime(.3f); Canvas.ForceUpdateCanvases();
            var resume = Button("Resume");
            var rect = (RectTransform)resume.transform;
            var point = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center + Vector2.up * (rect.rect.height * .45f)));
            Assert.That(RectTransformUtility.RectangleContainsScreenPoint(rect, point), Is.True);
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = point }, hits);
            Assert.That(hits.Count, Is.GreaterThan(0));
            Assert.That(ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject), Is.EqualTo(resume.gameObject));
            ExecuteEvents.Execute(resume.gameObject, new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
            Assert.That(session.Phase, Is.EqualTo(GamePhase.Preparing));
        }

        [UnityTest] public IEnumerator FrontendButtonsHaveAtLeast44PointHitRects()
        {
            var catalog = Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog");
            session = new GameObject("Audit frontend UX session").AddComponent<GameSessionController>();
            frontend = new GameObject("Audit frontend UX").AddComponent<AppFlowController>();
            savePath = Path.Combine(Path.GetTempPath(), $"dc-ux-{Guid.NewGuid():N}.json");
            frontend.Initialize(catalog, session, false, savePath);
            yield return new WaitForSecondsRealtime(.95f);
            Call(frontend, "CompleteLocalGuestSignIn"); Call(frontend, "OpenSettings");
            Canvas.ForceUpdateCanvases(); yield return null;
            AssertHitRects(frontend.GetComponentsInChildren<Button>(true));
            var import = UiQuery.FindById<Button>(frontend, "Import Guest Progress");
            Call(frontend, "CloseSettings");
            yield return new WaitForSecondsRealtime(.25f);
            import.gameObject.SetActive(true);
            Assert.That(import.gameObject.activeInHierarchy, Is.True);
            Assert.That(ExecuteEvents.Execute(import.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerDownHandler), Is.True);
            yield return new WaitForSecondsRealtime(.12f);
            Assert.That(import.transform.localScale.y, Is.LessThan(.99f), "Press feedback must actually shrink the visual");
            AssertHitRects(new[] { import });
        }

        private static void AssertHitRects(Button[] buttons)
        {

            foreach (var button in buttons)
            {
                var root = (RectTransform)button.transform;
                // Transparent raycast padding is a child so the original visuals retain their layout.
                var hit = UiQuery.FindById(button, "Touch Target") as RectTransform;
                var size = hit != null ? hit.rect.size : root.rect.size;
                var canvas = button.GetComponentInParent<Canvas>();
                if (canvas != null) size = Vector2.Scale(size, new Vector2(
                    Mathf.Abs(root.lossyScale.x / canvas.transform.lossyScale.x),
                    Mathf.Abs(root.lossyScale.y / canvas.transform.lossyScale.y)));
                if (size.x <= 0 || size.y <= 0) continue; // A lazy inactive screen has not been laid out yet.
                Debug.Log($"[UX_HIT_RECT] {button.name} visual={root.rect.size} hit={size}");
                Assert.That(size.x * UiMetrics.PointsPerCanvasUnit(canvas).x, Is.GreaterThanOrEqualTo(44), button.name + " width");
                Assert.That(size.y * UiMetrics.PointsPerCanvasUnit(canvas).y, Is.GreaterThanOrEqualTo(44), button.name + " height");
                if (hit != null) Assert.That(hit.GetComponent<Image>().raycastTarget, Is.True);
            }
        }

        [UnityTest] public IEnumerator SpecializationModalFreezesCombatAndRestoresEverySpeed()
        {
            Create(); var tower = SelectTower(); session.TryUpgradeOutpost(0);
            session.StartNextWave(); yield return null;
            foreach (int speedStep in new[] { 0, 1, 2, 3 })
            {
                while (session.CurrentGameSpeed != GameSessionController.SpeedForStep(speedStep)) Call(session, "ToggleGameSpeed");
                // Spawn through the normal battle clock, then observe actual frame progression.
                session.AdvanceBattle(.1f);
                var enemy = session.Enemies[0];
                hud.ShowSpecializationChoices(tower, session);
                float battle = session.BattleElapsedSeconds, wave = session.WaveElapsedSeconds;
                var position = enemy.Position;
                yield return new WaitForSecondsRealtime(2.1f);
                Assert.That(session.BattleElapsedSeconds, Is.EqualTo(battle));
                Assert.That(session.WaveElapsedSeconds, Is.EqualTo(wave));
                Assert.That(enemy.Position, Is.EqualTo(position));
                ModalClose().onClick.Invoke();
                Assert.That(session.Phase, Is.EqualTo(GamePhase.Wave));
                Assert.That(Time.timeScale, Is.EqualTo(GameSessionController.SpeedForStep(speedStep)));
                yield return new WaitForSecondsRealtime(.08f);
                Assert.That(session.BattleElapsedSeconds, Is.GreaterThan(battle));
            }
        }

        private Button ModalClose() => Field<GameObject>(hud, "specializationModal").GetComponentsInChildren<Button>()
            .Single(b => Label(b).StartsWith("BACK") || Label(b).StartsWith("GERİ"));

        [UnityTest] public IEnumerator AlreadyPausedAndTerminalSessionsDoNotResumeOnModalClose()
        {
            Create(); var tower = SelectTower(); session.TryUpgradeOutpost(0); session.TogglePause();
            hud.ShowSpecializationChoices(tower, session); ModalClose().onClick.Invoke();
            Assert.That(session.Phase, Is.EqualTo(GamePhase.Paused)); Assert.That(Time.timeScale, Is.Zero);
            yield return null;
            session.TogglePause(); hud.ShowSpecializationChoices(tower, session);
            Call(session, "SetPhase", GamePhase.Defeat);
            yield return null;
            Assert.That(Field<GameObject>(hud, "specializationModal") == null, Is.True);
            Assert.That(session.Phase, Is.EqualTo(GamePhase.Defeat));
        }

        [UnityTest] public IEnumerator BackgroundPauseAndDisabledHudReleaseModalWithoutUnexpectedResume()
        {
            Create(); var tower = SelectTower(); session.TryUpgradeOutpost(0);
            Call(session, "ToggleGameSpeed");
            hud.ShowSpecializationChoices(tower, session);
            Call(session, "ToggleGameSpeed"); session.TogglePause();
            Assert.That(session.CurrentGameSpeed, Is.EqualTo(2)); Assert.That(Time.timeScale, Is.Zero);
            Call(session, "PauseForBackground"); ModalClose().onClick.Invoke();
            Assert.That(session.Phase, Is.EqualTo(GamePhase.Paused)); Assert.That(Time.timeScale, Is.Zero);
            yield return null;
            session.TogglePause(); hud.ShowSpecializationChoices(tower, session);
            hud.gameObject.SetActive(false);
            Assert.That(session.Phase, Is.EqualTo(GamePhase.Preparing)); Assert.That(Time.timeScale, Is.EqualTo(2));
        }

        [UnityTest] public IEnumerator ChoosingBranchRestoresPauseBeforeApplyingTheUpgrade()
        {
            Create(); var tower = SelectTower(); session.TryUpgradeOutpost(0);
            hud.ShowSpecializationChoices(tower, session);
            var branch = TowerProgression.Branch(OutpostKind.Archer, 0);
            UiQuery.FindById<Button>(Field<GameObject>(hud, "specializationModal").transform, branch.ToString()).onClick.Invoke();
            Assert.That(tower.Specialization, Is.EqualTo(branch));
            Assert.That(session.Phase, Is.EqualTo(GamePhase.Preparing));
            Assert.That(Time.timeScale, Is.EqualTo(session.CurrentGameSpeed));
            yield return null;
        }

        [UnityTest] public IEnumerator Levels11And50ThreatsFollowLanguageChanges()
        {
            foreach (int level in new[] { 11, 50 })
            {
                GameLocalization.SetLanguage(GameLanguage.English);
                Create(level); Time.timeScale = 0;
                Assert.That(CampaignLevelFactory.Create(level).waves[0].preview, Is.EqualTo("Fast raiders"));
                Assert.That(Field<TMP_Text>(hud, "previewText").text, Is.EqualTo(FrontendCopy.UpcomingCount.Format(session.UpcomingEnemyCount)));
                Assert.That(Field<TMP_Text>(hud, "phaseText").text, Is.EqualTo(FrontendCopy.HudPreparing.Value));
                GameLocalization.SetLanguage(GameLanguage.Turkish);
                Assert.That(CampaignLevelFactory.Create(level).waves[0].preview, Is.EqualTo("Hızlı yağmacılar"));
                Assert.That(Field<WaveDefinition[]>(session, "waves")[0].preview, Is.EqualTo("Hızlı yağmacılar"));
                Assert.That(Field<TMP_Text>(hud, "previewText").text, Is.EqualTo(FrontendCopy.UpcomingCount.Format(session.UpcomingEnemyCount)));
                Assert.That(Field<TMP_Text>(hud, "phaseText").text, Is.EqualTo(FrontendCopy.HudPreparing.Value));
                GameLocalization.SetLanguage(GameLanguage.English);
                foreach (var wave in Field<WaveDefinition[]>(session, "waves"))
                    Assert.That(wave.preview, Does.Not.Match("[çğıöşüÇĞİÖŞÜ]|yağmac|öncü|birliği|muhafız"));
                Object.Destroy(session.gameObject); yield return null;
            }
        }
    }
}
