using System;
using System.Collections.Generic;
using AllinDefense.Data;
using UnityEngine;
using UnityEngine.Rendering;

namespace AllinDefense.Presentation
{
    /// <summary>
    /// Adds production-only scenic geometry around the authored battlefield. All meshes are cosmetic:
    /// they have no colliders and never participate in navigation, targeting or combat timing.
    /// </summary>
    public sealed class DefenderBattlefieldDressing : MonoBehaviour
    {
        private static readonly Vector3[] DefaultRoadPath =
        {
            new(-2f, 0f, 2f), new(4f, 0f, 2f), new(4f, 0f, 8f), new(10f, 0f, 8f),
            new(10f, 0f, 3.5f), new(16f, 0f, 3.5f), new(16f, 0f, 9f), new(24f, 0f, 9f)
        };

        private Texture2D groundTexture;
        private Texture2D roadTexture;
        private Texture2D featherTexture;
        private IReadOnlyList<Vector3> slots;
        private readonly List<List<Vector3>> roundedPaths = new();
        private readonly List<Mesh> runtimeMeshes = new();
        private readonly List<Material> runtimeMaterials = new();
        private IReadOnlyList<Vector3> roadPath = DefaultRoadPath;
        private int biomeIndex;
        private IReadOnlyList<Vector3[]> routes;
        private readonly List<IReadOnlyList<Vector3>> ownedRoadPaths = new();

        public static DefenderBattlefieldDressing Install(Transform parent, DefenderPresentationCatalog catalog,
            Camera camera, IReadOnlyList<Vector3> path = null, int biomeIndex = 0,
            IReadOnlyList<Vector3[]> routes = null, IReadOnlyList<Vector3> slots = null)
        {
            if (parent == null || catalog == null) return null;
            Transform existing = parent.Find("Scenic Battlefield Dressing");
            if (existing != null) return existing.GetComponent<DefenderBattlefieldDressing>();

            var root = new GameObject("Scenic Battlefield Dressing");
            root.transform.SetParent(parent, false);
            DefenderBattlefieldDressing dressing = root.AddComponent<DefenderBattlefieldDressing>();
            if (path != null && path.Count > 1) dressing.roadPath = path;
            dressing.biomeIndex = Mathf.Clamp(biomeIndex, 0, 4);
            dressing.routes = routes;
            dressing.slots = slots;
            dressing.ownedRoadPaths.Add(dressing.roadPath);
            if (routes != null)
                for (int index = 1; index < routes.Count; index++)
                {
                    int count = CampaignLevelFactory.OwnedBranchSegments(routes[index], dressing.roadPath);
                    if (count == 0) continue;
                    var owned = new Vector3[count + 1];
                    for (int point = 0; point <= count; point++) owned[point] = routes[index][point];
                    dressing.ownedRoadPaths.Add(owned);
                }
            dressing.Build(catalog, camera);
            return dressing;
        }

        public const float DecorationSlotClearance = 1.1f;
        public const float DecorationRouteClearance = 1.2f;

        public static bool IsDecoration(Transform piece, Transform root)
        {
            bool decoration = false;
            for (Transform current = piece; current != null && current != root; current = current.parent)
            {
                string name = current.name;
                if (Contains(name, "Ground") || Contains(name, "Ridge") || Contains(name, "Backdrop"))
                    return false;
                decoration |= Contains(name, "Tree") || Contains(name, "Pine") || Contains(name, "Rock") ||
                              Contains(name, "Bush") || Contains(name, "Shrub") || Contains(name, "Landmark");
            }
            return decoration;
        }

        private static bool Contains(string name, string token) =>
            name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;

        /// <summary>Tests the visible footprint, including overhanging foliage, on the gameplay XZ plane.</summary>
        public static bool DecorationOverlapsGameplay(Bounds bounds, IReadOnlyList<Vector3> slots,
            IReadOnlyList<Vector3[]> routes)
        {
            if (slots != null)
                foreach (Vector3 slot in slots)
                {
                    Vector3 nearest = bounds.ClosestPoint(slot);
                    Vector2 delta = new(nearest.x - slot.x, nearest.z - slot.z);
                    if (delta.sqrMagnitude <= DecorationSlotClearance * DecorationSlotClearance) return true;
                }
            if (routes != null)
            {
                // A padded footprint is conservative at corners and also catches a route crossing
                // a large prop whose pivot and bounding-box centre are both outside the corridor.
                Vector2 min = new(bounds.min.x - DecorationRouteClearance, bounds.min.z - DecorationRouteClearance);
                Vector2 max = new(bounds.max.x + DecorationRouteClearance, bounds.max.z + DecorationRouteClearance);
                foreach (IReadOnlyList<Vector3> route in routes)
                    for (int segment = 0; segment < route.Count - 1; segment++)
                        if (SegmentIntersectsFootprint(route[segment], route[segment + 1], min, max)) return true;
            }
            return false;
        }

