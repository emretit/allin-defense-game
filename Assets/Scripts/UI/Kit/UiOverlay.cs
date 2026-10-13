using UnityEngine;
using UnityEngine.EventSystems;

namespace AllinDefense.UI.Kit
{
    public sealed class UiOverlay : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private RectTransform card, body;
        [SerializeField] private bool bottom;
        [SerializeField] private UnityEngine.UI.Button close;
        [SerializeField] private bool dismissOnScrim = true;
        public bool DismissOnScrim { get => dismissOnScrim; set => dismissOnScrim = value; }
        private UiMotion.Handle fade;
        public RectTransform Body => body;
        public bool IsOpen { get; private set; } = true;
        public void Configure(CanvasGroup canvasGroup, RectTransform panel, RectTransform content, bool sheet, UnityEngine.UI.Button closeButton)
        { group = canvasGroup; card = panel; body = content; bottom = sheet; close = closeButton; Wire(); }
        private void OnEnable() => Wire();
        private void Wire() { if (close != null) { close.onClick.RemoveListener(Hide); close.onClick.AddListener(Hide); } }
        public void Show()
        {
            gameObject.SetActive(true); fade?.Cancel(); IsOpen = true;
            UiSounds.Click();
            group.blocksRaycasts = true; group.interactable = false;
            var motion = GetComponent<UiMotion>() ?? gameObject.AddComponent<UiMotion>();
            fade = motion.Animate(t =>
            {
                group.alpha = t;
                card.localScale = Vector3.one * Mathf.Lerp(UiTokens.PressScale, 1, t);
            }, UiTokens.FadeDuration, () => group.interactable = true);
        }
        public void Hide() => Hide(null);
        public void Hide(System.Action completed)
        {
            if (!IsOpen) return;
            fade?.Cancel(); IsOpen = false; group.interactable = false;
            UiSounds.Click();
            float from = group.alpha;
            var motion = GetComponent<UiMotion>() ?? gameObject.AddComponent<UiMotion>();
            fade = motion.Animate(t =>
            {
                group.alpha = from * (1 - t);
                card.localScale = Vector3.one * Mathf.Lerp(1, UiTokens.PressScale, t);
            }, UiTokens.FadeDuration, () => { group.blocksRaycasts = false; gameObject.SetActive(false); completed?.Invoke(); });
        }
        public void OnPointerClick(PointerEventData eventData)
        {
            if (dismissOnScrim && !RectTransformUtility.RectangleContainsScreenPoint(card, eventData.position, eventData.pressEventCamera)) Hide();
        }
        private void OnDisable() { fade?.Cancel(); }
    }
}
