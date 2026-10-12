using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AllinDefense.UI.Kit
{
    [Serializable]
    public struct UiRadialOption
    {
        public string Id;
        public Sprite Icon;
        public int Price;
        public UiRadialOption(string id, Sprite icon, int price) { Id = id; Icon = icon; Price = price; }
    }

    public sealed class UiRadialMenu : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private UiRadialLayout layout;
        [SerializeField] private UnityEngine.UI.Button[] buttons;
        [SerializeField] private UiRadialOption[] options;
        private bool wired;
        public int PreviewIndex { get; private set; } = -1;
        public bool IsOpen => gameObject.activeSelf;
        public event Action<UiRadialOption> Previewed;
        public event Action<UiRadialOption> Confirmed;
        public event Action Closed;
        public void Configure(UiRadialLayout positions, UnityEngine.UI.Button[] choices, UiRadialOption[] items)
        { layout = positions; buttons = choices; options = items; Wire(); }
        private void OnEnable() => Wire();
        private void Wire()
        {
            if (wired || buttons == null) return;
            for (int i = 0; i < buttons.Length; i++) { int index = i; buttons[i].onClick.AddListener(() => Choose(index)); }
            wired = true;
        }
        public void Open(Vector2 screenPoint, Camera uiCamera = null)
        {
            var rect = (RectTransform)transform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screenPoint, uiCamera, out Vector2 local);
            Rect safe = Screen.safeArea;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, safe.min, uiCamera, out Vector2 min);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, safe.max, uiCamera, out Vector2 max);
            OpenLocal(local, Rect.MinMaxRect(min.x, min.y, max.x, max.y));
        }
        public void OpenWorld(Vector3 worldPoint, Camera worldCamera, Camera uiCamera = null) => Open(worldCamera.WorldToScreenPoint(worldPoint), uiCamera);
        public void OpenLocal(Vector2 localPoint, Rect localSafeArea)
        {
            gameObject.SetActive(true); PreviewIndex = -1;
            for (int i = 0; i < buttons.Length; i++) if (buttons[i].interactable) buttons[i].GetComponent<UiVisualState>().SetState(UiState.Normal);
            layout.Place(localPoint, localSafeArea);
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform);
        }
        public void SetAvailable(int index, bool available)
        {
            if (index < 0 || index >= buttons.Length) throw new ArgumentOutOfRangeException(nameof(index));
            buttons[index].GetComponent<UiVisualState>().SetState(available ? UiState.Normal : UiState.Disabled);
            if (!available && PreviewIndex == index) PreviewIndex = -1;
        }
        public void Choose(int index)
        {
            if (!IsOpen || index < 0 || index >= buttons.Length || !buttons[index].interactable) return;
            if (PreviewIndex == index) { Confirmed?.Invoke(options[index]); Close(); return; }
            PreviewIndex = index;
            for (int i = 0; i < buttons.Length; i++) if (buttons[i].interactable)
                buttons[i].GetComponent<UiVisualState>().SetState(i == index ? UiState.Selected : UiState.Normal);
            Previewed?.Invoke(options[index]);
        }
        public void Close() { PreviewIndex = -1; gameObject.SetActive(false); Closed?.Invoke(); }
        public void OnPointerClick(PointerEventData eventData)
        {
            foreach (var button in buttons)
                if (RectTransformUtility.RectangleContainsScreenPoint((RectTransform)button.transform, eventData.position, eventData.pressEventCamera)) return;
            Close();
        }
    }
}
