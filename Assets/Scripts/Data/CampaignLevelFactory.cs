using System;
using System.Collections.Generic;
using AllinDefense.UI;
using UnityEngine;

namespace AllinDefense.Data
{
    public sealed class RuntimeCampaignLevel
    {
        public CampaignLevelDefinition campaign;
        public Vector3[] path;
        public Vector3[][] routes;
        public Vector3[] outpostSlots;
        public Vector3 guardianPosition;
        public WaveDefinition[] waves;
        public int entranceCount;
        public int biomeIndex;
    }

    public sealed class MapDefinition
    {
        public string id;
        public int biomeIndex;
        public int entranceCount;
        public Vector3[][] routes;
        public Vector3[] outpostSlots;
        public Vector3 guardianPosition;

        public Vector3[] PrimaryRoute => routes != null && routes.Length > 0 ? routes[0] : System.Array.Empty<Vector3>();
    }

    public sealed class BiomeDefinition
    {
        public int index;
        public string id;
        public string displayName;
        public Color groundColor;
        public Color accentColor;
        public Color fogColor;
        public Color keyLightColor;
    }

    public static class CampaignLevelFactory
    {
        private static readonly Vector3[][] RegionOnePaths =
        {
            new[]
            {
                new Vector3(-2f, 0f, 2f), new Vector3(4f, 0f, 2f), new Vector3(4f, 0f, 8f),
                new Vector3(10f, 0f, 8f), new Vector3(10f, 0f, 3.5f), new Vector3(16f, 0f, 3.5f),
                new Vector3(16f, 0f, 9f), new Vector3(24f, 0f, 9f)
            },
            new[]
            {
                new Vector3(-2f, 0f, 2.5f), new Vector3(3f, 0f, 2.5f), new Vector3(3f, 0f, 8.5f),
                new Vector3(8f, 0f, 8.5f), new Vector3(8f, 0f, 4f), new Vector3(14f, 0f, 4f),
                new Vector3(14f, 0f, 9.5f), new Vector3(19f, 0f, 9.5f), new Vector3(19f, 0f, 5f),
                new Vector3(24f, 0f, 5f)
            },
            new[]
            {
                new Vector3(-2f, 0f, 8.5f), new Vector3(4f, 0f, 8.5f), new Vector3(4f, 0f, 3f),
                new Vector3(9f, 0f, 3f), new Vector3(9f, 0f, 9f), new Vector3(15f, 0f, 9f),
                new Vector3(15f, 0f, 2.5f), new Vector3(20f, 0f, 2.5f), new Vector3(20f, 0f, 8f),
                new Vector3(24f, 0f, 8f)
            },
            new[]
            {
                new Vector3(-2f, 0f, 3f), new Vector3(5f, 0f, 3f), new Vector3(5f, 0f, 9.5f),
                new Vector3(11f, 0f, 9.5f), new Vector3(11f, 0f, 5f), new Vector3(17f, 0f, 5f),
                new Vector3(17f, 0f, 2f), new Vector3(21f, 0f, 2f), new Vector3(21f, 0f, 8f),
                new Vector3(24f, 0f, 8f)
            },
            new[]
            {
                new Vector3(-2f, 0f, 6f), new Vector3(2.5f, 0f, 6f), new Vector3(2.5f, 0f, 2f),
                new Vector3(7.5f, 0f, 2f), new Vector3(7.5f, 0f, 9.5f), new Vector3(13f, 0f, 9.5f),
                new Vector3(13f, 0f, 3.5f), new Vector3(18.5f, 0f, 3.5f), new Vector3(18.5f, 0f, 9f),
                new Vector3(24f, 0f, 9f)
            }
        };

        private static readonly Vector3[] ExistingOutposts =
        {
            new(2f, 0.15f, 0.42f), new(2.42f, 0.15f, 5f), new(6.5f, 0.15f, 6.42f), new(8f, 0.15f, 9.58f),
            new(12f, 0.15f, 1.92f), new(14.42f, 0.15f, 6f), new(17.58f, 0.15f, 6.8f), new(20.5f, 0.15f, 10.58f)
        };

