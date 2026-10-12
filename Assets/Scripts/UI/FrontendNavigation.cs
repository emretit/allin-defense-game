using System;
using System.Collections;
using UnityEngine;

namespace AllinDefense.UI
{
    // Session-only state survives a battle scene reload; never changes the save schema.
    public sealed class FrontendNavigationState
    {
        public AppScreen AccountReturn = AppScreen.MainMenu;
        public int SelectedLevel = 1;
        public int Region;
        public bool HasMapPosition;
        public float Horizontal;
        public readonly float[] Vertical = { 1f, 1f, 1f, 1f, 1f };
    }

    public sealed class FrontendNavigation : MonoBehaviour
    {
        public const float Duration = 0.15f;
        private CanvasGroup current;
        private Coroutine animation;
        public bool IsTransitioning => animation != null;

        public void Show(GameObject target, bool animate, Action completed = null)
        {
            if (current != null && current.gameObject == target && target.activeSelf) return;
            Cancel();
            if (current != null) current.gameObject.SetActive(false);
            current = target.GetComponent<CanvasGroup>();
            if (current == null) current = target.AddComponent<CanvasGroup>();
            target.SetActive(true);
            if (animate) animation = StartCoroutine(Reveal(current, completed));
            else { Reset(current); completed?.Invoke(); }
        }

        public void Cancel()
        {
            if (animation != null) StopCoroutine(animation);
            animation = null;
            if (current != null) Reset(current);
        }

        private IEnumerator Reveal(CanvasGroup group, Action completed)
        {
            RectTransform rect = (RectTransform)group.transform;
            float duration = FrontendPreferences.ReducedMotion ? 0.06f : Duration;
            group.alpha = 0.15f;
            group.interactable = false;
            group.blocksRaycasts = true;
            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = 1f - Mathf.Pow(1f - Mathf.Clamp01(elapsed / duration), 3f);
                group.alpha = Mathf.Lerp(0.15f, 1, t);
                rect.anchoredPosition = FrontendPreferences.ReducedMotion ? Vector2.zero : new Vector2(12f * (1 - t), 0);
                yield return null;
            }
            Reset(group);
            animation = null;
            completed?.Invoke();
        }

        private static void Reset(CanvasGroup group)
        {
            group.alpha = 1;
            group.interactable = true;
            group.blocksRaycasts = true;
            ((RectTransform)group.transform).anchoredPosition = Vector2.zero;
        }

        private void OnDisable() => Cancel();
    }
}
