using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AllinDefense.UI
{
    // A vertical route can still be swiped sideways to change the region.
    public sealed class FrontendRegionScrollRect : ScrollRect
    {
        public ScrollRect Pages;
        private bool horizontalGesture;

        public override void OnInitializePotentialDrag(PointerEventData data)
        {
            base.OnInitializePotentialDrag(data);
            Pages?.OnInitializePotentialDrag(data);
        }
        public override void OnBeginDrag(PointerEventData data)
        {
            horizontalGesture = Mathf.Abs(data.delta.x) > Mathf.Abs(data.delta.y);
            if (horizontalGesture && Pages != null) Pages.OnBeginDrag(data);
            else base.OnBeginDrag(data);
        }
        public override void OnDrag(PointerEventData data)
        {
            if (horizontalGesture && Pages != null) Pages.OnDrag(data);
            else base.OnDrag(data);
        }
        public override void OnEndDrag(PointerEventData data)
        {
            if (horizontalGesture && Pages != null) Pages.OnEndDrag(data);
            else base.OnEndDrag(data);
        }
    }
}
