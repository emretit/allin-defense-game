using System;
using TMPro;
using UnityEngine;

namespace AllinDefense.UI.Kit
{
    public sealed class UiStatPill : MonoBehaviour
    {
        [SerializeField] private TMP_Text number, delta;
        [SerializeField] private int value;
        private UiMotion.Handle count;
        private float deltaUntil;
        public string DeltaText => delta.text;
        private void Update() { if (deltaUntil > 0 && Time.unscaledTime >= deltaUntil) { delta.text = ""; deltaUntil = 0; } }
        public int Value => value;
        public event Action<int> Changed;
        public void Configure(TMP_Text text, TMP_Text change, int initial)
        {
            number = text; delta = change; value = initial;
            Render(initial); delta.text = "";
        }
        private void Render(int amount) => number.text = "<mspace=0.65em>" + amount + "</mspace>";
        public void SetValue(int next, bool animate = true, bool showDelta = true)
        {
            if (next == value) return;
            int from = value, difference = next - from; value = next;
            delta.text = !showDelta || difference == 0 ? "" : (difference > 0 ? "+" : "") + difference;
            deltaUntil = Time.unscaledTime + .4f;
            delta.color = difference >= 0 ? UiTokens.Success : UiTokens.Danger;
            count?.Cancel();
            if (animate && isActiveAndEnabled) count = Motion().Count(from, next, Render); else Render(next);
            Changed?.Invoke(difference);
        }
        private UiMotion Motion() => GetComponent<UiMotion>() ?? gameObject.AddComponent<UiMotion>();
        private void OnDisable() { count?.Cancel(); if (number != null) Render(value); }
    }
}
