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
            internal Action Finish;
            public bool IsComplete { get; internal set; }
            public bool IsCancelled { get; private set; }
            public void Cancel() => IsCancelled = true;
        }

        public Handle Animate(Action<float> apply, float duration = UiTokens.FadeDuration, Action completed = null)
        {
            var handle = new Handle();
            handle.Finish = () =>
            {
                if (handle.IsComplete || handle.IsCancelled) return;
                apply(1f); handle.IsComplete = true; running.Remove(handle); completed?.Invoke();
            };
            if (FrontendPreferences.ReducedMotion || !Application.isPlaying || !isActiveAndEnabled || duration <= 0f)
            {
                handle.Finish();
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
                if (handle.IsCancelled || handle.IsComplete) { running.Remove(handle); yield break; }
                if (FrontendPreferences.ReducedMotion) break;
                yield return null;
                if (handle.IsCancelled || handle.IsComplete) { running.Remove(handle); yield break; }
                elapsed += Time.unscaledDeltaTime;
                apply(UiTokens.EvaluateEaseOut(elapsed / duration));
            }
            if (handle.IsCancelled || handle.IsComplete) { running.Remove(handle); yield break; }
            handle.Finish();
        }

        private void OnEnable() => FrontendPreferences.MotionChanged += FinishReducedMotion;
        private void FinishReducedMotion()
        {
            if (!FrontendPreferences.ReducedMotion) return;
            foreach (var handle in new List<Handle>(running)) handle.Finish();
        }

        private void OnDisable()
        {
            FrontendPreferences.MotionChanged -= FinishReducedMotion;
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
                (1f + (UiTokens.FocusScale - 1f) * (t >= 1 ? 0 : Mathf.Abs(Mathf.Sin(t * count * Mathf.PI)))); }, UiTokens.SlideDuration * count);
        }

        public Handle Shake(RectTransform target)
        {
            Vector2 start = target.anchoredPosition;
            return Animate(t =>
            {
                if (target != null) target.anchoredPosition = start + Vector2.right *
                    (t >= 1 ? 0 : Mathf.Sin(t * Mathf.PI * 6) * UiTokens.Space1 * (1 - t));
            }, UiTokens.SlideDuration);
        }

        public Handle Count(int from, int to, Action<int> apply) =>
            Animate(t => apply(Mathf.RoundToInt(Mathf.Lerp(from, to, t))), UiTokens.SlideDuration);
    }
}
