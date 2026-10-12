using System.Collections;
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
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace AllinDefense.Tests
{
    public sealed class BattleRadialInteractionTests
    {
        private GameSessionController session;
        private DefenderHudController hud;
        private GameLanguage language;
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static object Call(object target, string method, params object[] args) => target.GetType().GetMethod(method, Private).Invoke(target, args);
        private GreyboxOutpostSlot Slot(int index) => session.GetComponentsInChildren<GreyboxOutpostSlot>().First(s => s.Index == index);
        private Button Choice(string id) => UiQuery.FindById<Button>(hud.BattleRadial, id);
        private TMP_Text Hint => UiQuery.FindById<TMP_Text>(hud, "Hint");
        [SetUp] public void Setup()
        {
            language = GameLocalization.CurrentLanguage;
            GameLocalization.SetLanguage(GameLanguage.Turkish);
            session = new GameObject("Radial interaction fixture").AddComponent<GameSessionController>();
            session.ConfigurePresentation(Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog"));
            session.SetCampaignLevel(10); session.Initialize();
            hud = session.GetComponentInChildren<DefenderHudController>(); Time.timeScale = 0;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Object.Destroy(session.gameObject); yield return null;
            GameLocalization.SetLanguage(language); Time.timeScale = 1;
        }
        [UnityTest] public IEnumerator BuildPreviewsThenConfirmsAndChangingSlotCannotSpendOnPreviousPreview()
        {
            Call(session, "SelectSlot", Slot(0)); int gold = session.Gold;
            Choice("Archer").onClick.Invoke();
            Assert.That(session.Gold, Is.EqualTo(gold)); Assert.That(Slot(0).Occupant, Is.Null);
            Assert.That(Hint.text, Does.Contain("75"));
            Assert.That(hud.GetComponentsInChildren<CombatRangeVisual>().Any(v => v.name == "Radial preview range"), Is.True);
            Call(session, "SelectSlot", Slot(1));
            Assert.That(hud.GetComponentsInChildren<CombatRangeVisual>().First(v => v.name == "Radial preview range").GetComponent<LineRenderer>().enabled, Is.False);
            Choice("Archer").onClick.Invoke();
            Assert.That(session.Gold, Is.EqualTo(gold));
            Choice("Archer").onClick.Invoke();
            Assert.That(Slot(0).Occupant, Is.Null); Assert.That(Slot(1).Occupant, Is.Not.Null);
            Assert.That(session.Gold, Is.EqualTo(gold - TowerProgression.Base(OutpostKind.Archer).cost));
            Assert.That(Choice("Upgrade").gameObject.activeSelf, Is.True);
            yield return null;
        }
        [UnityTest] public IEnumerator UnaffordableChoiceExplainsMissingGoldAndRejectsConfirmation()
        {
            Call(session, "SpendGold", session.Gold - 50);
            Call(session, "SelectSlot", Slot(0));
            int rejects = 0; hud.RadialRejected += _ => rejects++;
            var archer = Choice("Archer"); Assert.That(archer.interactable, Is.True);
            archer.onClick.Invoke(); Assert.That(Hint.text, Does.Contain("25 eksik"));
            archer.onClick.Invoke(); Assert.That(rejects, Is.EqualTo(1));
            Assert.That(session.Gold, Is.EqualTo(50)); Assert.That(session.Outposts, Is.Empty);
            Assert.That(archer.GetComponent<UiVisualState>().Price.color, Is.EqualTo(UiTokens.Danger));
            yield return null;
        }
        [UnityTest] public IEnumerator WorldBackgroundClickClosesWithoutScrimBlockingWorldSelection()
        {
            Call(session, "SelectSlot", Slot(0)); Choice("Archer").onClick.Invoke();
            Assert.That(hud.BattleRadial.GetComponent<Image>().raycastTarget, Is.False);
            var mouse = InputSystem.AddDevice<Mouse>();
            try
            {
                InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(5, Screen.height * .5f) }.WithButton(MouseButton.Left));
                InputSystem.Update();
                Call(session, "HandleWorldPointer");
                Assert.That(hud.BattleRadial.IsOpen, Is.False);
                Assert.That(UiQuery.FindById(hud, "SelectedPanel").gameObject.activeSelf, Is.False);
                Assert.That(session.Outposts, Is.Empty);
            }
            finally { InputSystem.RemoveDevice(mouse); }
            yield return null;
        }
        [UnityTest] public IEnumerator UpgradePreviewsActualDeltaThenSpendsOnce()
        {
            Call(session, "EarnGold", 1000); session.TryBuildOutpost(0, OutpostKind.Archer);
            int gold = session.Gold;
            Choice("Upgrade").onClick.Invoke();
            Assert.That(session.Outposts[0].Level, Is.EqualTo(1)); Assert.That(session.Gold, Is.EqualTo(gold));
            Assert.That(UiQuery.FindById<TMP_Text>(hud, "Description").text, Does.Contain("24→42"));
            yield return null; Canvas.ForceUpdateCanvases();
            var stats = UiQuery.FindById<TMP_Text>(hud, "Description"); stats.ForceMeshUpdate();
            Assert.That(stats.isTextOverflowing, Is.False);
            Assert.That(hud.BattleRadial.GetComponentsInChildren<Transform>(true).Any(t => t.name == "StateStripe"), Is.False);
            Choice("Upgrade").onClick.Invoke();
            Assert.That(session.Outposts[0].Level, Is.EqualTo(2)); Assert.That(session.Gold, Is.EqualTo(gold - 55));
            yield return null;
        }
        [UnityTest] public IEnumerator EdgeRadialStaysOnScreenAndAvoidsFrameAndInformationCard()
        {
            Call(session, "SelectSlot", Slot(0)); yield return null; Canvas.ForceUpdateCanvases();
            int intersections = 0;
            foreach (var point in new[] { new Vector2(0, 0), new Vector2(Screen.width, Screen.height), new Vector2(Screen.width / 2, 0) })
            {
                Call(hud, "PositionBattleRadial", point); Canvas.ForceUpdateCanvases();
                foreach (var button in hud.BattleRadial.GetComponentsInChildren<Button>())
                {
                    var corners = new Vector3[4]; ((RectTransform)button.transform).GetWorldCorners(corners);
                    Vector2 min = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
                    Vector2 max = RectTransformUtility.WorldToScreenPoint(null, corners[2]);
                    Assert.That(min.x, Is.GreaterThanOrEqualTo(Screen.safeArea.xMin));
                    Assert.That(min.y, Is.GreaterThanOrEqualTo(Screen.safeArea.yMin));
                    Assert.That(max.x, Is.LessThanOrEqualTo(Screen.safeArea.xMax));
                    Assert.That(max.y, Is.LessThanOrEqualTo(Screen.safeArea.yMax));
                    var rect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
                    intersections += hud.GetComponent<DefenderHudResponsiveLayout>().FrameScreenRects().Count(r => r.Overlaps(rect));
                    Assert.That(intersections, Is.Zero);
                }
            }
            Debug.Log($"[RADIAL_FRAME_INTERSECTIONS] {intersections}");
        }
        [UnityTest] public IEnumerator InformationCardRendersAllStatsAndSpecializationDifferencesInBothLanguages()
        {
            Call(session, "EarnGold", 1000);
            foreach (var value in new[] { GameLanguage.Turkish, GameLanguage.English })
            {
                GameLocalization.SetLanguage(value);
                Call(session, "SelectSlot", Slot(0)); Choice("Archer").onClick.Invoke();
                yield return null; Canvas.ForceUpdateCanvases();
                var stats = UiQuery.FindById<TMP_Text>(hud, "Description"); stats.ForceMeshUpdate();
                Assert.That(stats.isTextOverflowing, Is.False, stats.text);
                Assert.That(stats.textInfo.lineCount, Is.EqualTo(1));
                Call(session, "CancelSelection");
            }
            session.TryBuildOutpost(0, OutpostKind.Archer); session.TryUpgradeOutpost(0);
            foreach (var value in new[] { GameLanguage.Turkish, GameLanguage.English })
            {
                GameLocalization.SetLanguage(value);
                Choice("RapidFire").onClick.Invoke();
                yield return null; Canvas.ForceUpdateCanvases();
                var stats = UiQuery.FindById<TMP_Text>(hud, "Description"); stats.ForceMeshUpdate();
                Assert.That(stats.isTextOverflowing, Is.False, stats.text);
                Assert.That(stats.textInfo.lineCount, Is.EqualTo(2));
                hud.BattleRadial.Close(); Call(session, "SelectOutpost", session.Outposts[0]);
            }
        }
        [UnityTest] public IEnumerator TutorialArrowTracksItsWorldTargetAndCanBeRecalled()
        {
            var target = Slot(0).transform;
            hud.SetTutorialGuide(0, target, Camera.main); yield return null;
            Assert.That(hud.TutorialTarget, Is.EqualTo(target));
            Vector3 old = hud.TutorialArrow.position;
            target.position += Vector3.right * 2;
            yield return null;
            Assert.That(Vector3.Distance(hud.TutorialArrow.position, old), Is.GreaterThan(1));
            UiQuery.FindById<Button>(hud, "TutorialSkip").onClick.Invoke(); yield return null;
            Assert.That(hud.TutorialArrow.gameObject.activeInHierarchy, Is.False);
            UiQuery.FindById<Button>(hud, "TutorialHelp").onClick.Invoke(); yield return null;
            Assert.That(hud.TutorialArrow.gameObject.activeInHierarchy, Is.True);
            Call(session, "SelectSlot", Slot(0)); yield return null;
            Assert.That(hud.TutorialArrow.gameObject.activeInHierarchy, Is.False, "Guide must not cover radial actions");
            UiQuery.FindById<Button>(hud, "TutorialHelp").onClick.Invoke(); yield return null;
            Assert.That(hud.BattleRadial.IsOpen, Is.False);
            Assert.That(hud.TutorialArrow.gameObject.activeInHierarchy, Is.True);
            hud.SetTutorialGuide(3, null, Camera.main); yield return null;
            Assert.That(UiQuery.FindById(hud, "TutorialAbilityRing").gameObject.activeInHierarchy, Is.True);
        }
    }
}
