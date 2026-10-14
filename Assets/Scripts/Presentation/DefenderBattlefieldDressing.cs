using System;
using System.Collections.Generic;
using AllinDefense.Data;
using AllinDefense.UI;
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
        private Texture2D sceneryPalette;
        private IReadOnlyList<Vector3> slots;
        private readonly List<List<Vector3>> roundedPaths = new();
        private readonly List<Mesh> runtimeMeshes = new();
        private readonly List<Material> runtimeMaterials = new();
        private IReadOnlyList<Vector3> roadPath = DefaultRoadPath;
        private int biomeIndex;
        private BiomeLook Look => BiomeLook.ForRegion(biomeIndex);
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
            CreateSceneryClusters(camera, catalog);
            CreateLandmarkReadabilityFields(catalog.fireMaterial, catalog.magicMaterial);
            CreateRoadSurface();
            CreateRoadCobbles();
            CreateRoadWear(catalog.earthMaterial);
            // The battle fills the screen with terrain; no raised board edge or sky band.
        }

        private void CreateContinuousGround(Material material)
        {
            const string name = "Continuous Battlefield Ground";
            material = CreateRuntimeMaterial("DC Moonlit Ground", Look.GroundColor, false, material);
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

        private readonly List<Bounds> sceneryFootprints = new();
        public IReadOnlyList<Bounds> SceneryFootprints => sceneryFootprints;
        public Rect KeepHudExclusion { get; private set; }

        private sealed class SceneryBatch
        {
            public readonly List<Vector3> vertices = new();
            public readonly List<int> indices = new();
        }

        public static bool DecorationOverlapsHud(Bounds bounds, Camera camera, Rect keepPlate)
        {
            if (camera == null) return false;
            Vector2 min = Vector2.one * float.MaxValue, max = Vector2.one * float.MinValue;
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 p = camera.WorldToViewportPoint(bounds.center + Vector3.Scale(bounds.extents,
                    new Vector3((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1)));
                min = Vector2.Min(min, p); max = Vector2.Max(max, p);
            }
            var projected = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            return projected.Overlaps(new Rect(0, .915f, .23f, .085f)) ||
                   projected.Overlaps(new Rect(.40f, .915f, .23f, .085f)) ||
                   projected.Overlaps(new Rect(.82f, .915f, .18f, .085f)) ||
                   projected.Overlaps(new Rect(0, 0, .24f, .20f)) ||
                   projected.Overlaps(new Rect(.78f, 0, .22f, .13f)) ||
                   projected.Overlaps(keepPlate);
        }

        private Rect CalculateKeepHudExclusion(Camera camera)
        {
            if (camera == null) return default;
            var bounds = new Bounds(roadPath[^1] + Vector3.up * 1.7f, new Vector3(3, 3.4f, 3));
            foreach (Transform piece in transform.parent.GetComponentsInChildren<Transform>())
            {
                if (piece.name != "Defender Keep Licensed") continue;
                Renderer[] renderers = piece.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0) break;
                bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
                break;
            }
            Vector2 logical = UiMetrics.LogicalScreenSize;
            float top = float.MinValue, left = float.MaxValue;
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 p = camera.WorldToViewportPoint(bounds.center + Vector3.Scale(bounds.extents,
                    new Vector3((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1)));
                top = Mathf.Max(top, p.y); left = Mathf.Min(left, p.x);
            }
            Rect safe = Screen.safeArea;
            Vector2 min = safe.size.sqrMagnitude > 0 ? safe.min / new Vector2(Screen.width, Screen.height) : Vector2.zero;
            Vector2 max = safe.size.sqrMagnitude > 0 ? safe.max / new Vector2(Screen.width, Screen.height) : Vector2.one;
            float x = camera.WorldToViewportPoint(bounds.center).x;
            if (top > max.y - 122f / logical.y) x = left - 83f / logical.x;
            x = Mathf.Clamp(x, min.x + 83f / logical.x, max.x - 83f / logical.x);
            float y = Mathf.Clamp(top + 32f / logical.y, min.y + 40f / logical.y, max.y - 90f / logical.y);
            return new Rect(x - 85f / logical.x, y - 34f / logical.y, 170f / logical.x, 68f / logical.y);
        }

        private void CreateSceneryClusters(Camera camera, DefenderPresentationCatalog catalog)
        {
            KeepHudExclusion = CalculateKeepHudExclusion(camera);
            var random = new System.Random(6006 + biomeIndex * 197 + Mathf.RoundToInt(roadPath[1].x * 100));
            float Next(float a, float b) => Mathf.Lerp(a, b, (float)random.NextDouble());
            var crowns = new[] { new SceneryBatch(), new SceneryBatch(), new SceneryBatch() };
            var trunks = new SceneryBatch(); var rocks = new SceneryBatch();
            var undergrowth = new SceneryBatch(); var glints = new SceneryBatch();
            var accents = new SceneryBatch();
            var mossPatches = new List<Vector4>();
            void Patch(Vector3 center, float radius, float yaw) => mossPatches.Add(new Vector4(center.x, center.z, radius, yaw));
            bool Clear(Bounds bounds) => !DecorationOverlapsGameplay(bounds, slots, routes) &&
                !DecorationOverlapsLandmarks(bounds, roadPath) && !DecorationOverlapsHud(bounds, camera, KeepHudExclusion);
            var plane = new Plane(Vector3.up, Vector3.zero);
            int smallClusters = 0;
            float minRouteZ = float.MaxValue, maxRouteZ = float.MinValue;
            foreach (Vector3 point in roadPath) { minRouteZ = Mathf.Min(minRouteZ, point.z); maxRouteZ = Mathf.Max(maxRouteZ, point.z); }
            for (int attempt = 0; attempt < 4000 && sceneryFootprints.Count < 36; attempt++)
            {
                Vector2 viewport = new(Next(.015f, .985f), Next(.045f, .94f));
                Vector3 center;
                if (camera != null)
                {
                    Ray ray = camera.ViewportPointToRay(viewport);
                    if (!plane.Raycast(ray, out float distance)) continue;
                    center = ray.GetPoint(distance);
                }
                else center = new Vector3(Next(-4f, 27f), 0, Next(-5f, 20f));
                float edge = Mathf.Max(Mathf.Abs(viewport.x - .5f) * 2, Mathf.Abs(viewport.y - .5f) * 2);
                float routeDistance = float.MaxValue;
                foreach (var route in ownedRoadPaths) routeDistance = Mathf.Min(routeDistance, CampaignLevelFactory.DistanceToRoute(center, route));
                bool interior = attempt < 300 && smallClusters < 4;
                if (interior && (center.x < 0 || center.x > 24 || center.z < minRouteZ + 1.7f || center.z > maxRouteZ - 1.7f)) continue;
                bool small = interior || routeDistance < 3.2f || random.NextDouble() < .24;
                if (small && smallClusters >= 8) continue;
                float radius = small ? Next(.4f, .65f) : Next(1.0f, 1.75f);
                float height = small ? .65f : Next(1.7f, 2.8f);
                var bounds = new Bounds(center + Vector3.up * (height - .14f) * .5f, new Vector3(radius * 2, height + .14f, radius * 2));
                if (!Clear(bounds)) continue;
                if (sceneryFootprints.Exists(other => new Vector2(other.center.x - center.x, other.center.z - center.z).magnitude <
                    other.extents.x + radius + Mathf.Lerp(1.05f, .35f, edge))) continue;
                if (random.NextDouble() > Mathf.Lerp(.52f, 1f, edge)) continue;
                sceneryFootprints.Add(bounds);
                if (small) smallClusters++;
                int cluster = sceneryFootprints.Count;
                int treeCount = small ? 0 : random.Next(1, 8);
                if (Look.Silhouette == BiomeSilhouette.SunkenColumns) treeCount = Mathf.Min(treeCount, 3);
                if (Look.Silhouette == BiomeSilhouette.CitadelBanners) treeCount = Mathf.Min(treeCount, 2);
                for (int tree = 0; tree < treeCount; tree++)
                {
                    float angle = Next(0, Mathf.PI * 2);
                    float size = height / 2.25f * Next(.72f, 1f);
                    float spread = (radius - .52f * size) * Mathf.Sqrt(Next(0, 1));
                    Vector3 p = center + new Vector3(Mathf.Cos(angle) * spread, 0, Mathf.Sin(angle) * spread);
                    float yaw = Next(0, Mathf.PI * 2);
                    AddLathe(trunks.vertices, trunks.indices, p, new[] { .09f * size, .065f * size }, new[] { 0f, .9f * size }, yaw);
                    SceneryBatch crown = crowns[(tree + cluster / 3) % 3];
                    if (Look.Silhouette != BiomeSilhouette.Forest)
                    {
                        AddBiomeTree(crown, trunks, accents, p, size, yaw, (tree + cluster) % 3);
                        continue;
                    }
                    switch ((tree + cluster) % 3)
                    {
                        case 0:
                            AddLathe(crown.vertices, crown.indices, p, new[] { .5f * size, 0f }, new[] { .55f * size, 1.7f * size }, yaw);
                            AddLathe(crown.vertices, crown.indices, p, new[] { .36f * size, 0f }, new[] { 1.04f * size, 2.25f * size }, yaw + .2f);
                            break;
                        case 1:
                            AddLathe(crown.vertices, crown.indices, p, new[] { 0f, .48f * size, .5f * size, .28f * size, 0f },
                                new[] { .65f * size, .82f * size, 1.3f * size, 1.68f * size, 1.8f * size }, yaw);
                            break;
                        default:
                            AddLathe(crown.vertices, crown.indices, p, new[] { 0f, .28f * size, .32f * size, .14f * size, 0f },
                                new[] { .6f * size, .9f * size, 1.5f * size, 1.94f * size, 2.2f * size }, yaw);
                            break;
                    }
                }
                int stones = small ? random.Next(1, 3) : random.Next(2, 5);
                for (int stone = 0; stone < stones; stone++)
                {
                    Vector3 p = center + new Vector3(Next(-radius * .35f, radius * .35f), .10f, Next(-radius * .35f, radius * .35f));
                    float size = small ? .3f : Next(.35f, .65f);
                    float yaw = Next(0, 6.28f);
                    if (Look.Silhouette == BiomeSilhouette.FrostPines)
                        AddLathe(rocks.vertices, rocks.indices, p, new[] { size * .42f, size * .3f, 0f },
                            new[] { -.1f, size * .65f, size * 1.45f }, yaw);
                    else if (Look.Silhouette == BiomeSilhouette.AshBranches)
                        AddLathe(rocks.vertices, rocks.indices, p, new[] { size * .48f, size * .40f, size * .32f, 0f },
                            new[] { -.1f, size * .2f, size, size * 1.08f }, yaw);
                    else
                        AddCobble(rocks.vertices, rocks.indices, p, yaw, new Vector3(size, size * .65f, size * .8f));
                }
                for (int bush = 0; bush < (small ? 1 : 2); bush++)
                {
                    Vector3 p = center + new Vector3(Next(-radius * .35f, radius * .35f), 0, Next(-radius * .35f, radius * .35f));
                    float size = small ? radius * .34f : Next(.22f, .36f);
                    AddLathe(undergrowth.vertices, undergrowth.indices, p, new[] { 0f, size, size * .75f, 0f },
                        new[] { 0f, size * .3f, size, size * 1.5f }, Next(0, 6.28f));
                    for (int leaf = 0; leaf < (Look.Silhouette == BiomeSilhouette.Forest ? 5 : 0); leaf++)
                    {
                        float angle = leaf * 1.256f + cluster;
                        Vector3 along = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * size;
                        Vector3 across = new Vector3(-along.z, 0, along.x) * .3f;
                        int start = undergrowth.vertices.Count;
                        undergrowth.vertices.AddRange(new[] { p, p + along + across + Vector3.up * size * .8f,
                            p + along * 1.7f + Vector3.up * size * .4f, p + along - across + Vector3.up * size * .8f });
                        undergrowth.indices.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
                    }
                }
                if (cluster % 3 == 0)
                    for (int spark = 0; spark < 3; spark++)
                    {
                        Vector3 p = center + new Vector3(Next(-radius * .35f, radius * .35f), .2f, Next(-radius * .35f, radius * .35f));
                        AddLathe(trunks.vertices, trunks.indices, p, new[] { .025f, .02f }, new[] { -.18f, 0f }, 0);
                        AddLathe(glints.vertices, glints.indices, p, new[] { .08f, .09f, 0f }, new[] { 0f, .04f, .09f }, 0);
                    }
                Patch(center, radius * Next(1.65f, 2.2f), Next(0, 360));
                Patch(center + new Vector3(radius * .7f, 0, -radius * .5f), radius * 1.3f, Next(0, 360));
            }
            // Low moss at the verge connects isolated groves without covering the road surface.
            foreach (var route in roundedPaths)
                for (int point = 0; point < route.Count; point += 4)
                    foreach (float side in new[] { -1f, 1f })
                    {
                        Vector3 p = route[point] + RoadSide(route, point) * Next(1.25f, 1.8f);
                        Patch(p, Next(.9f, 1.4f), Next(0, 360));
                    }
            // Small boundary props reuse the bark and firefly batches, without real lights.
            foreach (float side in new[] { -1f, 1f })
            {
                Vector3 p = roadPath[^1] + new Vector3(-1.1f, 0, side * 3.1f);
                var bounds = new Bounds(p + Vector3.up * .6f, new Vector3(.9f, 1.2f, .7f));
                if (!Clear(bounds)) continue;
                sceneryFootprints.Add(bounds);
                AddLathe(trunks.vertices, trunks.indices, p, new[] { .06f, .045f }, new[] { 0f, 1.1f }, 0);
                AddCobble(glints.vertices, glints.indices, p + Vector3.up * .92f, 0, new Vector3(.19f, .24f, .19f));
                AddLathe(trunks.vertices, trunks.indices, p + Vector3.up * 1.07f, new[] { .16f, 0f }, new[] { 0f, .13f }, 0);
            }
            foreach (float side in new[] { -1f, 1f })
            {
                Vector3 p = roadPath[0] + new Vector3(-.6f, 0, side * 3.1f);
                var bounds = new Bounds(p + Vector3.up * .35f, new Vector3(1f, .7f, .8f));
                if (!Clear(bounds)) continue;
                sceneryFootprints.Add(bounds);
                for (int thorn = 0; thorn < 5; thorn++)
                    AddLathe(trunks.vertices, trunks.indices, p + new Vector3(Next(-.35f, .35f), 0, Next(-.2f, .2f)),
                        new[] { .12f, 0f }, new[] { 0f, Next(.25f, .65f) }, Next(0, 6.28f));
            }
            Color[] colors = Look.SceneryPalette();
            sceneryPalette = new Texture2D(8, 1, TextureFormat.RGBA32, false)
                { name = "Grove Shared Palette", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            sceneryPalette.SetPixels(colors); sceneryPalette.Apply(false, true);
            Material palette = CreateRuntimeMaterial("DC Grove Shared Palette", Color.white, false);
            palette.SetTexture("_BaseMap", sceneryPalette);
            var combined = new SceneryBatch(); var uvs = new List<Vector2>();
            var batches = new[] { crowns[0], crowns[1], crowns[2], trunks, rocks, undergrowth, accents };
            for (int tone = 0; tone < batches.Length; tone++)
            {
                SceneryBatch batch = batches[tone];
                foreach (int index in batch.indices)
                {
                    combined.indices.Add(combined.vertices.Count);
                    combined.vertices.Add(batch.vertices[index]);
                    uvs.Add(new Vector2((tone + .5f) / 8f, .5f));
                }
            }
            CreateMeshObject("Grove Combined Scenery", combined.vertices, combined.indices, palette, false);
            transform.Find("Grove Combined Scenery").GetComponent<MeshFilter>().sharedMesh.SetUVs(0, uvs);
            Material accentMaterial = biomeIndex == 0 ? catalog.fireMaterial :
                CreateRuntimeMaterial("DC Biome Shared Accents", Look.DecorativeAccentColor, false);
            CreateMeshObject("Grove Lanterns and Mushrooms", glints.vertices, glints.indices, accentMaterial, false);
            // Keep scenery draws together while preserving depth-tested visibility.
            if (biomeIndex != 0)
                foreach (string name in new[] { "Grove Combined Scenery", "Grove Lanterns and Mushrooms" })
                    transform.Find(name).GetComponent<MeshRenderer>().sortingOrder = -10;
            PaintGroundMoss(mossPatches);
        }

        private void AddBiomeTree(SceneryBatch crown, SceneryBatch trunks, SceneryBatch accents,
            Vector3 p, float size, float yaw, int variant)
        {
            switch (Look.Silhouette)
            {
                case BiomeSilhouette.AshBranches:
                    AddLathe(trunks.vertices, trunks.indices, p, new[] { .07f * size, .025f * size, 0f },
                        new[] { .8f * size, 1.7f * size, 2.2f * size }, yaw);
                    for (int branch = 0; branch < 3; branch++)
                    {
                        float angle = yaw + branch * 2.1f;
                        Vector3 from = p + Vector3.up * (1.0f + branch * .24f) * size;
                        Vector3 to = p + new Vector3(Mathf.Cos(angle) * .43f, 1.65f + branch * .20f,
                            Mathf.Sin(angle) * .43f) * size;
                        AddBranch(crown, from, to, .045f * size);
                    }
                    break;
                case BiomeSilhouette.FrostPines:
                    for (int tier = 0; tier < 3; tier++)
                        AddLathe(crown.vertices, crown.indices, p,
                            new[] { (.48f - tier * .12f) * size, 0f },
                            new[] { (.50f + tier * .52f) * size, (1.45f + tier * .40f) * size }, yaw);
                    break;
                case BiomeSilhouette.SunkenColumns:
                    AddCobble(crown.vertices, crown.indices, p + Vector3.up * .10f * size, yaw,
                        new Vector3(.86f, .20f, .86f) * size);
                    AddLathe(crown.vertices, crown.indices, p, new[] { .25f * size, .22f * size, .23f * size, .13f * size },
                        new[] { .2f * size, .35f * size, (1.45f + variant * .18f) * size, (1.65f + variant * .18f) * size }, yaw);
                    AddLathe(accents.vertices, accents.indices, p, new[] { .26f * size, .26f * size },
                        new[] { .35f * size, .42f * size }, yaw);
                    break;
                case BiomeSilhouette.CitadelBanners:
                    AddCobble(crown.vertices, crown.indices, p + Vector3.up * .53f * size, yaw,
                        new Vector3(.86f, 1.06f, .40f) * size);
                    for (int merlon = 0; merlon < 2; merlon++)
                    {
                        Vector3 offset = new(Mathf.Cos(yaw) * (merlon == 0 ? -.26f : .26f), 1.16f,
                            Mathf.Sin(yaw) * (merlon == 0 ? -.26f : .26f));
                        AddCobble(crown.vertices, crown.indices, p + offset * size, yaw, new Vector3(.28f, .30f, .42f) * size);
                    }
                    AddLathe(trunks.vertices, trunks.indices, p, new[] { .025f * size, .025f * size },
                        new[] { .9f * size, 2.2f * size }, yaw);
                    Vector3 along = new Vector3(Mathf.Cos(yaw), 0, Mathf.Sin(yaw)) * .46f * size;
                    int start = accents.vertices.Count;
                    accents.vertices.AddRange(new[] { p + Vector3.up * 2.12f * size,
                        p + along + Vector3.up * 2.05f * size, p + along + Vector3.up * 1.39f * size,
                        p + along * .15f + Vector3.up * 1.50f * size });
                    accents.indices.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
                    break;
            }
        }

        private static void AddBranch(SceneryBatch batch, Vector3 from, Vector3 to, float radius)
        {
            int start = batch.vertices.Count;
            AddLathe(batch.vertices, batch.indices, Vector3.zero, new[] { radius, 0f },
                new[] { 0f, Vector3.Distance(from, to) }, 0);
            Quaternion rotation = Quaternion.FromToRotation(Vector3.up, to - from);
            for (int i = start; i < batch.vertices.Count; i++) batch.vertices[i] = from + rotation * batch.vertices[i];
        }

        private void PaintGroundMoss(List<Vector4> patches)
        {
            const int size = 128;
            const float minX = -12, minZ = -16, span = 46;
            groundTexture = new Texture2D(size, size, TextureFormat.RGBA32, true)
                { name = "Battlefield Ground With Soft Moss", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var pixels = new Color[size * size];
            var rotations = patches.ConvertAll(p => new Vector2(Mathf.Cos(p.w * Mathf.Deg2Rad), Mathf.Sin(p.w * Mathf.Deg2Rad)));
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float worldX = minX + x * span / (size - 1), worldZ = minZ + y * span / (size - 1);
                    float u = (worldX + 39) / 100, v = (worldZ + 44.5f) / 100;
                    float edge = Mathf.SmoothStep(0f, 1f, Mathf.Max(Mathf.Abs(u - .5f) * 3.3f, Mathf.Abs(v - .5f) * 4f));
                    float noise = Mathf.PerlinNoise(u * 75f + 6f, v * 75f + 6f);
                    Color color = Color.Lerp(Look.GroundTextureCenter, Look.GroundTextureEdge, edge) * (.92f + noise * .16f);
                    float moss = 0;
                    for (int patch = 0; patch < patches.Count; patch++)
                    {
                        Vector4 p = patches[patch];
                        float dx = worldX - p.x, dz = worldZ - p.y;
                        if (Mathf.Abs(dx) > p.z || Mathf.Abs(dz) > p.z) continue;
                        Vector2 rotation = rotations[patch];
                        float along = (dx * rotation.x + dz * rotation.y) / p.z;
                        float across = (-dx * rotation.y + dz * rotation.x) / (p.z * .72f);
                        float radius = Mathf.Sqrt(along * along + across * across);
                        float alpha = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.15f, 1, radius + noise * .08f));
                        moss = 1 - (1 - moss) * (1 - alpha * .82f);
                    }
                    pixels[y * size + x] = Color.Lerp(color, Look.MossColor * (.88f + noise * .18f), moss);
                }
            groundTexture.SetPixels(pixels); groundTexture.Apply(true, true);
            Transform ground = transform.Find("Continuous Battlefield Ground");
            ground.GetComponent<Renderer>().sharedMaterial.SetTexture("_BaseMap", groundTexture);
            Mesh mesh = ground.GetComponent<MeshFilter>().sharedMesh;
            var uvs = new List<Vector2>();
            foreach (Vector3 vertex in mesh.vertices) uvs.Add(new Vector2((vertex.x - minX) / span, (vertex.z - minZ) / span));
            mesh.SetUVs(0, uvs);
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
            if (sceneryPalette != null) Destroy(sceneryPalette);
            foreach (Mesh mesh in runtimeMeshes)
                if (mesh != null) Destroy(mesh);
            foreach (Material material in runtimeMaterials)
                if (material != null) Destroy(material);
        }
    }
}
