using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace DefenderChronicles.Presentation
{
    /// <summary>
    /// Adds production-only scenic geometry around the authored battlefield. All meshes are cosmetic:
    /// they have no colliders and never participate in navigation, targeting or combat timing.
    /// </summary>
    public sealed class DefenderBattlefieldDressing : MonoBehaviour
    {
        private static readonly Vector3[] RoadPath =
        {
            new(-2f, 0f, 2f), new(4f, 0f, 2f), new(4f, 0f, 8f), new(10f, 0f, 8f),
            new(10f, 0f, 3.5f), new(16f, 0f, 3.5f), new(16f, 0f, 9f), new(24f, 0f, 9f)
        };

        private readonly List<Mesh> runtimeMeshes = new();
        private readonly List<Material> runtimeMaterials = new();

        public static DefenderBattlefieldDressing Install(Transform parent, DefenderPresentationCatalog catalog,
            Camera camera)
        {
            if (parent == null || catalog == null) return null;
            Transform existing = parent.Find("Scenic Battlefield Dressing");
            if (existing != null) return existing.GetComponent<DefenderBattlefieldDressing>();

            var root = new GameObject("Scenic Battlefield Dressing");
            root.transform.SetParent(parent, false);
            DefenderBattlefieldDressing dressing = root.AddComponent<DefenderBattlefieldDressing>();
            dressing.Build(catalog, camera);
            return dressing;
        }

        private void Build(DefenderPresentationCatalog catalog, Camera camera)
        {
            CreateCliffSkirt(catalog.darkMaterial);
            CreateRoadCobbles(catalog.stoneMaterial);
            CreateRoadWear(catalog.earthMaterial);
            CreateNightBackdrop(camera);
        }

        private void CreateCliffSkirt(Material material)
        {
            Vector3[] rim =
            {
                new(-3.1f, -0.18f, -1.85f), new(1.4f, -0.12f, -2.05f), new(6.4f, -0.17f, -1.82f),
                new(11.5f, -0.12f, -2.03f), new(16.8f, -0.17f, -1.82f), new(21.8f, -0.13f, -2.02f),
                new(25.2f, -0.18f, -1.72f), new(25.4f, -0.15f, 2.8f), new(25.15f, -0.12f, 7.4f),
                new(25.35f, -0.17f, 12.9f), new(20.4f, -0.13f, 13.15f), new(15.3f, -0.18f, 12.9f),
                new(10.2f, -0.12f, 13.14f), new(5.1f, -0.17f, 12.92f), new(-0.2f, -0.12f, 13.12f),
                new(-3.15f, -0.18f, 12.82f), new(-3.4f, -0.14f, 7.5f), new(-3.2f, -0.18f, 2.4f)
            };
            var vertices = new List<Vector3>(rim.Length * 2);
            var triangles = new List<int>(rim.Length * 12);
            for (int i = 0; i < rim.Length; i++)
            {
                Vector3 top = rim[i];
                float depth = 1.0f + 0.24f * Mathf.Sin(i * 2.13f);
                vertices.Add(top);
                vertices.Add(top + Vector3.down * depth);
            }
            for (int i = 0; i < rim.Length; i++)
            {
                int next = (i + 1) % rim.Length;
                AddDoubleSidedQuad(triangles, i * 2, next * 2, next * 2 + 1, i * 2 + 1);
            }
            CreateMeshObject("Dark Irregular Cliff Skirt", vertices, triangles, material, true);
        }

        private void CreateRoadCobbles(Material material)
        {
            Material cobble = CreateRuntimeMaterial("DC Road Cobble", new Color(0.40f, 0.36f, 0.32f), false, material);
            var vertices = new List<Vector3>(8000);
            var triangles = new List<int>(12000);
            int stoneIndex = 0;
            for (int segment = 0; segment < RoadPath.Length - 1; segment++)
            {
                Vector3 from = RoadPath[segment];
                Vector3 to = RoadPath[segment + 1];
                Vector3 direction = (to - from).normalized;
                Vector3 side = new(-direction.z, 0f, direction.x);
                float length = Vector3.Distance(from, to);
                int count = Mathf.Max(1, Mathf.CeilToInt(length / 0.52f));
                for (int i = 0; i <= count; i++)
                {
                    float distance = Mathf.Min(length, i * length / count);
                    Vector3 along = from + direction * distance;
                    for (int edge = -1; edge <= 1; edge += 2)
                    {
                        float wave = Mathf.Sin((stoneIndex + 1) * 4.73f);
                        Vector3 center = along + side * (edge * (0.92f + wave * 0.025f));
                        center += direction * Mathf.Sin((stoneIndex + 3) * 2.19f) * 0.035f;
                        center.y = 0.105f + Mathf.Abs(wave) * 0.025f;
                        float yaw = Mathf.Atan2(direction.z, direction.x) + wave * 0.08f;
                        AddCobble(vertices, triangles, center, yaw,
                            new Vector3(0.43f + wave * 0.035f, 0.19f + Mathf.Abs(wave) * 0.035f,
                                0.27f - wave * 0.02f));
                        stoneIndex++;
                    }
                }
            }
            CreateMeshObject("Road Hand-Laid Stone Borders", vertices, triangles, cobble, true);
        }

        private void CreateRoadWear(Material source)
        {
            Material wear = CreateRuntimeMaterial("DC Road Wear", new Color(0.46f, 0.33f, 0.23f), false, source);
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            int patch = 0;
            for (int segment = 0; segment < RoadPath.Length - 1; segment++)
            {
                Vector3 from = RoadPath[segment];
                Vector3 to = RoadPath[segment + 1];
                Vector3 direction = (to - from).normalized;
                Vector3 side = new(-direction.z, 0f, direction.x);
                float length = Vector3.Distance(from, to);
                for (float distance = 0.85f; distance < length - 0.35f; distance += 1.35f)
                {
                    float wave = Mathf.Sin((patch + 2) * 3.71f);
                    Vector3 center = from + direction * distance + side * wave * 0.22f;
                    center.y = 0.153f;
                    AddFlatPatch(vertices, triangles, center, direction, side,
                        0.17f + Mathf.Abs(wave) * 0.11f, 0.035f + Mathf.Abs(wave) * 0.025f);
                    patch++;
                }
            }
            CreateMeshObject("Road Travel Wear", vertices, triangles, wear, false);
        }

        private void CreateNightBackdrop(Camera camera)
        {
            Material farMountain = CreateRuntimeMaterial("DC Far Mountain", new Color(0.08f, 0.19f, 0.32f), true);
            Material nearMountain = CreateRuntimeMaterial("DC Near Mountain", new Color(0.035f, 0.11f, 0.19f), true);
            Material moon = CreateRuntimeMaterial("DC Moon", new Color(0.82f, 1.08f, 1.42f), true);
            Material moonDetail = CreateRuntimeMaterial("DC Moon Craters", new Color(0.34f, 0.56f, 0.78f), true);

            CreateMountainLayer("Far Moonlit Mountains", 14.2f, -0.65f, 2.2f,
                new[]
                {
                    0.18f, 0.44f, 0.27f, 0.62f, 0.31f, 0.76f, 0.38f, 0.56f, 0.24f, 0.83f, 0.35f,
                    0.67f, 0.28f, 0.91f, 0.42f, 0.63f, 0.26f, 0.74f, 0.34f, 0.51f, 0.19f
                },
                farMountain);
            CreateMountainLayer("Near Pine Ridge Silhouette", 13.15f, -0.75f, 1.55f,
                new[]
                {
                    0.22f, 0.47f, 0.29f, 0.58f, 0.26f, 0.68f, 0.35f, 0.52f, 0.24f, 0.62f, 0.31f,
                    0.71f, 0.27f, 0.55f, 0.23f, 0.66f, 0.34f, 0.49f, 0.25f, 0.57f, 0.21f
                },
                nearMountain);

            GameObject moonObject = new("Moon Disc");
            moonObject.transform.SetParent(transform, false);
            moonObject.transform.localPosition = new Vector3(-2.8f, 0.9f, 13.1f);
            if (camera != null)
                moonObject.transform.rotation = Quaternion.LookRotation(-camera.transform.forward, camera.transform.up);
            Mesh disc = BuildDisc(40);
            runtimeMeshes.Add(disc);
            MeshFilter filter = moonObject.AddComponent<MeshFilter>();
            filter.sharedMesh = disc;
            MeshRenderer renderer = moonObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = moon;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            moonObject.transform.localScale = Vector3.one * 0.74f;
            CreateMoonMark(moonObject.transform, disc, moonDetail, new Vector3(-0.28f, 0.23f, 0.012f), 0.18f);
            CreateMoonMark(moonObject.transform, disc, moonDetail, new Vector3(0.26f, 0.10f, 0.012f), 0.13f);
            CreateMoonMark(moonObject.transform, disc, moonDetail, new Vector3(-0.02f, -0.30f, 0.012f), 0.21f);
            CreateMoonMark(moonObject.transform, disc, moonDetail, new Vector3(0.36f, -0.34f, 0.012f), 0.09f);
        }

        private static void CreateMoonMark(Transform parent, Mesh mesh, Material material, Vector3 localPosition,
            float scale)
        {
            var mark = new GameObject("Moon Crater");
            mark.transform.SetParent(parent, false);
            mark.transform.localPosition = localPosition;
            mark.transform.localScale = Vector3.one * scale;
            MeshFilter filter = mark.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            MeshRenderer renderer = mark.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private void CreateMountainLayer(string objectName, float z, float bottom, float height, float[] profile,
            Material material)
        {
            const float minX = -7f;
            const float maxX = 31f;
            var vertices = new List<Vector3>(profile.Length * 2);
            var triangles = new List<int>((profile.Length - 1) * 12);
            for (int i = 0; i < profile.Length; i++)
            {
                float x = Mathf.Lerp(minX, maxX, i / (float)(profile.Length - 1));
                vertices.Add(new Vector3(x, bottom, z));
                vertices.Add(new Vector3(x, bottom + height * profile[i], z));
            }
            for (int i = 0; i < profile.Length - 1; i++)
                AddDoubleSidedQuad(triangles, i * 2, (i + 1) * 2, (i + 1) * 2 + 1, i * 2 + 1);
            CreateMeshObject(objectName, vertices, triangles, material, false);
        }

        private void CreateMeshObject(string objectName, List<Vector3> vertices, List<int> triangles,
            Material material, bool receiveShadows)
        {
            var mesh = new Mesh { name = objectName + " Mesh", indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            runtimeMeshes.Add(mesh);

            var go = new GameObject(objectName);
            go.transform.SetParent(transform, false);
            MeshFilter filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = receiveShadows;
        }

        private Material CreateRuntimeMaterial(string materialName, Color color, bool unlit, Material source = null)
        {
            Shader shader = unlit ? Shader.Find("Universal Render Pipeline/Unlit") : null;
            Material material = source != null ? new Material(source) : new Material(shader ?? Shader.Find("Universal Render Pipeline/Lit"));
            material.name = materialName;
            material.color = color;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_Cull")) material.SetFloat("_Cull", 0f);
            runtimeMaterials.Add(material);
            return material;
        }

        private static void AddCobble(List<Vector3> vertices, List<int> triangles, Vector3 center, float yaw,
            Vector3 size)
        {
            int start = vertices.Count;
            float c = Mathf.Cos(yaw);
            float s = Mathf.Sin(yaw);
            float halfX = size.x * 0.5f;
            float halfZ = size.z * 0.5f;
            float chamfer = Mathf.Min(halfX, halfZ) * 0.3f;
            for (int level = 0; level < 2; level++)
            {
                float inset = level == 0 ? 1f : 0.86f;
                float y = level == 0 ? -size.y * 0.5f : size.y * 0.5f;
                for (int i = 0; i < 8; i++)
                {
                    Vector2 point = i switch
                    {
                        0 => new Vector2(-halfX + chamfer, -halfZ),
                        1 => new Vector2(halfX - chamfer, -halfZ),
                        2 => new Vector2(halfX, -halfZ + chamfer),
                        3 => new Vector2(halfX, halfZ - chamfer),
                        4 => new Vector2(halfX - chamfer, halfZ),
                        5 => new Vector2(-halfX + chamfer, halfZ),
                        6 => new Vector2(-halfX, halfZ - chamfer),
                        _ => new Vector2(-halfX, -halfZ + chamfer)
                    };
                    float x = point.x * inset;
                    float z = point.y * inset;
                    vertices.Add(center + new Vector3(x * c - z * s, y, x * s + z * c));
                }
            }
            for (int i = 1; i < 7; i++)
            {
                triangles.Add(start + 8);
                triangles.Add(start + 8 + i + 1);
                triangles.Add(start + 8 + i);
            }
            for (int i = 0; i < 8; i++)
            {
                int next = (i + 1) % 8;
                triangles.Add(start + i);
                triangles.Add(start + next + 8);
                triangles.Add(start + i + 8);
                triangles.Add(start + i);
                triangles.Add(start + next);
                triangles.Add(start + next + 8);
            }
        }

        private static void AddFlatPatch(List<Vector3> vertices, List<int> triangles, Vector3 center,
            Vector3 direction, Vector3 side, float halfLength, float halfWidth)
        {
            int start = vertices.Count;
            vertices.Add(center - direction * halfLength - side * halfWidth);
            vertices.Add(center + direction * halfLength - side * halfWidth * 0.72f);
            vertices.Add(center + direction * halfLength * 0.78f + side * halfWidth);
            vertices.Add(center - direction * halfLength * 0.85f + side * halfWidth * 0.65f);
            triangles.Add(start);
            triangles.Add(start + 2);
            triangles.Add(start + 1);
            triangles.Add(start);
            triangles.Add(start + 3);
            triangles.Add(start + 2);
        }

        private static Mesh BuildDisc(int segments)
        {
            var vertices = new Vector3[segments + 1];
            var triangles = new int[segments * 3];
            vertices[0] = Vector3.zero;
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                vertices[i + 1] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = (i + 1) % segments + 1;
            }
            var mesh = new Mesh { name = "Moon Disc Mesh", vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddDoubleSidedQuad(List<int> triangles, int a, int b, int c, int d)
        {
            triangles.Add(a); triangles.Add(b); triangles.Add(c);
            triangles.Add(a); triangles.Add(c); triangles.Add(d);
            triangles.Add(a); triangles.Add(c); triangles.Add(b);
            triangles.Add(a); triangles.Add(d); triangles.Add(c);
        }

        private void OnDestroy()
        {
            foreach (Mesh mesh in runtimeMeshes)
                if (mesh != null) Destroy(mesh);
            foreach (Material material in runtimeMaterials)
                if (material != null) Destroy(material);
        }
    }
}
