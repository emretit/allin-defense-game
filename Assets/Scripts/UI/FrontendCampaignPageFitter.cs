using UnityEngine;
using UnityEngine.UI;

namespace AllinDefense.UI
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
        private RectTransform[] routeNodes, routeLines;
        private float lastWidth = -1f;

        public void Configure(RectTransform targetViewport, float minimumPageWidth)
        {
            page = GetComponent<RectTransform>();
            layoutElement = GetComponent<LayoutElement>();
            viewport = targetViewport;
            ApplyWidth();
        }

        public void ConfigureRoute(RectTransform[] nodes, RectTransform[] lines)
        {
            routeNodes = nodes; routeLines = lines;
        }

        private void LateUpdate()
        {
            ApplyWidth();
            if (routeNodes == null) return;
            for (int i = 0; i < routeLines.Length; i++)
            {
                var line = routeLines[i];
                Vector2 from = line.parent.InverseTransformPoint(routeNodes[i].TransformPoint(routeNodes[i].rect.center));
                Vector2 to = line.parent.InverseTransformPoint(routeNodes[i + 1].TransformPoint(routeNodes[i + 1].rect.center));
                Vector2 delta = to - from;
                line.localPosition = from;
                line.sizeDelta = new Vector2(delta.magnitude, UiTokens.Space1 / 2);
                line.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            }
        }

        private void ApplyWidth()
        {
            if (page == null || viewport == null || layoutElement == null) return;
            float width = Mathf.Max(1f, viewport.rect.width);
            if (Mathf.Abs(width - lastWidth) < 0.5f) return;
            lastWidth = width;
            layoutElement.preferredWidth = width;
            Vector2 size = page.sizeDelta;
            size.x = width;
            page.sizeDelta = size;
        }
    }
}
