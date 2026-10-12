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

        private readonly List<Mesh> runtimeMeshes = new();
        private readonly List<Material> runtimeMaterials = new();
        private IReadOnlyList<Vector3> roadPath = DefaultRoadPath;
        private int biomeIndex;
        private IReadOnlyList<Vector3[]> routes;
        private readonly List<IReadOnlyList<Vector3>> ownedRoadPaths = new();

        public static DefenderBattlefieldDressing Install(Transform parent, DefenderPresentationCatalog catalog,
            Camera camera, IReadOnlyList<Vector3> path = null, int biomeIndex = 0,
            IReadOnlyList<Vector3[]> routes = null)
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
            CreateContinuousGround(catalog.forestMaterial);
            CreateGroundValueFields(catalog.grassAccentMaterial, catalog.earthMaterial);
            CreateLandmarkReadabilityFields(catalog.fireMaterial, catalog.magicMaterial);
            CreateRoadSurface();
            CreateRoadCobbles();
            CreateRoadWear(catalog.earthMaterial);
            // The battle fills the screen with terrain; no raised board edge or sky band.
        }

        private void CreateContinuousGround(Material material)
        {
            const string name = "Continuous Battlefield Ground";
            // Extend well beyond all supported camera frames, under the HUD and device insets.
            // Match the authored ground height without adding a collider to the battle path.
            CreateMeshObject(name, new List<Vector3>
            {
                new(-39f, -0.145f, -44.5f), new(61f, -0.145f, -44.5f),
                new(61f, -0.145f, 55.5f), new(-39f, -0.145f, 55.5f)
            }, new List<int> { 0, 2, 1, 0, 3, 2 }, material, true);
            transform.Find(name).GetComponent<MeshRenderer>().receiveShadows = true;
        }

        private void CreateGroundValueFields(Material grassSource, Material earthSource)
        {
            BiomeDefinition palette = CampaignLevelFactory.Biome(biomeIndex);
            Material coldGrass = CreateRuntimeMaterial("DC Cold Grass Value", Color.Lerp(palette.groundColor, Color.black, 0.28f), false, grassSource);
            Material moonGrass = CreateRuntimeMaterial("DC Moon Grass Value", Color.Lerp(palette.groundColor, palette.accentColor, 0.22f), false, grassSource);
            Material warmScuff = CreateRuntimeMaterial("DC Gate Ground Scuff", Color.Lerp(palette.groundColor, new Color(0.08f, 0.05f, 0.03f), 0.48f), false, earthSource);

            var coldVertices = new List<Vector3>(96);
            var coldTriangles = new List<int>(180);
            AddGroundPatch(coldVertices, coldTriangles, new Vector3(2.5f, 0.068f, 5.3f), 2.35f, 0.56f, -6f, 0.12f);
            AddGroundPatch(coldVertices, coldTriangles, new Vector3(7.9f, 0.066f, 5.2f), 1.72f, 0.48f, 11f, 1.33f);
            AddGroundPatch(coldVertices, coldTriangles, new Vector3(13.1f, 0.067f, 6.4f), 2.12f, 0.58f, -12f, 2.25f);
            AddGroundPatch(coldVertices, coldTriangles, new Vector3(20.0f, 0.066f, 5.0f), 2.38f, 0.54f, 8f, 3.4f);
            CreateMeshObject("Ground Low-Contrast Valley Fields", coldVertices, coldTriangles, coldGrass, true);

            var moonVertices = new List<Vector3>(80);
            var moonTriangles = new List<int>(150);
            AddGroundPatch(moonVertices, moonTriangles, new Vector3(7.0f, 0.071f, 9.6f), 1.70f, 0.42f, 18f, 0.72f);
            AddGroundPatch(moonVertices, moonTriangles, new Vector3(12.2f, 0.072f, 2.1f), 1.92f, 0.40f, -3f, 1.62f);
            AddGroundPatch(moonVertices, moonTriangles, new Vector3(18.5f, 0.071f, 10.5f), 1.62f, 0.44f, 9f, 2.83f);
            CreateMeshObject("Ground Moonlit Grass Breakup", moonVertices, moonTriangles, moonGrass, true);

            var scuffVertices = new List<Vector3>(48);
            var scuffTriangles = new List<int>(90);
            AddGroundPatch(scuffVertices, scuffTriangles, new Vector3(-1.0f, 0.074f, 2.0f), 1.15f, 0.42f, 0f, 0.9f);
            AddGroundPatch(scuffVertices, scuffTriangles, new Vector3(3.8f, 0.073f, 2.4f), 1.05f, 0.26f, 16f, 1.9f);
            CreateMeshObject("Ground Warm Entry Scuffs", scuffVertices, scuffTriangles, warmScuff, true);
        }

        private void CreateLandmarkReadabilityFields(Material fireSource, Material magicSource)
        {
            Material gateRead = CreateRuntimeMaterial("DC Gate Readability Pool", new Color(0.22f, 0.08f, 0.045f), false, fireSource);
            Material keepRead = CreateRuntimeMaterial("DC Keep Readability Pool", new Color(0.036f, 0.125f, 0.185f), false, magicSource);

            var gateVertices = new List<Vector3>(32);
            var gateTriangles = new List<int>(60);
            AddGroundPatch(gateVertices, gateTriangles, new Vector3(-2.05f, 0.086f, 2.0f), 0.92f, 0.46f, 0f, 0.15f);
            AddGroundPatch(gateVertices, gateTriangles, new Vector3(0.25f, 0.084f, 2.0f), 0.68f, 0.22f, 0f, 1.2f);
            CreateMeshObject("Enemy Gate Warm Read Pool", gateVertices, gateTriangles, gateRead, false);

            var keepVertices = new List<Vector3>(32);
            var keepTriangles = new List<int>(60);
            AddGroundPatch(keepVertices, keepTriangles, new Vector3(23.25f, 0.086f, 9.0f), 1.12f, 0.52f, 0f, 0.4f);
            AddGroundPatch(keepVertices, keepTriangles, new Vector3(20.4f, 0.084f, 9.0f), 0.74f, 0.26f, 0f, 1.6f);
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

        private void CreateRoadSurface()
        {
            BiomeDefinition palette = CampaignLevelFactory.Biome(biomeIndex);
            Color roadTint = Color.Lerp(new Color(0.455f, 0.355f, 0.245f), palette.accentColor, biomeIndex == 0 ? 0.04f : 0.16f);
            Material surface = CreateRuntimeMaterial("DC Organic Road Surface", roadTint, false);
            var vertices = new List<Vector3>(420);
            var triangles = new List<int>(720);
            int sample = 0;
            for (int routeIndex = 0; routeIndex < ownedRoadPaths.Count; routeIndex++)
            {
                IReadOnlyList<Vector3> roadPath = ownedRoadPaths[routeIndex];
                vertices.Clear();
                triangles.Clear();
                for (int segment = 0; segment < roadPath.Count - 1; segment++)
                {
                    Vector3 from = roadPath[segment];
                    Vector3 to = roadPath[segment + 1];
                    Vector3 direction = (to - from).normalized;
                    Vector3 side = new(-direction.z, 0f, direction.x);
                    float length = Vector3.Distance(from, to);
                    int count = Mathf.Max(2, Mathf.CeilToInt(length / 0.72f));
                    int start = vertices.Count;

                    for (int i = 0; i <= count; i++)
                    {
                        float t = i / (float)count;
                        float endpointFeather = Mathf.Sin(t * Mathf.PI);
                        Vector3 center = Vector3.Lerp(from - direction * 0.22f, to + direction * 0.22f, t);
                        float wobble = Mathf.Sin((sample + i) * 1.83f) * 0.055f +
                                       Mathf.Cos((sample + i) * 3.17f) * 0.025f;
                        center += side * wobble * endpointFeather;
                        center.y = 0.082f;

                        float width = 0.78f + Mathf.Sin((sample + i) * 2.41f) * 0.055f;
                        float leftNoise = 1f + Mathf.Sin((sample + i) * 4.13f) * 0.045f;
                        float rightNoise = 1f + Mathf.Cos((sample + i) * 3.77f) * 0.045f;
                        vertices.Add(center - side * width * leftNoise);
                        vertices.Add(center + side * width * rightNoise);
                    }

                    for (int i = 0; i < count; i++)
                    {
                        int a = start + i * 2;
                        triangles.Add(a);
                        triangles.Add(a + 3);
                        triangles.Add(a + 1);
                        triangles.Add(a);
                        triangles.Add(a + 2);
                        triangles.Add(a + 3);
                    }

                    sample += count + 1;
                }

                for (int i = 1; i < roadPath.Count - 1; i++)
                    AddGroundPatch(vertices, triangles, roadPath[i] + Vector3.up * 0.084f, 1.06f, 0.84f,
                        i % 2 == 0 ? 0f : 90f, i * 0.61f);

                AddGroundPatch(vertices, triangles, roadPath[0] + Vector3.up * 0.084f, 0.96f, 0.70f, 0f, 0.13f);
                AddGroundPatch(vertices, triangles, roadPath[^1] + Vector3.up * 0.084f, 1.14f, 0.82f, 0f, 1.47f);
                CreateMeshObject(routeIndex == 0 ? "Road Organic Earth Surface" : "Road Surface", vertices, triangles, surface, true);
            }
        }

        private void CreateRoadCobbles()
        {
            Material cobble = CreateRuntimeMaterial("DC Road Cobble", new Color(0.30f, 0.315f, 0.30f), false);
            var vertices = new List<Vector3>(8000);
            var triangles = new List<int>(12000);
            int stoneIndex = 0;
            for (int routeIndex = 0; routeIndex < ownedRoadPaths.Count; routeIndex++)
            {
                IReadOnlyList<Vector3> roadPath = ownedRoadPaths[routeIndex];
                vertices.Clear();
                triangles.Clear();
                for (int segment = 0; segment < roadPath.Count - 1; segment++)
                {
                    Vector3 from = roadPath[segment];
                    Vector3 to = roadPath[segment + 1];
                    Vector3 direction = (to - from).normalized;
                    Vector3 side = new(-direction.z, 0f, direction.x);
                    float length = Vector3.Distance(from, to);
                    float distance = -0.08f;
                    while (distance <= length + 0.18f)
                    {
                        float clampedDistance = Mathf.Clamp(distance, 0f, length);
                        Vector3 along = from + direction * clampedDistance;
                        for (int edge = -1; edge <= 1; edge += 2)
                        {
                            bool nearEndpoint = clampedDistance < 0.55f || clampedDistance > length - 0.55f;
                            float omitSignal = Mathf.Sin((stoneIndex + segment * 13) * 2.37f + edge * 0.51f);
                            if (!nearEndpoint && omitSignal > 0.82f)
                            {
                                stoneIndex++;
                                continue;
                            }

                            float wave = Mathf.Sin((stoneIndex + 1) * 4.73f);
                            float scatter = Mathf.Cos((stoneIndex + 5) * 3.09f);
                            Vector3 center = along + side * (edge * (0.88f + wave * 0.055f));
                            center += direction * scatter * 0.085f;
                            center.y = 0.105f + Mathf.Abs(wave) * 0.018f;
                            float yaw = Mathf.Atan2(direction.z, direction.x) + wave * 0.16f + scatter * 0.055f;
                            if (ShouldOmitRoadBorder(center, routeIndex, routes))
                            {
                                stoneIndex++;
                                continue;
                            }
                            AddCobble(vertices, triangles, center, yaw,
                                new Vector3(0.36f + wave * 0.075f, 0.13f + Mathf.Abs(wave) * 0.04f,
                                    0.23f - wave * 0.035f));
                            stoneIndex++;
                        }
                        distance += 0.54f + Mathf.Abs(Mathf.Sin((stoneIndex + segment) * 1.91f)) * 0.22f;
                    }
                }

                for (int i = 1; i < roadPath.Count - 1; i++)
                {
                    Vector3 from = roadPath[i - 1];
                    Vector3 corner = roadPath[i];
                    Vector3 to = roadPath[i + 1];
                    Vector3 incoming = (corner - from).normalized;
                    Vector3 outgoing = (to - corner).normalized;
                    Vector3 sideA = new(-incoming.z, 0f, incoming.x);
                    Vector3 sideB = new(-outgoing.z, 0f, outgoing.x);
                    for (int edge = -1; edge <= 1; edge += 2)
                    {
                        float yaw = Mathf.Atan2((incoming + outgoing).z, (incoming + outgoing).x) + edge * 0.13f;
                        Vector3 center = corner + (sideA + sideB).normalized * edge * 0.88f;
                        center.y = 0.125f;
                        if (ShouldOmitRoadBorder(center, routeIndex, routes)) continue;
                        AddCobble(vertices, triangles, center, yaw,
                            new Vector3(0.52f, 0.17f, 0.34f));
                    }
                }
                CreateMeshObject(routeIndex == 0 ? "Road Hand-Laid Stone Borders" : "Road Edge", vertices, triangles, cobble, true);
            }
        }

        private void CreateRoadWear(Material source)
        {
            Material wear = CreateRuntimeMaterial("DC Road Wear", new Color(0.31f, 0.235f, 0.18f), false, source);
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            int patch = 0;
            for (int routeIndex = 0; routeIndex < ownedRoadPaths.Count; routeIndex++)
            {
                IReadOnlyList<Vector3> roadPath = ownedRoadPaths[routeIndex];
                vertices.Clear();
                triangles.Clear();
                for (int segment = 0; segment < roadPath.Count - 1; segment++)
                {
                    Vector3 from = roadPath[segment];
                    Vector3 to = roadPath[segment + 1];
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
            foreach (Mesh mesh in runtimeMeshes)
                if (mesh != null) Destroy(mesh);
            foreach (Material material in runtimeMaterials)
                if (material != null) Destroy(material);
        }
    }
}
