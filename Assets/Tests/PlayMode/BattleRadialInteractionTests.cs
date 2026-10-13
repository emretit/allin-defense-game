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
        private InputSettings.BackgroundBehavior backgroundBehavior;
        private InputSettings.EditorInputBehaviorInPlayMode editorInputBehavior;
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static object Call(object target, string method, params object[] args) => target.GetType().GetMethod(method, Private).Invoke(target, args);
        private GreyboxOutpostSlot Slot(int index) => session.GetComponentsInChildren<GreyboxOutpostSlot>().First(s => s.Index == index);
        private Button Choice(string id) => UiQuery.FindById<Button>(hud.BattleRadial, id);
        private TMP_Text Hint => UiQuery.FindById<TMP_Text>(hud, "Hint");
        [UnitySetUp] public IEnumerator Setup()
        {
            backgroundBehavior = InputSystem.settings.backgroundBehavior;
            editorInputBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
            yield return null;
            language = GameLocalization.CurrentLanguage;
            GameLocalization.SetLanguage(GameLanguage.Turkish);
            session = new GameObject("Radial interaction fixture").AddComponent<GameSessionController>();
            session.ConfigurePresentation(Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog"));
            session.SetCampaignLevel(10); session.Initialize();
            hud = session.GetComponentInChildren<DefenderHudController>(); Time.timeScale = 0;
            yield return null;
            Physics.SyncTransforms();
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Object.Destroy(session.gameObject); yield return null;
            InputSystem.settings.backgroundBehavior = backgroundBehavior;
            InputSystem.settings.editorInputBehaviorInPlayMode = editorInputBehavior;
#if UNITY_EDITOR
            var review = System.AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType("AllinDefense.Editor.FrontendExperienceReview")).FirstOrDefault(type => type != null);
            review?.GetMethod("RestoreSize").Invoke(null, null);
#endif
            GameLocalization.SetLanguage(language); Time.timeScale = 1;
        }
        [UnityTest] public IEnumerator PaintedRadialIconsAreLargeMaskedAndLockedOptionsStayAccessible()
        {
            session.SetCampaignLevel(1);
            Call(session, "SelectSlot", Slot(0)); yield return null; Canvas.ForceUpdateCanvases();
            foreach (string id in new[] { "Archer", "Barracks", "Mage" })
            {
                var button = Choice(id); var state = button.GetComponent<UiVisualState>();
                Assert.That(state.Icon.rectTransform.rect.width / ((RectTransform)button.transform).rect.width, Is.GreaterThanOrEqualTo(.75f));
                Assert.That(state.Icon.GetComponentInParent<Mask>(), Is.Not.Null);
                Assert.That(button.GetComponentsInChildren<Image>().Any(image => image.name == "PriceCoin"), Is.True);
                Assert.That(button.GetComponentsInChildren<TMP_Text>().Any(text => text.text.Contains(FrontendCopy.Locked.Value)), Is.False);
                if (!session.CanBuild(System.Enum.Parse<OutpostKind>(id)))
                {
                    Assert.That(button.GetComponent<UiAccessibleButton>().Label, Does.Contain(FrontendCopy.Locked.Value));
                    Assert.That(button.transform.Find("LockBadge").gameObject.activeSelf, Is.True);
                }
            }
            var catalog = Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog");
            foreach (string id in new[] { "Upgrade", "Sell", "RapidFire", "Sharpshooter", "ShieldGuard", "AssaultGuard", "Blast", "Frost" })
            {
                var sprite = (Sprite)typeof(DefenderPresentationCatalog).GetField("radial" + id + "Icon").GetValue(catalog);
                Assert.That(Choice(id).GetComponent<UiVisualState>().Icon.sprite, Is.SameAs(sprite), id);
                Assert.That(sprite, Is.Not.Null, id);
            }
        }

        [UnityTest] public IEnumerator ReducedMotionKeepsSelectionRingAndEmptySiteLightSteady()
        {
            bool reduced = FrontendPreferences.ReducedMotion;
            try
            {
                FrontendPreferences.ReducedMotion = true;
                Call(session, "SelectSlot", Slot(0)); Choice("Archer").onClick.Invoke(); yield return null;
                Assert.That(Choice("Archer").transform.localScale, Is.EqualTo(Vector3.one));
                Assert.That(Choice("Archer").transform.Find("ConfirmationMark").gameObject.activeSelf, Is.True);
                var ring = Slot(1).transform.Find("SlotGoldRing").GetComponent<Renderer>();
                Assert.That(ring.sharedMaterial.IsKeywordEnabled("_EMISSION"), Is.True);
                var properties = new MaterialPropertyBlock(); ring.GetPropertyBlock(properties);
                Color emission = properties.GetColor("_EmissionColor");
                Assert.That(emission.maxColorComponent, Is.GreaterThan(0));
                yield return new WaitForSecondsRealtime(.12f); ring.GetPropertyBlock(properties);
                Assert.That(properties.GetColor("_EmissionColor"), Is.EqualTo(emission));
                Call(session, "EarnGold", 1000); session.TryBuildOutpost(1, OutpostKind.Archer); Slot(1).SetSelected(false);
                ring.GetPropertyBlock(properties);
                Assert.That(properties.GetColor("_EmissionColor"), Is.EqualTo(Color.black));
                Assert.That(Slot(1).GetComponent<DefenderSlotVisual>().ShowsBuildPlus, Is.False);
            }
            finally { FrontendPreferences.ReducedMotion = reduced; }
        }

        [UnityTest] public IEnumerator VisibleBuildUpgradeAndSpecializeActionsSpendOnlyAfterConfirmation()
        {
            Call(session, "EarnGold", 1000); Call(session, "SelectSlot", Slot(0));
            int gold = session.Gold; Choice("Archer").onClick.Invoke();
            Assert.That(hud.RadialAction.gameObject.activeInHierarchy, Is.True);
            Assert.That(hud.RadialAction.GetComponentInChildren<TMP_Text>().text, Is.EqualTo("KUR · 75"));
            Assert.That(session.Gold, Is.EqualTo(gold)); Assert.That(Slot(0).Occupant, Is.Null);
            Assert.That(Hint.text, Does.Contain(FrontendCopy.RadialTapBuild.Value));
            yield return null; Canvas.ForceUpdateCanvases(); AssertActionFits();
            Choice("Barracks").onClick.Invoke(); Assert.That(hud.RadialAction.GetComponentInChildren<TMP_Text>().text, Is.EqualTo("KUR · 100"));
            Choice("Archer").onClick.Invoke(); hud.RadialAction.onClick.Invoke();
            Assert.That(Slot(0).Occupant.Kind, Is.EqualTo(OutpostKind.Archer)); Assert.That(session.Gold, Is.EqualTo(gold - 75));
            Choice("Upgrade").onClick.Invoke();
            Assert.That(hud.RadialAction.GetComponentInChildren<TMP_Text>().text, Is.EqualTo("YÜKSELT · 55"));
            hud.RadialAction.onClick.Invoke(); Assert.That(Slot(0).Occupant.Level, Is.EqualTo(2));
            Choice("RapidFire").onClick.Invoke();
            Assert.That(hud.RadialAction.GetComponentInChildren<TMP_Text>().text, Is.EqualTo("SEÇ · 160"));
            yield return null; Canvas.ForceUpdateCanvases(); AssertActionFits();
            hud.RadialAction.onClick.Invoke();
            Assert.That(Slot(0).Occupant.Specialization, Is.EqualTo(OutpostSpecialization.RapidFire));
            Assert.That(session.Gold, Is.EqualTo(gold - 75 - 55 - 160));
        }

        [UnityTest] public IEnumerator BuildActionFitsPhoneAspectsAndRendersWithoutTextOverflow()
        {
            foreach (var size in new[] { new Vector2Int(800, 450), new Vector2Int(844, 390) })
            {
#if UNITY_EDITOR
                var review = System.AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType("AllinDefense.Editor.FrontendExperienceReview")).First(type => type != null);
                review.GetMethod("SetSize").Invoke(null, new object[] { size.x, size.y });
#else
                Screen.SetResolution(size.x, size.y, FullScreenMode.Windowed);
#endif
                yield return null; yield return null;
                Call(session, "SelectSlot", Slot(0)); Choice("Archer").onClick.Invoke();
                yield return null; Canvas.ForceUpdateCanvases(); AssertActionFits();
                var label = hud.RadialAction.GetComponentInChildren<TMP_Text>(); label.ForceMeshUpdate();
                Assert.That(label.isTextOverflowing, Is.False, label.text);
                hud.BattleRadial.Close();
            }
        }

        private void AssertActionFits()
        {
            var action = (RectTransform)hud.RadialAction.transform;
            var corners = new Vector3[4]; action.GetWorldCorners(corners);
            var bounds = Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
            Assert.That(bounds.height, Is.GreaterThanOrEqualTo(52));
            Assert.That(Screen.safeArea.Contains(bounds.min), Is.True); Assert.That(Screen.safeArea.Contains(bounds.max), Is.True);
            foreach (var obstacle in hud.GetComponent<DefenderHudResponsiveLayout>().FrameScreenRects())
                Assert.That(bounds.Overlaps(obstacle), Is.False);
            foreach (var button in hud.BattleRadial.GetComponentsInChildren<Button>().Where(button => button != hud.RadialAction))
            {
                ((RectTransform)button.transform).GetWorldCorners(corners);
                Assert.That(bounds.Overlaps(Rect.MinMaxRect(corners[0].x,corners[0].y,corners[2].x,corners[2].y)), Is.False, button.name);
            }
        }

        [UnityTest] public IEnumerator EmptySlotsHavePlusAndNonOverlappingMinimumTouchAreasAcrossPhoneAspects()
        {
            var slots = session.GetComponentsInChildren<GreyboxOutpostSlot>();
            foreach (var slot in slots) Assert.That(slot.GetComponent<DefenderSlotVisual>().ShowsBuildPlus, Is.True);
            session.TryBuildOutpost(0, OutpostKind.Archer);
            Assert.That(Slot(0).GetComponent<DefenderSlotVisual>().ShowsBuildPlus, Is.False);
            foreach (var size in new[] { new Vector2Int(800, 450), new Vector2Int(844, 390) })
            {
#if UNITY_EDITOR
                var review = System.AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType("AllinDefense.Editor.FrontendExperienceReview")).First(type => type != null);
                review.GetMethod("SetSize").Invoke(null, new object[] { size.x, size.y });
#else
                Screen.SetResolution(size.x, size.y, FullScreenMode.Windowed);
#endif
                yield return null; yield return null;
                Assert.That(Screen.width, Is.EqualTo(size.x)); Assert.That(Screen.height, Is.EqualTo(size.y));
                var rects = slots.Select(slot => DefenderSlotVisual.TouchScreenRect(slot.transform, Camera.main)).ToArray();
                foreach (var rect in rects)
                {
                    Assert.That(rect.width * UiMetrics.LogicalScreenSize.x / Screen.width, Is.GreaterThanOrEqualTo(56));
                    Assert.That(rect.height * UiMetrics.LogicalScreenSize.y / Screen.height, Is.GreaterThanOrEqualTo(56));
                }
                for (int a = 0; a < rects.Length; a++)
                    for (int b = a + 1; b < rects.Length; b++)
                    {
                        Vector2 direction = (rects[b].center - rects[a].center).normalized;
                        float supportA = Vector2.Scale(rects[a].size / 2, direction).magnitude;
                        float supportB = Vector2.Scale(rects[b].size / 2, direction).magnitude;
                        Assert.That(Vector2.Distance(rects[a].center, rects[b].center), Is.GreaterThanOrEqualTo(supportA + supportB), $"{size}/{a}/{b}");
                    }
            }
        }

        [UnityTest] public IEnumerator StableRadialPositionDoesNotAllocateAfterWarmup()
        {
            Call(session, "SelectSlot", Slot(0)); yield return null; Canvas.ForceUpdateCanvases();
            var place = (System.Action<Vector2>)typeof(DefenderHudController).GetMethod("PositionBattleRadial", Private)
                .CreateDelegate(typeof(System.Action<Vector2>), hud);
            Vector2 point = Camera.main.WorldToScreenPoint(Slot(0).transform.position);
            for (int i = 0; i < 10; i++) place(point);
            long before = System.GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 10; i++) place(point);
            long allocated = System.GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero);
        }
        [UnityTest] public IEnumerator TutorialCounterShowsDistinctFifthAndSixthSteps()
        {
            hud.SetTutorialGuide(4, Slot(0).transform, Camera.main);
            var text = UiQuery.FindById<TMP_Text>(hud, "TutorialCopy");
            Assert.That(text.text, Does.StartWith("5/6"));
            hud.SetTutorialGuide(5, Slot(0).transform, Camera.main);
            Assert.That(text.text, Does.StartWith("6/6"));
            yield return null;
        }

        [UnityTest] public IEnumerator SelectedTowerAndTutorialPreserveMessageUntilItsDurationExpires()
        {
            session.TryBuildOutpost(0, OutpostKind.Archer);
            Call(session, "ShowMessage", "Kaleyi aştı", .6f);
            var message = (TMP_Text)typeof(DefenderHudController).GetField("messageText", Private).GetValue(hud);
            for (int i = 0; i < 4; i++)
            {
                yield return new WaitForSecondsRealtime(.1f);
                hud.SetSelected(session.Outposts[0], Slot(0), true);
                hud.SetTutorialGuide(1, Slot(1).transform, Camera.main);
                Assert.That(message.gameObject.activeInHierarchy, Is.True);
                Assert.That(message.text, Is.EqualTo("Kaleyi aştı"));
            }
            yield return new WaitForSecondsRealtime(.3f);
            Assert.That(message.gameObject.activeSelf, Is.False);
        }
        [UnityTest] public IEnumerator LevelTwoSellShowsRefundAndHidesPreviousRangePreview()
        {
            Call(session, "EarnGold", 1000); session.TryBuildOutpost(0, OutpostKind.Archer);
            Choice("Upgrade").onClick.Invoke(); Choice("Sell").onClick.Invoke();
            Assert.That(hud.GetComponentsInChildren<CombatRangeVisual>().First(v => v.name == "Radial preview range").GetComponent<LineRenderer>().enabled, Is.False);
            hud.BattleRadial.Close(); session.TryUpgradeOutpost(0);
            Call(session, "SpendGold", session.Gold - 100);
            Call(session, "SelectOutpost", session.Outposts[0]);
            Assert.That(Hint.text, Does.Not.Contain("eksik"));
            Choice("RapidFire").onClick.Invoke();
            Assert.That(Hint.text, Does.Contain("60 eksik"));
            Choice("Sell").onClick.Invoke();
            Assert.That(Hint.text, Is.EqualTo(FrontendCopy.SellingReturnsGold.Format(session.Outposts[0].SellValue)));
            Assert.That(Hint.text, Does.Not.Contain("eksik"));
            Assert.That(session.Phase, Is.Not.EqualTo(GamePhase.Paused));
            yield return null;
        }
        [UnityTest] public IEnumerator TowerAndSlotFootprintsRemainClearForCentralAndEdgeArcs()
        {
            Call(session, "EarnGold", 1000);
            foreach (int index in new[] { 0, 1 })
            {
                foreach (int level in new[] { 0, 1, 2 })
                {
                    if (level == 1) session.TryBuildOutpost(index, OutpostKind.Archer);
                    if (level == 2) session.TryUpgradeOutpost(index);
                    Call(session, "SelectSlot", Slot(index));
                    if (Slot(index).Occupant != null) Call(session, "SelectOutpost", Slot(index).Occupant);
                    yield return null; Canvas.ForceUpdateCanvases();
                    Vector3 slotPosition = Slot(index).transform.position;
                    var tower = Slot(index).Occupant;
                    Vector3 towerPosition = tower != null ? tower.transform.position : Vector3.zero;
                    Vector3 actual = Camera.main.WorldToScreenPoint(slotPosition);
                    foreach (var point in new[] { (Vector2)actual, new Vector2(12, 12), new Vector2(Screen.width - 12, Screen.height - 12) })
                    {
                        Vector3 delta = Camera.main.ScreenToWorldPoint(new Vector3(point.x, point.y, actual.z)) - slotPosition;
                        Slot(index).transform.position = slotPosition + delta;
                        if (tower != null) tower.transform.position = towerPosition + delta;
                        Call(hud, "PositionBattleRadial", point); Canvas.ForceUpdateCanvases();
                        var renderers = Slot(index).GetComponentsInChildren<Renderer>().AsEnumerable();
                        if (tower != null) renderers = renderers.Concat(tower.GetComponentsInChildren<Renderer>());
                        var projected = renderers.Where(r => r.enabled && r is not LineRenderer && r.gameObject.activeInHierarchy)
                            .SelectMany(r => Enumerable.Range(0, 8).Select(i => (Vector2)Camera.main.WorldToScreenPoint(r.bounds.center +
                                Vector3.Scale(r.bounds.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1))))).ToArray();
                        Assert.That(projected, Is.Not.Empty);
                        var footprint = Rect.MinMaxRect(projected.Min(p => p.x), projected.Min(p => p.y), projected.Max(p => p.x), projected.Max(p => p.y));
                        foreach (var button in hud.BattleRadial.GetComponentsInChildren<Button>())
                        {
                            var corners = new Vector3[4]; ((RectTransform)button.transform).GetWorldCorners(corners);
                            Vector2 min = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
                            Vector2 max = RectTransformUtility.WorldToScreenPoint(null, corners[2]);
                            Assert.That(Rect.MinMaxRect(min.x,min.y,max.x,max.y).Overlaps(footprint), Is.False, $"{index}/{level}/{button.name}/{point}");
                        }
                    }
                    Slot(index).transform.position = slotPosition;
                    if (tower != null) tower.transform.position = towerPosition;
                }
                hud.BattleRadial.Close();
            }
        }
        [UnityTest] public IEnumerator PausedSpecializationBackgroundPointerClosesAndRestoresPhase()
        {
            Call(session, "EarnGold", 1000); session.TryBuildOutpost(0, OutpostKind.Archer); session.TryUpgradeOutpost(0);
            Call(session, "SelectOutpost", session.Outposts[0]);
            var phase = session.Phase; Choice("RapidFire").onClick.Invoke();
            Assert.That(session.Phase, Is.EqualTo(GamePhase.Paused));
            var mouse = InputSystem.AddDevice<Mouse>();
            try
            {
                InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(5, Screen.height * .5f) }.WithButton(MouseButton.Left));
                InputSystem.Update();
                Assert.That(mouse.leftButton.wasPressedThisFrame, Is.True, $"mouse={Mouse.current == mouse}, mode={InputSystem.settings.updateMode}");
                Call(hud, "DismissPausedRadialOnBackground");
                Assert.That(hud.BattleRadial.IsOpen, Is.False);
                Assert.That(session.Phase, Is.EqualTo(phase));
            }
            finally { InputSystem.RemoveDevice(mouse); }
            yield return null;
        }
        [UnityTest] public IEnumerator WorldPointerSelectsAnotherSlotAndTowerWhileRadialIsOpen()
        {
            Call(session, "EarnGold", 1000); session.TryBuildOutpost(1, OutpostKind.Archer);
            var mouse = InputSystem.AddDevice<Mouse>();
            try
            {
                foreach (int index in new[] { 2, 1 })
                {
                    Call(session, "SelectSlot", Slot(0));
                    InputSystem.QueueStateEvent(mouse, new MouseState()); InputSystem.Update();
                    Physics.SyncTransforms();
                    Vector2 point = Camera.main.WorldToScreenPoint(Slot(index).transform.position);
                    InputSystem.QueueStateEvent(mouse, new MouseState { position = point }.WithButton(MouseButton.Left));
                    InputSystem.Update(); Call(session, "HandleWorldPointer");
                    Assert.That(typeof(DefenderHudController).GetField("radialSlot", Private).GetValue(hud), Is.EqualTo(Slot(index)));
                    Assert.That(hud.BattleRadial.IsOpen, Is.True);
                }
            }
            finally { InputSystem.RemoveDevice(mouse); }
            yield return null;
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
            Assert.That(hud.RadialAction.gameObject.activeInHierarchy, Is.True);
            Assert.That(hud.RadialAction.interactable, Is.False);
            Assert.That(hud.RadialAction.GetComponentInChildren<TMP_Text>().text, Does.Contain("25 eksik"));
            hud.RadialAction.GetComponent<UiRadialAction>().OnPointerClick(new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left });
            Assert.That(rejects, Is.EqualTo(1)); rejects = 0;
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
                Assert.That(mouse.leftButton.wasPressedThisFrame, Is.True, $"mouse={Mouse.current == mouse}, mode={InputSystem.settings.updateMode}, phase={session.Phase}");
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
            Call(session, "SelectSlot", Slot(0)); Choice("Archer").onClick.Invoke(); yield return null; Canvas.ForceUpdateCanvases();
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
                    Assert.That(intersections, Is.Zero, $"{point}/{button.name}/{rect}");
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
            Assert.That(hud.TutorialArrow.gameObject.activeInHierarchy, Is.True);
            Assert.That(hud.TutorialTarget, Is.EqualTo(Choice("Archer").transform));
            Choice("Archer").onClick.Invoke(); yield return null;
            Assert.That(hud.TutorialTarget, Is.EqualTo(hud.RadialAction.transform));
            Assert.That(UiQuery.FindById<TMP_Text>(hud, "TutorialCopy").text, Does.Contain(FrontendCopy.TutorialConfirmBuild.Value));
            UiQuery.FindById<Button>(hud, "TutorialHelp").onClick.Invoke(); yield return null;
            Assert.That(hud.BattleRadial.IsOpen, Is.False);
            Assert.That(hud.TutorialArrow.gameObject.activeInHierarchy, Is.True);
            hud.SetTutorialGuide(3, null, Camera.main); yield return null;
            Assert.That(UiQuery.FindById(hud, "TutorialAbilityRing").gameObject.activeInHierarchy, Is.True);
        }
    }
}
