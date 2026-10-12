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
        private UiMotion.Handle fade, scale;
        public RectTransform Body => body;
        public bool IsOpen { get; private set; } = true;
        public void Configure(CanvasGroup canvasGroup, RectTransform panel, RectTransform content, bool sheet, UnityEngine.UI.Button closeButton)
        { group = canvasGroup; card = panel; body = content; bottom = sheet; close = closeButton; Wire(); }
        private void OnEnable() => Wire();
        private void Wire() { if (close != null) { close.onClick.RemoveListener(Hide); close.onClick.AddListener(Hide); } }
        public void Show()
        {
            gameObject.SetActive(true); fade?.Cancel(); scale?.Cancel(); IsOpen = true;
            group.alpha = 0; group.blocksRaycasts = group.interactable = true;
            var motion = GetComponent<UiMotion>() ?? gameObject.AddComponent<UiMotion>();
            fade = motion.Fade(group, 1);
            card.localScale = Vector3.one * UiTokens.PressScale;
            scale = motion.Scale(card, Vector3.one);
            if (bottom)
            {
                body.anchoredPosition += Vector2.down * UiTokens.Space3;
                scale = motion.Slide(body, body.anchoredPosition + Vector2.up * UiTokens.Space3);
            }
        }
        public void Hide()
        {
            fade?.Cancel(); scale?.Cancel(); IsOpen = false; group.interactable = false;
            var motion = GetComponent<UiMotion>() ?? gameObject.AddComponent<UiMotion>();
            fade = motion.Fade(group, 0, () => { group.blocksRaycasts = false; gameObject.SetActive(false); });
        }
        public void OnPointerClick(PointerEventData eventData)
        {
            if (!RectTransformUtility.RectangleContainsScreenPoint(card, eventData.position, eventData.pressEventCamera)) Hide();
        }
        private void OnDisable() { fade?.Cancel(); scale?.Cancel(); }
    }
}
