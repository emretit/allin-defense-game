using UnityEngine;
using UnityEngine.UI;

namespace DefenderChronicles.UI
{
    /// <summary>
    /// Keeps each campaign region equal to the ScrollRect viewport width so one complete
    /// map page is shown on phones and tablets without exposing a clipped next region.
    /// </summary>
    [RequireComponent(typeof(RectTransform), typeof(LayoutElement))]
    public sealed class FrontendCampaignPageFitter : MonoBehaviour
    {
        private RectTransform page;
        private RectTransform viewport;
        private LayoutElement layoutElement;
        private float minimumWidth;
        private float lastWidth = -1f;

        public void Configure(RectTransform targetViewport, float minimumPageWidth)
        {
            page = GetComponent<RectTransform>();
            layoutElement = GetComponent<LayoutElement>();
            viewport = targetViewport;
            minimumWidth = Mathf.Max(1f, minimumPageWidth);
            ApplyWidth();
        }

        private void LateUpdate()
        {
            ApplyWidth();
        }

        private void ApplyWidth()
        {
            if (page == null || viewport == null || layoutElement == null) return;
            float width = Mathf.Max(minimumWidth, viewport.rect.width);
            if (Mathf.Abs(width - lastWidth) < 0.5f) return;
            lastWidth = width;
            layoutElement.preferredWidth = width;
            Vector2 size = page.sizeDelta;
            size.x = width;
            page.sizeDelta = size;
        }
    }
}
