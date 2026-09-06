using System.Collections.Generic;
using UnityEngine;

namespace DefenderChronicles.Data
{
    public sealed class RuntimeCampaignLevel
    {
        public CampaignLevelDefinition campaign;
        public Vector3[] path;
        public Vector3[] outpostSlots;
        public Vector3 guardianPosition;
        public WaveDefinition[] waves;
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
            Vector3[] path = ClonePath(RegionOnePaths[campaign.pathPreset % RegionOnePaths.Length]);
            if (campaign.region % 2 == 0)
                MirrorPath(path);
            bool existingFinal = campaign.number == 10;
            var runtime = new RuntimeCampaignLevel
            {
                campaign = campaign,
                path = existingFinal ? ClonePath(RegionOnePaths[0]) : path,
                outpostSlots = existingFinal ? (Vector3[])ExistingOutposts.Clone() : BuildOutpostSlots(path),
                guardianPosition = existingFinal ? new Vector3(21.7f, 0.12f, 8.15f) : BuildGuardianPosition(path),
                waves = existingFinal ? BuildExistingBossWaves() : BuildProgressiveWaves(campaign)
            };
            if (campaign.number <= FirstRegionCampaign.LevelCount)
            { runtime.waves = FirstRegionCampaign.CreateWaves(campaign.number); return runtime; }
            foreach (var wave in runtime.waves)
                foreach (var group in wave.groups) { group.startDelay *= .5f; group.interval *= .5f; }
            return runtime;
        }

        private static WaveDefinition[] BuildProgressiveWaves(CampaignLevelDefinition level)
        {
            var waves = new WaveDefinition[level.waveCount];
            for (int waveIndex = 0; waveIndex < waves.Length; waveIndex++)
            {
                var groups = new List<SpawnGroupDefinition>();
                int regionPressure = (level.region - 1) * 2;
                int raiders = level.number == 1 ? new[] { 22, 28, 40 }[waveIndex] : 4 + level.indexInRegion + regionPressure + waveIndex * 2;
                groups.Add(new SpawnGroupDefinition(EnemyArchetype.Raider, raiders, 0f,
                    Mathf.Max(0.42f, 0.92f - level.region * 0.045f - level.indexInRegion * 0.012f - waveIndex * 0.025f)));

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

        private static Vector3[] BuildOutpostSlots(IReadOnlyList<Vector3> path)
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
                    float offset = slotIndex % 2 == 0 ? 1.75f : -1.75f;
                    point += side * offset;
                    point.x = Mathf.Clamp(point.x, -0.5f, 22.5f);
                    point.z = Mathf.Clamp(point.z, 0f, 11.2f);
                    point.y = 0.15f;
                    slots[slotIndex] = point;
                    break;
                }
            }
            return slots;
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

        private static void MirrorPath(Vector3[] path)
        {
            for (int index = 0; index < path.Length; index++)
                path[index].z = 11.5f - path[index].z;
        }
    }
}