        private static bool SegmentIntersectsFootprint(Vector3 from, Vector3 to, Vector2 min, Vector2 max)
        {
            float enter = 0f;
            float exit = 1f;
            for (int axis = 0; axis < 2; axis++)
            {
                float origin = axis == 0 ? from.x : from.z;
                float delta = axis == 0 ? to.x - from.x : to.z - from.z;
                if (Mathf.Abs(delta) < 0.0001f)
                {
                    if (origin < min[axis] || origin > max[axis]) return false;
                    continue;
                }
                float a = (min[axis] - origin) / delta;
                float b = (max[axis] - origin) / delta;
                enter = Mathf.Max(enter, Mathf.Min(a, b));
                exit = Mathf.Min(exit, Mathf.Max(a, b));
                if (enter > exit) return false;
            }
            return true;
        }

        public static void HideObstructingDecoration(Transform root, IReadOnlyList<Vector3> slots,
            IReadOnlyList<Vector3[]> routes)
        {
            if (root == null) return;
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                if (IsDecoration(renderer.transform, root) && DecorationOverlapsGameplay(renderer.bounds, slots, routes))
                    renderer.enabled = false;
        }

        /// <summary>Open the incoming side of a T junction while preserving its opposite primary border.</summary>
        public static bool ShouldOmitRoadBorder(Vector3 center, int routeIndex, IReadOnlyList<Vector3[]> routes)
        {
            if (routes == null || routes.Count < 2) return false;
            if (routeIndex > 0 && CampaignLevelFactory.DistanceToRoute(center, routes[0]) < 1.12f) return true;
            for (int index = 1; index < routes.Count; index++)
            {
                if (index == routeIndex) continue;
                IReadOnlyList<Vector3> branch = routes[index];
                int owned = CampaignLevelFactory.OwnedBranchSegments(branch, routes[0]);
                if (owned == 0) continue;
                Vector3 junction = branch[owned];
                Vector3 outward = (branch[owned - 1] - junction).normalized;
                Vector3 offset = center - junction;
                float incomingSide = Vector3.Dot(offset, outward);
                float across = Mathf.Abs(offset.x * outward.z - offset.z * outward.x);
                // Include the stone half-length so the lip cannot protrude into the mouth.
                if (incomingSide > 0.05f && incomingSide < 1.3f && across < 1.12f) return true;
            }
            return false;
        }

        private void Build(DefenderPresentationCatalog catalog, Camera camera)
        {
            foreach (var route in ownedRoadPaths) roundedPaths.Add(RoundRoute(route));
            CreateContinuousGround(catalog.forestMaterial);
            CreateSceneryClusters(catalog);
            CreateLandmarkReadabilityFields(catalog.fireMaterial, catalog.magicMaterial);
            CreateRoadSurface();
            CreateRoadCobbles();
            CreateRoadWear(catalog.earthMaterial);
            // The battle fills the screen with terrain; no raised board edge or sky band.
        }

        private void CreateContinuousGround(Material material)
        {
            const string name = "Continuous Battlefield Ground";
            material = CreateRuntimeMaterial("DC Moonlit Ground", new Color(0.21f, 0.25f, 0.25f), false, material);
            const int size = 128;
            groundTexture = new Texture2D(size, size, TextureFormat.RGBA32, true)
                { name = "Battlefield Ground Tonal Noise", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)(size - 1), v = y / (float)(size - 1);
                    float edge = Mathf.SmoothStep(0f, 1f, Mathf.Max(Mathf.Abs(u - .5f) * 3.3f, Mathf.Abs(v - .5f) * 4f));
                    float noise = Mathf.PerlinNoise(u * 75f + 6f, v * 75f + 6f);
                    pixels[y * size + x] = Color.Lerp(new Color(.79f, .87f, .86f), new Color(.29f, .39f, .48f), edge) * (.92f + noise * .16f);
                }
            groundTexture.SetPixels(pixels);
            groundTexture.Apply(true, true);
            material.SetTexture("_BaseMap", groundTexture);
            // Extend well beyond all supported camera frames, under the HUD and device insets.
            // Match the authored ground height without adding a collider to the battle path.
            CreateMeshObject(name, new List<Vector3>
            {
                new(-39f, -0.145f, -44.5f), new(61f, -0.145f, -44.5f),
                new(61f, -0.145f, 55.5f), new(-39f, -0.145f, 55.5f)
            }, new List<int> { 0, 2, 1, 0, 3, 2 }, material, true);
            transform.Find(name).GetComponent<MeshRenderer>().receiveShadows = true;
        }

