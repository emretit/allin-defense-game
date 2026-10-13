using AllinDefense.UI.Kit;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AllinDefense.UI
{
    public sealed class FrontendButtonFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,
        IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        private Vector3 restingScale = Vector3.one;
        private UiMotion.Handle animationRoutine;

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
            animationRoutine?.Cancel();
            animationRoutine = null;
            transform.localScale = restingScale;
        }

        private void AnimateTo(Vector3 target)
        {
            animationRoutine?.Cancel();
            animationRoutine = (GetComponent<UiMotion>() ?? gameObject.AddComponent<UiMotion>()).Scale(transform, target);
        }
    }
}
