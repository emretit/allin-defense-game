using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AllinDefense.UI.Kit
{
    public sealed class UiRadialCooldown : MonoBehaviour
    {
        [SerializeField] private Image fill;
        [SerializeField] private TMP_Text seconds, ready;
        [SerializeField] private UiVisualState visual;
        [SerializeField] private float remaining;
        private bool returning;
        public float Remaining => remaining;
        public UiVisualState Visual => visual;
        public RectTransform ActionRect => (RectTransform)visual.transform;
        public RectTransform BadgeRect => (RectTransform)(IsReady ? ready.transform.parent : seconds.transform.parent);
        public float Fill => fill.fillAmount;
        public bool IsReady => Remaining <= 0f;
        public void Configure(Image ring, TMP_Text time, TMP_Text badge, UiVisualState state)
        { fill = ring; seconds = time; ready = badge; visual = state; SetCooldown(0, 1); }
        public void SetReturning(bool returning)
        {
            if (this.returning == returning) return;
            this.returning = returning;
            if (returning)
            {
                visual.SetState(UiState.Disabled);
                seconds.transform.parent.gameObject.SetActive(false);
                ready.transform.parent.gameObject.SetActive(false);
                fill.gameObject.SetActive(false);
            }
            else RefreshState();
        }
        private void RefreshState()
        {
            seconds.transform.parent.gameObject.SetActive(!IsReady); ready.transform.parent.gameObject.SetActive(IsReady);
            visual.SetState(IsReady ? UiState.Ready : UiState.Normal);
            fill.gameObject.SetActive(!IsReady);
        }
        // Gameplay owns the clock; paused games can still animate the surrounding UI.
        public void SetCooldown(float remaining, float duration)
        {
            this.remaining = Mathf.Max(0, remaining);
            fill.fillAmount = duration > 0 ? Mathf.Clamp01(Remaining / duration) : 0;
            seconds.text = FrontendCopy.Seconds.Format(Mathf.CeilToInt(Remaining));
            if (!returning) RefreshState();
        }
    }
}
