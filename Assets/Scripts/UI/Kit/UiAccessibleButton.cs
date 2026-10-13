using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.UI;

namespace AllinDefense.UI.Kit
{
    public sealed class UiAccessibleButton : MonoBehaviour
    {
        [SerializeField] private string turkish, english;
        private AccessibilityHierarchy hierarchy;
        private AccessibilityNode node;
        private readonly Vector3[] corners = new Vector3[4];
        public string Label => GameLocalization.Text(turkish, english);

        public void Configure(LocalizedCopy copy)
        {
            turkish = copy.Turkish; english = copy.English;
            UpdateLabel();
        }

        private void OnEnable()
        {
            GameLocalization.LanguageChanged += UpdateLabel;
            if (Application.platform != RuntimePlatform.IPhonePlayer) return;
            hierarchy = AssistiveSupport.activeHierarchy ?? new AccessibilityHierarchy();
            node = hierarchy.AddNode(Label, null);
            node.role = AccessibilityRole.Button;
            node.frameGetter = ScreenFrame;
            node.invoked += Invoke;
            AssistiveSupport.activeHierarchy = hierarchy;
        }

        private void UpdateLabel() { if (node != null) node.label = Label; }
        private bool Invoke()
        {
            var button = GetComponent<Button>();
            if (button == null || !button.IsActive() || !button.IsInteractable()) return false;
            button.onClick.Invoke();
            return true;
        }

        private Rect ScreenFrame()
        {
            var rect = (RectTransform)transform;
            rect.GetWorldCorners(corners);
            var canvas = GetComponentInParent<Canvas>();
            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            Vector2 min = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
            Vector2 max = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private void OnDisable()
        {
            GameLocalization.LanguageChanged -= UpdateLabel;
            if (node != null) { node.invoked -= Invoke; hierarchy.RemoveNode(node); node = null; }
        }
    }
}
