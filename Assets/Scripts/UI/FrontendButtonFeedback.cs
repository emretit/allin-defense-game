using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DefenderChronicles.UI
{
    public sealed class FrontendButtonFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,
        IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        private Vector3 restingScale = Vector3.one;
        private Coroutine animationRoutine;

        private void Awake()
        {
            restingScale = transform.localScale;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (TryGetComponent<UnityEngine.UI.Selectable>(out var button) && !button.IsInteractable()) return;
            AnimateTo(restingScale * UiTokens.PressScale);
        }
        public void OnPointerUp(PointerEventData eventData) => AnimateTo(restingScale);
        public void OnPointerExit(PointerEventData eventData) => AnimateTo(restingScale);
        public void OnSelect(BaseEventData eventData) => AnimateTo(restingScale * UiTokens.FocusScale);
        public void OnDeselect(BaseEventData eventData) => AnimateTo(restingScale);

        private void OnDisable()
        {
            if (animationRoutine != null) StopCoroutine(animationRoutine);
            animationRoutine = null;
            transform.localScale = restingScale;
        }

        private void AnimateTo(Vector3 target)
        {
            if (FrontendPreferences.ReducedMotion)
            {
                transform.localScale = target;
                return;
            }
            if (animationRoutine != null) StopCoroutine(animationRoutine);
            animationRoutine = StartCoroutine(AnimateScale(target));
        }

        private IEnumerator AnimateScale(Vector3 target)
        {
            Vector3 start = transform.localScale;
            const float duration = UiTokens.ButtonPressDuration;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                t = UiTokens.EvaluateEaseOut(t);
                transform.localScale = Vector3.LerpUnclamped(start, target, t);
                yield return null;
            }
            transform.localScale = target;
            animationRoutine = null;
        }
    }
}
