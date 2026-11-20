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
    public sealed class BattleLayoutStabilityTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly FieldInfo LogicalCache = typeof(UiMetrics).GetField("cachedLogicalSize", BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly FieldInfo ScreenCache = typeof(UiMetrics).GetField("cachedScreen", BindingFlags.Static | BindingFlags.NonPublic);
        private GameSessionController session;
        private DefenderHudController hud;
        private DefenderHudResponsiveLayout layout;
        private Vector2 logicalSize;
        private Rect safe;
        private object originalLogical, originalScreen;
        private bool reducedMotion;

        private static object Call(object target, string name, params object[] args) =>
            target.GetType().GetMethod(name, Private).Invoke(target, args);
        private RectTransform RectAt(string path) => (RectTransform)hud.transform.Find(path);
        private Rect Bounds(string path) => Bounds(RectAt(path));
        private Rect Bounds(RectTransform rect)
        {
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            Vector2 scale = new(logicalSize.x / Screen.width, logicalSize.y / Screen.height);
            Vector2 min = Vector2.Scale(RectTransformUtility.WorldToScreenPoint(null, corners[0]), scale);
            Vector2 max = Vector2.Scale(RectTransformUtility.WorldToScreenPoint(null, corners[2]), scale);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }
        private void Viewport(Vector2 size, Rect safeArea)
        {
            logicalSize = size; safe = safeArea;
            ScreenCache.SetValue(null, new Vector2Int(Screen.width, Screen.height));
            LogicalCache.SetValue(null, size);
            ApplyLayout();
        }
        private void ApplyLayout()
        {
            Vector2 scale = new(Screen.width / logicalSize.x, Screen.height / logicalSize.y);
            var metrics = DefenderHudResponsiveLayout.CalculateLayoutMetrics(((RectTransform)hud.transform).rect.size,
                new Rect(Vector2.Scale(safe.position, scale), Vector2.Scale(safe.size, scale)), new Vector2Int(Screen.width, Screen.height));
            Call(layout, "ApplyLayout", metrics);
        }
        private void SelectPoint(Vector2 logicalPoint, bool newSelection = true)
        {
            if (newSelection) layout.ClearContextualSelection();
            ApplyLayout();
            layout.SetContextualSelectionFromScreenPoint(Vector2.Scale(logicalPoint,
                new Vector2(Screen.width / logicalSize.x, Screen.height / logicalSize.y)), true);
            ApplyLayout();
        }
        private static void Same(Rect a, Rect b, string label)
        {
            Assert.That(b.x, Is.EqualTo(a.x).Within(.01f), label);
            Assert.That(b.y, Is.EqualTo(a.y).Within(.01f), label);
            Assert.That(b.width, Is.EqualTo(a.width).Within(.01f), label);
            Assert.That(b.height, Is.EqualTo(a.height).Within(.01f), label);
        }
        [UnitySetUp] public IEnumerator Setup()
        {
            originalLogical = LogicalCache.GetValue(null); originalScreen = ScreenCache.GetValue(null);
            reducedMotion = FrontendPreferences.ReducedMotion; FrontendPreferences.ReducedMotion = true;
            session = new GameObject("Layout stability fixture").AddComponent<GameSessionController>();
            session.ConfigurePresentation(Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog"));
            session.SetCampaignLevel(10); session.Initialize();
            hud = session.GetComponentInChildren<DefenderHudController>();
            layout = hud.GetComponent<DefenderHudResponsiveLayout>();
            Time.timeScale = 0;
            yield return null;
            Viewport(new Vector2(844, 390), new Rect(47, 21, 750, 369));
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (session != null) Object.Destroy(session.gameObject);
            LogicalCache.SetValue(null, originalLogical); ScreenCache.SetValue(null, originalScreen);
            FrontendPreferences.ReducedMotion = reducedMotion; Time.timeScale = 1;
            yield return null;
        }

        [Test] public void RestartConfirmationUsesPhonePointsReadableTypeAndSafeDefaultFocus()
        {
            session.TogglePause(); hud.Refresh(session);
            UiQuery.FindById<Button>(hud, "PauseRestart").onClick.Invoke();
            ApplyLayout(); Canvas.ForceUpdateCanvases();
            string card = "RestartConfirmation/RestartConfirmation.Card";
            Assert.That(Bounds(card).width, Is.GreaterThanOrEqualTo(300));
            Assert.That(RectAt(card + "/RestartConfirmation.Close").gameObject.activeSelf, Is.False);
            var warning = UiQuery.FindById<TMP_Text>(hud, "RestartWarning");
            float pointScale = Bounds(warning.rectTransform).height / warning.rectTransform.rect.height;
            Assert.That(warning.fontSize * pointScale, Is.GreaterThanOrEqualTo(13));
            foreach (string id in new[] { "ReturnToGame", "ConfirmRestart" })
                Assert.That(UiQuery.FindById<Button>(hud, id).GetComponentInChildren<TMP_Text>().fontSize * pointScale,
                    Is.GreaterThanOrEqualTo(13.99f));
            Assert.That(RectAt(card + "/RestartConfirmation.Title").GetComponent<TMP_Text>().fontSize * pointScale,
                Is.EqualTo(20).Within(.1f));
            Assert.That(EventSystem.current.currentSelectedGameObject,
                Is.SameAs(UiQuery.FindById<Button>(hud, "ReturnToGame").gameObject));
        }

        [Test] public void OpeningAndClosingSelectionNeverReflowsTopBarIncluding814PointSafeWidth()
        {
            foreach (var size in new[] { new Vector2(844, 390), new Vector2(932, 430), new Vector2(1024, 768) })
            {
                float inset = size.x == 932 ? 59 : size.x == 844 ? 47 : 0;
                Viewport(size, new Rect(inset, 21, size.x - 2 * inset, size.y - 21));
                RectAt("SelectedPanel").gameObject.SetActive(false); ApplyLayout();
                var rects = RectAt("TopBar").GetComponentsInChildren<RectTransform>(true);
                var before = rects.Select(Bounds).ToArray();
                SelectPoint(safe.min + new Vector2(120, 120));
                RectAt("SelectedPanel").gameObject.SetActive(true); ApplyLayout();
                for (int i = 0; i < rects.Length; i++) Same(before[i], Bounds(rects[i]), rects[i].name);
                RectAt("SelectedPanel").gameObject.SetActive(false); layout.ClearContextualSelection(); ApplyLayout();
                for (int i = 0; i < rects.Length; i++) Same(before[i], Bounds(rects[i]), rects[i].name);
            }
        }

        [Test] public void CompactTopBarKeepsOnlyGoldWaveSpeedAndPauseInsideFiftyPointBand()
        {
            foreach (var size in new[] { new Vector2(667, 375), new Vector2(844, 390), new Vector2(932, 430), new Vector2(1024, 768) })
            {
                float inset = size.x == 844 ? 47 : size.x == 932 ? 59 : 0;
                Viewport(size, new Rect(inset, inset > 0 ? 21 : 0, size.x - 2 * inset, size.y - (inset > 0 ? 21 : 0)));
                Assert.That(RectAt("TopBar/Lives").gameObject.activeSelf, Is.False);
                Assert.That(RectAt("TopBar/WavePanel/PhaseChip").gameObject.activeSelf, Is.False);
                var paths = new[] { "TopBar/Gold", "TopBar/WavePanel", "TopBar/Speed", "TopBar/Pause" };
                var rects = paths.Select(Bounds).ToArray();
                Assert.That(rects[0].xMin, Is.EqualTo(safe.xMin + 6).Within(.01));
                Assert.That(rects[0].width, Is.EqualTo(96).Within(.01));
                Assert.That(rects[1].width, Is.EqualTo(164).Within(.01));
                Assert.That(rects[2].width, Is.EqualTo(60).Within(.01));
                for (int i = 0; i < rects.Length; i++)
                {
                    Assert.That(rects[i].height, Is.EqualTo(44).Within(.01), paths[i]);
                    Assert.That(rects[i].yMin, Is.GreaterThanOrEqualTo(safe.yMax - 50.01f), paths[i]);
                    Assert.That(rects[i].yMax, Is.LessThanOrEqualTo(safe.yMax), paths[i]);
                    Assert.That(rects[i].xMin, Is.GreaterThanOrEqualTo(safe.xMin), paths[i]);
                    Assert.That(rects[i].xMax, Is.LessThanOrEqualTo(safe.xMax), paths[i]);
                    for (int j = i + 1; j < rects.Length; j++)
                        Assert.That(rects[i].Overlaps(rects[j]), Is.False, paths[i] + " / " + paths[j]);
                }
            }
            var plate = session.GetComponentInChildren<DefenderKeepPlate>();
            plate.Refresh();
            Assert.That(plate.Amount.gameObject.activeInHierarchy, Is.True);
            Assert.That(plate.Amount.text, Is.EqualTo($"{session.Lives}/{session.StartingLivesForLevel}"));
        }

        [UnityTest] public IEnumerator LongChaptersUseTwoPointGapsAndReadableSegmentsWhileShortChaptersKeepEight()
        {
            var wavesField = typeof(GameSessionController).GetField("waves", Private);
            var original = (WaveDefinition[])wavesField.GetValue(session);
            foreach (int count in new[] { 12, 7 })
            {
                wavesField.SetValue(session, Enumerable.Repeat(original[0], count).ToArray());
                typeof(GameSessionController).GetField("currentWaveIndex", Private).SetValue(session, count == 12 ? 10 : 0);
                hud.Refresh(session);
                yield return null;
                ApplyLayout(); Canvas.ForceUpdateCanvases();
                var progress = RectAt("TopBar/WavePanel").GetComponentsInChildren<UiProgressSegments>().Single();
                Assert.That(progress.Count, Is.EqualTo(count));
                Assert.That(progress.GetComponent<HorizontalLayoutGroup>().spacing, Is.EqualTo(count > 8 ? 2 : 8));
                for (int i = 0; i < count; i++)
                {
                    var segment = (RectTransform)progress.transform.GetChild(i);
                    Assert.That(Bounds(segment).width, Is.GreaterThanOrEqualTo(9.99f));
                    if (i > 0)
                        Assert.That(Bounds(segment).xMin - Bounds((RectTransform)progress.transform.GetChild(i - 1)).xMax,
                            Is.EqualTo(count > 8 ? 2 : 8).Within(.01));
                }
                var wave = RectAt("TopBar/WavePanel/Wave").GetComponent<TMP_Text>();
                Assert.That(Bounds(wave.rectTransform).width, Is.GreaterThanOrEqualTo(99.99f));
                Assert.That(wave.GetPreferredValues(wave.text).x, Is.LessThanOrEqualTo(wave.rectTransform.rect.width));
                if (count == 12) Assert.That(wave.text, Does.EndWith("10/12"));
            }
        }

        [Test] public void ToastSlotSurvivesSelectionHeightChangesAndInterruptedEntrance()
        {
            hud.SetMessage("Kaleye sızma: Yağmacı", true); ApplyLayout();
            Rect rest = Bounds("MessageChrome"), textRest = Bounds("Message");
            Assert.That(rest.xMin, Is.EqualTo(safe.xMin + 8).Within(.1));
            Assert.That(rest.yMin, Is.EqualTo(safe.yMin + 8).Within(.1));
            foreach (float height in new[] { 120f, 148f, 182f })
            {
                SelectPoint(safe.min + new Vector2(140, 100));
                RectAt("SelectedPanel").gameObject.SetActive(true);
                layout.SetInformationCardHeight(height); ApplyLayout();
                Same(rest, Bounds("MessageChrome"), "card height");
                layout.SetMessageEntranceOffset(-3); ApplyLayout();
                Assert.That(Bounds("MessageChrome").y, Is.EqualTo(rest.y - 3).Within(.01));
                layout.SetMessageEntranceOffset(-3);
                Assert.That(Bounds("MessageChrome").y, Is.EqualTo(rest.y - 3).Within(.01), "offset is absolute");
                layout.SetMessageEntranceOffset(0);
                Same(rest, Bounds("MessageChrome"), "entrance completion");
                Same(textRest, Bounds("Message"), "text entrance completion");
                RectAt("SelectedPanel").gameObject.SetActive(false); layout.ClearContextualSelection(); ApplyLayout();
                Same(rest, Bounds("MessageChrome"), "selection dismissed");
            }
        }

        [Test] public void InformationCardUsesClearStableCornerAndDoesNotInterceptWorldTouches()
        {
            foreach (var viewport in new[] { new Rect(0, 0, 667, 375), new Rect(47, 21, 750, 369), new Rect(59, 21, 814, 409) })
            {
                Viewport(new Vector2(viewport.width + 2 * viewport.x, viewport.height + viewport.y), viewport);
                foreach (Vector2 point in new[] { safe.min + new Vector2(150, 110), safe.min + new Vector2(150, 300), safe.center, safe.max - new Vector2(100, 110) })
                {
                    SelectPoint(point); RectAt("SelectedPanel").gameObject.SetActive(true);
                    layout.SetInformationCardHeight(148); ApplyLayout();
                    Rect card = Bounds("SelectedPanel");
                    if (point.x < safe.center.x) Assert.That(card.center.x, Is.GreaterThan(safe.center.x), $"{viewport} {point}");
                    if (point.x > safe.center.x) Assert.That(card.center.x, Is.LessThan(safe.center.x), $"{viewport} {point}");
                    Assert.That(card.Overlaps(new Rect(point - Vector2.one * 28, Vector2.one * 56)), Is.False, $"{viewport} {point} {card}");
                    foreach (string path in new[] { "TopBar/Gold", "TopBar/WavePanel", "TopBar/Speed", "TopBar/Pause",
                        "TopBar/WaveForecast", "BottomBar/Hero", "BottomBar/AbilityCooldown", "BottomBar/NextWave", "MessageChrome" })
                        Assert.That(card.Overlaps(Bounds(path)), Is.False, path + $" {viewport} {card} {Bounds(path)}");
                    SelectPoint(safe.max - (point - safe.min), false);
                    layout.SetInformationCardHeight(120); ApplyLayout();
                    Vector2 scale = new(logicalSize.x / Screen.width, logicalSize.y / Screen.height);
                    Rect reserved = layout.InformationCardScreenRect(148);
                    Same(card, new Rect(Vector2.Scale(reserved.position, scale), Vector2.Scale(reserved.size, scale)), "frozen corner reserve");
                }
            }
            Assert.That(RectAt("SelectedPanel").GetComponentsInChildren<Button>(true), Is.Empty);
            Assert.That(RectAt("SelectedPanel").GetComponentsInChildren<Graphic>(true).All(g => !g.raycastTarget), Is.True);
        }

        [Test] public void EverySelectedWorldSlotStaysOutsideItsInformationCard()
        {
            foreach (var size in new[] { new Vector2(667, 375), new Vector2(844, 390), new Vector2(932, 430) })
            {
                float inset = size.x == 844 ? 47 : size.x == 932 ? 59 : 0;
                Viewport(size, new Rect(inset, inset > 0 ? 21 : 0, size.x - 2 * inset, size.y - (inset > 0 ? 21 : 0)));
                foreach (var slot in session.GetComponentsInChildren<GreyboxOutpostSlot>())
                {
                    var scale = new Vector2(size.x / Screen.width, size.y / Screen.height);
                    Vector2 point = Vector2.Scale(Camera.main.WorldToScreenPoint(slot.transform.position), scale);
                    SelectPoint(point); layout.SetInformationCardHeight(148); ApplyLayout();
                    Rect touch = DefenderSlotVisual.TouchScreenRect(slot.transform, Camera.main);
                    touch = new Rect(Vector2.Scale(touch.position, scale), Vector2.Scale(touch.size, scale));
                    Assert.That(Bounds("SelectedPanel").Overlaps(touch), Is.False, $"{size} slot {slot.Index}: {touch} / {Bounds("SelectedPanel")}");
                }
            }
        }

        [Test] public void HeroLabelIs120PointsAndRightAlignedInsideSafeAreaAndWaveLabelFitsDoubleDigits()
        {
            ApplyLayout();
            Assert.That(Bounds("BottomBar/Hero").width, Is.EqualTo(120).Within(.1));
            Assert.That(Bounds("BottomBar/Hero").xMax, Is.EqualTo(Bounds("BottomBar/AbilityCooldown").xMax).Within(.1));
            Assert.That(Bounds("BottomBar/Hero").xMin, Is.GreaterThanOrEqualTo(safe.xMin));
            Assert.That(Bounds("BottomBar/Hero").xMax, Is.LessThanOrEqualTo(safe.xMax));
            Assert.That(RectAt("BottomBar/Hero").GetComponent<TMP_Text>().alignment, Is.EqualTo(TextAlignmentOptions.Right));
            var wave = RectAt("TopBar/WavePanel/Wave").GetComponent<TMP_Text>();
            wave.text = "DALGA 10/12";
            Assert.That(Bounds(wave.rectTransform).width, Is.GreaterThanOrEqualTo(99.9));
            Assert.That(wave.GetPreferredValues(wave.text).x, Is.LessThanOrEqualTo(wave.rectTransform.rect.width));
        }

        [Test] public void AbilityStaysAtRightSafeEdgeAcrossPreparationWaveAndPause()
        {
            foreach (var viewport in new[] { new Rect(0, 0, 667, 375), new Rect(47, 21, 750, 369), new Rect(59, 21, 814, 409) })
            {
                Viewport(new Vector2(viewport.width + 2 * viewport.x, viewport.height + viewport.y), viewport);
                typeof(GameSessionController).GetProperty("Phase").SetValue(session, GamePhase.Preparing);
                hud.Refresh(session); ApplyLayout(); Canvas.ForceUpdateCanvases();
                Rect before = Bounds("BottomBar/AbilityCooldown");
                var cooldown = hud.GetComponentInChildren<UiRadialCooldown>();
                Assert.That(before.xMax, Is.EqualTo(safe.xMax - 8).Within(.5));
                Assert.That(Bounds(cooldown.ActionRect).width, Is.EqualTo(76).Within(.5));
                Assert.That(Bounds(cooldown.ActionRect).height, Is.EqualTo(76).Within(.5));
                Assert.That(Bounds("BottomBar/NextWave").xMax, Is.LessThanOrEqualTo(before.xMin - 7.99f));
                foreach (var phase in new[] { GamePhase.Wave, GamePhase.Paused })
                {
                    typeof(GameSessionController).GetProperty("Phase").SetValue(session, phase);
                    hud.Refresh(session); ApplyLayout(); Canvas.ForceUpdateCanvases();
                    Same(before, Bounds("BottomBar/AbilityCooldown"), phase.ToString());
                }
            }
        }

        [UnityTest] public IEnumerator AbilityReadyGlowTracksCooldownPauseReturnTutorialAndReducedMotion()
        {
            var glow = hud.GetComponentsInChildren<Image>(true).Single(i => i.name == "AbilityReadyGlow");
            typeof(GreyboxHero).GetProperty("AbilityRemaining").SetValue(session.Hero, 0f);
            hud.Refresh(session); yield return null;
            Assert.That(glow.gameObject.activeInHierarchy, Is.True);
            var scale = glow.transform.localScale; var colour = glow.color;
            yield return new WaitForSecondsRealtime(.1f);
            Assert.That(glow.transform.localScale, Is.EqualTo(scale));
            Assert.That(glow.color, Is.EqualTo(colour));
            FrontendPreferences.ReducedMotion = false;
            yield return new WaitForSecondsRealtime(.1f);
            Assert.That(glow.transform.localScale, Is.Not.EqualTo(scale));
            typeof(GreyboxHero).GetProperty("AbilityRemaining").SetValue(session.Hero, 4f);
            hud.Refresh(session);
            Assert.That(glow.gameObject.activeInHierarchy, Is.False);
            typeof(GreyboxHero).GetProperty("AbilityRemaining").SetValue(session.Hero, 0f);
            session.TogglePause(); hud.Refresh(session);
            Assert.That(glow.gameObject.activeInHierarchy, Is.False);
            session.TogglePause(); hud.Refresh(session);
            hud.SetTutorialGuide(3, null, Camera.main); yield return null;
            Assert.That(glow.gameObject.activeInHierarchy, Is.False);
            hud.SetTutorialGuide(2, null, Camera.main); yield return null;
            Assert.That(glow.gameObject.activeInHierarchy, Is.True);
            session.Hero.TakeDamage(int.MaxValue); hud.Refresh(session);
            Assert.That(glow.gameObject.activeInHierarchy, Is.False);
        }

        [Test] public void RetiredPreviewNeverParticipatesInFrameLayoutOrObstacles()
        {
            var preview = RectAt("Preview");
            preview.gameObject.SetActive(true);
            preview.anchoredPosition = new Vector2(111, 222); preview.sizeDelta = new Vector2(17, 19);
            ApplyLayout();
            Assert.That(preview.anchoredPosition, Is.EqualTo(new Vector2(111, 222)));
            Assert.That(preview.sizeDelta, Is.EqualTo(new Vector2(17, 19)));
            var corners = new Vector3[4]; preview.GetWorldCorners(corners);
            var rect = Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
            Assert.That(layout.FrameScreenRects().Contains(rect), Is.False);
        }
    }
}
