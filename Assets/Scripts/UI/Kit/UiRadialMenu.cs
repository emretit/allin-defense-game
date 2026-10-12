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
        private UnityEngine.Events.UnityAction[] listeners;
        private bool[] affordable;
        public bool CloseOnConfirm { get; set; } = true;
        public int PreviewIndex { get; private set; } = -1;
        public bool IsOpen => gameObject.activeSelf;
        public event Action<UiRadialOption> Previewed;
        public event Action<UiRadialOption> Confirmed;
        public event Action Closed;
        public event Action<UiRadialOption> Rejected;
        public void Configure(UiRadialLayout positions, UnityEngine.UI.Button[] choices, UiRadialOption[] items)
        {
            Unwire();
            layout = positions; buttons = choices; options = items;
            affordable = new bool[items.Length];
            for (int i = 0; i < affordable.Length; i++)
            {
                affordable[i] = true;
                buttons[i].GetComponent<UiVisualState>().SetState(UiState.Normal);
            }
            PreviewIndex = -1;
            Wire();
        }
        private void OnEnable() => Wire();
        private void OnDestroy() => Unwire();
        private void Unwire()
        {
            if (listeners == null || buttons == null) return;
            for (int i = 0; i < listeners.Length; i++)
                if (buttons[i] != null) buttons[i].onClick.RemoveListener(listeners[i]);
            listeners = null;
        }
        private void Wire()
        {
            if (listeners != null || buttons == null) return;
            if (affordable == null)
            {
                affordable = new bool[buttons.Length];
                for (int i = 0; i < affordable.Length; i++) affordable[i] = true;
            }
            listeners = new UnityEngine.Events.UnityAction[buttons.Length];
            for (int i = 0; i < buttons.Length; i++)
            {
                int index = i;
                listeners[i] = () => Choose(index);
                buttons[i].onClick.AddListener(listeners[i]);
            }
        }
        public void SetAffordable(int index, bool value)
        {
            if (affordable[index] == value) return;
            affordable[index] = value;
            if (buttons[index].interactable)
                buttons[index].GetComponent<UiVisualState>().SetState(value
                    ? PreviewIndex == index ? UiState.Selected : UiState.Normal : UiState.Unaffordable);
        }
        public void ResetPreview() => PreviewIndex = -1;
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
            for (int i = 0; i < buttons.Length; i++) if (buttons[i].interactable) buttons[i].GetComponent<UiVisualState>().SetState(affordable[i] ? UiState.Normal : UiState.Unaffordable);
            layout.Place(localPoint, localSafeArea);
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform);
        }
        public void SetAvailable(int index, bool available)
        {
            if (index < 0 || index >= buttons.Length) throw new ArgumentOutOfRangeException(nameof(index));
            if (buttons[index].interactable == available) return;
            buttons[index].GetComponent<UiVisualState>().SetState(available ?
                affordable[index] ? PreviewIndex == index ? UiState.Selected : UiState.Normal : UiState.Unaffordable : UiState.Disabled);
            if (!available && PreviewIndex == index) PreviewIndex = -1;
        }
        public void Choose(int index)
        {
            if (!IsOpen || index < 0 || index >= buttons.Length || !buttons[index].interactable) return;
            if (PreviewIndex == index)
            {
                if (affordable != null && !affordable[index]) { Rejected?.Invoke(options[index]); return; }
                var choice = options[index];
                Confirmed?.Invoke(choice);
                if (CloseOnConfirm) Close();
                return;
            }
            PreviewIndex = index;
            for (int i = 0; i < buttons.Length; i++) if (buttons[i].interactable)
                buttons[i].GetComponent<UiVisualState>().SetState(affordable != null && !affordable[i] ? UiState.Unaffordable : i == index ? UiState.Selected : UiState.Normal);
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