        public static RuntimeCampaignLevel Create(int levelNumber)
        {
            CampaignLevelDefinition campaign = CampaignCatalog.Level(levelNumber);
            MapDefinition map = BuildMap(campaign);
            var runtime = new RuntimeCampaignLevel
            {
                campaign = campaign,
                path = ClonePath(map.PrimaryRoute),
                routes = CloneRoutes(map.routes),
                outpostSlots = (Vector3[])map.outpostSlots.Clone(),
                guardianPosition = map.guardianPosition,
                waves = CampaignContent.CreateWaves(campaign.number),
                entranceCount = map.entranceCount,
                biomeIndex = map.biomeIndex
            };
            return runtime;
        }

        public static int EntranceCount(int level) => level >= 43 ? 3 : level >= 14 ? 2 : 1;

        public static MapDefinition Map(string mapId)
        {
            if (string.IsNullOrWhiteSpace(mapId))
                throw new InvalidOperationException("Campaign map id is missing.");
            for (int level = 1; level <= CampaignCatalog.LevelCount; level++)
            {
                CampaignLevelDefinition definition = CampaignCatalog.Level(level);
                if (definition.mapId == mapId) return BuildMap(definition);
            }
            throw new InvalidOperationException($"Campaign map '{mapId}' is not registered.");
        }

        public static BiomeDefinition Biome(int index)
        {
            int clamped = Mathf.Clamp(index, 0, 4);
            Color[] ground =
            {
                new(0.07f, 0.24f, 0.18f), new(0.22f, 0.12f, 0.08f), new(0.08f, 0.20f, 0.27f),
                new(0.10f, 0.17f, 0.23f), new(0.23f, 0.12f, 0.10f)
            };
            Color[] accent =
            {
                new(0.25f, 0.82f, 0.55f), new(1f, 0.38f, 0.13f), new(0.35f, 0.82f, 1f),
                new(0.20f, 0.75f, 0.82f), new(1f, 0.72f, 0.25f)
            };
            Color[] fog =
            {
                new(0.026f, 0.068f, 0.105f), new(0.10f, 0.045f, 0.03f), new(0.035f, 0.11f, 0.16f),
                new(0.04f, 0.08f, 0.14f), new(0.12f, 0.055f, 0.04f)
            };
            Color[] light =
            {
                new(0.66f, 0.78f, 0.96f), new(1f, 0.62f, 0.40f), new(0.62f, 0.84f, 1f),
                new(0.55f, 0.75f, 0.90f), new(1f, 0.78f, 0.52f)
            };
            string[] names = { "Yeşil Nöbet", "Kül Geçidi", "Buz Sınırı", "Batık Krallık", "Son Hisar" };
            return new BiomeDefinition
            {
                index = clamped, id = $"biome-{clamped + 1:00}", displayName = names[clamped],
                groundColor = ground[clamped], accentColor = accent[clamped], fogColor = fog[clamped],
                keyLightColor = light[clamped]
            };
        }

        // Branch and slot searches are deterministic but not free; build each level's map once.
        private static readonly Dictionary<int, MapDefinition> MapCache = new();

        private static MapDefinition BuildMap(CampaignLevelDefinition level)
        {
            if (!MapCache.TryGetValue(level.number, out MapDefinition map))
                MapCache[level.number] = map = GenerateMap(level);
            return new MapDefinition
            {
                id = map.id, biomeIndex = map.biomeIndex, entranceCount = map.entranceCount,
                routes = CloneRoutes(map.routes), outpostSlots = (Vector3[])map.outpostSlots.Clone(),
                guardianPosition = map.guardianPosition
            };
        }

        private static MapDefinition GenerateMap(CampaignLevelDefinition level)
        {
            bool existingFinal = level.number == 10;
            int entranceCount = EntranceCount(level.number);
            Vector3[][] routes = BuildRoutes(level, entranceCount, existingFinal);
            Vector3[] primary = routes[0];
            return new MapDefinition
            {
                id = level.mapId,
                biomeIndex = level.region - 1,
                entranceCount = entranceCount,
                routes = routes,
                outpostSlots = existingFinal ? (Vector3[])ExistingOutposts.Clone() : BuildOutpostSlots(primary, routes),
                guardianPosition = existingFinal ? new Vector3(21.7f, 0.12f, 8.15f) : BuildGuardianPosition(primary)
            };
        }

