using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DefenderChronicles.UI
{
    public sealed class FrontendCampaignPager : ScrollRect
    {
        public int PageCount = 5;
        private Coroutine snap;
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
            snap = StartCoroutine(Snap(target));
        }
        private IEnumerator Snap(float target)
        {
            float start = horizontalNormalizedPosition;
            float elapsed = 0;
            while (elapsed < .12f)
            {
                elapsed += Time.unscaledDeltaTime;
                horizontalNormalizedPosition = Mathf.Lerp(start, target, Mathf.Clamp01(elapsed / .12f));
                yield return null;
            }
            snap = null;
        }
        public override void StopMovement()
        {
            if (snap != null) StopCoroutine(snap);
            snap = null;
            base.StopMovement();
        }
        protected override void OnDisable() { StopMovement(); base.OnDisable(); }
    }
}
