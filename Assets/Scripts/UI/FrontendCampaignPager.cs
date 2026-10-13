using AllinDefense.UI.Kit;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AllinDefense.UI
{
    public sealed class FrontendCampaignPager : ScrollRect
    {
        public int PageCount = 5;
        private UiMotion.Handle snap;
        public override void OnBeginDrag(PointerEventData data)
        {
            StopMovement();
            base.OnBeginDrag(data);
        }
        public override void OnEndDrag(PointerEventData data)
        {
            base.OnEndDrag(data);
            velocity = Vector2.zero;
            float target = Mathf.Round(horizontalNormalizedPosition * (PageCount - 1)) / Mathf.Max(1, PageCount - 1);
            snap?.Cancel();
            float start = horizontalNormalizedPosition;
            snap = (GetComponent<UiMotion>() ?? gameObject.AddComponent<UiMotion>()).Animate(t =>
                horizontalNormalizedPosition = Mathf.Lerp(start, target, t), UiTokens.ButtonPressDuration);
        }
        public override void StopMovement()
        {
            snap?.Cancel();
            snap = null;
            base.StopMovement();
        }
        protected override void OnDisable() { StopMovement(); base.OnDisable(); }
    }
}