        private static Vector3[][] BuildRoutes(CampaignLevelDefinition level, int entranceCount, bool existingFinal)
        {
            if (existingFinal) return new[] { ClonePath(RegionOnePaths[0]) };
            int pattern = (level.number * 7 + level.region * 3 + level.indexInRegion) % RegionOnePaths.Length;
            Vector3[] primary = ClonePath(RegionOnePaths[pattern]);
            float shift = ((level.number * 13) % 7 - 3) * 0.18f;
            float xShift = ((level.number * 17) % 11 - 5) * 0.11f;
            for (int i = 0; i < primary.Length; i++)
            {
                primary[i].z = Mathf.Clamp(primary[i].z + shift, 0.8f, 10.7f);
                if (i > 0 && i < primary.Length - 1)
                    primary[i].x = Mathf.Clamp(primary[i].x + xShift, 0f, 22.8f);
            }
            if (level.region % 2 == 0) MirrorPath(primary);
            if (entranceCount == 1) return new[] { primary };

            var routes = new List<Vector3[]>(entranceCount) { primary };
            for (int route = 1; route < entranceCount; route++)
                routes.Add(BuildBranchRoute(primary, routes, route) ??
                           throw new InvalidOperationException($"Level {level.number} has no clear branch route {route + 1}."));
            return routes.ToArray();
        }

        // A branch keeps this centreline distance from every other road (road edges are 1.92 wide,
        // so ~0.5 grass remains) and joins the primary route once, at a perpendicular T-junction.
        public const float RouteSeparation = 2.4f;
        private const float RouteMinZ = 0.8f;
        private const float RouteMaxZ = 10.7f;
        private const float RouteMaxX = 22.8f;
        private const float LeftEntryX = -2f;
        private const float TopEntryZ = 10.9f;
        private const float BottomEntryZ = 0.6f;
        private const float EntrySeparation = 3.5f;
        private const float JunctionCornerClearance = 1.4f;
        private const float JunctionApproach = RouteSeparation + 0.3f;

