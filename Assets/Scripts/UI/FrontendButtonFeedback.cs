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

        public void OnPointerDown(PointerEventData eventData) => AnimateTo(restingScale * 0.975f);
        public void OnPointerUp(PointerEventData eventData) => AnimateTo(restingScale);
        public void OnPointerExit(PointerEventData eventData) => AnimateTo(restingScale);
        public void OnSelect(BaseEventData eventData) => AnimateTo(restingScale * 1.025f);
        public void OnDeselect(BaseEventData eventData) => AnimateTo(restingScale);

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
            const float duration = 0.09f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                t = 1f - Mathf.Pow(1f - t, 3f);
                transform.localScale = Vector3.LerpUnclamped(start, target, t);
                yield return null;
            }
            transform.localScale = target;
            animationRoutine = null;
        }
    }
}
