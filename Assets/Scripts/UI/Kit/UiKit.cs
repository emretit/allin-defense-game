using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AllinDefense.UI.Kit
{
    public sealed class UiKit
    {
        public UiDesignSystemAssets Assets { get; }
        public UiKit(UiDesignSystemAssets assets) => Assets = assets != null ? assets : throw new ArgumentNullException(nameof(assets));

        public static RectTransform Node(Transform parent, string id)
        {
            var root = new GameObject(id, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            UiId.Assign(root.transform, id);
            return (RectTransform)root.transform;
        }

        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        public static LayoutElement Size(RectTransform rect, float width = -1, float height = -1, float flex = 0)
        {
            var size = rect.GetComponent<LayoutElement>();
            if (size == null) size = rect.gameObject.AddComponent<LayoutElement>();
            size.preferredWidth = width; size.preferredHeight = height; size.flexibleWidth = flex;
            return size;
        }

        public static HorizontalOrVerticalLayoutGroup Layout(RectTransform rect, bool vertical, float padding = UiTokens.Space1)
        {
            HorizontalOrVerticalLayoutGroup layout = vertical ? rect.gameObject.AddComponent<VerticalLayoutGroup>() :
                rect.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset((int)padding, (int)padding, (int)padding, (int)padding);
            layout.spacing = UiTokens.Space1;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = vertical;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.MiddleCenter;
            return layout;
        }

        public TMP_Text Text(Transform parent, string id, LocalizedCopy copy, float size = UiTokens.TypeLabel, bool bold = false)
        {
            var rect = Node(parent, id);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = bold ? Assets.boldFont : Assets.regularFont;
            text.fontSize = size; text.color = UiTokens.TextPrimary;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            if (copy.IsLocalized) rect.gameObject.AddComponent<UiKitText>().Configure(text, copy);
            else text.text = copy.Value;
            Size(rect).minHeight = Mathf.Max(size, text.GetPreferredValues(copy.Value).y);
            return text;
        }

        public Image Icon(Transform parent, string id, Sprite sprite, float size = UiTokens.Space3)
        {
            var rect = Node(parent, id);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite; image.preserveAspect = true; image.color = UiTokens.Moon; image.raycastTarget = false;
            rect.gameObject.AddComponent<UiProportionalIcon>();
            Size(rect, size, size);
            return image;
        }

        public Image Decoration(Transform parent, string id, Sprite sprite, Color color)
        {
            var rect = Node(parent, id); Stretch(rect);
            Size(rect).ignoreLayout = true;
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite; image.type = sprite != null && sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            if (sprite == Assets.pill) rect.gameObject.AddComponent<UiPillShape>();
            image.color = color; image.raycastTarget = false;
            return image;
        }

        public static void SetHorizontalFill(Image image, float fraction)
        {
            fraction = Mathf.Clamp01(fraction);
            image.fillAmount = fraction;
            image.type = Image.Type.Sliced;
            image.rectTransform.anchorMin = Vector2.zero;
            image.rectTransform.anchorMax = new Vector2(fraction, 1);
            image.rectTransform.offsetMin = image.rectTransform.offsetMax = Vector2.zero;
            image.enabled = fraction > 0;
        }

        public RectTransform Surface(Transform parent, string id, float radius = UiTokens.RadiusCard, bool vertical = true)
        {
            var root = Node(parent, id);
            Paint(root, RadiusSprite(radius), UiButtonVariant.Glass);
            Layout(root, vertical, UiTokens.Space2);
            return root;
        }
        public RectTransform Card(Transform parent, string id) => Surface(parent, id);
        private Sprite RadiusSprite(float radius) => radius <= UiTokens.RadiusSmall ? Assets.roundedSmall :
            radius <= UiTokens.RadiusMedium ? Assets.roundedMedium : radius <= UiTokens.RadiusCard ? Assets.roundedCard : Assets.pill;

        private UiVisualState Paint(RectTransform root, Sprite shape, UiButtonVariant variant,
            TMP_Text label = null, Image icon = null, TMP_Text price = null)
        {
            var shadow = Decoration(root, root.name + ".Shadow", Assets.softShadow, UiTokens.Shadow);
            shadow.rectTransform.offsetMin = new Vector2(-UiTokens.Space1, -UiTokens.Space2);
            shadow.rectTransform.offsetMax = new Vector2(UiTokens.Space1, 0);
            var body = Decoration(root, root.name + ".Surface", shape, UiTokens.KitSurface);
            body.raycastTarget = true;
            var outline = body.gameObject.AddComponent<Outline>();
            outline.effectDistance = Vector2.one * UiTokens.HairlineWidth;
            outline.useGraphicAlpha = false;
            if (variant != UiButtonVariant.Gold) body.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            var gradient = Decoration(body.transform, root.name + ".Gradient",
                variant == UiButtonVariant.Gold ? Assets.goldGradient : Assets.verticalGradient, UiTokens.White);
            var ring = Decoration(root, root.name + ".ReadyRing", Assets.cooldownRing, UiTokens.Gold);
            ring.preserveAspect = true;
            var state = root.gameObject.AddComponent<UiVisualState>();
            state.Configure(body, gradient, shadow, ring, outline, variant, label, icon, price);
            return state;
        }

        public UnityEngine.UI.Button Button(Transform parent, string id, LocalizedCopy copy,
            UiButtonVariant variant = UiButtonVariant.Glass, Sprite icon = null, int? price = null, Action clicked = null, bool verticalContent = false)
        {
            var root = Node(parent, id);
            var button = root.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.transition = Selectable.Transition.None;
            root.gameObject.AddComponent<UiAccessibleButton>().Configure(copy);
            var pointSize = root.gameObject.AddComponent<UiPointSize>();
            pointSize.Configure(variant == UiButtonVariant.Gold ? UiMetrics.PrimaryCtaPt : UiMetrics.MinTouchPt);
            var content = Node(root, id + ".Content");
            var contentLayout = Layout(content, verticalContent, verticalContent ? UiTokens.Space1 / 4 : UiTokens.Space1);
            if (verticalContent) contentLayout.spacing = UiTokens.Space1 / 2;
            Image glyph = icon == null ? null : Icon(content, id + ".Icon", icon);
            TMP_Text label = Text(content, id + ".Label", copy, UiTokens.TypeCaption, true);
            if (string.IsNullOrEmpty(copy.Value)) label.gameObject.SetActive(false);
            TMP_Text cost = null;
            if (price.HasValue)
            {
                var chip = Chip(content, id + ".Price", price.Value.ToString(), verticalContent ? UiTokens.TypeCompact : UiTokens.TypeCaption);
                cost = chip.GetComponentInChildren<TMP_Text>(); cost.color = UiTokens.Gold;
            }
            var state = Paint(root, Assets.roundedMedium, variant, label, glyph, cost);
            content.SetAsLastSibling();
            Layout(root, false, 0);
            button.targetGraphic = state.Surface;
            if (clicked != null) button.onClick.AddListener(() => clicked());
            return button;
        }

        public UnityEngine.UI.Button IconButton(Transform parent, string id, Sprite icon, Action clicked = null, bool pill = false, bool verticalContent = false)
        {
            if (icon == null) throw new ArgumentNullException(nameof(icon));
            var button = Button(parent, id, "", UiButtonVariant.Glass, icon, null, clicked, verticalContent);
            var surface = button.GetComponent<UiVisualState>().Surface;
            surface.sprite = pill ? Assets.pill : Assets.filledCircle;
            surface.type = pill ? Image.Type.Sliced : Image.Type.Simple;
            if (pill) surface.gameObject.AddComponent<UiPillShape>();
            button.GetComponent<UiVisualState>().SetCircular(!pill);
            button.GetComponent<UiVisualState>().Icon.GetComponent<UiProportionalIcon>().Configure(button.GetComponent<UiPointSize>());
            Size((RectTransform)button.transform, UiMetrics.MinTouchPt, UiMetrics.MinTouchPt);
            return button;
        }

        public RectTransform Chip(Transform parent, string id, LocalizedCopy copy, float fontSize = UiTokens.TypeCaption)
        {
            var root = Node(parent, id);
            Decoration(root, id + ".Surface", Assets.pill, UiTokens.Surface);
            var layout = Layout(root, false);
            layout.padding.top = layout.padding.bottom = (int)(UiTokens.Space1 / 4);
            Text(root, id + ".Label", copy, fontSize);
            return root;
        }

        public UiStatPill StatPill(Transform parent, string id, Sprite icon, int value)
        {
            var root = Surface(parent, id, UiTokens.RadiusPill, false);
            var glyph = Icon(root, id + ".Icon", icon, UiTokens.Space4);
            var number = Text(root, id + ".Number", "", UiTokens.TypeBody, true);
            var delta = Text(root, id + ".Delta", "", UiTokens.TypeCaption, true);
            var control = root.gameObject.AddComponent<UiStatPill>();
            control.Configure(number, delta, value);
            root.GetComponent<UiVisualState>().BindContent(number, glyph);
            return control;
        }

        public UiSwitch Switch(Transform parent, string id, bool value, Action<bool> changed = null)
        {
            var button = Button(parent, id, FrontendCopy.SwitchOff, UiButtonVariant.Glass, Assets.iconClose);
            var control = button.gameObject.AddComponent<UiSwitch>();
            control.Configure(button, button.GetComponentInChildren<TMP_Text>(), Assets, value);
            button.GetComponent<UiAccessibleButton>().Configure(FrontendCopy.AccessibleSwitch);
            if (changed != null) control.Changed += changed;
            return control;
        }

        public UnityEngine.UI.Slider Slider(Transform parent, string id, Sprite icon, float value, Action<float> changed = null)
        {
            var root = Surface(parent, id, UiTokens.RadiusMedium, false);
            root.gameObject.AddComponent<UiPointSize>().Configure(UiMetrics.MinTouchPt);
            var glyph = Icon(root, id + ".Icon", icon);
            var track = Node(root, id + ".Track");
            Size(track, UiMetrics.AbilityPt * 2, UiTokens.Space1, 1);
            var background = track.gameObject.AddComponent<Image>(); background.sprite = Assets.pill; background.type = Image.Type.Sliced;
            track.gameObject.AddComponent<UiPillShape>();
            background.color = UiTokens.HairlineStrong;
            var fill = Decoration(track, id + ".Fill", Assets.pill, UiTokens.Gold);
            var thumb = Decoration(track, id + ".Thumb", Assets.filledCircle, UiTokens.GoldHigh);
            thumb.rectTransform.sizeDelta = Vector2.one * UiTokens.Space3;
            thumb.raycastTarget = true;
            var slider = root.gameObject.AddComponent<UnityEngine.UI.Slider>();
            slider.fillRect = fill.rectTransform; slider.handleRect = thumb.rectTransform;
            slider.targetGraphic = thumb; slider.transition = Selectable.Transition.None;
            slider.value = Mathf.Clamp01(value);
            root.gameObject.AddComponent<UiAccessibleButton>().Configure(FrontendCopy.AccessibleSlider);
            root.GetComponent<UiVisualState>().BindContent(null, glyph);
            if (changed != null) slider.onValueChanged.AddListener(v => changed(v));
            return slider;
        }

        public UiSegmentedControl SegmentedControl(Transform parent, string id, LocalizedCopy[] labels, int selected = 0)
        {
            if (labels == null || labels.Length == 0) throw new ArgumentException("At least one segment is required.", nameof(labels));
            var root = Surface(parent, id, UiTokens.RadiusMedium, false);
            var buttons = new UnityEngine.UI.Button[labels.Length];
            for (int i = 0; i < labels.Length; i++) buttons[i] = Button(root, id + "." + i, labels[i]);
            var control = root.gameObject.AddComponent<UiSegmentedControl>(); control.Configure(buttons, selected);
            root.gameObject.AddComponent<UiAccessibleButton>().Configure(FrontendCopy.AccessibleOptions);
            return control;
        }

        public UiProgressSegments ProgressSegments(Transform parent, string id, int count)
        {
            if (count < 1) throw new ArgumentOutOfRangeException(nameof(count));
            var root = Node(parent, id); Layout(root, false, 0);
            var fills = new Image[count];
            for (int i = 0; i < count; i++)
            {
                var track = Node(root, id + "." + i); Size(track, UiTokens.Space4, UiTokens.Space1, 1);
                var bg = track.gameObject.AddComponent<Image>(); bg.sprite = Assets.pill; bg.type = Image.Type.Sliced; bg.color = UiTokens.HairlineStrong; bg.raycastTarget = false;
                track.gameObject.AddComponent<UiPillShape>();
                fills[i] = Decoration(track, id + ".Fill." + i, Assets.pill, UiTokens.Gold);
                fills[i].type = Image.Type.Sliced;
            }
            var control = root.gameObject.AddComponent<UiProgressSegments>(); control.Configure(fills); return control;
        }

        public UiRadialCooldown RadialCooldown(Transform parent, string id, Sprite icon)
        {
            var root = Node(parent, id); Layout(root, true, 0);
            root.GetComponent<VerticalLayoutGroup>().childForceExpandWidth = false;
            var timeBadge = Chip(root, id + ".TimeBadge", "");
            var readyBadge = Chip(root, id + ".ReadyBadge", FrontendCopy.Ready);
            foreach (RectTransform badge in new[] { timeBadge, readyBadge })
            {
                badge.GetComponentInChildren<Image>().color = UiTokens.Gold;
                badge.GetComponentInChildren<TMP_Text>().color = UiTokens.GoldText;
            }
            var button = IconButton(root, id + ".Action", icon);
            button.GetComponent<UiPointSize>().Configure(UiMetrics.AbilityPt);
            var actionRect = (RectTransform)button.transform;
            Size(actionRect, UiMetrics.AbilityPt, UiMetrics.AbilityPt);
            button.GetComponent<UiVisualState>().Icon.GetComponent<UiProportionalIcon>().Configure(button.GetComponent<UiPointSize>());
            var fill = Decoration(actionRect, id + ".Cooldown", Assets.cooldownRing, UiTokens.Gold);
            fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Radial360; fill.fillOrigin = (int)Image.Origin360.Top; fill.fillClockwise = true;
            var control = root.gameObject.AddComponent<UiRadialCooldown>();
            control.Configure(fill, timeBadge.GetComponentInChildren<TMP_Text>(), readyBadge.GetComponentInChildren<TMP_Text>(),
                button.GetComponent<UiVisualState>()); return control;
        }

        public UiStarRow StarRow(Transform parent, string id, int earned)
        {
            var root = Node(parent, id); Layout(root, false, 0);
            var stars = new Image[3];
            for (int i = 0; i < stars.Length; i++) stars[i] = Icon(root, id + "." + i, Assets.iconStarEmpty, UiMetrics.MinTouchPt);
            var control = root.gameObject.AddComponent<UiStarRow>(); control.Configure(stars, Assets); control.SetStars(earned); return control;
        }

        public UiOverlay Modal(Transform parent, string id, LocalizedCopy title) => Overlay(parent, id, title, false);
        public UiOverlay BottomSheet(Transform parent, string id, LocalizedCopy title) => Overlay(parent, id, title, true);
        private UiOverlay Overlay(Transform parent, string id, LocalizedCopy title, bool bottom)
        {
            var root = Node(parent, id); Stretch(root);
            if (!bottom) root.gameObject.AddComponent<UiAccessibleButton>().ConfigureModalScope();
            var scrim = root.gameObject.AddComponent<Image>(); scrim.color = UiTokens.SettingsScrim;
            var group = root.gameObject.AddComponent<CanvasGroup>();
            var layout = root.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = bottom ? TextAnchor.LowerCenter : TextAnchor.MiddleCenter;
            layout.padding = new RectOffset((int)UiTokens.Space1, (int)UiTokens.Space1, (int)UiTokens.Space1, (int)UiTokens.Space1);
            layout.childControlWidth = layout.childControlHeight = true; layout.childForceExpandWidth = bottom; layout.childForceExpandHeight = false;
            var card = Card(root, id + ".Card"); Size(card, UiMetrics.AbilityPt * 5);
            Text(card, id + ".Title", title, UiTokens.TypeSubheading, true);
            var body = Node(card, id + ".Body"); Layout(body, true);
            var close = Button(card, id + ".Close", FrontendCopy.Cancel, UiButtonVariant.Glass, Assets.iconClose);
            var overlay = root.gameObject.AddComponent<UiOverlay>(); overlay.Configure(group, card, body, bottom, close);
            return overlay;
        }

        public UiRadialMenu RadialMenu(Transform parent, string id, UiRadialOption[] options)
        {
            if (options == null || options.Length == 0) throw new ArgumentException("At least one option is required.", nameof(options));
            var root = Node(parent, id); Stretch(root);
            var dismiss = root.gameObject.AddComponent<Image>(); dismiss.color = UiTokens.Clear; dismiss.raycastTarget = true;
            var layout = root.gameObject.AddComponent<UiRadialLayout>();
            var buttons = new UnityEngine.UI.Button[options.Length];
            for (int i = 0; i < options.Length; i++)
            {
                buttons[i] = Button(root, id + "." + options[i].Id, "", UiButtonVariant.Glass, options[i].Icon, options[i].Price, verticalContent: true);
                buttons[i].GetComponent<UiVisualState>().Surface.sprite = Assets.roundedCard;
                buttons[i].GetComponent<UiPointSize>().Configure(UiMetrics.AbilityPt);
                buttons[i].GetComponent<UiVisualState>().Icon.GetComponent<UiProportionalIcon>().Configure(buttons[i].GetComponent<UiPointSize>());
            }
            var menu = root.gameObject.AddComponent<UiRadialMenu>(); menu.Configure(layout, buttons, options); return menu;
        }
    }
}
