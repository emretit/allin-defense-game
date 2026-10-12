using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AllinDefense.UI.Kit
{
    public sealed class UiMotion : MonoBehaviour
    {
        private readonly HashSet<Handle> running = new HashSet<Handle>();

        public sealed class Handle
        {
            public bool IsComplete { get; internal set; }
            public bool IsCancelled { get; private set; }
            public void Cancel() => IsCancelled = true;
        }

        public Handle Animate(Action<float> apply, float duration = UiTokens.FadeDuration, Action completed = null)
        {
            var handle = new Handle();
            if (FrontendPreferences.ReducedMotion || !Application.isPlaying || !isActiveAndEnabled || duration <= 0f)
            {
                apply(1f);
                handle.IsComplete = true;
                completed?.Invoke();
            }
            else { running.Add(handle); StartCoroutine(Run(handle, apply, duration, completed)); }
            return handle;
        }

        private IEnumerator Run(Handle handle, Action<float> apply, float duration, Action completed)
        {
            float elapsed = 0f;
            apply(0f);
            while (elapsed < duration)
            {
                if (handle.IsCancelled) { running.Remove(handle); yield break; }
                if (FrontendPreferences.ReducedMotion) break;
                yield return null;
                if (handle.IsCancelled) { running.Remove(handle); yield break; }
                elapsed += Time.unscaledDeltaTime;
                apply(UiTokens.EvaluateEaseOut(elapsed / duration));
            }
            if (handle.IsCancelled) { running.Remove(handle); yield break; }
            apply(1f);
            handle.IsComplete = true;
            running.Remove(handle);
            completed?.Invoke();
        }

        private void OnDisable()
        {
            foreach (var handle in running) handle.Cancel();
            StopAllCoroutines(); running.Clear();
        }

        public Handle Fade(CanvasGroup group, float to, Action completed = null)
        {
            float from = group.alpha;
            return Animate(t => { if (group != null) group.alpha = Mathf.Lerp(from, to, t); }, UiTokens.FadeDuration, completed);
        }

        public Handle Scale(Transform target, Vector3 to)
        {
            Vector3 from = target.localScale;
            return Animate(t => { if (target != null) target.localScale = Vector3.LerpUnclamped(from, to, t); }, UiTokens.ButtonPressDuration);
        }

        public Handle Slide(RectTransform target, Vector2 to)
        {
            Vector2 from = target.anchoredPosition;
            return Animate(t => { if (target != null) target.anchoredPosition = Vector2.LerpUnclamped(from, to, t); }, UiTokens.SlideDuration);
        }

        public Handle Punch(Transform target, int pulses = 1)
        {
            Vector3 start = target.localScale;
            int count = Mathf.Clamp(pulses, 1, 2);
            return Animate(t => { if (target != null) target.localScale = start *
                (1f + (UiTokens.FocusScale - 1f) * Mathf.Abs(Mathf.Sin(t * count * Mathf.PI))); }, UiTokens.SlideDuration * count);
        }

        public Handle Count(int from, int to, Action<int> apply) =>
            Animate(t => apply(Mathf.RoundToInt(Mathf.Lerp(from, to, t))), UiTokens.SlideDuration);
    }
}
