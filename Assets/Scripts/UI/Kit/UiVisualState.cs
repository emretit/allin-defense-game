using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AllinDefense.UI.Kit
{
    public enum UiState { Normal, Pressed, Disabled, Selected, Ready, Unaffordable }
    public enum UiButtonVariant { Gold, Glass, Danger }

    public sealed class UiVisualState : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private Image surface, gradient, glow, ring, icon;
        [SerializeField] private Outline outline;
        [SerializeField] private TMP_Text label, price;
        [SerializeField] private UiButtonVariant variant;
        [SerializeField] private UiState state;
        [SerializeField] private Selectable selectable;
        [SerializeField] private bool circular;
        private UiState resting;
        private bool previewed;
        private UiMotion.Handle scaleMotion, readyMotion;
        public float SelectedScale { get; set; } = 1f;
        public bool PaintedIcon { get; set; }
        public bool GoldBorder { get; set; }
        public UiState State => state;
        public Image Surface => surface;
        public TMP_Text Price => price;
        public Image Icon => icon;
        public int ReadyPulseCount { get; private set; }

        public void Configure(Image background, Image sheen, Image shadow, Image readyRing, Outline border,
            UiButtonVariant style, TMP_Text text = null, Image glyph = null, TMP_Text cost = null)
        {
            surface = background; gradient = sheen; glow = shadow; ring = readyRing; outline = border;
            variant = style; label = text; icon = glyph; price = cost; selectable = GetComponent<Selectable>();
            SetState(UiState.Normal);
        }

        public void BindContent(TMP_Text text, Image glyph, TMP_Text cost = null)
        {
            label = text; icon = glyph; price = cost; SetState(state);
        }

        public void SetPreviewed(bool value)
        {
            if (previewed == value) return;
            previewed = value; SetState(state);
        }

        public void SetCircular(bool value) { circular = value; SetState(state); }

        public void SetState(UiState value)
        {
            bool enteringReady = value == UiState.Ready && state != UiState.Ready;
            state = value;
            if (value != UiState.Pressed) resting = value;
            bool disabled = value == UiState.Disabled || value == UiState.Unaffordable ||
                value == UiState.Pressed && resting == UiState.Unaffordable;
            bool gold = variant == UiButtonVariant.Gold && !disabled;
            bool selected = previewed || value == UiState.Selected || value == UiState.Ready;
            if (selectable == null) selectable = GetComponent<Selectable>();
            if (selectable != null) selectable.interactable = value != UiState.Disabled;
            if (surface != null) surface.color = disabled ? UiTokens.Surface :
                value == UiState.Pressed ? UiTokens.NightDeep : gold ? UiTokens.Gold : UiTokens.KitSurface;
            if (gradient != null)
            {
                gradient.gameObject.SetActive(!disabled);
                gradient.color = value == UiState.Pressed ? UiTokens.PressedTint : UiTokens.White;
            }
            if (label != null) label.color = disabled ? UiTokens.TextSecondary : gold ? UiTokens.GoldText :
                variant == UiButtonVariant.Danger ? UiTokens.Danger : UiTokens.TextPrimary;
            if (icon != null) icon.color = PaintedIcon ? value == UiState.Disabled ? UiTokens.TextSecondary : UiTokens.White : disabled ? UiTokens.TextSecondary : gold ? UiTokens.GoldText : UiTokens.Moon;
            if (price != null) price.color = disabled ? UiTokens.Danger : UiTokens.Gold;
            if (outline != null) outline.effectColor = selected ? UiTokens.Gold : GoldBorder ? UiTokens.GoldLow :
                variant == UiButtonVariant.Danger ? UiTokens.Danger : gold ? UiTokens.GoldHigh : UiTokens.HairlineStrong;
            if (glow != null) glow.color = selected ? UiTokens.GoldShadow : UiTokens.Shadow;
            if (ring != null) ring.gameObject.SetActive(value == UiState.Ready && circular);
            var motion = GetComponent<UiMotion>();
            if (motion == null) motion = gameObject.AddComponent<UiMotion>();
            scaleMotion?.Cancel();
            if (value != UiState.Ready)
            {
                readyMotion?.Cancel();
                if (ring != null) ring.transform.localScale = Vector3.one;
            }
            scaleMotion = motion.Scale(transform, Vector3.one * (value == UiState.Pressed ? UiTokens.PressScale : selected && !FrontendPreferences.ReducedMotion ? SelectedScale : 1f));
            if (enteringReady)
            {
                ReadyPulseCount = FrontendPreferences.ReducedMotion ? 0 : 2;
                if (ring != null) readyMotion = motion.Punch(circular ? ring.transform : glow.transform, 2);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && state != UiState.Disabled) SetState(UiState.Pressed);
        }
        public void OnPointerUp(PointerEventData eventData) { if (state == UiState.Pressed) SetState(resting); }
        public void OnPointerExit(PointerEventData eventData) { if (state == UiState.Pressed) SetState(resting); }
        private void MotionPreferenceChanged() { if (SelectedScale > 1 && previewed) SetState(state); }
        private void OnEnable() { FrontendPreferences.MotionChanged += MotionPreferenceChanged; if (surface != null) SetState(state); }
        private void OnDisable() { FrontendPreferences.MotionChanged -= MotionPreferenceChanged; scaleMotion?.Cancel(); readyMotion?.Cancel(); transform.localScale = Vector3.one; }
    }
}
