using System;
using UnityEngine;
using UnityEngine.UI;

namespace AllinDefense.UI.Kit
{
    public sealed class UiSegmentedControl : MonoBehaviour
    {
        [SerializeField] private UnityEngine.UI.Button[] buttons;
        [SerializeField] private int selected;
        private bool wired;
        public int Selected => selected;
        public int Count => buttons?.Length ?? 0;
        public Button SelectedButton => buttons != null && selected >= 0 && selected < buttons.Length ? buttons[selected] : null;
        public event Action<int> Changed;
        public void Configure(UnityEngine.UI.Button[] targets, int initial) { buttons = targets; Wire(); Select(initial, false); }
        private void OnEnable() => Wire();
        private void Wire()
        {
            if (wired || buttons == null) return;
            for (int i = 0; i < buttons.Length; i++) { int index = i; buttons[i].onClick.AddListener(() => Select(index)); }
            wired = true;
        }
        public void Select(int index, bool notify = true)
        {
            if (buttons == null || index < 0 || index >= buttons.Length || !buttons[index].interactable) return;
            bool changed = selected != index; selected = index;
            for (int i = 0; i < buttons.Length; i++)
                if (buttons[i].interactable) buttons[i].GetComponent<UiVisualState>().SetState(i == index ? UiState.Selected : UiState.Normal);
            if (changed && notify) Changed?.Invoke(index);
        }
    }
}