        // Searches T-junction points along the primary route and orthogonal left-edge paths into them,
        // falling back to top/bottom edge entrances when the left edge is crowded.
        private static Vector3[] BuildBranchRoute(Vector3[] primary, List<Vector3[]> routes, int routeIndex)
        {
            float primaryLength = PathLength(primary, 0);
            float targetFraction = 0.42f + 0.18f * (routeIndex - 1);
            var path = new List<Vector3>(4);
            List<Vector3> best = null;
            int bestSegment = -1;
            float bestScore = float.MinValue;

            void Consider(int segment, float fraction, float penalty)
            {
                float length = PathLength(path, 0) + Vector3.Distance(path[^1], primary[segment + 1]) +
                               PathLength(primary, segment + 1);
                float score = -Mathf.Abs(fraction - targetFraction) * 4f - Mathf.Abs(length / primaryLength - 0.9f) * 3f -
                              (path.Count - 2) * 0.4f - penalty;
                if (score <= bestScore || !IsClearBranch(path, routes)) return;
                bestScore = score;
                best = new List<Vector3>(path);
                bestSegment = segment;
            }

            float traversed = 0f;
            for (int segment = 0; segment < primary.Length - 1; segment++)
            {
                Vector3 from = primary[segment];
                Vector3 to = primary[segment + 1];
                float segmentLength = Vector3.Distance(from, to);
                bool horizontal = Mathf.Abs(to.z - from.z) < 0.0001f;
                int steps = (int)(segmentLength / 0.5f);
                for (int step = 1; step < steps; step++)
                {
                    float t = step / (float)steps;
                    Vector3 merge = Vector3.Lerp(from, to, t);
                    if (Mathf.Min(Vector3.Distance(merge, from), Vector3.Distance(merge, to)) < JunctionCornerClearance) continue;
                    float fraction = (traversed + segmentLength * t) / primaryLength;
                    if (fraction < 0.18f || fraction > 0.8f) continue;

                    int rows = (int)((RouteMaxZ - RouteMinZ) / 0.2f);
                    for (int row = 0; row <= rows + (horizontal ? 0 : 1); row++)
                    {
                        float entryZ = row <= rows ? RouteMinZ + row * 0.2f : merge.z;
                        var entry = new Vector3(LeftEntryX, 0f, entryZ);
                        if (horizontal)
                        {
                            if (Mathf.Abs(merge.z - entryZ) < 1.6f) continue;
                            SetPath(path, entry, new Vector3(merge.x, 0f, entryZ), merge);
                            Consider(segment, fraction, 0f);
                            continue;
                        }
                        if (Mathf.Abs(entryZ - merge.z) < 0.000001f)
                        {
                            SetPath(path, entry, merge);
                            Consider(segment, fraction, 0f);
                        }
                        if (Mathf.Abs(entryZ - merge.z) < 1f) continue;
                        for (int column = 0; column < (int)(merge.x / 0.5f); column++)
                        {
                            float bendX = column * 0.5f;
                            if (merge.x - bendX < 1.6f) continue;
                            SetPath(path, entry, new Vector3(bendX, 0f, entryZ), new Vector3(bendX, 0f, merge.z), merge);
                            Consider(segment, fraction, 0f);
                        }
                    }

                    for (int edge = 0; edge < 2; edge++)
                    {
                        float edgeZ = edge == 0 ? TopEntryZ : BottomEntryZ;
                        if (horizontal)
                        {
                            SetPath(path, new Vector3(merge.x, 0f, edgeZ), merge);
                            Consider(segment, fraction, 0.6f);
                            continue;
                        }
                        for (int column = 2; column < (int)(RouteMaxX / 0.5f); column++)
                        {
                            float entryX = column * 0.5f;
                            if (Mathf.Abs(entryX - merge.x) < JunctionApproach) continue;
                            SetPath(path, new Vector3(entryX, 0f, edgeZ), new Vector3(entryX, 0f, merge.z), merge);
                            Consider(segment, fraction, 0.6f);
                        }
                    }
                }
                traversed += segmentLength;
            }

            if (best == null) return null;
            for (int index = bestSegment + 1; index < primary.Length; index++) best.Add(primary[index]);
            return best.ToArray();
        }

        private static void SetPath(List<Vector3> path, Vector3 a, Vector3 b)
        {
            path.Clear();
            path.Add(a); path.Add(b);
        }

        private static void SetPath(List<Vector3> path, Vector3 a, Vector3 b, Vector3 c)
        {
            SetPath(path, a, b);
            path.Add(c);
        }

        private static void SetPath(List<Vector3> path, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            SetPath(path, a, b, c);
            path.Add(d);
        }

        private static bool IsClearBranch(List<Vector3> path, List<Vector3[]> routes)
        {
            for (int index = 0; index < path.Count; index++)
            {
                Vector3 point = path[index];
                float minZ = index == 0 ? BottomEntryZ : RouteMinZ;
                float maxZ = index == 0 ? TopEntryZ : RouteMaxZ;
                if (point.x < LeftEntryX - 0.01f || point.x > RouteMaxX + 0.01f ||
                    point.z < minZ - 0.01f || point.z > maxZ + 0.01f) return false;
            }
            for (int route = 0; route < routes.Count; route++)
                if (FlatDistance(path[0], routes[route][0]) < EntrySeparation) return false;
            if (Vector3.Distance(path[^2], path[^1]) < JunctionApproach) return false;

            // The final approach leg is perpendicular to the primary road, so only the part
            // before it has to keep the full separation.
            float clearLength = PathLength(path, 0) - (RouteSeparation + 0.1f);
            float traversed = 0f;
            for (int segment = 0; segment < path.Count - 1; segment++)
            {
                float length = Vector3.Distance(path[segment], path[segment + 1]);
                int samples = Mathf.Max(1, (int)(length / 0.25f));
                for (int sample = 0; sample <= samples; sample++)
                {
                    float t = sample / (float)samples;
                    if (traversed + length * t > clearLength) return true;
                    Vector3 point = Vector3.Lerp(path[segment], path[segment + 1], t);
                    for (int route = 0; route < routes.Count; route++)
                        if (DistanceToRoute(point, routes[route]) < RouteSeparation) return false;
                }
                traversed += length;
            }
            return true;
        }

