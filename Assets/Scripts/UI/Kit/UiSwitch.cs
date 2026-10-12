using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AllinDefense.UI.Kit
{
    public sealed class UiSwitch : MonoBehaviour
    {
        [SerializeField] private UnityEngine.UI.Button button;
        [SerializeField] private TMP_Text label;
        [SerializeField] private UiDesignSystemAssets assets;
        [SerializeField] private bool value;
        public bool Value => value;
        public event Action<bool> Changed;
        public void Configure(UnityEngine.UI.Button target, TMP_Text text, UiDesignSystemAssets set, bool initial)
        {
            button = target; label = text; assets = set; Wire(); SetValue(initial, false);
        }
        private void Wire() { if (button != null) { button.onClick.RemoveListener(Toggle); button.onClick.AddListener(Toggle); } }
        private void OnEnable() { Wire(); GameLocalization.LanguageChanged += Refresh; Refresh(); }
        private void OnDisable() => GameLocalization.LanguageChanged -= Refresh;
        private void Toggle() => SetValue(!value);
        public void SetValue(bool next, bool notify = true)
        {
            bool changed = value != next; value = next; Refresh(); if (changed && notify) Changed?.Invoke(value);
        }
        private void Refresh()
        {
            if (label == null) return;
            label.GetComponent<UiKitText>().Configure(label, value ? FrontendCopy.SwitchOn : FrontendCopy.SwitchOff);
            var state = GetComponent<UiVisualState>();
            state.Icon.sprite = value ? assets.iconCheck : assets.iconClose;
            if (state.State != UiState.Disabled) state.SetState(value ? UiState.Selected : UiState.Normal);
        }
    }
}
