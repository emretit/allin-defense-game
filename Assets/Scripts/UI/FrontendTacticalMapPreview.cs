using System.Collections.Generic;
using AllinDefense.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AllinDefense.UI
{
    [DisallowMultipleComponent]
    public sealed class FrontendTacticalMapPreview : MonoBehaviour
    {
        private const float Padding = 22f;
        private RectTransform root;
        private MapDefinition map;
        private Color accent;
        private TMP_FontAsset font;
        private Vector2 previousSize;

        public int RouteSegmentCount { get; private set; }
        public int SlotMarkerCount { get; private set; }
        public int EntranceMarkerCount { get; private set; }

        public void Configure(MapDefinition definition, Color regionAccent, TMP_FontAsset labelFont)
        {
            map = definition;
            accent = regionAccent;
            font = labelFont;
            root = transform as RectTransform;
            Rebuild();
        }

        private void LateUpdate()
        {
            if (root != null && root.rect.size != previousSize) Rebuild();
        }

        private void Rebuild()
        {
            if (root == null || map?.routes == null || root.rect.width <= 1f || root.rect.height <= 1f) return;
            for (int index = transform.childCount - 1; index >= 0; index--)
            {
                GameObject child = transform.GetChild(index).gameObject;
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }

            previousSize = root.rect.size;
            RouteSegmentCount = 0;
            SlotMarkerCount = 0;
            EntranceMarkerCount = 0;
            Rect bounds = CalculateWorldBounds(map);

            for (int routeIndex = 0; routeIndex < map.routes.Length; routeIndex++)
            {
                Vector3[] route = map.routes[routeIndex];
                Color routeColor = routeIndex == 0 ? accent : Color.Lerp(accent, Color.white, 0.28f);
                for (int point = 0; point < route.Length - 1; point++)
                {
                    CreateLine($"Route {routeIndex + 1}.{point + 1}", Project(route[point], bounds),
                        Project(route[point + 1], bounds), routeColor, routeIndex == 0 ? 7f : 5f);
                    RouteSegmentCount++;
                }
                CreateMarker($"Entrance {routeIndex + 1}", Project(route[0], bounds), new Vector2(24f, 24f),
                    UiTokens.Danger, 45f);
                CreateNumber(routeIndex + 1, Project(route[0], bounds));
                EntranceMarkerCount++;
            }

            if (map.outpostSlots != null)
            {
                for (int index = 0; index < map.outpostSlots.Length; index++)
                {
                    CreateMarker($"Tower Slot {index + 1}", Project(map.outpostSlots[index], bounds),
                        new Vector2(14f, 14f), UiTokens.Moon, 0f);
                    SlotMarkerCount++;
                }
            }

            Vector3[] primary = map.PrimaryRoute;
            if (primary.Length > 0)
                CreateMarker("Keep", Project(primary[^1], bounds), new Vector2(28f, 28f),
                    UiTokens.Gold, 0f);
        }

        private static Rect CalculateWorldBounds(MapDefinition definition)
        {
            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minZ = float.MaxValue;
            float maxZ = float.MinValue;
            var points = new List<Vector3>();
            foreach (Vector3[] route in definition.routes)
                if (route != null) points.AddRange(route);
            if (definition.outpostSlots != null) points.AddRange(definition.outpostSlots);
            foreach (Vector3 point in points)
            {
                minX = Mathf.Min(minX, point.x);
                maxX = Mathf.Max(maxX, point.x);
                minZ = Mathf.Min(minZ, point.z);
                maxZ = Mathf.Max(maxZ, point.z);
            }
            if (points.Count == 0) return new Rect(0f, 0f, 1f, 1f);
            return Rect.MinMaxRect(minX, minZ, Mathf.Max(minX + 0.01f, maxX), Mathf.Max(minZ + 0.01f, maxZ));
        }

        private Vector2 Project(Vector3 world, Rect bounds)
        {
            float width = Mathf.Max(1f, root.rect.width - Padding * 2f);
            float height = Mathf.Max(1f, root.rect.height - Padding * 2f);
            float x = Mathf.InverseLerp(bounds.xMin, bounds.xMax, world.x);
            float y = Mathf.InverseLerp(bounds.yMin, bounds.yMax, world.z);
            return new Vector2((x - 0.5f) * width, (y - 0.5f) * height);
        }

        private void CreateLine(string objectName, Vector2 from, Vector2 to, Color color, float thickness)
        {
            Image image = CreateImage(objectName, color);
            RectTransform rect = image.rectTransform;
            Vector2 delta = to - from;
            rect.anchoredPosition = (from + to) * 0.5f;
            rect.sizeDelta = new Vector2(delta.magnitude, thickness);
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            rect.SetAsFirstSibling();
        }

        private void CreateMarker(string objectName, Vector2 position, Vector2 size, Color color, float rotation)
        {
            Image image = CreateImage(objectName, color);
            image.rectTransform.anchoredPosition = position;
            image.rectTransform.sizeDelta = size;
            image.rectTransform.localRotation = Quaternion.Euler(0f, 0f, rotation);
        }

        private Image CreateImage(string objectName, Color color)
        {
            var item = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            item.transform.SetParent(transform, false);
            RectTransform rect = item.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            Image image = item.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private void CreateNumber(int value, Vector2 position)
        {
            var item = new GameObject($"Entrance Label {value}", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            item.transform.SetParent(transform, false);
            RectTransform rect = item.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(24f, 24f);
            TextMeshProUGUI label = item.GetComponent<TextMeshProUGUI>();
            label.text = value.ToString();
            label.font = font;
            label.fontSize = 13f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;
        }
    }
}
