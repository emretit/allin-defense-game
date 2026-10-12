using System;
using System.Linq;
using AllinDefense.UI;
using AllinDefense.UI.Kit;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.U2D;

namespace AllinDefense.Tests
{
    public sealed class UiKitTests
    {
        private GameObject root;
        private UiKit kit;
        private bool reduced;
        [SetUp] public void SetUp()
        {
            root = new GameObject("KitTest", typeof(RectTransform), typeof(Canvas));
            root.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            ((RectTransform)root.transform).sizeDelta = new Vector2(1000, 1000);
            kit = new UiKit(AssetDatabase.LoadAssetAtPath<UiDesignSystemAssets>("Assets/Art/GeneratedPresentation/UiDesignSystemAssets.asset"));
            reduced = FrontendPreferences.ReducedMotion;
            FrontendPreferences.ReducedMotion = true;
        }
        [TearDown] public void TearDown() { UnityEngine.Object.DestroyImmediate(root); FrontendPreferences.ReducedMotion = reduced; }
        [TestCase(UiButtonVariant.Gold)] [TestCase(UiButtonVariant.Glass)] [TestCase(UiButtonVariant.Danger)]
        public void ButtonsKeepIdentityTouchSizeAndAllStates(UiButtonVariant variant)
        {
            var button = kit.Button(root.transform, "action", FrontendCopy.UpgradeStat, variant, kit.Assets.iconUpgrade, 120);
            var visual = button.GetComponent<UiVisualState>();
            Assert.That(button.GetComponent<UiId>().Id, Is.EqualTo("action"));
            Assert.That(button.GetComponent<UiPointSize>().minHeight, Is.GreaterThanOrEqualTo(variant == UiButtonVariant.Gold ? UiMetrics.PrimaryCtaPt : UiMetrics.MinTouchPt));
            foreach (UiState state in Enum.GetValues(typeof(UiState)))
            {
                visual.SetState(state);
                Assert.That(visual.State, Is.EqualTo(state));
                Assert.That(button.interactable, Is.EqualTo(state != UiState.Disabled));
                Assert.That(button.transform.localScale.x, Is.EqualTo(state == UiState.Pressed ? UiTokens.PressScale : 1f).Within(.0001f));
                if (state == UiState.Disabled)
                {
                    Assert.That(visual.Icon.color, Is.EqualTo(UiTokens.TextSecondary));
                    Assert.That(visual.Price.color, Is.EqualTo(UiTokens.Danger));
                    Assert.That(visual.Surface.color.a, Is.EqualTo(1));
                }
            }
            visual.SetState(UiState.Selected);
            visual.OnPointerDown(new PointerEventData(null)); visual.OnPointerUp(new PointerEventData(null));
            Assert.That(visual.State, Is.EqualTo(UiState.Selected));
        }
        [TestCase(10f)] [TestCase(14f)] [TestCase(20f)]
        public void SurfaceHasClippedHighlightShadowAndHairline(float radius)
        {
            var card = kit.Surface(root.transform, "card", radius);
            Assert.That(card.GetComponent<LayoutGroup>(), Is.Not.Null);
            var images = card.GetComponentsInChildren<Image>(true);
            Assert.That(images.Any(i => i.sprite == kit.Assets.verticalGradient), Is.True);
            Assert.That(images.Any(i => i.sprite == kit.Assets.softShadow), Is.True);
            Assert.That(card.GetComponentInChildren<Outline>().effectDistance, Is.EqualTo(Vector2.one * UiTokens.HairlineWidth));
            foreach (UiState state in Enum.GetValues(typeof(UiState))) { card.GetComponent<UiVisualState>().SetState(state); Assert.That(card.GetComponent<UiVisualState>().State, Is.EqualTo(state)); }
        }
        [Test] public void IconButtonHasCircularShapeAnd44PointMinimum()
        {
            var button = kit.IconButton(root.transform, "pause", kit.Assets.iconPause);
            Assert.That(button.GetComponent<UiPointSize>().minWidth, Is.GreaterThanOrEqualTo(44));
            Assert.That(button.GetComponent<UiVisualState>().Surface.sprite, Is.SameAs(kit.Assets.filledCircle));
        }
        [Test] public void StatPillUsesTabularNumbersAndReportsDelta()
        {
            var stat = kit.StatPill(root.transform, "gold", kit.Assets.iconSell, 1500);
            int delta = 0; stat.Changed += amount => delta = amount; stat.SetValue(1512);
            stat.GetComponent<UiVisualState>().SetState(UiState.Disabled);
            Assert.That(stat.GetComponent<UiVisualState>().Icon.color, Is.EqualTo(UiTokens.TextSecondary));
            Assert.That(delta, Is.EqualTo(12)); Assert.That(stat.Value, Is.EqualTo(1512));
            Assert.That(stat.GetComponentsInChildren<TMP_Text>().Any(t => t.text.Contains("mspace")), Is.True);
            Assert.That(stat.GetComponentsInChildren<TMP_Text>().Any(t => t.text == "+12"), Is.True);
        }
        [Test] public void ChipSizesToItsContentAndCarriesIdentity()
        {
            var chip = kit.Chip(root.transform, "price", "120");
            LayoutRebuilder.ForceRebuildLayoutImmediate(chip);
            Assert.That(LayoutUtility.GetPreferredWidth(chip), Is.GreaterThan(0));
            Assert.That(chip.GetComponent<UiId>().Id, Is.EqualTo("price"));
        }
        [Test] public void SwitchShowsWordsAndSymbolsAndRaisesOneChange()
        {
            var control = kit.Switch(root.transform, "switch", false);
            int calls = 0; control.Changed += _ => calls++;
            control.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Assert.That(control.Value, Is.True); Assert.That(calls, Is.EqualTo(1));
            Assert.That(control.GetComponentInChildren<TMP_Text>().text, Is.EqualTo(FrontendCopy.SwitchOn.Value));
            Assert.That(control.GetComponent<UiVisualState>().Icon.sprite, Is.SameAs(kit.Assets.iconCheck));
            control.SetValue(false);
            Assert.That(control.GetComponentInChildren<TMP_Text>().text, Is.EqualTo(FrontendCopy.SwitchOff.Value));
        }
        [Test] public void SliderClampsValueHasIconAnd44PointTarget()
        {
            var slider = kit.Slider(root.transform, "volume", kit.Assets.iconSettings, 2);
            Assert.That(slider.value, Is.EqualTo(1));
            Assert.That(slider.fillRect, Is.Not.Null);
            Assert.That(slider.handleRect, Is.Not.Null);
            Assert.That(slider.GetComponent<UiPointSize>().minHeight, Is.GreaterThanOrEqualTo(44));
            slider.value = .25f; Assert.That(slider.normalizedValue, Is.EqualTo(.25f));
            slider.GetComponent<UiVisualState>().SetState(UiState.Disabled);
            Assert.That(slider.interactable, Is.False);
            Assert.That(slider.GetComponent<UiVisualState>().Icon.color, Is.EqualTo(UiTokens.TextSecondary));
        }
        [Test] public void SegmentsSelectExactlyOneAndIgnoreDisabledChoices()
        {
            var control = kit.SegmentedControl(root.transform, "speed", new LocalizedCopy[] { "1×", "2×", "3×" });
            int selected = -1; control.Changed += index => selected = index;
            control.Select(1); Assert.That(selected, Is.EqualTo(1));
            var buttons = control.GetComponentsInChildren<UnityEngine.UI.Button>();
            Assert.That(buttons.Count(b => b.GetComponent<UiVisualState>().State == UiState.Selected), Is.EqualTo(1));
            buttons[2].GetComponent<UiVisualState>().SetState(UiState.Disabled); control.Select(2);
            Assert.That(control.Selected, Is.EqualTo(1));
        }
        [Test] public void ProgressSegmentsFillCompleteAndPartialWaves()
        {
            var progress = kit.ProgressSegments(root.transform, "wave", 7); progress.SetProgress(2.5f);
            var fills = progress.GetComponentsInChildren<Image>().Where(i => i.type == Image.Type.Filled).ToArray();
            Assert.That(fills.Select(i => i.fillAmount), Is.EqualTo(new[] { 1f, 1f, .5f, 0, 0, 0, 0 }));
            progress.SetProgress(100); Assert.That(progress.Progress, Is.EqualTo(7));
        }
        [Test] public void CooldownUsesRadialFillSecondsAndReadyBadge()
        {
            var cooldown = kit.RadialCooldown(root.transform, "ability", kit.Assets.iconShield);
            Assert.That(cooldown.Visual.GetComponent<UiPointSize>().Points, Is.EqualTo(72));
            cooldown.SetCooldown(3.2f, 8); Assert.That(cooldown.Fill, Is.EqualTo(.4f).Within(.0001f));
            Assert.That(cooldown.GetComponentsInChildren<TMP_Text>().Any(t => t.text == FrontendCopy.Seconds.Format(4)), Is.True);
            cooldown.SetCooldown(0, 8); Assert.That(cooldown.IsReady, Is.True);
            Assert.That(cooldown.Visual.State, Is.EqualTo(UiState.Ready));
            Assert.That(cooldown.GetComponentsInChildren<TMP_Text>().Any(t => t.text == FrontendCopy.Ready.Value), Is.True);
            cooldown.SetCooldown(-5, 0); Assert.That(cooldown.Fill, Is.Zero);
        }
        [TestCase(0, 0)] [TestCase(500, 0)] [TestCase(0, 400)] [TestCase(500, 400)] [TestCase(250, 200)]
        public void RadialMenuClampsTargetsWithoutOverlap(float x, float y)
        {
            var safe = new Rect(0, 0, 500, 400);
            var positions = UiRadialLayout.Calculate(safe, new Vector2(x, y), 4, 72, out bool half);
            Assert.That(half, Is.EqualTo(x != 250 || y != 200));
            for (int i = 0; i < positions.Length; i++)
            {
                Assert.That(positions[i].x, Is.InRange(36, 464)); Assert.That(positions[i].y, Is.InRange(36, 364));
                for (int j = i + 1; j < positions.Length; j++) Assert.That(Vector2.Distance(positions[i], positions[j]), Is.GreaterThanOrEqualTo(72));
            }
        }
        [Test] public void RadialMenuPreviewsThenConfirmsOnlySameAvailableChoice()
        {
            var menu = kit.RadialMenu(root.transform, "build", new[] { new UiRadialOption("a", kit.Assets.iconShield, 100), new UiRadialOption("b", kit.Assets.iconUpgrade, 120) });
            int previews = 0, confirmations = 0, closes = 0;
            menu.Previewed += _ => previews++; menu.Confirmed += _ => confirmations++; menu.Closed += () => closes++;
            menu.Choose(0); Assert.That(previews, Is.EqualTo(1)); Assert.That(confirmations, Is.Zero);
            menu.Choose(1); Assert.That(previews, Is.EqualTo(2)); Assert.That(confirmations, Is.Zero);
            menu.Choose(1); Assert.That(confirmations, Is.EqualTo(1)); Assert.That(menu.IsOpen, Is.False);
            menu.OpenLocal(Vector2.zero, new Rect(-500, -500, 1000, 1000));
            menu.SetAvailable(0, false); menu.Choose(0); Assert.That(menu.PreviewIndex, Is.EqualTo(-1));
            menu.OnPointerClick(new PointerEventData(null) { position = new Vector2(-10000, -10000) });
            Assert.That(menu.IsOpen, Is.False); Assert.That(closes, Is.EqualTo(2));
        }
        [TestCase(false)] [TestCase(true)] public void OverlaysShowAndDismissImmediatelyWithReducedMotion(bool bottom)
        {
            var overlay = bottom ? kit.BottomSheet(root.transform, "sheet", FrontendCopy.SettingsTitle) : kit.Modal(root.transform, "modal", FrontendCopy.SettingsTitle);
            overlay.Hide(); Assert.That(overlay.gameObject.activeSelf, Is.False);
            overlay.Show(); Assert.That(overlay.IsOpen, Is.True); Assert.That(overlay.GetComponent<CanvasGroup>().alpha, Is.EqualTo(1));
            overlay.GetComponentInChildren<UnityEngine.UI.Button>().onClick.Invoke(); Assert.That(overlay.IsOpen, Is.False);
        }
        [Test] public void StarsClampAndReducedMotionFinishesImmediately()
        {
            var stars = kit.StarRow(root.transform, "stars", 3); Assert.That(stars.VisibleStars, Is.EqualTo(3));
            stars.SetStars(5); Assert.That(stars.Earned, Is.EqualTo(3)); stars.SetStars(-1); Assert.That(stars.VisibleStars, Is.Zero);
        }
        [Test] public void EveryMotionHelperCompletesSynchronouslyWithReducedMotion()
        {
            var motion = root.AddComponent<UiMotion>(); var group = root.AddComponent<CanvasGroup>();
            var rect = (RectTransform)root.transform; int number = 0;
            Assert.That(motion.Fade(group, .25f).IsComplete, Is.True); Assert.That(group.alpha, Is.EqualTo(.25f));
            Assert.That(motion.Scale(rect, Vector3.one * 2).IsComplete, Is.True); Assert.That(rect.localScale.x, Is.EqualTo(2));
            Assert.That(motion.Slide(rect, Vector2.one * 10).IsComplete, Is.True); Assert.That(rect.anchoredPosition, Is.EqualTo(Vector2.one * 10));
            Assert.That(motion.Punch(rect, 2).IsComplete, Is.True); Assert.That(rect.localScale.x, Is.EqualTo(2).Within(.0001f));
            Assert.That(motion.Count(0, 200, n => number = n).IsComplete, Is.True); Assert.That(number, Is.EqualTo(200));
        }
        [Test] public void ComponentTextMeetsBodyContrastIncludingDisabledPriceAndDanger()
        {
            foreach (var foreground in new[] { UiTokens.TextPrimary, UiTokens.TextSecondary, UiTokens.Danger, UiTokens.Gold, UiTokens.Success })
                Assert.That(Contrast(foreground, UiTokens.Surface), Is.GreaterThanOrEqualTo(4.5f), foreground.ToString());
            foreach (var gold in new[] { UiTokens.GoldLow, UiTokens.Gold, UiTokens.GoldHigh })
            {
                Assert.That(Contrast(UiTokens.GoldText, gold), Is.GreaterThanOrEqualTo(4.5f));
                Assert.That(Contrast(UiTokens.GoldText, gold * UiTokens.PressedTint), Is.GreaterThanOrEqualTo(4.5f));
            }
        }
        [Test] public void AllFifteenSystemIconsAreWhiteAndPackedAlongsidePaintedIcons()
        {
            var fields = typeof(UiDesignSystemAssets).GetFields().Where(f => f.Name.StartsWith("icon")).ToArray();
            Assert.That(fields.Length, Is.EqualTo(15));
            var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>("Assets/Art/UI/AllinDefenseUI.spriteatlas");
            var packables = SpriteAtlasExtensions.GetPackables(atlas);
            foreach (var field in fields)
            {
                var sprite = (Sprite)field.GetValue(kit.Assets); Assert.That(sprite, Is.Not.Null, field.Name);
                Assert.That(packables, Does.Contain(AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GetAssetPath(sprite))));
                var texture = new Texture2D(2, 2);
                try
                {
                    texture.LoadImage(System.IO.File.ReadAllBytes(AssetDatabase.GetAssetPath(sprite)));
                    var visible = texture.GetPixels32().Where(c => c.a > 0).ToArray(); Assert.That(visible.Length, Is.GreaterThan(100));
                    Assert.That(visible.All(c => c.r == 255 && c.g == 255 && c.b == 255), Is.True);
                }
                finally { UnityEngine.Object.DestroyImmediate(texture); }
            }
        }
        [Test] public void CooldownBadgeDoesNotIntersectRingAndIconsRemainLegible()
        {
            var cooldown = kit.RadialCooldown(root.transform, "ability", kit.Assets.iconShield);
            ((RectTransform)cooldown.transform).sizeDelta = new Vector2(120, 160);
            foreach (float remaining in new[] { 18f, 0f })
            {
                cooldown.SetCooldown(remaining, 24);
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)cooldown.transform);
                var action = new Vector3[4]; var badge = new Vector3[4];
                cooldown.ActionRect.GetWorldCorners(action); cooldown.BadgeRect.GetWorldCorners(badge);
                Assert.That(badge[0].y, Is.GreaterThanOrEqualTo(action[1].y));
                float ratio = cooldown.Visual.Icon.rectTransform.rect.width / cooldown.ActionRect.rect.width;
                Assert.That(ratio, Is.InRange(.5f, .65f));
            }
            var button = kit.IconButton(root.transform, "pause.ratio", kit.Assets.iconPause);
            ((RectTransform)button.transform).sizeDelta = Vector2.one * UiMetrics.MinTouchPt;
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)button.transform);
            Assert.That(button.GetComponent<UiVisualState>().Icon.rectTransform.rect.width / UiMetrics.MinTouchPt, Is.InRange(.5f, .65f));
        }
        [Test] public void TextCannotShrinkBelowItsFullLineHeight()
        {
            var button = kit.Button(root.transform, "price.minimum", "NORMAL", UiButtonVariant.Gold, kit.Assets.iconUpgrade, 120);
            ((RectTransform)button.transform).sizeDelta = new Vector2(400, 52);
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)button.transform);
            foreach (var text in button.GetComponentsInChildren<TMP_Text>())
            {
                text.ForceMeshUpdate();
                Assert.That(text.rectTransform.rect.height, Is.GreaterThanOrEqualTo(text.preferredHeight - .1f), text.name);
                Assert.That(text.textInfo.characterCount, Is.GreaterThan(0), text.name);
            }
        }
        private static float Contrast(Color a, Color b) => (Mathf.Max(Luminance(a), Luminance(b)) + .05f) / (Mathf.Min(Luminance(a), Luminance(b)) + .05f);
        private static float Luminance(Color c) => .2126f * Linear(c.r) + .7152f * Linear(c.g) + .0722f * Linear(c.b);
        private static float Linear(float value) => value <= .04045f ? value / 12.92f : Mathf.Pow((value + .055f) / 1.055f, 2.4f);
    }
}
