using AllinDefense.UI;
using AllinDefense.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AllinDefense.Presentation
{
    public sealed class DefenderRewardPopupPool : MonoBehaviour
    {
        public const int Capacity = 24;
        public const float Lifetime = .8f;
        private sealed class Popup
        {
            public RectTransform rect;
            public CanvasGroup group;
            public TMP_Text text;
            public Vector3 anchor;
            public float elapsed;
            public float size;
            public uint order;
            public bool active;
            public bool damage;
            public Image icon;
        }

#if UNITY_EDITOR
        // TMP's char-array overload creates an Inspector string in the Editor.
        private static readonly string[] editorRewards = CreateEditorRewards();
        private static string[] CreateEditorRewards()
        {
            var rewards = new string[512];
            for (int i = 0; i < rewards.Length; i++) rewards[i] = "+" + i;
            return rewards;
        }
#endif
        private static readonly string[] damageAmounts = CreateDamageAmounts();
        private static string[] CreateDamageAmounts()
        {
            var values = new string[128];
            for (int i = 0; i < values.Length; i++) values[i] = "-" + i;
            return values;
        }
        private readonly Popup[] pool = new Popup[Capacity];
        private readonly char[] digits = new char[11];
        private Canvas canvas;
        private Camera worldCamera;
        private GameSessionController owner;
        private DefenderKeepPlate keepPlate;
        private Material textMaterial;
        private uint sequence;
        private bool reducedMotion;
        public int ActiveCount { get; private set; }
        public int ActiveDamageCount { get; private set; }

        public void Initialize(DefenderPresentationCatalog catalog, Camera camera)
        {
            if (canvas != null) return;
            worldCamera = camera;
            owner = GetComponentInParent<GameSessionController>();
            reducedMotion = FrontendPreferences.ReducedMotion;
            FrontendPreferences.MotionChanged += OnMotionChanged;
            var root = new GameObject("Reward Popups", typeof(RectTransform), typeof(Canvas));
            root.transform.SetParent(transform, false);
            canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5;
            TMP_FontAsset font = catalog.bodyFont != null ? catalog.bodyFont : TMP_Settings.defaultFontAsset;
            textMaterial = new Material(font.material);
            textMaterial.EnableKeyword("OUTLINE_ON");
            textMaterial.SetColor(ShaderUtilities.ID_OutlineColor, UiTokens.GoldText);
            textMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, .22f);
            for (int i = 0; i < Capacity; i++)
            {
                var go = new GameObject("Reward Popup", typeof(RectTransform), typeof(CanvasGroup));
                go.transform.SetParent(root.transform, false);
                var popup = new Popup { rect = (RectTransform)go.transform, group = go.GetComponent<CanvasGroup>() };
                popup.rect.anchorMin = popup.rect.anchorMax = Vector2.zero;
                popup.rect.sizeDelta = new Vector2(160, 40);
                popup.group.blocksRaycasts = popup.group.interactable = false;
                var icon = new GameObject("Gold Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                icon.transform.SetParent(go.transform, false);
                var image = icon.GetComponent<Image>();
                popup.icon = image;
                image.sprite = catalog.goldIcon;
                image.preserveAspect = true;
                image.raycastTarget = false;
                var iconRect = (RectTransform)icon.transform;
                iconRect.anchorMin = iconRect.anchorMax = new Vector2(0, .5f);
                iconRect.anchoredPosition = new Vector2(12, 0);
                iconRect.sizeDelta = new Vector2(22, 22);
                var label = new GameObject("Reward Amount", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                label.transform.SetParent(go.transform, false);
                popup.text = label.GetComponent<TextMeshProUGUI>();
                popup.text.font = font;
                popup.text.fontSharedMaterial = textMaterial;
                popup.text.fontSize = 24;
                popup.text.fontStyle = FontStyles.Bold;
                popup.text.color = UiTokens.Gold;
                popup.text.alignment = TextAlignmentOptions.MidlineLeft;
                popup.text.raycastTarget = false;
                popup.text.textWrappingMode = TextWrappingModes.NoWrap;
                popup.text.rectTransform.anchorMin = Vector2.zero;
                popup.text.rectTransform.anchorMax = Vector2.one;
                popup.text.rectTransform.offsetMin = new Vector2(28, 0);
                popup.text.rectTransform.offsetMax = Vector2.zero;
                popup.text.SetText("+-0123456789");
                popup.text.ForceMeshUpdate();
                popup.text.SetText("+0");
                go.SetActive(false);
                pool[i] = popup;
            }
        }

        public void Play(PresentationSignal signal)
        {
            bool damage = signal.cue == PresentationCue.UiError && signal.value > 0;
            if (canvas == null || (!damage && signal.cue != PresentationCue.EnemyDefeated) || signal.value <= 0) return;
            Popup selected = pool[0];
            for (int i = 0; i < Capacity; i++)
            {
                if (!pool[i].active) { selected = pool[i]; break; }
                if (pool[i].order < selected.order) selected = pool[i];
            }
            if (selected.active && selected.damage) ActiveDamageCount--;
            if (!selected.active) ActiveCount++;
            selected.damage = damage;
            if (damage) ActiveDamageCount++;
            selected.icon.enabled = !damage;
            selected.text.rectTransform.offsetMin = new Vector2(damage ? 0 : 28, 0);
            if (damage && keepPlate == null && owner != null) keepPlate = owner.GetComponentInChildren<DefenderKeepPlate>();
            selected.text.color = damage ? UiTokens.Danger : UiTokens.Gold;
            selected.active = true;
            selected.order = ++sequence;
            selected.elapsed = 0;
            bool boss = signal.actor == PresentationActorKind.Boss;
            selected.anchor = signal.position + Vector3.up * (damage ? 4.6f : boss ? 2.6f : 1.65f);
            selected.size = boss ? 1.25f : 1f;
            selected.group.alpha = 1;
            int length = 1;
            int number = signal.value;
            do { digits[length++] = (char)('0' + number % 10); number /= 10; } while (number > 0);
            digits[0] = damage ? '-' : '+';
            for (int left = 1, right = length - 1; left < right; left++, right--)
                (digits[left], digits[right]) = (digits[right], digits[left]);
            if (damage && signal.value < damageAmounts.Length) selected.text.SetText(damageAmounts[signal.value]);
            else
            {
#if UNITY_EDITOR
            if (signal.value < editorRewards.Length) selected.text.SetText(editorRewards[signal.value]);
            else selected.text.SetCharArray(digits, 0, length);
#else
            selected.text.SetCharArray(digits, 0, length);
#endif
            }
            selected.rect.sizeDelta = new Vector2((damage ? 0 : 28f) + length * 15f, 40f);
            selected.rect.gameObject.SetActive(true);
            Place(selected);
        }

        private void LateUpdate()
        {
            if (canvas == null || ActiveCount == 0) return;
            for (int i = 0; i < Capacity; i++)
            {
                Popup popup = pool[i];
                if (!popup.active) continue;
                if (Time.timeScale > 0) popup.elapsed += Time.deltaTime;
                if (popup.elapsed >= Lifetime)
                {
                    popup.active = false;
                    ActiveCount--;
                    if (popup.damage) ActiveDamageCount--;
                    popup.rect.gameObject.SetActive(false);
                    continue;
                }
                popup.group.alpha = 1f - Mathf.Clamp01((popup.elapsed - .45f) / (Lifetime - .45f));
                Place(popup);
            }
        }

        private void Place(Popup popup)
        {
            if (worldCamera == null) return;
            Vector3 screen = worldCamera.WorldToScreenPoint(popup.anchor);
            Vector2 logical = UiMetrics.LogicalScreenSize;
            float pixelsPerPoint = Screen.height / Mathf.Max(1f, logical.y);
            popup.rect.localScale = Vector3.one * (pixelsPerPoint * popup.size);
            if (popup.damage)
            {
                if (keepPlate != null)
                {
                    Vector2 anchor = keepPlate.ScreenAnchor();
                    screen.x = anchor.x;
                    screen.y = anchor.y + 44 * pixelsPerPoint;
                }
                Rect safe = Screen.safeArea;
                screen.x = Mathf.Clamp(screen.x, safe.xMin + 40 * pixelsPerPoint, safe.xMax - 80 * pixelsPerPoint);
                screen.y = Mathf.Clamp(screen.y, safe.yMin + 40 * pixelsPerPoint, safe.yMax - 76 * pixelsPerPoint);
            }
            popup.rect.anchoredPosition = new Vector2(screen.x, screen.y + (reducedMotion ? 0 : 26f * pixelsPerPoint * popup.elapsed / Lifetime));
            popup.group.alpha = screen.z > 0 ? popup.group.alpha : 0;
        }

        private void OnMotionChanged() => reducedMotion = FrontendPreferences.ReducedMotion;
        private void OnDestroy()
        {
            FrontendPreferences.MotionChanged -= OnMotionChanged;
            if (textMaterial != null) Destroy(textMaterial);
        }
    }
}
