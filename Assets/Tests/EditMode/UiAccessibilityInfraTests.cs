using System;
using AllinDefense.UI;
using AllinDefense.UI.Kit;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AllinDefense.Tests
{
    public sealed class UiAccessibilityInfraTests
    {
        private const string MotionKey = "AllinDefense.Frontend.ReducedMotion";
        private const string ShakeKey = "AllinDefense.Frontend.CameraShake";
        private GameObject root, events;
        private UiKit kit;
        private bool hadMotion, hadShake;
        private int motion, shake;
        private IUiSystemAccessibilitySource originalSource;
        private AccessibilityHierarchy originalHierarchy, hierarchy;
        private sealed class FakeSource : IUiSystemAccessibilitySource
        {
            public bool ReducedMotion { get; private set; }
            public event Action Changed;
            public void Set(bool value) { ReducedMotion = value; Changed?.Invoke(); }
        }
        [SetUp] public void SetUp()
        {
            hadMotion = PlayerPrefs.HasKey(MotionKey); motion = PlayerPrefs.GetInt(MotionKey);
            hadShake = PlayerPrefs.HasKey(ShakeKey); shake = PlayerPrefs.GetInt(ShakeKey);
            originalSource = UiSystemAccessibility.Source;
            originalHierarchy = AssistiveSupport.activeHierarchy;
            hierarchy = new AccessibilityHierarchy(); AssistiveSupport.activeHierarchy = hierarchy;
            root = new GameObject("A11yInfra", typeof(RectTransform), typeof(Canvas));
            events = new GameObject("A11yEvents", typeof(EventSystem));
            typeof(EventSystem).GetMethod("OnEnable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(events.GetComponent<EventSystem>(), null);
            EventSystem.current = events.GetComponent<EventSystem>();
            kit = new UiKit(AssetDatabase.LoadAssetAtPath<UiDesignSystemAssets>("Assets/Art/GeneratedPresentation/UiDesignSystemAssets.asset"));
        }
        [TearDown] public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(root);
            typeof(EventSystem).GetMethod("OnDisable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(events.GetComponent<EventSystem>(), null); UnityEngine.Object.DestroyImmediate(events);
            hierarchy.Clear(); AssistiveSupport.activeHierarchy = originalHierarchy;
            if (hadMotion) PlayerPrefs.SetInt(MotionKey, motion); else PlayerPrefs.DeleteKey(MotionKey);
            if (hadShake) PlayerPrefs.SetInt(ShakeKey, shake); else PlayerPrefs.DeleteKey(ShakeKey);
            UiSystemAccessibility.Source = originalSource; PlayerPrefs.Save();
        }
        [Test] public void MarkedTextIsStaticTextInTheHierarchyAndTracksLiveText()
        {
            var text = kit.AccessibleText(root.transform, "info", "Enemy information");
            var accessible = text.GetComponent<UiAccessibleText>();
            Assert.That(hierarchy.ContainsNode(accessible.Node), Is.True);
            Assert.That(accessible.Node.role, Is.EqualTo(AccessibilityRole.StaticText));
            Assert.That(accessible.Node.role, Is.Not.EqualTo(AccessibilityRole.Button));
            text.text = "Updated information"; accessible.RefreshAccessibility();
            Assert.That(accessible.Node.label, Is.EqualTo("Updated information"));
            text.gameObject.SetActive(false); accessible.RefreshAccessibility();
            Assert.That(accessible.Node, Is.Null);
        }
        [Test] public void LocalizedTextCanBindWithoutAnInteractiveControl()
        {
            var rect = UiKit.Node(root.transform, "localized");
            rect.gameObject.AddComponent<TextMeshProUGUI>();
            var accessible = rect.gameObject.AddComponent<UiAccessibleText>();
            accessible.Configure(new LocalizedCopy("Bilgi", "Information"));
            Assert.That(accessible.Node.label, Is.EqualTo(GameLocalization.Text("Bilgi", "Information")));
            Assert.That(rect.GetComponent<Selectable>(), Is.Null);
        }
        [Test] public void ModalExcludesBackgroundAndRestoresOpeningControl()
        {
            var opener = kit.Button(root.transform, "open", "Open").GetComponent<UiAccessibleButton>();
            var background = kit.AccessibleText(root.transform, "background", "Background").GetComponent<UiAccessibleText>();
            opener.RefreshAccessibility(); EventSystem.current.SetSelectedGameObject(opener.gameObject);
            var overlay = kit.Modal(root.transform, "modal", "Title");
            var first = kit.AccessibleText(overlay.Body, "modal.info", "Information").GetComponent<UiAccessibleText>();
            overlay.FocusFirst();
            Assert.That(opener.IsAccessible, Is.False); Assert.That(opener.Node, Is.Null);
            Assert.That(background.IsAccessible, Is.False); Assert.That(background.Node, Is.Null);
            Assert.That(UiAccessibleButton.FocusedElement, Is.SameAs(first));
            overlay.Hide();
            Assert.That(opener.IsAccessible, Is.True); Assert.That(hierarchy.ContainsNode(opener.Node), Is.True);
            Assert.That(UiAccessibleButton.FocusedElement, Is.SameAs(opener));
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.SameAs(opener.gameObject));
            overlay.Show(); Assert.That(opener.IsAccessible, Is.False);
            overlay.Hide(); Assert.That(UiAccessibleButton.FocusedElement, Is.SameAs(opener));
        }
        [Test] public void BottomSheetPreservesBackgroundAccessAndFocus()
        {
            var opener = kit.Button(root.transform, "open", "Open").GetComponent<UiAccessibleButton>();
            Assert.That(opener.Focus(), Is.True);
            var sheet = kit.BottomSheet(root.transform, "sheet", "Selection");
            sheet.FocusFirst();
            Assert.That(opener.IsAccessible, Is.True);
            Assert.That(UiAccessibleButton.FocusedElement, Is.SameAs(opener));
            sheet.Hide(); sheet.Show();
            Assert.That(opener.IsAccessible, Is.True);
            Assert.That(UiAccessibleButton.FocusedElement, Is.SameAs(opener));
        }
        [Test] public void NestedModalRestoresParentFocusBeforeReturningToOpener()
        {
            var opener = kit.Button(root.transform, "open", "Open").GetComponent<UiAccessibleButton>();
            Assert.That(opener.Focus(), Is.True);
            var outer = kit.Modal(root.transform, "outer", "Outer");
            var parent = kit.Button(outer.Body, "parent", "Details").GetComponent<UiAccessibleButton>();
            outer.FocusFirst();
            var inner = kit.Modal(root.transform, "inner", "Inner"); inner.FocusFirst();
            Assert.That(parent.IsAccessible, Is.False);
            inner.Hide();
            Assert.That(UiAccessibleButton.FocusedElement, Is.SameAs(parent));
            Assert.That(opener.IsAccessible, Is.False);
            outer.Hide(); Assert.That(UiAccessibleButton.FocusedElement, Is.SameAs(opener));
        }
        [Test] public void FocusingClippedControlRevealsItInScrollViewport()
        {
            var viewport = UiKit.Node(root.transform, "viewport"); viewport.sizeDelta = new Vector2(200, 200);
            var scroll = viewport.gameObject.AddComponent<ScrollRect>(); scroll.viewport = viewport; scroll.horizontal = false;
            var content = UiKit.Node(viewport, "content"); content.sizeDelta = new Vector2(200, 800); scroll.content = content;
            var target = UiKit.Node(content, "clipped"); target.sizeDelta = new Vector2(100, 40); target.anchoredPosition = new Vector2(0, -300);
            target.gameObject.AddComponent<Button>();
            var accessible = target.gameObject.AddComponent<UiAccessibleButton>(); accessible.Configure("Action");
            Assert.That(accessible.Focus(), Is.True);
            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, target);
            Assert.That(bounds.min.y, Is.GreaterThanOrEqualTo(viewport.rect.yMin - .01f));
            Assert.That(bounds.max.y, Is.LessThanOrEqualTo(viewport.rect.yMax + .01f));
        }
        [Test] public void DynamicValueUpdatesExistingNodeAndNullRestoresAutomaticValue()
        {
            var control = kit.Button(root.transform, "speed", "Speed").GetComponent<UiAccessibleButton>();
            control.RefreshAccessibility(); var node = control.Node;
            control.SetValue("2x");
            Assert.That(control.Node, Is.SameAs(node)); Assert.That(node.value, Is.EqualTo("2x"));
            control.SetValue(null); Assert.That(node.value, Is.Empty);
        }
        [Test] public void FailedFocusTrapWaitsForASelectionChange()
        {
            var opener = kit.Button(root.transform, "open", "Open");
            var other = kit.Button(root.transform, "other", "Other");
            EventSystem.current.SetSelectedGameObject(opener.gameObject);
            var overlay = kit.Modal(root.transform, "modal", "Title");
            foreach (var button in overlay.GetComponentsInChildren<Button>()) button.interactable = false;
            var update = typeof(UiOverlay).GetMethod("LateUpdate", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            update.Invoke(overlay, null);
            var available = kit.Button(overlay.Body, "available", "Available");
            update.Invoke(overlay, null);
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.SameAs(opener.gameObject));
            EventSystem.current.SetSelectedGameObject(other.gameObject);
            update.Invoke(overlay, null);
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.SameAs(available.gameObject));
        }
        [Test] public void PreferredModalFocusWinsOverEarlierControls()
        {
            var overlay = kit.Modal(root.transform, "modal", "Title");
            kit.Button(overlay.Body, "first", "First");
            var safe = kit.Button(overlay.Body, "safe", "Safe").GetComponent<UiAccessibleButton>();
            overlay.SetFirstFocus(safe); overlay.FocusFirst();
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.SameAs(safe.gameObject));
        }
        [TestCase(0f, UiState.Ready)] [TestCase(5f, UiState.Normal)]
        public void LeavingReturningRestoresCooldownState(float remaining, UiState state)
        {
            var cooldown = kit.RadialCooldown(root.transform, "cooldown", kit.Assets.iconClose);
            cooldown.SetCooldown(remaining, 10);
            float fill = cooldown.Fill;
            cooldown.SetReturning(true); cooldown.SetReturning(true);
            Assert.That(cooldown.Visual.State, Is.EqualTo(UiState.Disabled));
            cooldown.SetReturning(false);
            Assert.That(cooldown.Visual.State, Is.EqualTo(state));
            Assert.That(cooldown.Fill, Is.EqualTo(fill));
            Assert.That(cooldown.BadgeRect.gameObject.activeSelf, Is.True);
        }
        [Test] public void SystemMotionSuppliesDefaultsAndNotifiesWithoutWritingPreferences()
        {
            PlayerPrefs.DeleteKey(MotionKey); PlayerPrefs.DeleteKey(ShakeKey);
            var source = new FakeSource(); UiSystemAccessibility.Source = source;
            int changes = 0; Action changed = () => changes++;
            FrontendPreferences.MotionChanged += changed;
            try
            {
                Assert.That(FrontendPreferences.ReducedMotion, Is.False); Assert.That(FrontendPreferences.CameraShake, Is.True);
                source.Set(true);
                Assert.That(FrontendPreferences.ReducedMotion, Is.True); Assert.That(FrontendPreferences.CameraShake, Is.False);
                Assert.That(changes, Is.EqualTo(1)); Assert.That(PlayerPrefs.HasKey(MotionKey), Is.False);
            }
            finally { FrontendPreferences.MotionChanged -= changed; }
        }
        [TestCase(false)] [TestCase(true)] public void ExplicitMotionAndShakePreferencesSurviveSystemChanges(bool value)
        {
            var source = new FakeSource(); UiSystemAccessibility.Source = source;
            FrontendPreferences.ReducedMotion = value; FrontendPreferences.CameraShake = value;
            source.Set(!value);
            Assert.That(FrontendPreferences.ReducedMotion, Is.EqualTo(value));
            Assert.That(FrontendPreferences.CameraShake, Is.EqualTo(value));
            Assert.That(PlayerPrefs.GetInt(MotionKey), Is.EqualTo(value ? 1 : 0));
        }
    }
}
