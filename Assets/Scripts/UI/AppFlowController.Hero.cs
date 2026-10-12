using AllinDefense.Core;
using TMPro;
using UnityEngine;

namespace AllinDefense.UI
{
    public sealed partial class AppFlowController
    {
        private GameObject heroNameModal;
        private RectTransform heroNameCard;
        private TMP_InputField heroNameInput;
        private TextMeshProUGUI heroNameStatus;
        private string heroNameProfileKey;

        private void BuildHeroNameModal()
        {
            heroNameModal = new GameObject("Hero Name Modal", typeof(RectTransform));
            heroNameModal.transform.SetParent(canvas.transform, false);
            Stretch(heroNameModal.GetComponent<RectTransform>());
            var scrim = CreateImage("Hero Name Scrim", heroNameModal.transform, UiTokens.HeroNameScrim);
            Stretch(scrim.GetComponent<RectTransform>());
            var scrimImage = scrim.GetComponent<UnityEngine.UI.Image>();
            scrimImage.raycastTarget = true;
            var dismiss = scrim.AddComponent<UnityEngine.UI.Button>();
            dismiss.targetGraphic = scrimImage;
            dismiss.transition = UnityEngine.UI.Selectable.Transition.None;
            dismiss.onClick.AddListener(CloseHeroName);

            var card = CreatePanel("Hero Name Card", heroNameModal.transform, theme.panel);
            heroNameCard = card.GetComponent<RectTransform>();
            SetRect(heroNameCard, Vector2.zero, new Vector2(900, 720), Center(), Center());
            AddCardAccent(card.transform, theme.commandGold);
            var title = CreateText("Hero Name Title", card.transform, FrontendCopy.MyHero, 46, catalog.titleFont,
                FontStyles.Bold, TextAlignmentOptions.Center, theme.textPrimary);
            SetRect(title.rectTransform, new Vector2(0, 250), new Vector2(760, 64), Center(), Center());
            var description = CreateText("Hero Name Description", card.transform,
                FrontendCopy.HeroNameDescription, 28, catalog.bodyFont,
                FontStyles.Normal, TextAlignmentOptions.Center, theme.textPrimary);
            SetRect(description.rectTransform, new Vector2(0, 162), new Vector2(760, 80), Center(), Center());

            var field = CreateImage("Hero Name Input", card.transform, theme.night);
            SetRect(field.GetComponent<RectTransform>(), new Vector2(0, 42), new Vector2(760, 132), Center(), Center());
            var background = field.GetComponent<UnityEngine.UI.Image>();
            background.raycastTarget = true;
            var viewport = new GameObject("Text Area", typeof(RectTransform), typeof(UnityEngine.UI.RectMask2D));
            viewport.transform.SetParent(field.transform, false);
            var viewportRect = viewport.GetComponent<RectTransform>();
            Stretch(viewportRect, new Vector2(24, 12), new Vector2(-24, -12));
            var inputText = CreateText("Name Text", viewport.transform, "", 38, catalog.bodyFont,
                FontStyles.Normal, TextAlignmentOptions.MidlineLeft, theme.textPrimary);
            Stretch(inputText.rectTransform);
            inputText.richText = false;
            inputText.enableWordWrapping = false;
            var placeholder = CreateText("Name Placeholder", viewport.transform, FrontendCopy.Warrior, 38, catalog.bodyFont,
                FontStyles.Normal, TextAlignmentOptions.MidlineLeft, theme.textMuted);
            Stretch(placeholder.rectTransform);
            heroNameInput = field.AddComponent<TMP_InputField>();
            heroNameInput.targetGraphic = background;
            heroNameInput.textViewport = viewportRect;
            heroNameInput.textComponent = inputText;
            heroNameInput.placeholder = placeholder;
            heroNameInput.lineType = TMP_InputField.LineType.SingleLine;
            heroNameInput.contentType = TMP_InputField.ContentType.Standard;
            heroNameInput.characterLimit = HeroIdentity.MaxNameLength * 2;
            heroNameInput.richText = false;
            heroNameInput.shouldHideMobileInput = true;
            heroNameInput.onValueChanged.AddListener(_ => heroNameStatus.text = "");
            heroNameInput.onSubmit.AddListener(_ => SaveHeroName());

            var hint = CreateText("Hero Name Hint", card.transform,
                FrontendCopy.HeroNameHint, 25, catalog.bodyFont,
                FontStyles.Normal, TextAlignmentOptions.Center, theme.textMuted);
            SetRect(hint.rectTransform, new Vector2(0, -64), new Vector2(760, 54), Center(), Center());
            heroNameStatus = CreateText("Hero Name Status", card.transform, "", 28, catalog.bodyFont,
                FontStyles.Normal, TextAlignmentOptions.Center, theme.textPrimary);
            SetRect(heroNameStatus.rectTransform, new Vector2(0, -132), new Vector2(760, 64), Center(), Center());
            heroNameStatus.richText = false;
            var cancel = CreateButton("Cancel Hero Name", card.transform, FrontendCopy.Cancel, new Vector2(350, 132),
                theme.night, CloseHeroName);
            SetRect(cancel.GetComponent<RectTransform>(), new Vector2(-205, 42), new Vector2(350, 132), BottomCenter(), BottomCenter());
            var save = CreateButton("Save Hero Name", card.transform, FrontendCopy.Save, new Vector2(350, 132),
                theme.commandGold, SaveHeroName);
            SetRect(save.GetComponent<RectTransform>(), new Vector2(205, 42), new Vector2(350, 132), BottomCenter(), BottomCenter());
            heroNameModal.SetActive(false);
        }

