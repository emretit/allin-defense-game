using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AllinDefense.UI
{
    // Only this small surface survives scene replacement, not the old game session.
    public sealed class FrontendLoadingOverlay : MonoBehaviour
    {
        private static FrontendLoadingOverlay instance;
        private TMP_Text label;
        public static bool Active => instance != null;
        public static float LongestFrameMs { get; private set; }
        public static double FirstFeedbackMs { get; private set; }
        private double shownAt;
        private void Update() => LongestFrameMs = Mathf.Max(LongestFrameMs, Time.unscaledDeltaTime * 1000);

        public static void Show(TMP_FontAsset font)
        {
            if (instance != null) return;
            var root = new GameObject("Battle Loading", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            DontDestroyOnLoad(root);
            instance = root.AddComponent<FrontendLoadingOverlay>();
            instance.shownAt = Time.realtimeSinceStartupAsDouble;
            LongestFrameMs = 0; FirstFeedbackMs = 0;
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 2000;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            var shade = new GameObject("Shade", typeof(RectTransform), typeof(Image));
            shade.transform.SetParent(root.transform, false);
            var rect = (RectTransform)shade.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.sizeDelta = Vector2.zero;
            shade.GetComponent<Image>().color = UiTokens.LoadingSurface;
            var text = new GameObject("Status", typeof(RectTransform), typeof(TextMeshProUGUI));
            text.transform.SetParent(shade.transform, false);
            rect = (RectTransform)text.transform;
            rect.anchorMin = new Vector2(.1f, .35f); rect.anchorMax = new Vector2(.9f, .65f); rect.sizeDelta = Vector2.zero;
            instance.label = text.GetComponent<TextMeshProUGUI>();
            instance.label.font = font; instance.label.fontSize = UiTokens.TypeSubheading;
            instance.label.alignment = TextAlignmentOptions.Center;
            instance.label.color = UiTokens.Gold;
            SetStatus(GameLocalization.Text("Savaş alanı hazırlanıyor…", "Preparing the battlefield…"));
        }

        public static void SetStatus(string text) { if (instance != null) instance.label.text = text; }
        public static void Hide()
        {
            if (instance == null) return;
            instance.gameObject.SetActive(false);
            Destroy(instance.gameObject);
            instance = null;
        }
        public static void Reload(int buildIndex, Action<string> failed, Action beforeLoad = null)
        {
            instance.StartCoroutine(instance.Load(buildIndex, failed, beforeLoad));
        }
        private IEnumerator Load(int buildIndex, Action<string> failed, Action beforeLoad)
        {
            yield return null; // Paint feedback before starting scene work.
            FirstFeedbackMs = (Time.realtimeSinceStartupAsDouble - shownAt) * 1000;
            beforeLoad?.Invoke();
            AsyncOperation operation = null;
            string error = null;
            try { operation = SceneManager.LoadSceneAsync(buildIndex); }
            catch (Exception exception) { error = exception.Message; }
            if (operation == null)
            {
                Hide(); failed?.Invoke(error ?? "Scene could not be loaded."); yield break;
            }
            while (!operation.isDone) yield return null;
            // The destination owns dismissal, after orientation and session readiness.
        }
    }
}