        private static float PathLength(IReadOnlyList<Vector3> path, int startIndex)
        {
            float length = 0f;
            for (int index = startIndex; index < path.Count - 1; index++)
                length += Vector3.Distance(path[index], path[index + 1]);
            return length;
        }

        /// <summary>Number of leading segments a branch route owns before it joins the primary road.</summary>
        public static int OwnedBranchSegments(IReadOnlyList<Vector3> branch, IReadOnlyList<Vector3> primary)
        {
            for (int segment = 0; segment < branch.Count - 1; segment++)
                if (DistanceToRoute(branch[segment], primary) < 0.05f && DistanceToRoute(branch[segment + 1], primary) < 0.05f)
                    return segment;
            return branch.Count - 1;
        }

        public static float DistanceToRoute(Vector3 point, IReadOnlyList<Vector3> route)
        {
            float minimum = float.MaxValue;
            for (int segment = 0; segment < route.Count - 1; segment++)
                minimum = Mathf.Min(minimum, DistanceToSegment(point, route[segment], route[segment + 1]));
            return minimum;
        }

        private static WaveDefinition[] BuildProgressiveWaves(CampaignLevelDefinition level, int entranceCount)
        {
            var waves = new WaveDefinition[level.waveCount];
            for (int waveIndex = 0; waveIndex < waves.Length; waveIndex++)
            {
                var groups = new List<SpawnGroupDefinition>();
                int regionPressure = (level.region - 1) * 2;
                int raiders = level.number == 1 ? new[] { 22, 28, 40 }[waveIndex] : 4 + level.indexInRegion + regionPressure + waveIndex * 2;
                groups.Add(new SpawnGroupDefinition(EnemyArchetype.Raider, raiders, 0f,
                    Mathf.Max(0.42f, 0.92f - level.region * 0.045f - level.indexInRegion * 0.012f - waveIndex * 0.025f), 0));
                if (entranceCount > 1 && waveIndex >= waves.Length / 3)
                    groups.Add(new SpawnGroupDefinition(EnemyArchetype.Raider,
                        Mathf.Max(2, raiders / 2), 0.25f, 0.72f, 1));
                if (entranceCount > 2 && waveIndex >= waves.Length / 2)
                    groups.Add(new SpawnGroupDefinition(EnemyArchetype.Vanguard,
                        Mathf.Max(1, level.indexInRegion / 3), 0.65f, 1.25f, 2));

                if (level.number >= 2 && waveIndex >= waves.Length / 2)
                    groups.Add(new SpawnGroupDefinition(EnemyArchetype.Vanguard,
                        1 + level.indexInRegion / 3 + (level.region - 1) + waveIndex / 3, 0.7f, 1.2f));
                if (level.number >= 4 && waveIndex >= waves.Length - 2)
                    groups.Add(new SpawnGroupDefinition(EnemyArchetype.Regenerator,
                        1 + level.region / 2 + level.indexInRegion / 4, 1.1f, 1.55f));
                if (level.number >= 6 && waveIndex >= waves.Length - 2)
                    groups.Add(new SpawnGroupDefinition(EnemyArchetype.Support,
                        1 + (level.region - 1) / 2 + level.indexInRegion / 5, 1.5f, 2.2f));
                if (level.number >= 8 && waveIndex == waves.Length - 1)
                    groups.Add(new SpawnGroupDefinition(EnemyArchetype.Siege,
                        1 + (level.region - 1) / 2 + level.indexInRegion / 5, 2f, 2.5f));
                if (level.isBoss && waveIndex == waves.Length - 1)
                    groups.Add(new SpawnGroupDefinition(EnemyArchetype.Boss, 1, 2.6f, 1f));

                EnemyArchetype strongest = level.number >= 8 && waveIndex == waves.Length - 1
                    ? EnemyArchetype.Siege
                    : level.number >= 6 && waveIndex >= waves.Length - 2
                        ? EnemyArchetype.Support
                        : level.number >= 4 && waveIndex >= waves.Length - 2
                            ? EnemyArchetype.Regenerator
                            : level.number >= 2 && waveIndex >= waves.Length / 2
                                ? EnemyArchetype.Vanguard
                                : EnemyArchetype.Raider;
                if (level.isBoss && waveIndex == waves.Length - 1) strongest = EnemyArchetype.Boss;
                waves[waveIndex] = new WaveDefinition($"level_{level.number:00}_wave_{waveIndex + 1:00}",
                    PreviewFor(strongest), 18 + level.number * 2 + waveIndex * 5,
                    level.isBoss && waveIndex == waves.Length - 1, groups.ToArray());
            }
            return waves;
        }

