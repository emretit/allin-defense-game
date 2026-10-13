using System.Collections.Generic;
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

        public static void Install(GameObject root, DefenderPresentationCatalog catalog)
        {
            if (plinthMesh == null) plinthMesh = BuildMesh(false);
            if (ringMesh == null) ringMesh = BuildMesh(true);
            if (stoneMaterial == null)
            {
                stoneMaterial = new Material(catalog.stoneMaterial) { name = "Slot Neutral Stone", color = new Color(.40f, .40f, .37f) };
                restingRingMaterial = new Material(catalog.fireMaterial) { name = "Slot Resting Gold", color = UiTokens.GoldLow * .72f };
                restingRingMaterial.SetColor("_EmissionColor", Color.black);
                selectedRingMaterial = new Material(catalog.fireMaterial) { name = "Slot Selected Gold", color = UiTokens.GoldHigh };
                selectedRingMaterial.SetColor("_EmissionColor", UiTokens.Gold * 1.5f);
            }
            instances++;
            var visual = root.AddComponent<DefenderSlotVisual>();
            CreatePart(root.transform, "SlotStone", plinthMesh, stoneMaterial);
            visual.ring = CreatePart(root.transform, "SlotGoldRing", ringMesh, restingRingMaterial);
            visual.SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            ring.sharedMaterial = selected ? selectedRingMaterial : restingRingMaterial;
            ring.transform.localScale = selected ? new Vector3(1.6f, 1f, 1.6f) : Vector3.one;
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
                    vertices.Add(new Vector3(Mathf.Cos(angle) * radii[row], heights[row], Mathf.Sin(angle) * radii[row]));
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
