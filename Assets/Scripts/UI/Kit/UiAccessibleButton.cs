using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace AllinDefense.UI.Kit
{
    public sealed class UiAccessibleButton : MonoBehaviour
    {
        [SerializeField] private string turkish, english;
        [SerializeField] private bool modalScope;
        private static readonly HashSet<UiAccessibleButton> scopes = new();
        private static readonly List<CanvasGroup> canvasGroups = new();
        private static readonly HashSet<UiAccessibleButton> controls = new();
        public static Component FocusedElement { get; private set; }
        private string explicitValue;
        private AccessibilityHierarchy hierarchy;
        private AccessibilityNode node;
        private readonly Vector3[] corners = new Vector3[4];
        public string Label => GameLocalization.Text(turkish, english);
        public AccessibilityRole Role => GetComponent<Slider>() != null ? AccessibilityRole.Slider :
            GetComponent<Toggle>() != null || GetComponent<UiSwitch>() != null ? AccessibilityRole.Toggle :
            GetComponent<UiSegmentedControl>() != null ? AccessibilityRole.TabBar :
            GetComponentInParent<UiSegmentedControl>() != null ? AccessibilityRole.TabButton : AccessibilityRole.Button;
        public bool IsSelected
        {
            get
            {
                var toggle = GetComponent<Toggle>(); if (toggle != null) return toggle.isOn;
                var control = GetComponent<UiSwitch>(); if (control != null) return control.Value;
                var segments = GetComponentInParent<UiSegmentedControl>();
                return segments != null && segments.SelectedButton != null && segments.SelectedButton.gameObject == gameObject;
            }
        }
        public string Value
        {
            get
            {
                if (explicitValue != null) return explicitValue;
                var slider = GetComponent<Slider>();
                if (slider != null) return FrontendCopy.AccessiblePercent.Format(Mathf.RoundToInt(slider.normalizedValue * 100));
                var segments = GetComponent<UiSegmentedControl>();
                if (segments != null) return segments.SelectedButton?.GetComponent<UiAccessibleButton>()?.Label ?? string.Empty;
                if (Role == AccessibilityRole.Toggle) return (IsSelected ? FrontendCopy.SwitchOn : FrontendCopy.SwitchOff).Value;
                if (Role == AccessibilityRole.TabButton && IsSelected) return FrontendCopy.AccessibleSelected.Value;
                return string.Empty;
            }
        }
        public bool IsAccessible
        {
            get
            {
                if (!isActiveAndEnabled || modalScope || string.IsNullOrWhiteSpace(Label)) return false;
                return IsInAccessibleScope(transform);
            }
        }

        public AccessibilityNode Node => node;
        public bool Focus() => Focus(this);
        public void SetValue(string value) { explicitValue = value; UpdateLabel(); }
        public static bool IsInAccessibleScope(Transform target)
        {
            UiAccessibleButton top = null;
            foreach (var scope in scopes)
            {
                if (scope == null || !scope.isActiveAndEnabled) continue;
                var scopeCanvas = scope.GetComponentInParent<Canvas>();
                var topCanvas = top == null ? null : top.GetComponentInParent<Canvas>();
                if (top == null || (scopeCanvas != null && topCanvas != null && scopeCanvas.sortingOrder != topCanvas.sortingOrder
                    ? scopeCanvas.sortingOrder > topCanvas.sortingOrder : IsAbove(scope.transform, top.transform))) top = scope;
            }
            if (top != null && !target.IsChildOf(top.transform)) return false;
            target.GetComponentsInParent(false, canvasGroups);
            foreach (var group in canvasGroups) if (group.alpha <= 0) return false;
            return true;
        }

        internal static void RefreshAll()
        {
            foreach (var control in controls) if (control != null) control.RefreshAccessibility();
            UiAccessibleText.RefreshAll();
        }

        internal static bool Focus(Component target)
        {
            if (target == null || !target.gameObject.activeInHierarchy || !IsInAccessibleScope(target.transform)) return false;
            var button = target as UiAccessibleButton;
            var text = target as UiAccessibleText;
            if (button != null && !button.IsAccessible || text != null && !text.IsAccessible) return false;
            var selectable = target.GetComponent<Selectable>();
            if (selectable != null && !selectable.IsInteractable()) return false;
            FocusedElement = target;
            Reveal(target.transform);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(target.gameObject);
            button?.RefreshAccessibility(); text?.RefreshAccessibility();
            var focusNode = button != null ? button.Node : text != null ? text.Node : null;
            if (Application.platform == RuntimePlatform.IPhonePlayer && AssistiveSupport.isScreenReaderEnabled && focusNode != null)
                AssistiveSupport.notificationDispatcher.SendScreenChanged(focusNode);
            return true;
        }

        internal static void Reveal(Transform target)
        {
            var scroll = target.GetComponentInParent<ScrollRect>();
            if (scroll == null || scroll.content == null || !target.IsChildOf(scroll.content)) return;
            var viewport = scroll.viewport != null ? scroll.viewport : (RectTransform)scroll.transform;
            Canvas.ForceUpdateCanvases();
            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, target);
            var view = viewport.rect;
            var offset = Vector3.zero;
            if (scroll.vertical) offset.y = bounds.min.y < view.yMin ? view.yMin - bounds.min.y : bounds.max.y > view.yMax ? view.yMax - bounds.max.y : 0;
            if (scroll.horizontal) offset.x = bounds.min.x < view.xMin ? view.xMin - bounds.min.x : bounds.max.x > view.xMax ? view.xMax - bounds.max.x : 0;
            scroll.StopMovement();
            scroll.content.position += viewport.TransformVector(offset);
        }

        internal static void RecordFocus(Component target)
        {
            FocusedElement = target; Reveal(target.transform);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(target.gameObject);
        }
        private void FocusChanged(AccessibilityNode focused, bool hasFocus)
        { if (hasFocus) RecordFocus(this); }

        internal static bool IsAbove(Transform candidate, Transform current)
        {
            if (candidate.IsChildOf(current)) return true;
            if (current.IsChildOf(candidate)) return false;
            while (candidate.parent != current.parent)
            {
                int candidateDepth = 0, currentDepth = 0;
                for (var parent = candidate.parent; parent != null; parent = parent.parent) candidateDepth++;
                for (var parent = current.parent; parent != null; parent = parent.parent) currentDepth++;
                if (candidateDepth >= currentDepth) candidate = candidate.parent;
                if (currentDepth >= candidateDepth) current = current.parent;
            }
            return candidate.GetSiblingIndex() > current.GetSiblingIndex();
        }

        public void Configure(LocalizedCopy copy)
        {
            controls.Add(this);
            turkish = copy.Turkish; english = copy.English;
            UpdateLabel();
        }
        public void ConfigureModalScope()
        {
            modalScope = true;
            if (isActiveAndEnabled) scopes.Add(this);
            RemoveNode();
            RefreshAll();
        }
        private void OnEnable()
        {
            controls.Add(this);
            GameLocalization.LanguageChanged += UpdateLabel;
            if (modalScope) scopes.Add(this);
            RefreshNode();
        }
        private void LateUpdate() => RefreshNode();
        public void RefreshAccessibility() { controls.Add(this); RefreshNode(true); }
        private void RefreshNode(bool force = false)
        {
            if (!force && Application.platform != RuntimePlatform.IPhonePlayer) return;
            if ((!force && !AssistiveSupport.isScreenReaderEnabled) || !IsAccessible) { RemoveNode(); return; }
            if (node == null)
            {
                hierarchy = AssistiveSupport.activeHierarchy ?? new AccessibilityHierarchy();
                node = hierarchy.AddNode(Label, null);
                node.frameGetter = ScreenFrame;
                node.focusChanged += FocusChanged;
                node.invoked += Invoke;
                node.incremented += Increment;
                node.decremented += Decrement;
                if (Application.platform == RuntimePlatform.IPhonePlayer) AssistiveSupport.activeHierarchy = hierarchy;
            }
            UpdateLabel();
            node.role = Role;
            var selectable = GetComponent<Selectable>();
            node.state = selectable != null && !selectable.IsInteractable() ? AccessibilityState.Disabled :
                IsSelected ? AccessibilityState.Selected : AccessibilityState.None;
        }
        private void UpdateLabel() { if (node != null) { node.label = Label; node.value = Value; } }
        public bool Invoke()
        {
            if (!CanInteract()) return false;
            var toggle = GetComponent<Toggle>();
            if (toggle != null) { toggle.isOn = !toggle.isOn; return true; }
            var button = GetComponent<Button>();
            if (button != null) { button.onClick.Invoke(); UpdateLabel(); return true; }
            var segments = GetComponent<UiSegmentedControl>();
            if (segments != null && segments.Count > 0)
            {
                segments.Select((segments.Selected + 1) % segments.Count);
                UpdateLabel(); return true;
            }
            return Adjust(1);
        }
        private bool CanInteract()
        {
            if (!IsAccessible) return false;
            var selectable = GetComponent<Selectable>();
            if (selectable != null) return selectable.IsActive() && selectable.IsInteractable();
            foreach (var group in GetComponentsInParent<CanvasGroup>()) if (!group.interactable) return false;
            return GetComponent<UiSegmentedControl>() != null;
        }
        public bool Adjust(int direction)
        {
            if (!CanInteract() || direction == 0) return false;
            var slider = GetComponent<Slider>();
            if (slider != null)
            {
                float previous = slider.value;
                slider.value += Mathf.Sign(direction) * (slider.wholeNumbers ? 1 : (slider.maxValue - slider.minValue) * .1f);
                UpdateLabel(); return !Mathf.Approximately(previous, slider.value);
            }
            var segments = GetComponent<UiSegmentedControl>();
            if (segments == null) return false;
            int previousIndex = segments.Selected;
            segments.Select(previousIndex + (direction > 0 ? 1 : -1));
            UpdateLabel(); return previousIndex != segments.Selected;
        }
        private void Increment() => Adjust(1);
        private void Decrement() => Adjust(-1);
        private Rect ScreenFrame()
        {
            var rect = (RectTransform)transform;
            rect.GetWorldCorners(corners);
            var canvas = GetComponentInParent<Canvas>();
            var camera = canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            Vector2 min = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
            Vector2 max = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }
        private void RemoveNode()
        {
            if (node == null) return;
            node.focusChanged -= FocusChanged;
            node.invoked -= Invoke; node.incremented -= Increment; node.decremented -= Decrement;
            hierarchy.RemoveNode(node); node = null;
        }
        private void OnDisable()
        {
            GameLocalization.LanguageChanged -= UpdateLabel;
            controls.Remove(this);
            scopes.Remove(this);
            RemoveNode();
            if (FocusedElement == this) FocusedElement = null;
            if (modalScope) RefreshAll();
        }
    }
}
