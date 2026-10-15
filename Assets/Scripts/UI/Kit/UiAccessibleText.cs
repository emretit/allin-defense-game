using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Accessibility;

namespace AllinDefense.UI.Kit
{
    public sealed class UiAccessibleText : MonoBehaviour
    {
        [SerializeField] private TMP_Text target;
        [SerializeField] private string turkish, english;
        [SerializeField] private bool liveText = true;
        private static readonly HashSet<UiAccessibleText> texts = new();
        private AccessibilityHierarchy hierarchy;
        private AccessibilityNode node;
        private readonly Vector3[] corners = new Vector3[4];
        public string Label => liveText ? (target != null ? target.text : string.Empty) : GameLocalization.Text(turkish, english);
        public AccessibilityRole Role => AccessibilityRole.StaticText;
        public AccessibilityNode Node => node;
        public bool Focus() => UiAccessibleButton.Focus(this);
        public bool IsAccessible => isActiveAndEnabled && !string.IsNullOrWhiteSpace(Label) && UiAccessibleButton.IsInAccessibleScope(transform);

        public void Configure(TMP_Text text) { target = text; liveText = true; RefreshAccessibility(); }
        public void Configure(LocalizedCopy copy)
        { turkish = copy.Turkish; english = copy.English; liveText = false; RefreshAccessibility(); }
        private void OnEnable()
        {
            texts.Add(this); target = target != null ? target : GetComponent<TMP_Text>();
            GameLocalization.LanguageChanged += RefreshAccessibility;
        }
        private void LateUpdate()
        {
            if (Application.platform == RuntimePlatform.IPhonePlayer) RefreshNode();
        }
        internal static void RefreshAll()
        { foreach (var text in texts) if (text != null) text.RefreshAccessibility(); }
        public void RefreshAccessibility() { texts.Add(this); RefreshNode(true); }
        private void RefreshNode(bool force = false)
        {
            if (!force && !AssistiveSupport.isScreenReaderEnabled) { RemoveNode(); return; }
            if (!IsAccessible) { RemoveNode(); return; }
            if (node == null)
            {
                hierarchy = AssistiveSupport.activeHierarchy ?? new AccessibilityHierarchy();
                node = hierarchy.AddNode(Label, null); node.role = Role;
                node.frameGetter = ScreenFrame; node.focusChanged += FocusChanged;
                if (Application.platform == RuntimePlatform.IPhonePlayer) AssistiveSupport.activeHierarchy = hierarchy;
            }
            string label = Label;
            if (node.label != label) node.label = label;
        }
        private void FocusChanged(AccessibilityNode focused, bool hasFocus)
        { if (hasFocus) UiAccessibleButton.RecordFocus(this); }
        private Rect ScreenFrame()
        {
            ((RectTransform)transform).GetWorldCorners(corners);
            var canvas = GetComponentInParent<Canvas>();
            var camera = canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            Vector2 min = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
            Vector2 max = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }
        private void RemoveNode()
        {
            if (node == null) return;
            node.focusChanged -= FocusChanged; hierarchy.RemoveNode(node); node = null;
        }
        private void OnDisable()
        { texts.Remove(this); GameLocalization.LanguageChanged -= RefreshAccessibility; RemoveNode(); }
    }
}
