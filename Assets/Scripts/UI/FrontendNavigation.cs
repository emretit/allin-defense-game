using System;
using AllinDefense.UI.Kit;
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
        public const float Duration = UiTokens.FadeDuration;
        private CanvasGroup current;
        private UiMotion.Handle animation;
        private UiMotion.Handle exit;
        public bool IsTransitioning => animation != null && !animation.IsComplete && !animation.IsCancelled;

        public void Show(GameObject target, bool animate, Action completed = null)
        {
            if (current != null && current.gameObject == target && target.activeSelf) return;
            Cancel();
            var previous = current;
            current = target.GetComponent<CanvasGroup>() ?? target.AddComponent<CanvasGroup>();
            target.SetActive(true);
            var motion = GetComponent<UiMotion>() ?? gameObject.AddComponent<UiMotion>();
            if (previous != null)
            {
                previous.interactable = previous.blocksRaycasts = false;
                exit = motion.Animate(t =>
                {
                    if (previous == null) return;
                    previous.alpha = 1 - t;
                    ((RectTransform)previous.transform).anchoredPosition = Vector2.left * UiTokens.Space3 * t;
                }, animate ? Duration : 0, () => { if (previous != null) previous.gameObject.SetActive(false); });
            }
            var group = current;
            group.interactable = false;
            group.blocksRaycasts = true;
            animation = motion.Animate(t =>
            {
                group.alpha = t;
                ((RectTransform)group.transform).anchoredPosition = Vector2.right * UiTokens.Space3 * (1 - t);
            }, animate ? Duration : 0, () => { Reset(group); completed?.Invoke(); });
        }

        public void Cancel()
        {
            animation?.Cancel(); exit?.Cancel();
            animation = null; exit = null;
            foreach (var group in GetComponentsInChildren<CanvasGroup>(true))
                if (group != current && group.transform.parent == current?.transform.parent) group.gameObject.SetActive(false);
            if (current != null) Reset(current);
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
