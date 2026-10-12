using UnityEngine;
using UnityEngine.UI;

namespace AllinDefense.UI
{
    [DisallowMultipleComponent]
    public sealed class MobileTouchTarget : MonoBehaviour
    {
        private RectTransform visual;
        private RectTransform hitArea;
        private Vector2 previousSize, previousScale, previousPoints;

        public static void Attach(Button button)
        {
            if (button == null) return;
            var target = button.GetComponent<MobileTouchTarget>();
            if (target == null) target = button.gameObject.AddComponent<MobileTouchTarget>();
            target.UpdateBounds();
            UiId.AssignHierarchy(button.transform);
        }

        private void OnEnable()
        {
            Canvas.preWillRenderCanvases += UpdateBounds;
            UpdateBounds();
        }

        private void OnDisable() => Canvas.preWillRenderCanvases -= UpdateBounds;
        private void OnRectTransformDimensionsChange() => UpdateBounds();

        private void UpdateBounds()
        {
            if (visual == null) visual = (RectTransform)transform;
            var canvas = GetComponentInParent<Canvas>();
            Vector2 points = UiMetrics.PointsPerCanvasUnit(canvas);
            Vector2 scale = Vector2.one;
            for (Transform parent = visual; parent != null && parent.GetComponent<Canvas>() == null; parent = parent.parent)
                scale = Vector2.Scale(scale, new Vector2(Mathf.Abs(parent.localScale.x), Mathf.Abs(parent.localScale.y)));
            Vector2 size = visual.rect.size;
            if (hitArea != null && size == previousSize && scale == previousScale && points == previousPoints) return;
            previousSize = size; previousScale = scale; previousPoints = points;
            if (hitArea == null)
            {
                hitArea = transform.Find("Touch Target") as RectTransform;
                if (hitArea == null)
                {
                    var root = new GameObject("Touch Target", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    root.transform.SetParent(transform, false);
                    root.transform.SetAsFirstSibling();
                    UiId.Assign(root.transform, "Touch Target");
                    hitArea = (RectTransform)root.transform;
                    var image = root.GetComponent<Image>();
                    image.color = Color.clear;
                    image.raycastTarget = true;
                }
                hitArea.anchorMin = hitArea.anchorMax = hitArea.pivot = new Vector2(.5f, .5f);
                hitArea.anchoredPosition = Vector2.zero;
            }
            Vector2 minimum = UiMetrics.CanvasUnitsForPoints(UiMetrics.MinTouchPt + .01f, points);
            hitArea.sizeDelta = new Vector2(Mathf.Max(minimum.x / Mathf.Max(.001f, scale.x), size.x),
                Mathf.Max(minimum.y / Mathf.Max(.001f, scale.y), size.y));
        }
    }
}
