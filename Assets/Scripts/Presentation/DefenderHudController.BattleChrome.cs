using AllinDefense.UI;
using AllinDefense.UI.Kit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AllinDefense.Presentation
{
    public sealed partial class DefenderHudController
    {
        private DefenderKeepPlate keepPlate;
        private Image messageIcon;
        private CanvasGroup messageGroup, messageTextGroup;
        private string shownMessage;
        private float messageEntrance = 1;

        private void InitializeMessageChrome()
        {
            messageChromeImage.sprite = frameKit.Assets.pill;
            messageChromeImage.type = Image.Type.Sliced;
            messageChromeImage.color = UiTokens.HairlineGold;
            messageChromeImage.gameObject.AddComponent<UiPillShape>();
            var backing = frameKit.Decoration(messageChromeImage.transform, "Message Backing", frameKit.Assets.pill, UiTokens.GlassStrong);
            backing.rectTransform.offsetMin = Vector2.one;
            backing.rectTransform.offsetMax = -Vector2.one;
            messageIcon = frameKit.Icon(messageChromeImage.transform, "Message Icon", frameKit.Assets.iconEnemy, 18);
            var rect = messageIcon.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0, .5f);
            rect.anchoredPosition = new Vector2(20, 0);
            rect.sizeDelta = new Vector2(18, 18);
            messageText.textWrappingMode = TextWrappingModes.Normal;
            messageText.overflowMode = TextOverflowModes.Overflow;
            messageText.enableAutoSizing = false;
            messageText.fontSize = 13;
            messageText.fontSizeMin = 13;
            messageText.fontSizeMax = 13;
            messageGroup = messageChromeImage.gameObject.AddComponent<CanvasGroup>();
            messageTextGroup = messageText.gameObject.AddComponent<CanvasGroup>();
            messageGroup.blocksRaycasts = messageTextGroup.blocksRaycasts = false;
        }

        private void ShowMessageChrome(string body, bool visible, bool warning)
        {
            messageIcon.color = warning ? UiTokens.Danger : UiTokens.Gold;
            messageText.color = warning ? UiTokens.TextPrimary : UiTokens.TextSecondary;
            if (visible && body != shownMessage) messageEntrance = 0;
            shownMessage = body;
            responsiveLayout?.RefreshLayout();
        }

        private void TickMessageEntrance()
        {
            if (messageGroup == null || messageEntrance >= 1) return;
            messageEntrance = Mathf.Min(1, messageEntrance + Time.unscaledDeltaTime / UiTokens.FadeDuration);
            messageGroup.alpha = messageTextGroup.alpha = messageEntrance;
            float offset = FrontendPreferences.ReducedMotion ? 0 : -6 * (1 - UiTokens.EvaluateEaseOut(messageEntrance));
            messageChromeImage.rectTransform.localPosition += Vector3.up * (offset - messageOffset);
            messageText.rectTransform.localPosition += Vector3.up * (offset - messageOffset);
            messageOffset = offset;
        }

        private float messageOffset;
    }
}
