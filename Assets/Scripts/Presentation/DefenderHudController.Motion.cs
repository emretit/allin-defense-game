using AllinDefense.UI;
using AllinDefense.UI.Kit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AllinDefense.Presentation
{
    public sealed partial class DefenderHudController
    {
        private CanvasGroup waveBanner;
        private TMP_Text waveBannerText;
        private UiMotion.Handle waveBannerMotion;
        public bool WaveBannerVisible => waveBanner != null && waveBanner.gameObject.activeSelf;

        public void ShowWaveBanner(int wave, int total)
        {
            if (waveBanner == null)
            {
                var root = new GameObject("Wave Start Banner", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
                root.transform.SetParent(transform, false);
                var rect = (RectTransform)root.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, 1);
                rect.anchoredPosition = new Vector2(0, -64);
                rect.sizeDelta = new Vector2(440, 48);
                root.GetComponent<Image>().color = UiTokens.GlassStrong;
                root.GetComponent<Image>().raycastTarget = false;
                waveBanner = root.GetComponent<CanvasGroup>();
                waveBanner.interactable = waveBanner.blocksRaycasts = false;
                waveBannerText = frameKit.Text(root.transform, "Wave Start Label", "", UiTokens.TypeBody, true);
                waveBannerText.alignment = TextAlignmentOptions.Center;
                var textRect = waveBannerText.rectTransform;
                textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
                textRect.offsetMin = textRect.offsetMax = Vector2.zero;
            }
            waveBannerMotion?.Cancel();
            waveBanner.gameObject.SetActive(true);
            waveBannerText.text = $"{FrontendCopy.HudWave.Value} {wave}/{total}";
            waveBannerMotion = (GetComponent<UiMotion>() ?? gameObject.AddComponent<UiMotion>()).Animate(t =>
                waveBanner.alpha = t >= 1 ? 0 : Mathf.Min(t * 8, (1 - t) * 8),
                UiTokens.WaveBannerDuration, () => waveBanner.gameObject.SetActive(false));
        }
    }
}