        private static WaveDefinition[] BuildExistingBossWaves() => new[]
        {
            new WaveDefinition("level_10_wave_01", "Hızlı yağmacılar", 25, false,
                new SpawnGroupDefinition(EnemyArchetype.Raider, 10, 0f, 0.85f)),
            new WaveDefinition("level_10_wave_02", "Yağmacılar + zırhlı öncü", 35, false,
                new SpawnGroupDefinition(EnemyArchetype.Raider, 9, 0f, 0.78f),
                new SpawnGroupDefinition(EnemyArchetype.Vanguard, 4, 1.2f, 1.15f)),
            new WaveDefinition("level_10_wave_03", "İki koldan karma baskı", 45, false,
                new SpawnGroupDefinition(EnemyArchetype.Vanguard, 6, 0f, 1.05f),
                new SpawnGroupDefinition(EnemyArchetype.Raider, 12, 0.5f, 0.68f)),
            new WaveDefinition("level_10_wave_04", "Yenilenen muhafızlar", 55, false,
                new SpawnGroupDefinition(EnemyArchetype.Regenerator, 5, 0f, 1.35f),
                new SpawnGroupDefinition(EnemyArchetype.Raider, 12, 1f, 0.7f)),
            new WaveDefinition("level_10_wave_05", "Şifacı desteği", 70, false,
                new SpawnGroupDefinition(EnemyArchetype.Vanguard, 8, 0f, 0.95f),
                new SpawnGroupDefinition(EnemyArchetype.Support, 3, 0.5f, 2.1f),
                new SpawnGroupDefinition(EnemyArchetype.Raider, 10, 0.5f, 0.65f)),
            new WaveDefinition("level_10_wave_06", "Kuşatma birliği", 90, false,
                new SpawnGroupDefinition(EnemyArchetype.Siege, 3, 0f, 2.4f),
                new SpawnGroupDefinition(EnemyArchetype.Regenerator, 5, 0.4f, 1.2f),
                new SpawnGroupDefinition(EnemyArchetype.Vanguard, 9, 0.5f, 0.88f),
                new SpawnGroupDefinition(EnemyArchetype.Raider, 10, 0.5f, 0.58f)),
            new WaveDefinition("level_10_boss", "Kızıl Muhafız - üç safhalı boss", 150, true,
                new SpawnGroupDefinition(EnemyArchetype.Raider, 6, 0f, 0.72f),
                new SpawnGroupDefinition(EnemyArchetype.Vanguard, 4, 0.5f, 1.05f),
                new SpawnGroupDefinition(EnemyArchetype.Support, 2, 0.5f, 2.2f),
                new SpawnGroupDefinition(EnemyArchetype.Boss, 1, 2f, 1f))
        };

        // Slot centre distance from the nearest route centreline. The road border stones end
        // ~1.07 units out and the platform radius is 0.625, so this band seats every
        // platform against the road edge instead of out on the grass.
        public const float SlotRouteClearanceMin = 1.58f;
        public const float SlotRouteClearanceMax = 1.95f;
        private const float SlotRouteClearanceIdeal = 1.58f;
        private const float SlotPreferredSpacing = 2.6f;
        private const float SlotCoverageRange = 4.2f;
        private const float SlotKeepClearance = 3f;
        private const float SlotGateClearance = 2f;
        private static readonly float[] SlotEdgeOffsets = { SlotRouteClearanceIdeal, 1.62f, 1.68f };

