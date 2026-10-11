using System;
using System.Collections.Generic;
using UnityEngine;

namespace DefenderChronicles.Data
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
            new(2f, 0.15f, 0f), new(2f, 0.15f, 5f), new(6.5f, 0.15f, 6f), new(8f, 0.15f, 10.5f),
            new(12f, 0.15f, 1.2f), new(14f, 0.15f, 6f), new(18f, 0.15f, 6.8f), new(20.5f, 0.15f, 11.2f)
        };

        public static RuntimeCampaignLevel Create(int levelNumber)
        {
            CampaignLevelDefinition campaign = CampaignCatalog.Level(levelNumber);
            MapDefinition map = BuildMap(campaign);
            bool existingFinal = campaign.number == 10;
            return new RuntimeCampaignLevel
            {
                campaign = campaign,
                path = ClonePath(map.PrimaryRoute),
                routes = CloneRoutes(map.routes),
                outpostSlots = (Vector3[])map.outpostSlots.Clone(),
                guardianPosition = map.guardianPosition,
                waves = existingFinal ? BuildExistingBossWaves() : BuildProgressiveWaves(campaign, map.entranceCount),
                entranceCount = map.entranceCount,
                biomeIndex = map.biomeIndex
            };
        }

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

        private static MapDefinition BuildMap(CampaignLevelDefinition level)
        {
            bool existingFinal = level.number == 10;
            int entranceCount = level.number >= 43 ? 3 : level.number >= 14 ? 2 : 1;
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
            float mergeZ = Mathf.Clamp(primary[Mathf.Max(1, primary.Length - 3)].z, 3.2f, 8.2f);
            for (int route = 1; route < entranceCount; route++)
            {
                float entryZ = route == 1 ? 10.8f : 0.65f;
                float bendZ = route == 1 ? 8.9f : 2.4f;
                Vector3[] branch =
                {
                    new(-2f, 0f, entryZ), new(2.5f + route * 0.6f, 0f, entryZ),
                    new(5.0f + route * 0.6f, 0f, bendZ), new(10f, 0f, bendZ),
                    new(14f, 0f, mergeZ), new(primary[^2].x, 0f, primary[^2].z), primary[^1]
                };
                routes.Add(branch);
            }
            return routes.ToArray();
        }

        private static WaveDefinition[] BuildProgressiveWaves(CampaignLevelDefinition level, int entranceCount)
        {
            var waves = new WaveDefinition[level.waveCount];
            for (int waveIndex = 0; waveIndex < waves.Length; waveIndex++)
            {
                var groups = new List<SpawnGroupDefinition>();
                int regionPressure = (level.region - 1) * 2;
                int raiders = 4 + level.indexInRegion + regionPressure + waveIndex * 2;
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

        private static Vector3[] BuildOutpostSlots(IReadOnlyList<Vector3> path, IReadOnlyList<Vector3[]> routes)
        {
            var slots = new Vector3[8];
            float totalLength = 0f;
            for (int index = 0; index < path.Count - 1; index++)
                totalLength += Vector3.Distance(path[index], path[index + 1]);

            for (int slotIndex = 0; slotIndex < slots.Length; slotIndex++)
            {
                float target = totalLength * Mathf.Lerp(0.12f, 0.88f, slotIndex / 7f);
                float traversed = 0f;
                for (int segment = 0; segment < path.Count - 1; segment++)
                {
                    Vector3 from = path[segment];
                    Vector3 to = path[segment + 1];
                    float length = Vector3.Distance(from, to);
                    if (traversed + length < target)
                    {
                        traversed += length;
                        continue;
                    }

                    float t = Mathf.Clamp01((target - traversed) / Mathf.Max(0.01f, length));
                    Vector3 point = Vector3.Lerp(from, to, t);
                    Vector3 direction = (to - from).normalized;
                    Vector3 side = new(-direction.z, 0f, direction.x);
                    // Sample a ring around the path point instead of assuming that one
                    // perpendicular side is safe. At a tight bend the other segment can
                    // run directly through that side, which used to put a slot on the road.
                    float bestScore = float.MinValue;
                    float fallbackScore = float.MinValue;
                    Vector3 bestPoint = ClampSlot(point + side * 2.25f);
                    Vector3 fallbackPoint = bestPoint;
                    for (int radiusIndex = 0; radiusIndex < 4; radiusIndex++)
                    {
                        float radius = 2.1f + radiusIndex * 0.45f;
                        for (int directionIndex = 0; directionIndex < 16; directionIndex++)
                        {
                            float angle = directionIndex * Mathf.PI * 2f / 16f;
                            Vector3 candidate = ClampSlot(point + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius);
                            float clearance = MinimumRouteDistance(candidate, routes);
                            float minimumSpacing = float.MaxValue;
                            for (int previous = 0; previous < slotIndex; previous++)
                                minimumSpacing = Mathf.Min(minimumSpacing, Vector3.Distance(candidate, slots[previous]));
                            float score = clearance + Mathf.Min(2f, minimumSpacing) * 0.5f;
                            if (minimumSpacing > fallbackScore)
                            {
                                fallbackScore = minimumSpacing;
                                fallbackPoint = candidate;
                            }
                            // Keep the platform footprint separate from an earlier one
                            // whenever the map has room to do so.
                            if (slotIndex > 0 && minimumSpacing < 1.2f) continue;
                            if (score > bestScore)
                            {
                                bestScore = score;
                                bestPoint = candidate;
                            }
                        }
                    }
                    if (bestScore == float.MinValue) bestPoint = fallbackPoint;
                    point = bestPoint;
                    point.y = 0.15f;
                    slots[slotIndex] = point;
                    break;
                }
            }
            return slots;
        }

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
            {
                Vector3[] route = routes[routeIndex];
                for (int segment = 0; segment < route.Length - 1; segment++)
                    minimum = Mathf.Min(minimum, DistanceToSegment(point, route[segment], route[segment + 1]));
            }
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

        private static string PreviewFor(EnemyArchetype archetype) => archetype switch
        {
            EnemyArchetype.Vanguard => "Zırhlı öncüler",
            EnemyArchetype.Regenerator => "Yenilenen muhafızlar",
            EnemyArchetype.Support => "Şifacı destek birliği",
            EnemyArchetype.Siege => "Kuşatma birliği",
            EnemyArchetype.Boss => "Bölge lordu ve seçkin muhafızları",
            _ => "Hızlı yağmacılar"
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