        private void OpenHeroName()
        {
            heroNameProfileKey = progress.ProfileKey;
            heroNameInput.SetTextWithoutNotify(progress.Data.heroName ?? "");
            heroNameStatus.text = "";
            heroNameModal.SetActive(true);
            heroNameModal.transform.SetAsLastSibling();
        }

        private void SaveHeroName()
        {
            if (heroNameProfileKey != progress.ProfileKey) { CloseHeroName(); return; }
            if (!HeroIdentity.TryNormalize(heroNameInput.text, out string name))
            {
                heroNameStatus.text = GameLocalization.Text("En fazla 20 karakter kullan; satır sonu ve < > ekleme.",
                    "Use up to 20 characters, without line breaks or < >.");
                return;
            }
            if (!progress.TrySetHeroName(name))
            {
                heroNameStatus.text = GameLocalization.Text("Ad kaydedilemedi. Yeniden dene.", "Name could not be saved. Please try again.");
                return;
            }
            heroNameInput.SetTextWithoutNotify(name);
            heroNameInput.DeactivateInputField();
            heroNameStatus.text = GameLocalization.Text($"Kaydedildi: {progress.HeroName}", $"Saved: {progress.HeroName}");
        }

        private void CloseHeroName()
        {
            if (heroNameModal == null) return;
            heroNameInput.DeactivateInputField();
            heroNameModal.SetActive(false);
        }

        private void LateUpdate()
        {
            if (heroNameModal == null || !heroNameModal.activeSelf) return;
            var safe = Screen.safeArea;
            float bottom = TouchScreenKeyboard.visible ? Mathf.Max(safe.yMin, TouchScreenKeyboard.area.yMax) : safe.yMin;
            var canvasRect = (RectTransform)canvas.transform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, new Vector2(safe.xMin + 16, bottom + 16), null, out var min);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, new Vector2(safe.xMax - 16, safe.yMax - 16), null, out var max);
            heroNameCard.anchoredPosition = (min + max) * .5f;
            float scale = Mathf.Min(1, (max.x - min.x) / 900f, (max.y - min.y) / 720f);
            heroNameCard.localScale = Vector3.one * Mathf.Max(.1f, scale);
        }
    }
}
