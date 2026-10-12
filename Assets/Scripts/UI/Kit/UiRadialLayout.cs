using System;
using UnityEngine;
using UnityEngine.UI;

namespace AllinDefense.UI.Kit
{
    public sealed class UiRadialLayout : LayoutGroup
    {
        [SerializeField] private Vector2 center;
        [SerializeField] private Rect safeArea;
        public bool UsesHalfArc { get; private set; }
        public void Place(Vector2 localPoint, Rect localSafeArea) { center = localPoint; safeArea = localSafeArea; SetDirty(); }
        public override void CalculateLayoutInputHorizontal() { base.CalculateLayoutInputHorizontal(); SetLayoutInputForAxis(UiMetrics.AbilityPt, UiMetrics.AbilityPt * 4, -1, 0); }
        public override void CalculateLayoutInputVertical() => SetLayoutInputForAxis(UiMetrics.AbilityPt, UiMetrics.AbilityPt * 4, -1, 1);
        public override void SetLayoutHorizontal() => Arrange();
        public override void SetLayoutVertical() => Arrange();
        private void Arrange()
        {
            if (rectChildren.Count == 0) return;
            float diameter = UiMetrics.AbilityPt;
            foreach (RectTransform child in rectChildren)
                diameter = Mathf.Max(diameter, LayoutUtility.GetMinWidth(child), LayoutUtility.GetMinHeight(child),
                    LayoutUtility.GetPreferredWidth(child), LayoutUtility.GetPreferredHeight(child));
            Rect bounds = safeArea.width > 0 ? safeArea : rectTransform.rect;
            var positions = Calculate(bounds, center, rectChildren.Count, diameter, out bool half);
            UsesHalfArc = half;
            for (int i = 0; i < rectChildren.Count; i++)
            {
                SetChildAlongAxis(rectChildren[i], 0, positions[i].x - rectTransform.rect.xMin - diameter / 2, diameter);
                SetChildAlongAxis(rectChildren[i], 1, rectTransform.rect.yMax - positions[i].y - diameter / 2, diameter);
            }
        }

        public static Vector2[] Calculate(Rect safe, Vector2 origin, int count, float diameter, out bool halfArc)
        {
            if (count < 1) throw new ArgumentOutOfRangeException(nameof(count));
            float half = diameter / 2;
            var inset = Rect.MinMaxRect(safe.xMin + half, safe.yMin + half, safe.xMax - half, safe.yMax - half);
            if (inset.width < 0 || inset.height < 0) throw new ArgumentException("Safe area must fit a full touch target.", nameof(safe));
            float radius = Mathf.Max(diameter,
                count < 2 ? 0 : (diameter + UiTokens.Space1) / (2 * Mathf.Sin(Mathf.PI / count)));
            halfArc = origin.x - radius < inset.xMin || origin.x + radius > inset.xMax ||
                origin.y - radius < inset.yMin || origin.y + radius > inset.yMax;
            Vector2 direction = inset.center - origin;
            float middle = direction.sqrMagnitude > .01f ? Mathf.Atan2(direction.y, direction.x) : Mathf.PI / 2;
            if (halfArc && count > 1) radius = Mathf.Max(radius, (diameter + UiTokens.Space1) / (2 * Mathf.Sin(Mathf.PI / (2 * (count - 1)))));
            var points = new Vector2[count];
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue), max = new Vector2(float.MinValue, float.MinValue);
            for (int i = 0; i < count; i++)
            {
                float angle = halfArc ? middle + (count == 1 ? 0 : Mathf.Lerp(-Mathf.PI / 2, Mathf.PI / 2, i / (float)(count - 1))) :
                    Mathf.PI / 2 + i * Mathf.PI * 2 / count;
                points[i] = origin + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                min = Vector2.Min(min, points[i]); max = Vector2.Max(max, points[i]);
            }
            if (max.x - min.x <= inset.width && max.y - min.y <= inset.height)
            {
                Vector2 shift = new Vector2(Mathf.Max(0, inset.xMin - min.x) - Mathf.Max(0, max.x - inset.xMax),
                    Mathf.Max(0, inset.yMin - min.y) - Mathf.Max(0, max.y - inset.yMax));
                for (int i = 0; i < count; i++)
                    points[i] = new Vector2(Mathf.Clamp(points[i].x + shift.x, inset.xMin, inset.xMax),
                        Mathf.Clamp(points[i].y + shift.y, inset.yMin, inset.yMax));
                return points;
            }
            // Compact rows preserve touch target size when an arc cannot fit the available area.
            int columns = Mathf.Max(1, Mathf.FloorToInt((safe.width + UiTokens.Space1) / (diameter + UiTokens.Space1)));
            int rows = Mathf.CeilToInt(count / (float)columns);
            if (rows * diameter + (rows - 1) * UiTokens.Space1 > safe.height)
                throw new ArgumentException("Safe area cannot contain these touch targets without overlap.", nameof(safe));
            halfArc = true;
            for (int i = 0; i < count; i++) points[i] = new Vector2(inset.xMin + (i % columns) * (diameter + UiTokens.Space1),
                inset.yMax - (i / columns) * (diameter + UiTokens.Space1));
            return points;
        }
    }
}