        private static Vector3[] BuildOutpostSlots(IReadOnlyList<Vector3> path, IReadOnlyList<Vector3[]> routes)
        {
            var slots = new Vector3[8];
            float totalLength = 0f;
            for (int index = 0; index < path.Count - 1; index++)
                totalLength += Vector3.Distance(path[index], path[index + 1]);

            for (int slotIndex = 0; slotIndex < slots.Length; slotIndex++)
            {
                float target = totalLength * Mathf.Lerp(0.12f, 0.88f, slotIndex / 7f);
                // Search close to the even spread first; widen the window and relax spacing
                // only when bends or merging routes leave no road-edge spot nearby.
                if (TryPickSlot(path, routes, slots, slotIndex, target, totalLength, 2f, 1.5f, out Vector3 slot) ||
                    TryPickSlot(path, routes, slots, slotIndex, target, totalLength, 4f, 1.5f, out slot) ||
                    TryPickSlot(path, routes, slots, slotIndex, target, totalLength, 6f, 1.3f, out slot) ||
                    TryPickSlot(path, routes, slots, slotIndex, target, totalLength, totalLength, 1.2f, out slot))
                    slots[slotIndex] = slot;
                else
                    throw new InvalidOperationException($"No road-edge outpost slot fits slot {slotIndex + 1}.");
            }
            return slots;
        }

        private static bool TryPickSlot(IReadOnlyList<Vector3> path, IReadOnlyList<Vector3[]> routes, Vector3[] slots,
            int slotIndex, float target, float totalLength, float window, float minimumSpacing, out Vector3 slot)
        {
            slot = default;
            float bestScore = float.MinValue;
            int steps = Mathf.CeilToInt(window / 0.25f);
            for (int step = -steps; step <= steps; step++)
            {
                float shift = step * 0.25f;
                Vector3 point = PointAlongPath(path, Mathf.Clamp(target + shift, 0.5f, totalLength - 0.5f), out Vector3 direction);
                Vector3 side = new(-direction.z, 0f, direction.x);
                for (int sign = -1; sign <= 1; sign += 2)
                foreach (float offset in SlotEdgeOffsets)
                {
                    Vector3 candidate = ClampSlot(point + side * (offset * sign));
                    float clearance = MinimumRouteDistance(candidate, routes);
                    // Subtraction at world coordinates can round a valid 1.58 offset a few ulps below the boundary.
                    if (clearance + 0.0001f < SlotRouteClearanceMin || clearance > SlotRouteClearanceMax) continue;
                    if (IsNearRouteEnds(candidate, routes)) continue;
                    float spacing = float.MaxValue;
                    for (int previous = 0; previous < slotIndex; previous++)
                        spacing = Mathf.Min(spacing, Vector3.Distance(candidate, slots[previous]));
                    if (spacing < minimumSpacing) continue;
                    float score = -Mathf.Abs(clearance - SlotRouteClearanceIdeal) * 3f
                                  - Mathf.Abs(shift) * 0.35f
                                  + RouteCoverage(candidate, routes) * 0.03f
                                  - Mathf.Max(0f, SlotPreferredSpacing - spacing) * 3f;
                    if (score <= bestScore) continue;
                    bestScore = score;
                    slot = candidate;
                }
            }
            return bestScore > float.MinValue;
        }

        private static Vector3 PointAlongPath(IReadOnlyList<Vector3> path, float distance, out Vector3 direction)
        {
            float traversed = 0f;
            for (int segment = 0; segment < path.Count - 1; segment++)
            {
                Vector3 from = path[segment];
                Vector3 to = path[segment + 1];
                float length = Vector3.Distance(from, to);
                if (traversed + length >= distance || segment == path.Count - 2)
                {
                    direction = (to - from).normalized;
                    return Vector3.Lerp(from, to, Mathf.Clamp01((distance - traversed) / Mathf.Max(0.01f, length)));
                }
                traversed += length;
            }
            direction = Vector3.right;
            return path[^1];
        }

