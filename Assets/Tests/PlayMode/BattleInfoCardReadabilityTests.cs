using System;
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
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace AllinDefense.Tests
{
    public sealed class BattleInfoCardReadabilityTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private GameSessionController session;
        private DefenderHudController hud;
        private GameLanguage language;
        private Vector2 measurementScale = Vector2.one;
        private static object Call(object target, string method, params object[] args) => target.GetType().GetMethod(method, Private).Invoke(target, args);
        private GreyboxOutpostSlot Slot(int index) => session.GetComponentsInChildren<GreyboxOutpostSlot>().First(s => s.Index == index);
        private Button Choice(string id) => UiQuery.FindById<Button>(hud.BattleRadial, id);
        [UnitySetUp] public IEnumerator Setup()
        {
            language = GameLocalization.CurrentLanguage;
            session = new GameObject("Battle card readability fixture").AddComponent<GameSessionController>();
            session.ConfigurePresentation(Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog"));
            session.SetCampaignLevel(10); session.Initialize();
            hud = session.GetComponentInChildren<DefenderHudController>();
            ResizeLogicalCanvas(new Vector2(667, 375));
            Call(session, "EarnGold", 10000);
            Time.timeScale = 0;
            yield return null;
        }

        private void ResizeLogicalCanvas(Vector2 size)
        {
            var canvas = hud.GetComponentInParent<Canvas>().rootCanvas;
            canvas.GetComponent<CanvasScaler>().enabled = false;
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.scaleFactor = 1;
            var canvasRect = (RectTransform)canvas.transform;
            canvasRect.sizeDelta = size;
            canvasRect.position = new Vector3(size.x / 2, size.y / 2, 0);
            canvasRect.localScale = Vector3.one;
            var hudRect = (RectTransform)hud.transform;
            hudRect.anchorMin = Vector2.zero; hudRect.anchorMax = Vector2.one;
            hudRect.offsetMin = hudRect.offsetMax = Vector2.zero;
            hud.GetComponent<DefenderHudResponsiveLayout>().RefreshLayout();
        }

        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (session != null) Object.Destroy(session.gameObject);
            yield return null;
            GameLocalization.SetLanguage(language); Time.timeScale = 1;
        }

        [UnityTest] public IEnumerator TurkishBuildUpgradeAndAllSixBranchesFitSE() => AllPreviews(GameLanguage.Turkish);
        [UnityTest] public IEnumerator EnglishBuildUpgradeAndAllSixBranchesFitSE() => AllPreviews(GameLanguage.English);

        [UnityTest] public IEnumerator TurkishHeroControlsStayRightBesideWaveAndClearOfAllSelections() => HeroControls(GameLanguage.Turkish);
        [UnityTest] public IEnumerator EnglishHeroControlsStayRightBesideWaveAndClearOfAllSelections() => HeroControls(GameLanguage.English);

        [UnityTest] public IEnumerator CardHeightOnlyGrowsUntilSelectionChangesAndRoleColourResets()
        {
            session.TryBuildOutpost(0, OutpostKind.Barracks); session.TryUpgradeOutpost(0);
            Choice("ShieldGuard").onClick.Invoke(); yield return null; Canvas.ForceUpdateCanvases();
            var panel = (RectTransform)hud.transform.Find("SelectedPanel");
            float height = panel.rect.height;
            var role = UiQuery.FindById<TMP_Text>(hud, "Role"); role.color = UiTokens.Gold;
            Choice("AssaultGuard").onClick.Invoke(); yield return null; Canvas.ForceUpdateCanvases();
            Assert.That(panel.rect.height, Is.GreaterThanOrEqualTo(height));
            Assert.That(role.color, Is.EqualTo(UiTokens.TextSecondary));
            Assert.That(UiQuery.FindById<TMP_Text>(hud, "Hint").text, Is.EqualTo(FrontendCopy.BattlePaused.Value));
            Call(session, "SelectSlot", Slot(1)); Choice("Archer").onClick.Invoke();
            yield return null; Canvas.ForceUpdateCanvases();
            Assert.That(panel.rect.height, Is.LessThan(height), "A new selection releases the previous card height");
        }

        private IEnumerator HeroControls(GameLanguage value)
        {
            GameLocalization.SetLanguage(value);
            var screenCache = typeof(UiMetrics).GetField("cachedScreen", BindingFlags.Static | BindingFlags.NonPublic);
            var logicalCache = typeof(UiMetrics).GetField("cachedLogicalSize", BindingFlags.Static | BindingFlags.NonPublic);
            var originalScreen = screenCache.GetValue(null);
            var originalLogical = logicalCache.GetValue(null);
            try
            {
                foreach (var size in new[] { new Vector2(667, 375), new Vector2(874, 402), new Vector2(956, 440) })
                {
                    screenCache.SetValue(null, new Vector2Int(Screen.width, Screen.height));
                    logicalCache.SetValue(null, size);
                    measurementScale = new Vector2(size.x / Screen.width, size.y / Screen.height);
                    ResizeLogicalCanvas(new Vector2(Screen.width, Screen.height));
                    Call(session, "CancelSelection");
                    Assert.That(hud.BattleRadial.IsOpen, Is.False);
                    hud.Refresh(session);
                    hud.SetMessage(GameLocalization.Text("Kaleye sızma: Yağmacı — savunmayı güçlendir!", "Keep breached: Raider — strengthen your defences!"), true);
                    yield return null;
                    AssertHeroControls();
                    foreach (var slot in session.GetComponentsInChildren<GreyboxOutpostSlot>().Where(slot => slot.Occupant == null))
                    {
                        Call(session, "SelectSlot", slot);
                        Choice("Archer").onClick.Invoke();
                        yield return null;
                        AssertHeroControls();
                    }
                    Choice("Barracks").onClick.Invoke();
                    hud.RadialAction.onClick.Invoke();
                    Choice("Upgrade").onClick.Invoke();
                    hud.RadialAction.onClick.Invoke();
                    Choice("ShieldGuard").onClick.Invoke();
                    yield return null;
                    AssertHeroControls();
                    hud.BattleRadial.Close();
                }
            }
            finally
            {
                screenCache.SetValue(null, originalScreen);
                logicalCache.SetValue(null, originalLogical);
                measurementScale = Vector2.one;
            }
        }

        private void AssertHeroControls()
        {
            Canvas.ForceUpdateCanvases();
            var ability = hud.transform.Find("BottomBar/AbilityCooldown");
            var hero = hud.transform.Find("BottomBar/Hero");
            var next = Bounds(hud.transform.Find("BottomBar/NextWave"));
            var safe = Bounds(hud.transform);
            Assert.That(Bounds(ability).width, Is.EqualTo(64).Within(.1f));
            Assert.That(Bounds(ability).height, Is.EqualTo(88).Within(.1f));
            Assert.That(next.width, Is.EqualTo(208).Within(.1f));
            Assert.That(next.height, Is.EqualTo(54).Within(.1f));
            Assert.That(next.xMax, Is.EqualTo(safe.xMax - 8).Within(.1f));
            Assert.That(next.yMin, Is.EqualTo(safe.yMin + 8).Within(.1f));
            Assert.That(next.xMin - Bounds(ability).xMax, Is.EqualTo(8).Within(.1f));
            Assert.That(Bounds(ability).yMin, Is.EqualTo(next.yMin).Within(.1f));
            Assert.That(Bounds(hero).yMin - Bounds(ability).yMax, Is.EqualTo(8).Within(.1f));
            Assert.That(Bounds(hero).center.x, Is.EqualTo(Bounds(ability).center.x).Within(.1f));
            var cooldown = ability.GetComponent<UiRadialCooldown>();
            Assert.That(Bounds(cooldown.ActionRect).width, Is.GreaterThanOrEqualTo(UiMetrics.MinTouchPt));
            Assert.That(Bounds(cooldown.ActionRect).height, Is.GreaterThanOrEqualTo(UiMetrics.MinTouchPt));
            foreach (var control in new[] { ability, hero })
            {
                var bounds = Bounds(control);
                AssertContains(safe, bounds, control.name);
                Assert.That(bounds.xMin, Is.GreaterThan(safe.center.x), control.name);
                Assert.That(bounds.Overlaps(next), Is.False, control.name);
                foreach (string path in new[] { "BottomBar/NextWave", "MessageChrome", "TopBar/WaveForecast", "SelectedPanel" })
                {
                    var obstacle = hud.transform.Find(path);
                    if (obstacle.gameObject.activeInHierarchy)
                        AssertGap(bounds, Bounds(obstacle), control.name + "/" + path, path == "SelectedPanel" ? .9f : 7.9f);
                }
                if (hud.BattleRadial.IsOpen)
                    foreach (var button in hud.BattleRadial.GetComponentsInChildren<Button>())
                        AssertGap(bounds, Bounds(button.transform), control.name + "/radial/" + button.name);
            }
            var message = hud.transform.Find("MessageChrome");
            AssertContains(safe, Bounds(message), "message");
            AssertGap(Bounds(message), next, "message/wave");
            var selected = hud.transform.Find("SelectedPanel");
            if (selected.gameObject.activeInHierarchy) AssertGap(Bounds(message), Bounds(selected), "message/card");
            var forecast = hud.transform.Find("TopBar/WaveForecast");
            if (forecast.gameObject.activeInHierarchy) AssertGap(Bounds(message), Bounds(forecast), "message/forecast");
        }

        private static void AssertGap(Rect a, Rect b, string label, float minimum = 7.9f)
        {
            float gap = Mathf.Max(b.xMin - a.xMax, a.xMin - b.xMax, b.yMin - a.yMax, a.yMin - b.yMax);
            Assert.That(gap, Is.GreaterThanOrEqualTo(minimum), label + $": {a} / {b}");
        }

        private IEnumerator AllPreviews(GameLanguage value)
        {
            GameLocalization.SetLanguage(value);
            int index = 0;
            foreach (var kind in new[] { OutpostKind.Archer, OutpostKind.Barracks, OutpostKind.Mage })
            {
                Call(session, "SelectSlot", Slot(index));
                Assert.That(hud.transform.Find("SelectedPanel").gameObject.activeSelf, Is.False,
                    "An empty slot opens the radial choices before the information card");
                int gold = session.Gold;
                AssertUnlocked(kind.ToString());
                Choice(kind.ToString()).onClick.Invoke();
                Assert.That(hud.transform.Find("SelectedPanel").gameObject.activeSelf, Is.True);
                yield return null;
                AssertCard();
                Assert.That(UiQuery.FindById<TMP_Text>(hud, "Hint").text, Does.Not.Contain(FrontendCopy.BattleCardUnlockChapter.Format(1)));
                Assert.That(session.Gold, Is.EqualTo(gold));
                hud.RadialAction.onClick.Invoke();
                Assert.That(hud.transform.Find("SelectedPanel").gameObject.activeSelf, Is.True,
                    "A built tower keeps its information card visible");
                Choice("Upgrade").onClick.Invoke();
                yield return null;
                AssertCard();
                Assert.That(UiQuery.FindById<TMP_Text>(hud, "Hint").text, Does.Not.Contain(FrontendCopy.BattleCardUnlockChapter.Format(1)));
                var before = TowerProgression.Stats(kind, 1);
                var upgraded = TowerProgression.Stats(kind, 2);
                Assert.That(UiQuery.FindById<TMP_Text>(hud, "Description").text,
                    Is.EqualTo(FrontendCopy.BattleCardDelta.Format(before.damage, upgraded.damage)));
                hud.RadialAction.onClick.Invoke();
                for (int branchIndex = 0; branchIndex < 2; branchIndex++)
                {
                    var branch = TowerProgression.Branch(kind, branchIndex);
                    gold = session.Gold;
                    AssertUnlocked(branch.ToString());
                    Choice(branch.ToString()).onClick.Invoke();
                    yield return null;
                    AssertCard();
                    Assert.That(UiQuery.FindById<TMP_Text>(hud, "Hint").text, Does.Not.Contain(FrontendCopy.BattleCardUnlockChapter.Format(1)));
                    Assert.That(UiQuery.FindById<TMP_Text>(hud, "Role").text, Is.EqualTo(TowerProgression.Description(branch)));
                    Assert.That(UiQuery.FindById<TMP_Text>(hud, "Title").text, Is.EqualTo(TowerProgression.Name(branch)));
                    var after = TowerProgression.Stats(kind, 2, branch);
                    Assert.That(UiQuery.FindById<TMP_Text>(hud, "CardStatValue1").text,
                        Is.EqualTo(FrontendCopy.BattleCardDecimalDelta.Format(upgraded.range, after.range)));
                    if (branch == OutpostSpecialization.ShieldGuard)
                        Assert.That(UiQuery.FindById<TMP_Text>(hud, "CardDetail").text, Does.Contain("210→378"));
                    Assert.That(session.Gold, Is.EqualTo(gold));
                    Assert.That(Slot(index).Occupant.Specialization, Is.EqualTo(OutpostSpecialization.None));
                    if (branchIndex == 1)
                    {
                        hud.RadialAction.onClick.Invoke();
                        Assert.That(Slot(index).Occupant.Specialization, Is.EqualTo(branch));
                    }
                }
                hud.BattleRadial.Close();
                Assert.That(hud.transform.Find("SelectedPanel").gameObject.activeSelf, Is.False);
                Call(session, "SelectOutpost", Slot(index).Occupant);
                Assert.That(hud.transform.Find("SelectedPanel").gameObject.activeSelf, Is.True);
                Choice("Sell").onClick.Invoke();
                Choice("Sell").onClick.Invoke();
                Assert.That(Slot(index).Occupant, Is.Null);
                Assert.That(hud.transform.Find("SelectedPanel").gameObject.activeSelf, Is.False,
                    "Selling returns to the empty slot without an information card");
                index++;
            }
        }

        [UnityTest] public IEnumerator TurkishChapterOneChoicesHaveNoLocks() => ChapterOnePreviews(GameLanguage.Turkish);
        [UnityTest] public IEnumerator EnglishChapterOneChoicesHaveNoLocks() => ChapterOnePreviews(GameLanguage.English);

        private IEnumerator ChapterOnePreviews(GameLanguage value)
        {
            session.ConfigureProgress(1);
            yield return AllPreviews(value);
        }

        private void AssertUnlocked(string id)
        {
            var choice = Choice(id);
            Assert.That(choice.interactable, Is.True, id);
            Assert.That(choice.transform.Find("LockBadge").gameObject.activeSelf, Is.False, id);
            Assert.That(choice.GetComponent<UiAccessibleButton>().Label, Does.Not.Contain(FrontendCopy.Locked.Value), id);
        }

        [UnityTest] public IEnumerator SEHeaderHasEightPointGapsAndWarningsWrapWithoutClipping()
        {
            foreach (var value in new[] { GameLanguage.Turkish, GameLanguage.English })
            {
                GameLocalization.SetLanguage(value);
                Call(session, "CancelSelection");
                hud.Refresh(session); yield return null; Canvas.ForceUpdateCanvases();
                var gold = Bounds(hud.transform.Find("TopBar/Gold"));
                var wave = Bounds(hud.transform.Find("TopBar/WavePanel"));
                var speed = Bounds(hud.transform.Find("TopBar/Speed"));
                Assert.That(wave.xMin - gold.xMax, Is.GreaterThanOrEqualTo(7.9f));
                Assert.That(speed.xMin - wave.xMax, Is.GreaterThanOrEqualTo(7.9f));
                Call(session, "SelectSlot", Slot(0)); Choice("Archer").onClick.Invoke();
                hud.SetMessage(GameLocalization.Text("Kaleye sızma: Yağmacı — savunmayı güçlendir!", "Keep breached: Raider — strengthen your defences!"), true);
                yield return null; Canvas.ForceUpdateCanvases();
                var text = hud.transform.Find("Message").GetComponent<TMP_Text>();
                text.ForceMeshUpdate();
                Assert.That(text.fontSize, Is.GreaterThanOrEqualTo(13));
                Assert.That(text.textInfo.lineCount, Is.LessThanOrEqualTo(2));
                Assert.That(text.isTextOverflowing, Is.False);
                Assert.That(Bounds(hud.transform.Find("MessageChrome")).Overlaps(Bounds(hud.transform.Find("SelectedPanel"))), Is.False);
                AssertCard();
                Assert.That(UiQuery.FindById<TMP_Text>(hud, "Hint").text, Does.Not.Contain(FrontendCopy.BattleCardUnlockChapter.Format(1)));
            }
        }

        [UnityTest] public IEnumerator SpecializedShieldGuardAndHintFitInBothLanguages()
        {
            session.TryBuildOutpost(0, OutpostKind.Barracks); session.TryUpgradeOutpost(0);
            foreach (var value in new[] { GameLanguage.Turkish, GameLanguage.English })
            {
                GameLocalization.SetLanguage(value);
                Call(session, "SelectOutpost", Slot(0).Occupant);
                Choice("ShieldGuard").onClick.Invoke();
                yield return null;
                Assert.That(UiQuery.FindById<TMP_Text>(hud, "CardDetail").text, Is.Not.Empty);
                Assert.That(UiQuery.FindById<TMP_Text>(hud, "Hint").text, Is.Not.Empty);
                AssertCard();
                hud.BattleRadial.Close();
            }
            Assert.That(session.TrySpecializeOutpost(0, OutpostSpecialization.ShieldGuard), Is.True);
            foreach (var value in new[] { GameLanguage.Turkish, GameLanguage.English })
            {
                GameLocalization.SetLanguage(value);
                Call(session, "SelectOutpost", Slot(0).Occupant);
                Choice("Sell").onClick.Invoke();
                yield return null;
                Assert.That(UiQuery.FindById<TMP_Text>(hud, "Hint").text, Is.Not.Empty);
                AssertCard();
                Call(hud, "CancelSaleConfirmation");
            }
        }

        private void AssertCard()
        {
            Canvas.ForceUpdateCanvases();
            Assert.That(((RectTransform)hud.transform).rect.size, Is.EqualTo(new Vector2(667, 375)));
            var panel = hud.transform.Find("SelectedPanel");
            var bounds = Bounds(panel);
            Debug.Log($"[BATTLE_CARD_MEASURE] {GameLocalization.CurrentLanguage} {UiQuery.FindById<TMP_Text>(hud, "Title").text} {bounds.width:0}×{bounds.height:0} pt");
            Assert.That(bounds.width, Is.EqualTo(260).Within(.1f));
            float contentHeight = (float)typeof(DefenderHudController).GetField("selectionCardHeight", Private).GetValue(hud);
            Assert.That(contentHeight, Is.LessThanOrEqualTo(((RectTransform)panel).rect.height + .1f), "Measured content must fit the panel");
            Assert.That(Bounds(UiQuery.FindById<TMP_Text>(hud, "Hint").transform).yMin,
                Is.GreaterThanOrEqualTo(bounds.yMin - .1f), "Last content line must stay inside the card");
            AssertContains(Bounds(hud.transform), bounds, "667×375 pt safe area");
            foreach (var text in panel.GetComponentsInChildren<TMP_Text>())
            {
                text.ForceMeshUpdate();
                bool body = text.name is "Role" or "Description" or "CardStatValue1" or "CardStatValue2" or "CardDetail";
                Assert.That(text.fontSize, Is.GreaterThanOrEqualTo(body ? 13 : 11), text.name);
                Assert.That(text.enableAutoSizing, Is.False, text.name);
                Assert.That(text.overflowMode, Is.Not.EqualTo(TextOverflowModes.Ellipsis), text.name);
                Assert.That(text.isTextOverflowing, Is.False, text.name + ": " + text.text);
                AssertContains(bounds, Bounds(text.transform), text.name);
            }
            foreach (var text in hud.BattleRadial.GetComponentsInChildren<TMP_Text>())
            {
                text.ForceMeshUpdate();
                Assert.That(text.fontSize, Is.GreaterThanOrEqualTo(12), text.name);
                Assert.That(text.overflowMode, Is.Not.EqualTo(TextOverflowModes.Ellipsis), text.name);
                Assert.That(text.isTextOverflowing, Is.False, text.name + ": " + text.text);
            }
            foreach (var button in hud.BattleRadial.GetComponentsInChildren<Button>())
            {
                Assert.That(Bounds(button.transform).width, Is.GreaterThanOrEqualTo(UiMetrics.MinTouchPt));
                Assert.That(Bounds(button.transform).height, Is.GreaterThanOrEqualTo(UiMetrics.MinTouchPt));
            }
        }

        private static void AssertContains(Rect outer, Rect inner, string label)
        {
            Assert.That(inner.xMin, Is.GreaterThanOrEqualTo(outer.xMin - .1f), label);
            Assert.That(inner.yMin, Is.GreaterThanOrEqualTo(outer.yMin - .1f), label);
            Assert.That(inner.xMax, Is.LessThanOrEqualTo(outer.xMax + .1f), label);
            Assert.That(inner.yMax, Is.LessThanOrEqualTo(outer.yMax + .1f), label);
        }
        private Rect Bounds(Transform transform)
        {
            var corners = new Vector3[4]; ((RectTransform)transform).GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x * measurementScale.x, corners[0].y * measurementScale.y,
                corners[2].x * measurementScale.x, corners[2].y * measurementScale.y);
        }
    }
}
