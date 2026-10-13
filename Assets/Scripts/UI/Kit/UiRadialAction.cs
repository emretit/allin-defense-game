using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AllinDefense.UI.Kit
{
    public sealed class UiRadialAction : MonoBehaviour, IPointerClickHandler
    {
        private Action rejected;
        private Button button;
        public void Configure(Action onRejected) { rejected = onRejected; button = GetComponent<Button>(); }
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && button != null && !button.interactable) rejected?.Invoke();
        }
    }
}