        public static bool DecorationOverlapsLandmarks(Bounds bounds, IReadOnlyList<Vector3> path)
        {
            foreach (Vector3 endpoint in new[] { path[0], path[^1] })
            {
                Vector3 nearest = bounds.ClosestPoint(endpoint);
                if (new Vector2(nearest.x - endpoint.x, nearest.z - endpoint.z).magnitude < 2f) return true;
            }
            return false;
        }

        private void CreateSceneryClusters(DefenderPresentationCatalog catalog)
        {
            var random = new System.Random(6006 + biomeIndex * 197 + Mathf.RoundToInt(roadPath[1].x * 100));
            float Next(float a, float b) => Mathf.Lerp(a, b, (float)random.NextDouble());
            Material foliage = CreateRuntimeMaterial("DC Grove Foliage", new Color(.16f, .29f, .22f), false);
            Material lightFoliage = CreateRuntimeMaterial("DC Grove Sage", new Color(.23f, .34f, .26f), false);
            Material bark = CreateRuntimeMaterial("DC Grove Bark", new Color(.22f, .20f, .17f), false);
            Material rock = CreateRuntimeMaterial("DC Grove Stone", new Color(.27f, .32f, .34f), false);
            Material grass = CreateRuntimeMaterial("DC Feathered Grass", new Color(.07f, .13f, .10f, .5f), true);
            featherTexture = new Texture2D(64, 64, TextureFormat.RGBA32, true)
                { name = "Grass Soft Edge", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var pixels = new Color[4096];
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                {
                    float radius = new Vector2((x - 31.5f) / 31.5f, (y - 31.5f) / 31.5f).magnitude;
                    float alpha = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.25f, .98f, radius + .06f * Mathf.PerlinNoise(x * .15f, y * .15f)));
                    pixels[y * 64 + x] = new Color(1, 1, 1, alpha);
                }
            featherTexture.SetPixels(pixels); featherTexture.Apply(true, true);
            grass.SetTexture("_BaseMap", featherTexture);
            grass.SetFloat("_Surface", 1); grass.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            grass.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha); grass.SetFloat("_ZWrite", 0);
            grass.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            grass.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            grass.SetOverrideTag("RenderType", "Transparent");
            grass.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); grass.renderQueue = (int)RenderQueue.Transparent;
            var patchVertices = new List<Vector3>(); var patchTriangles = new List<int>();
            var patchUvs = new List<Vector2>();
            var occupied = new List<Bounds>();
            for (int cluster = 0; cluster < 8; cluster++)
            {
                Vector3 center = Vector3.zero;
                bool placed = false;
                for (int attempt = 0; attempt < 24; attempt++)
                {
                    center = new Vector3(Next(.3f, 22f), 0, cluster % 2 == 0 ? Next(12f, 13.2f) : Next(-1.4f, -.2f));
                    var bounds = new Bounds(center + Vector3.up, new Vector3(2.7f, 3, 2.2f));
                    if (DecorationOverlapsGameplay(bounds, slots, routes) || DecorationOverlapsLandmarks(bounds, roadPath) ||
                        occupied.Exists(other => other.Intersects(bounds))) continue;
                    occupied.Add(bounds);
                    placed = true;
                    break;
                }
                if (!placed) continue;
                var crowns = new List<Vector3>(); var crownIndices = new List<int>();
                var trunks = new List<Vector3>(); var trunkIndices = new List<int>();
                var rocks = new List<Vector3>(); var rockIndices = new List<int>();
                for (int tree = 0; tree < 3; tree++)
                {
                    Vector3 p = center + new Vector3(Next(-.65f, .65f), 0, Next(-.45f, .45f));
                    float size = Next(.65f, 1.12f), yaw = Next(0, Mathf.PI * 2);
                    AddLathe(trunks, trunkIndices, p, new[] { .09f, .07f }, new[] { 0f, .75f * size }, yaw);
                    if ((cluster + tree) % 3 == 0)
                        AddLathe(crowns, crownIndices, p, new[] { 0f, .42f * size, .46f * size, .25f * size, 0f },
                            new[] { .45f * size, .65f * size, 1.05f * size, 1.4f * size, 1.5f * size }, yaw);
                    else
                    {
                        AddLathe(crowns, crownIndices, p, new[] { .50f * size, .02f }, new[] { .45f * size, 1.5f * size }, yaw);
                        AddLathe(crowns, crownIndices, p, new[] { .36f * size, 0f }, new[] { .92f * size, 1.95f * size }, yaw + .2f);
                    }
                    Vector3 stone = p + new Vector3(Next(-.35f, .35f), .1f, Next(-.25f, .25f));
                    AddCobble(rocks, rockIndices, stone, yaw, new Vector3(Next(.3f, .6f), Next(.2f, .4f), Next(.3f, .5f)));
                }
                CreateMeshObject($"Grove Tree Crowns {cluster}", crowns, crownIndices, cluster % 3 == 0 ? lightFoliage : foliage, true);
                CreateMeshObject($"Grove Tree Trunks {cluster}", trunks, trunkIndices, bark, true);
                CreateMeshObject($"Grove Rock Cluster {cluster}", rocks, rockIndices, rock, true);
                int start = patchVertices.Count;
                center.y = -.13f;
                patchVertices.Add(center + new Vector3(-1.9f, 0, -1.3f));
                patchVertices.Add(center + new Vector3(1.9f, 0, -1.3f));
                patchVertices.Add(center + new Vector3(1.9f, 0, 1.3f));
                patchVertices.Add(center + new Vector3(-1.9f, 0, 1.3f));
                patchTriangles.AddRange(new[] { start, start + 2, start + 1, start, start + 3, start + 2 });
                patchUvs.AddRange(new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up });
            }
            CreateMeshObject("Ground Feathered Grove Patches", patchVertices, patchTriangles, grass, false);
            transform.Find("Ground Feathered Grove Patches").GetComponent<MeshFilter>().sharedMesh.SetUVs(0, patchUvs);
        }

        private static void AddLathe(List<Vector3> vertices, List<int> triangles, Vector3 center,
            float[] radii, float[] heights, float yaw)
        {
            const int sides = 7;
            int start = vertices.Count;
            for (int row = 0; row < radii.Length; row++)
                for (int i = 0; i <= sides; i++)
                {
                    float angle = yaw + i * Mathf.PI * 2 / sides;
                    vertices.Add(center + new Vector3(Mathf.Cos(angle) * radii[row], heights[row], Mathf.Sin(angle) * radii[row]));
                }
            for (int row = 0; row < radii.Length - 1; row++)
                for (int i = 0; i < sides; i++)
                {
                    int a = start + row * (sides + 1) + i;
                    triangles.AddRange(new[] { a, a + sides + 1, a + 1, a + 1, a + sides + 1, a + sides + 2 });
                }
        }

        private void CreateLandmarkReadabilityFields(Material fireSource, Material magicSource)
        {
            Material gateRead = CreateRuntimeMaterial("DC Gate Readability Pool", new Color(0.22f, 0.08f, 0.045f), false, fireSource);
            Material keepRead = CreateRuntimeMaterial("DC Keep Readability Pool", new Color(0.036f, 0.125f, 0.185f), false, magicSource);

            var gateVertices = new List<Vector3>(32);
            var gateTriangles = new List<int>(60);
            AddGroundPatch(gateVertices, gateTriangles, roadPath[0] + new Vector3(-.05f, .086f, 0), 0.92f, 0.46f, 0f, 0.15f);
            AddGroundPatch(gateVertices, gateTriangles, roadPath[0] + new Vector3(1.15f, .084f, 0), 0.68f, 0.22f, 0f, 1.2f);
            CreateMeshObject("Enemy Gate Warm Read Pool", gateVertices, gateTriangles, gateRead, false);

            var keepVertices = new List<Vector3>(32);
            var keepTriangles = new List<int>(60);
            AddGroundPatch(keepVertices, keepTriangles, roadPath[^1] + new Vector3(-.75f, .086f, 0), 1.12f, 0.52f, 0f, 0.4f);
            AddGroundPatch(keepVertices, keepTriangles, roadPath[^1] + new Vector3(-2.1f, .084f, 0), 0.74f, 0.26f, 0f, 1.6f);
            CreateMeshObject("Defender Keep Cool Read Pool", keepVertices, keepTriangles, keepRead, false);
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

        private static List<Vector3> RoundRoute(IReadOnlyList<Vector3> route)
        {
            var points = new List<Vector3> { route[0] };
            void LineTo(Vector3 end)
            {
                Vector3 start = points[^1];
                int steps = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(start, end) / .55f));
                for (int i = 1; i <= steps; i++) points.Add(Vector3.Lerp(start, end, i / (float)steps));
            }
            for (int i = 1; i < route.Count - 1; i++)
            {
                Vector3 incoming = (route[i] - route[i - 1]).normalized;
                Vector3 outgoing = (route[i + 1] - route[i]).normalized;
                float radius = Mathf.Min(1.05f, Vector3.Distance(route[i], route[i - 1]) * .3f,
                    Vector3.Distance(route[i], route[i + 1]) * .3f);
                Vector3 a = route[i] - incoming * radius, b = route[i] + outgoing * radius;
                LineTo(a);
                for (int step = 1; step <= 6; step++)
                {
                    float t = step / 6f;
                    points.Add((1 - t) * (1 - t) * a + 2 * (1 - t) * t * route[i] + t * t * b);
                }
            }
            LineTo(route[^1]);
            return points;
        }

        private static Vector3 RoadSide(List<Vector3> points, int i)
        {
            Vector3 tangent = (points[Mathf.Min(i + 1, points.Count - 1)] - points[Mathf.Max(0, i - 1)]).normalized;
            return new Vector3(-tangent.z, 0, tangent.x);
        }

        private static void ConnectRows(List<int> triangles, int a, int width)
        {
            for (int j = 0; j < width - 1; j++)
                triangles.AddRange(new[] { a + j, a + j + 1, a + j + width,
                    a + j + 1, a + j + width + 1, a + j + width });
        }

        private void CreateRoadSurface()
        {
            Material surface = CreateRuntimeMaterial("DC Organic Road Surface", new Color(.43f, .40f, .33f), false);
            roadTexture = new Texture2D(64, 64, TextureFormat.RGBA32, true)
                { name = "Road Fine Wear", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
            var pixels = new Color[64 * 64];
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                {
                    float tone = .97f + Mathf.PerlinNoise(x * .17f, y * .17f) * .045f;
                    pixels[y * 64 + x] = new Color(tone, tone, tone, 1);
                }
            roadTexture.SetPixels(pixels); roadTexture.Apply(true, true);
            surface.SetTexture("_BaseMap", roadTexture);
            for (int route = 0; route < roundedPaths.Count; route++)
            {
                var vertices = new List<Vector3>(); var triangles = new List<int>();
                var points = roundedPaths[route];
                for (int i = 0; i < points.Count; i++)
                {
                    Vector3 center = points[i]; center.y = .083f;
                    Vector3 side = RoadSide(points, i) * (.79f + .012f * Mathf.Sin(i * 1.7f));
                    vertices.Add(center - side); vertices.Add(center + side);
                    if (i < points.Count - 1) ConnectRows(triangles, i * 2, 2);
                }
                CreateMeshObject(route == 0 ? "Road Organic Earth Surface" : "Road Surface", vertices, triangles, surface, true);
            }
        }

        private void CreateRoadCobbles()
        {
            Material stone = CreateRuntimeMaterial("DC Road Cobble", new Color(.30f, .33f, .32f), false);
            for (int route = 0; route < roundedPaths.Count; route++)
            {
                var vertices = new List<Vector3>(); var triangles = new List<int>();
                var points = roundedPaths[route];
                foreach (int edge in new[] { -1, 1 })
                {
                    int start = vertices.Count;
                    for (int i = 0; i < points.Count; i++)
                    {
                        Vector3 side = RoadSide(points, i) * edge;
                        float wobble = .015f * Mathf.Sin(i * 2.17f);
                        for (int r = 0; r < 4; r++)
                        {
                            int row = edge < 0 ? 3 - r : r;
                            float offset = row switch { 0 => .77f, 1 => .80f, 2 => .92f, _ => .96f };
                            Vector3 point = points[i] + side * (offset + wobble);
                            point.y = row == 0 || row == 3 ? .045f : .13f + .009f * Mathf.Sin(i * 1.31f);
                            vertices.Add(point);
                        }
                        if (i < points.Count - 1 &&
                            !ShouldOmitRoadBorder((points[i] + points[i + 1]) * .5f + side * .86f, route, routes))
                            ConnectRows(triangles, start + i * 4, 4);
                    }
                }
                CreateMeshObject(route == 0 ? "Road Hand-Laid Stone Borders" : "Road Edge", vertices, triangles, stone, true);
            }
        }

        private void CreateRoadWear(Material source)
        {
            Material wear = CreateRuntimeMaterial("DC Road Wear", new Color(.40f, .38f, .32f), false);
            foreach (var points in roundedPaths)
            {
                var vertices = new List<Vector3>(); var triangles = new List<int>();
                foreach (float track in new[] { -.34f, .34f })
                {
                    int start = vertices.Count;
                    for (int i = 0; i < points.Count; i++)
                    {
                        Vector3 center = points[i]; center.y = .087f;
                        Vector3 side = RoadSide(points, i);
                        vertices.Add(center + side * (track - .035f));
                        vertices.Add(center + side * (track + .035f));
                        if (i < points.Count - 1) ConnectRows(triangles, start + i * 2, 2);
                    }
                }
                CreateMeshObject("Road Travel Wear", vertices, triangles, wear, false);
            }
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
            if (objectName == "Continuous Battlefield Ground")
                mesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
            if (material.mainTexture == roadTexture && roadTexture != null)
                mesh.uv = vertices.ConvertAll(v => new Vector2(v.x * .7f, v.z * .7f)).ToArray();
            if (objectName.StartsWith("Grove ", StringComparison.Ordinal))
            {
                var flat = new Vector3[triangles.Count];
                var indices = new int[triangles.Count];
                for (int i = 0; i < triangles.Count; i++) { flat[i] = vertices[triangles[i]]; indices[i] = i; }
                mesh.Clear(); mesh.vertices = flat; mesh.triangles = indices;
            }
            mesh.RecalculateNormals();
            if (objectName == "Road Organic Earth Surface" || objectName == "Road Surface")
            {
                var normals = new List<Vector3>(vertices.Count);
                for (int i = 0; i < vertices.Count; i++) normals.Add(Vector3.up);
                mesh.SetNormals(normals);
            }
            mesh.RecalculateBounds();
            runtimeMeshes.Add(mesh);

            var go = new GameObject(objectName);
            go.transform.SetParent(transform, false);
            MeshFilter filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private Material CreateRuntimeMaterial(string materialName, Color color, bool unlit, Material source = null)
        {
            Shader shader = unlit ? Shader.Find("Universal Render Pipeline/Unlit") : null;
            Material material = source != null ? new Material(source) : new Material(shader ?? Shader.Find("Universal Render Pipeline/Lit"));
            material.name = materialName;
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", .06f);
            if (material.HasProperty("_SpecularHighlights")) material.SetFloat("_SpecularHighlights", 0);
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

        private static void AddGroundPatch(List<Vector3> vertices, List<int> triangles, Vector3 center,
            float halfLength, float halfWidth, float yawDegrees, float phase)
        {
            const int segments = 14;
            int start = vertices.Count;
            vertices.Add(center);
            float yaw = yawDegrees * Mathf.Deg2Rad;
            float c = Mathf.Cos(yaw);
            float s = Mathf.Sin(yaw);
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                float wobble = 1f + Mathf.Sin(angle * 2.7f + phase) * 0.10f + Mathf.Cos(angle * 5.1f + phase) * 0.045f;
                float localX = Mathf.Cos(angle) * halfLength * wobble;
                float localZ = Mathf.Sin(angle) * halfWidth * wobble;
                vertices.Add(center + new Vector3(localX * c - localZ * s, 0f, localX * s + localZ * c));
            }
            for (int i = 0; i < segments; i++)
            {
                triangles.Add(start);
                triangles.Add(start + 1 + ((i + 1) % segments));
                triangles.Add(start + 1 + i);
            }
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
            if (groundTexture != null) Destroy(groundTexture);
            if (roadTexture != null) Destroy(roadTexture);
            if (featherTexture != null) Destroy(featherTexture);
            foreach (Mesh mesh in runtimeMeshes)
                if (mesh != null) Destroy(mesh);
            foreach (Material material in runtimeMaterials)
                if (material != null) Destroy(material);
        }
    }
}
