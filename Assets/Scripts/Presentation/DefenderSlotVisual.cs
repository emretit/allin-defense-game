using System.Collections.Generic;
using AllinDefense.Core;
using AllinDefense.Data;
using AllinDefense.UI;
using UnityEngine;
using UnityEngine.Rendering;

namespace AllinDefense.Presentation
{
    public sealed class DefenderSlotVisual : MonoBehaviour
    {
        private static Mesh plinthMesh;
        private static Mesh ringMesh;
        private static Material stoneMaterial;
        private static Material restingRingMaterial;
        private static Material selectedRingMaterial;
        private static int instances;
        private Renderer ring;
        private GameObject plus;
        private bool selected, occupied;
        private MaterialPropertyBlock ringProperties;
        public bool ShowsBuildPlus => plus != null && plus.activeSelf;
        public const float VisualScale = 1.38f;
        public const float MinimumTouchPoints = 56;

        public static void Install(GameObject root, DefenderPresentationCatalog catalog)
        {
            if (plinthMesh == null) plinthMesh = BuildMesh(false);
            if (ringMesh == null) ringMesh = BuildMesh(true);
            if (stoneMaterial == null)
            {
                stoneMaterial = new Material(catalog.stoneMaterial) { name = "Slot Neutral Stone", color = new Color(.40f, .40f, .37f) };
                restingRingMaterial = new Material(catalog.fireMaterial) { name = "Slot Resting Gold", color = UiTokens.GoldLow * .72f };
                restingRingMaterial.EnableKeyword("_EMISSION");
                restingRingMaterial.SetColor("_EmissionColor", Color.black);
                selectedRingMaterial = new Material(catalog.fireMaterial) { name = "Slot Selected Gold", color = UiTokens.GoldHigh };
                selectedRingMaterial.EnableKeyword("_EMISSION");
                selectedRingMaterial.SetColor("_EmissionColor", UiTokens.Gold * 1.5f);
            }
            instances++;
            var visual = root.AddComponent<DefenderSlotVisual>();
            CreatePart(root.transform, "SlotStone", plinthMesh, stoneMaterial);
            visual.ring = CreatePart(root.transform, "SlotGoldRing", ringMesh, restingRingMaterial);
            var marker = new GameObject("SlotBuildPlus"); marker.transform.SetParent(root.transform, false);
            foreach (var size in new[] { new Vector3(.36f, .018f, .075f), new Vector3(.075f, .018f, .36f) })
            {
                var bar = GameObject.CreatePrimitive(PrimitiveType.Cube); bar.name = "PlusBar";
                bar.transform.SetParent(marker.transform, false); bar.transform.localPosition = Vector3.up * .235f; bar.transform.localScale = size;
                Destroy(bar.GetComponent<Collider>());
                bar.GetComponent<Renderer>().sharedMaterial = selectedRingMaterial;
            }
            visual.plus = marker;
            visual.ringProperties = new MaterialPropertyBlock();
            visual.SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            this.selected = selected;
            ring.sharedMaterial = selected ? selectedRingMaterial : restingRingMaterial;
            ring.transform.localScale = selected ? new Vector3(1.2f, 1f, 1.2f) : Vector3.one;
            RefreshRing();
        }

        public void SetOccupied(bool value) { occupied = value; plus.SetActive(!value); RefreshRing(); }

        private void Update() => RefreshRing();

        private void RefreshRing()
        {
            if (ring == null || ringProperties == null) return;
            var session = GameSessionController.Instance;
            bool affordable = session != null && session.Gold >= TowerProgression.Base(OutpostKind.Archer).cost;
            float pulse = occupied || !affordable || FrontendPreferences.ReducedMotion ? 1 : 1 + .18f * Mathf.Sin(Time.unscaledTime * 2);
            Color color = selected ? UiTokens.GoldHigh : occupied ? UiTokens.GoldLow * .72f : affordable ? UiTokens.Moon : UiTokens.TextSecondary;
            ringProperties.SetColor("_BaseColor", color);
            ringProperties.SetColor("_EmissionColor", selected ? UiTokens.Gold * 1.5f : occupied ? Color.black : color * (affordable ? pulse : .35f));
            ring.SetPropertyBlock(ringProperties);
        }

        public static bool ContainsTouchPoint(Transform slot, Camera camera, Vector2 point)
        {
            var bounds = TouchScreenRect(slot, camera);
            Vector2 delta = point - bounds.center;
            delta = new Vector2(delta.x / (bounds.width / 2), delta.y / (bounds.height / 2));
            return delta.sqrMagnitude <= 1;
        }

        public static Rect TouchScreenRect(Transform slot, Camera camera)
        {
            Vector2 center = camera.WorldToScreenPoint(slot.position);
            Vector2 pixelsPerPoint = new Vector2(Screen.width / UiMetrics.LogicalScreenSize.x, Screen.height / UiMetrics.LogicalScreenSize.y);
            Vector2 size = pixelsPerPoint * MinimumTouchPoints;
            float radius = .62f * VisualScale;
            foreach (var direction in new[] { Vector3.right, Vector3.forward })
            {
                Vector2 delta = (Vector2)camera.WorldToScreenPoint(slot.position + direction * radius) - center;
                size.x = Mathf.Max(size.x, Mathf.Abs(delta.x) * 2);
                size.y = Mathf.Max(size.y, Mathf.Abs(delta.y) * 2);
            }
            return new Rect(center - size / 2, size);
        }

        private void OnDestroy()
        {
            if (--instances != 0) return;
            Destroy(stoneMaterial);
            Destroy(restingRingMaterial);
            Destroy(selectedRingMaterial);
            Destroy(plinthMesh);
            Destroy(ringMesh);
        }

        private static Renderer CreatePart(Transform parent, string name, Mesh mesh, Material material)
        {
            var part = new GameObject(name);
            part.transform.SetParent(parent, false);
            part.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = part.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = true;
            return renderer;
        }

        private static Mesh BuildMesh(bool ring)
        {
            int segments = ring ? 32 : 12;
            float[] radii = ring ? new[] { .535f, .57f } : new[] { .62f, .62f, .56f, 0f };
            float[] heights = ring ? new[] { .222f, .222f } : new[] { .025f, .15f, .22f, .22f };
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            for (int row = 0; row < radii.Length; row++)
                for (int i = 0; i < segments; i++)
                {
                    float angle = i * Mathf.PI * 2f / segments;
                    vertices.Add(new Vector3(Mathf.Cos(angle) * radii[row] * VisualScale, heights[row], Mathf.Sin(angle) * radii[row] * VisualScale));
                }
            for (int row = 0; row < radii.Length - 1; row++)
                for (int i = 0; i < segments; i++)
                {
                    int a = row * segments + i, b = row * segments + (i + 1) % segments;
                    if (ring) triangles.AddRange(new[] { a, b, a + segments, b, b + segments, a + segments });
                    else triangles.AddRange(new[] { a, a + segments, b, b, a + segments, b + segments });
                }
            var mesh = new Mesh { name = ring ? "Slot Gold Annulus" : "Slot Beveled Stone" };
            var flatVertices = new Vector3[triangles.Count];
            var flatTriangles = new int[triangles.Count];
            for (int i = 0; i < triangles.Count; i++)
            {
                flatVertices[i] = vertices[triangles[i]];
                flatTriangles[i] = i;
            }
            mesh.vertices = flatVertices;
            mesh.triangles = flatTriangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
