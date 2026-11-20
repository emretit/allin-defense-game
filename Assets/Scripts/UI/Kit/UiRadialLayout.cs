using System;
using UnityEngine;
using UnityEngine.UI;

namespace AllinDefense.UI.Kit
{
    public sealed class UiRadialLayout : LayoutGroup
    {
        [SerializeField] private Vector2 center;
        [SerializeField] private Rect safeArea;
        public float MinimumDiameter { get; set; } = UiMetrics.AbilityPt;
        // Opt-in: space children on MinimumDiameter but keep each child's own size, so a larger diameter fixes ring
        // positions without enlarging the buttons.
        public bool KeepChildSize { get; set; }
        private Rect[] obstacles = Array.Empty<Rect>();
        private Vector2[] orbitPositions = Array.Empty<Vector2>();
        public bool UsesFallback { get; private set; }
        public void SetObstacles(Rect[] value) => obstacles = value ?? Array.Empty<Rect>();
        public bool SetObstacles(System.Collections.Generic.List<Rect> value)
        {
            bool changed = obstacles.Length != value.Count;
            if (!changed)
                for (int i = 0; i < value.Count; i++)
                    if (obstacles[i] != value[i]) { changed = true; break; }
            if (!changed) return false;
            if (obstacles.Length != value.Count) obstacles = new Rect[value.Count];
            value.CopyTo(obstacles);
            return true;
        }
        public bool UsesHalfArc { get; private set; }
        public void Place(Vector2 localPoint, Rect localSafeArea) { center = localPoint; safeArea = localSafeArea; SetDirty(); }
        public override void CalculateLayoutInputHorizontal() { base.CalculateLayoutInputHorizontal(); SetLayoutInputForAxis(UiMetrics.AbilityPt, UiMetrics.AbilityPt * 4, -1, 0); }
        public override void CalculateLayoutInputVertical() => SetLayoutInputForAxis(UiMetrics.AbilityPt, UiMetrics.AbilityPt * 4, -1, 1);
        public override void SetLayoutHorizontal() => Arrange();
        public override void SetLayoutVertical() => Arrange();
        private void Arrange()
        {
            if (rectChildren.Count == 0) return;
            float diameter = MinimumDiameter;
            foreach (RectTransform child in rectChildren)
                diameter = Mathf.Max(diameter, LayoutUtility.GetMinWidth(child), LayoutUtility.GetMinHeight(child),
                    LayoutUtility.GetPreferredWidth(child), LayoutUtility.GetPreferredHeight(child));
            Rect bounds = safeArea.width > 0 ? safeArea : rectTransform.rect;
            var positions = Calculate(bounds, center, rectChildren.Count, diameter, out bool half);
            UsesFallback = false;
            if (!Fits(positions, diameter))
            {
                bool found = TryOrbit(bounds, diameter, out bool orbitHalf);
                if (found) { positions = orbitPositions; half = orbitHalf; }
                for (float distance = diameter / 2; distance <= Mathf.Max(bounds.width, bounds.height) && !found; distance += diameter / 2)
                    for (int direction = 0; direction < 16 && !found; direction++)
                    {
                        float angle = direction * Mathf.PI / 8;
                        var candidate = Calculate(bounds, center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance,
                            rectChildren.Count, diameter, out bool candidateHalf);
                        if (!Fits(candidate, diameter)) continue;
                        positions = candidate; half = candidateHalf; found = true;
                    }
                if (!found)
                {
                    UsesFallback = true;
                    for (float y = bounds.yMin + diameter / 2; y <= bounds.yMax - diameter / 2 && !found; y += diameter / 2)
                        for (float x = bounds.xMin + diameter / 2; x <= bounds.xMax - (rectChildren.Count - .5f) * (diameter + UiTokens.Space1) && !found; x += diameter / 2)
                        {
                            var candidate = new Vector2[rectChildren.Count];
                            for (int i = 0; i < candidate.Length; i++) candidate[i] = new Vector2(x + i * (diameter + UiTokens.Space1), y);
                            if (!Fits(candidate, diameter)) continue;
                            positions = candidate; found = true;
                        }
                }
            }
            UsesHalfArc = half;
            for (int i = 0; i < rectChildren.Count; i++)
            {
                RectTransform child = rectChildren[i];
                float width = KeepChildSize ? ChildSize(LayoutUtility.GetPreferredWidth(child), diameter) : diameter;
                float height = KeepChildSize ? ChildSize(LayoutUtility.GetPreferredHeight(child), diameter) : diameter;
                SetChildAlongAxis(child, 0, positions[i].x - rectTransform.rect.xMin - width / 2, width);
                SetChildAlongAxis(child, 1, rectTransform.rect.yMax - positions[i].y - height / 2, height);
            }
        }

        private static float ChildSize(float preferred, float diameter) => preferred > 0 ? Mathf.Min(preferred, diameter) : diameter;

        private bool TryOrbit(Rect bounds, float diameter, out bool halfArc)
        {
            if (orbitPositions.Length != rectChildren.Count) orbitPositions = new Vector2[rectChildren.Count];
            var inset = Rect.MinMaxRect(bounds.xMin + diameter / 2, bounds.yMin + diameter / 2,
                bounds.xMax - diameter / 2, bounds.yMax - diameter / 2);
            Vector2 inward = bounds.center - center;
            float middle = inward.sqrMagnitude > .01f ? Mathf.Atan2(inward.y, inward.x) : Mathf.PI / 2;
            for (int arc = 0; arc < 2; arc++)
            {
                float minimumRadius = arc == 0 || rectChildren.Count < 2 ? diameter :
                    Mathf.Max(diameter, (diameter + UiTokens.Space1) / (2 * Mathf.Sin(Mathf.PI / (4 * (rectChildren.Count - 1)))));
                for (float radius = minimumRadius; radius <= Mathf.Max(bounds.width, bounds.height); radius += diameter / 2)
                    for (int direction = 0; direction < 16; direction++)
                    {
                        bool inside = true;
                        for (int i = 0; i < orbitPositions.Length; i++)
                        {
                            float angle = middle + direction * Mathf.PI / 8 + (arc == 0 ? i * Mathf.PI * 2 / orbitPositions.Length :
                                orbitPositions.Length == 1 ? 0 : Mathf.Lerp(-Mathf.PI / 4, Mathf.PI / 4, i / (float)(orbitPositions.Length - 1)));
                            orbitPositions[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                            inside &= inset.Contains(orbitPositions[i]);
                        }
                        if (!inside || !Fits(orbitPositions, diameter)) continue;
                        halfArc = arc != 0;
                        return true;
                    }
            }
            halfArc = false;
            return false;
        }

        private bool Fits(Vector2[] positions, float diameter)
        {
            foreach (var point in positions)
                foreach (var obstacle in obstacles)
                    if (new Rect(point - Vector2.one * diameter / 2, Vector2.one * diameter).Overlaps(obstacle)) return false;
            return true;
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