        private static bool IsNearRouteEnds(Vector3 point, IReadOnlyList<Vector3[]> routes)
        {
            for (int routeIndex = 0; routeIndex < routes.Count; routeIndex++)
            {
                Vector3[] route = routes[routeIndex];
                if (FlatDistance(point, route[^1]) < SlotKeepClearance || FlatDistance(point, route[0]) < SlotGateClearance)
                    return true;
            }
            return false;
        }

        // Route length a ranged outpost at this point could reach; nudges ties toward bends.
        private static float RouteCoverage(Vector3 point, IReadOnlyList<Vector3[]> routes)
        {
            float covered = 0f;
            for (int routeIndex = 0; routeIndex < routes.Count; routeIndex++)
            {
                Vector3[] route = routes[routeIndex];
                for (int segment = 0; segment < route.Length - 1; segment++)
                {
                    float length = Vector3.Distance(route[segment], route[segment + 1]);
                    int samples = Mathf.Max(1, (int)(length / 0.25f));
                    for (int sample = 0; sample < samples; sample++)
                    {
                        Vector3 along = Vector3.Lerp(route[segment], route[segment + 1], (sample + 0.5f) / samples);
                        if (FlatDistance(point, along) <= SlotCoverageRange) covered += length / samples;
                    }
                }
            }
            return covered;
        }

        private static float FlatDistance(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));

        private static Vector3 ClampSlot(Vector3 point)
        {
            point.x = Mathf.Clamp(point.x, -0.65f, 22.65f);
            point.z = Mathf.Clamp(point.z, 0.15f, 11.15f);
            point.y = 0.15f;
            return point;
        }

        private static float MinimumRouteDistance(Vector3 point, IReadOnlyList<Vector3[]> routes)
        {
            float minimum = float.MaxValue;
            for (int routeIndex = 0; routeIndex < routes.Count; routeIndex++)
                minimum = Mathf.Min(minimum, DistanceToRoute(point, routes[routeIndex]));
            return minimum;
        }

        private static float DistanceToSegment(Vector3 point, Vector3 from, Vector3 to)
        {
            Vector3 segment = to - from;
            segment.y = 0f;
            Vector3 delta = point - from;
            delta.y = 0f;
            float denominator = segment.sqrMagnitude;
            float t = denominator <= 0.001f ? 0f : Mathf.Clamp01(Vector3.Dot(delta, segment) / denominator);
            Vector3 closest = from + segment * t;
            closest.y = point.y;
            return Vector3.Distance(point, closest);
        }

        private static Vector3 BuildGuardianPosition(IReadOnlyList<Vector3> path)
        {
            Vector3 end = path[^1];
            Vector3 direction = (end - path[^2]).normalized;
            Vector3 side = new(-direction.z, 0f, direction.x);
            Vector3 position = end - direction * 2.1f - side * 0.7f;
            position.y = 0.12f;
            return position;
        }

        internal static string PreviewFor(EnemyArchetype archetype) => archetype switch
        {
            EnemyArchetype.Vanguard => GameLocalization.Text("Zırhlı öncüler", "Armored vanguards"),
            EnemyArchetype.Regenerator => GameLocalization.Text("Yenilenen muhafızlar", "Regenerating guards"),
            EnemyArchetype.Support => GameLocalization.Text("Şifacı destek birliği", "Healer support unit"),
            EnemyArchetype.Siege => GameLocalization.Text("Kuşatma birliği", "Siege unit"),
            EnemyArchetype.Boss => GameLocalization.Text("Bölge lordu ve seçkin muhafızları", "Regional boss and elite guards"),
            _ => GameLocalization.Text("Hızlı yağmacılar", "Fast raiders")
        };

        private static Vector3[] ClonePath(Vector3[] source) => (Vector3[])source.Clone();

        private static Vector3[][] CloneRoutes(Vector3[][] source)
        {
            var result = new Vector3[source.Length][];
            for (int i = 0; i < source.Length; i++) result[i] = ClonePath(source[i]);
            return result;
        }

        private static void MirrorPath(Vector3[] path)
        {
            for (int index = 0; index < path.Length; index++)
                path[index].z = 11.5f - path[index].z;
        }
    }
}
