using System;
using System.Linq;
using UnityEngine;

namespace AllinDefense.Data
{
    [Serializable] public sealed class CampaignLevelContent
    {
        public int level, startingGold, targetMinSeconds, targetMaxSeconds;
        public float healthMultiplier, speedMultiplier, attackMultiplier, baseDamageMultiplier;
        public int[] killRewards;
        public string bossProfileId, priorityTargeting;
        public WaveDefinition[] waves;
    }

    [Serializable] public sealed class CampaignRegionContent
    {
        public string contentVersion;
        public CampaignLevelContent[] levels;
    }

    public static class CampaignContent
    {
        private static readonly CampaignRegionContent[] regions = new CampaignRegionContent[CampaignCatalog.RegionCount];

        public static CampaignRegionContent Region(int region)
        {
            if (region < 1 || region > regions.Length) throw new ArgumentOutOfRangeException(nameof(region));
            if (regions[region - 1] == null)
            {
                var resource = Resources.Load<TextAsset>($"Campaign/region-{region:00}");
                if (resource == null) throw new InvalidOperationException($"Campaign region {region} is missing.");
                regions[region - 1] = ParseRegion(resource.text, region);
            }
            return regions[region - 1];
        }

        public static CampaignLevelContent Level(int level)
        {
            if (level < 1 || level > CampaignCatalog.LevelCount) throw new ArgumentOutOfRangeException(nameof(level));
            return Region((level - 1) / CampaignCatalog.LevelsPerRegion + 1).levels[(level - 1) % CampaignCatalog.LevelsPerRegion];
        }

        public static string ContentVersion(int level)
        {
            if (level < 1 || level > CampaignCatalog.LevelCount) throw new ArgumentOutOfRangeException(nameof(level));
            return Region((level - 1) / CampaignCatalog.LevelsPerRegion + 1).contentVersion;
        }

        public static CampaignRegionContent ParseRegion(string json, int region)
        {
            var document = JsonUtility.FromJson<CampaignRegionContent>(json);
            Validate(document, region);
            return document;
        }

        public static void Validate(CampaignRegionContent document, int region)
        {
            if (region < 1 || region > CampaignCatalog.RegionCount || document == null ||
                string.IsNullOrWhiteSpace(document.contentVersion) || document.levels == null ||
                document.levels.Length != CampaignCatalog.LevelsPerRegion)
                throw new InvalidOperationException($"Invalid campaign region {region}.");
            for (int i = 0; i < document.levels.Length; i++)
            {
                var level = document.levels[i];
                int expected = (region - 1) * CampaignCatalog.LevelsPerRegion + i + 1;
                if (level == null || level.level != expected) throw new InvalidOperationException($"Expected campaign level {expected}.");
                if (level.killRewards == null || level.killRewards.Length != 6 || level.killRewards.Any(r => r < 0) ||
                    level.startingGold < 0 || level.targetMinSeconds <= 0 || level.targetMaxSeconds < level.targetMinSeconds ||
                    !Positive(level.healthMultiplier) || !Positive(level.speedMultiplier) ||
                    !Positive(level.attackMultiplier) || !Positive(level.baseDamageMultiplier))
                    throw new InvalidOperationException($"Invalid economy/stats for level {expected}.");
                if (level.priorityTargeting != "nearestExit" && level.priorityTargeting != "furthestProgress")
                    throw new InvalidOperationException($"Invalid targeting for level {expected}.");
                bool finale = expected % CampaignCatalog.LevelsPerRegion == 0;
                if (!string.IsNullOrEmpty(level.bossProfileId))
                {
                    if (!finale) throw new InvalidOperationException($"Boss profile outside region finale: {expected}.");
                    BossEncounterProfile.Resolve(level.bossProfileId);
                }
                if (level.waves == null || level.waves.Length == 0) throw new InvalidOperationException($"Missing waves for level {expected}.");
                foreach (var wave in level.waves)
                {
                    if (wave == null || string.IsNullOrWhiteSpace(wave.id) || wave.clearBonus < 0 || wave.groups == null || wave.groups.Length == 0)
                        throw new InvalidOperationException($"Invalid wave in level {expected}.");
                    bool hasBoss = wave.groups.Any(g => g != null && g.enemy == EnemyArchetype.Boss);
                    if ((hasBoss || wave.bossWave) && (!finale || string.IsNullOrEmpty(level.bossProfileId)) || hasBoss != wave.bossWave)
                        throw new InvalidOperationException($"Invalid boss wave in level {expected}.");
                    foreach (var group in wave.groups)
                        if (group == null || !Enum.IsDefined(typeof(EnemyArchetype), group.enemy) || group.count <= 0 ||
                            group.routeId < 0 || group.routeId >= CampaignLevelFactory.EntranceCount(expected) ||
                            !float.IsFinite(group.startDelay) || group.startDelay < 0 || !Positive(group.interval))
                            throw new InvalidOperationException($"Invalid spawn group in level {expected}.");
                }
            }
        }

        private static bool Positive(float value) => float.IsFinite(value) && value > 0;

        public static WaveDefinition[] CreateWaves(int level) => Level(level).waves.Select(w =>
            new WaveDefinition(w.id, level <= FirstRegionCampaign.LevelCount
                ? string.Join(" • ", w.groups.Select(g => $"{FirstRegionCampaign.EnemyName(g.enemy)} ×{g.count}"))
                : CampaignLevelFactory.PreviewFor(w.groups.Max(g => g.enemy)),
                w.clearBonus, w.bossWave, w.groups.Select(g => new SpawnGroupDefinition
                { enemy = g.enemy, count = g.count, startDelay = g.startDelay, interval = g.interval, routeId = g.routeId }).ToArray())).ToArray();
    }
}
