using System;
using System.Linq;
using AllinDefense.Data;
using NUnit.Framework;
using UnityEngine;

namespace AllinDefense.Tests
{
    public sealed class CampaignContentTests
    {
        [Test]
        public void FiveRegionResourcesSupplyEveryLevel()
        {
            int count = 0;
            for (int region = 1; region <= 5; region++)
            {
                Assert.That(Resources.Load<TextAsset>($"Campaign/region-{region:00}"), Is.Not.Null);
                var content = CampaignContent.Region(region);
                CampaignContent.Validate(content, region);
                foreach (var level in content.levels)
                {
                    count++;
                    Assert.That(level.level, Is.EqualTo(count));
                    var runtime = CampaignLevelFactory.Create(count);
                    Assert.That(runtime.waves.Length, Is.EqualTo(level.waves.Length));
                    Assert.That(runtime.campaign.startingGold, Is.EqualTo(level.startingGold));
                    Assert.That(runtime.campaign.targetMinutes, Is.EqualTo((level.targetMinSeconds + level.targetMaxSeconds) / 120f));
                    Assert.That(runtime.entranceCount, Is.EqualTo(CampaignLevelFactory.EntranceCount(count)));
                    foreach (var group in runtime.waves.SelectMany(w => w.groups))
                        Assert.That(group.routeId, Is.InRange(0, runtime.routes.Length - 1));
                }
            }
            Assert.That(count, Is.EqualTo(50));
            Assert.That(CampaignContent.ContentVersion(1), Is.EqualTo("first-region-v2"));
            Assert.That(CampaignContent.ContentVersion(10), Is.EqualTo(FirstRegionCampaign.Version));
            Assert.That(CampaignContent.ContentVersion(11), Is.EqualTo("region-02-v1"));
            Assert.That(CampaignContent.ContentVersion(21), Is.EqualTo("region-03-v1"));
            Assert.That(CampaignContent.ContentVersion(31), Is.EqualTo("region-04-v1"));
            Assert.That(CampaignContent.ContentVersion(41), Is.EqualTo("region-05-v1"));
            Assert.That(CampaignCatalog.Level(50).displayName, Is.EqualTo("Son Lord"));
        }

        [Test]
        public void RuntimeGroupsKeepRoutesAndExactFinalTimingsWithoutSharingMutableGroups()
        {
            var source = CampaignContent.Level(14).waves;
            var copy = CampaignContent.CreateWaves(14);
            Assert.That(source.SelectMany(w => w.groups).Any(g => g.routeId == 1), Is.True);
            for (int w = 0; w < source.Length; w++)
            for (int g = 0; g < source[w].groups.Length; g++)
            {
                Assert.That(copy[w].groups[g], Is.Not.SameAs(source[w].groups[g]));
                Assert.That(JsonUtility.ToJson(copy[w].groups[g]), Is.EqualTo(JsonUtility.ToJson(source[w].groups[g])));
                Assert.That(JsonUtility.ToJson(CampaignLevelFactory.Create(14).waves[w].groups[g]),
                    Is.EqualTo(JsonUtility.ToJson(source[w].groups[g])), "Factory must not apply a second timing multiplier.");
            }
        }

        [Test]
        public void CompatibilityLayerAlsoKeepsAuthoredRouteId()
        {
            var group = CampaignContent.Level(1).waves[0].groups[0];
            int route = group.routeId;
            try { group.routeId = 1; Assert.That(FirstRegionCampaign.CreateWaves(1)[0].groups[0].routeId, Is.EqualTo(1)); }
            finally { group.routeId = route; }
        }

        [TestCase("route-negative")]
        [TestCase("route-outside")]
        [TestCase("count-zero")]
        [TestCase("boss-outside-finale")]
        [TestCase("boss-profile-outside-finale")]
        [TestCase("rewards-length")]
        [TestCase("level-order")]
        [TestCase("missing-level")]
        [TestCase("unknown-profile")]
        [TestCase("unknown-targeting")]
        [TestCase("negative-delay")]
        [TestCase("zero-interval")]
        [TestCase("invalid-multiplier")]
        public void InvalidAuthoredDataIsRejected(string defect)
        {
            var region = JsonUtility.FromJson<CampaignRegionContent>(Resources.Load<TextAsset>("Campaign/region-02").text);
            var level = region.levels[0];
            var group = level.waves[0].groups[0];
            switch (defect)
            {
                case "route-negative": group.routeId = -1; break;
                case "route-outside": group.routeId = 1; break;
                case "count-zero": group.count = 0; break;
                case "boss-outside-finale": group.enemy = EnemyArchetype.Boss; level.waves[0].bossWave = true; break;
                case "boss-profile-outside-finale": level.bossProfileId = "region-lord"; break;
                case "rewards-length": level.killRewards = new int[5]; break;
                case "level-order": level.level = 12; break;
                case "missing-level": region.levels = region.levels.Take(9).ToArray(); break;
                case "unknown-profile": region.levels[9].bossProfileId = "missing-boss"; break;
                case "unknown-targeting": level.priorityTargeting = "unknown"; break;
                case "negative-delay": group.startDelay = -1; break;
                case "zero-interval": group.interval = 0; break;
                case "invalid-multiplier": level.healthMultiplier = float.NaN; break;
            }
            Assert.Throws<InvalidOperationException>(() => CampaignContent.Validate(region, 2));
        }

        [Test]
        public void BossProfilesResolveAndApprovedFinalesUseTheirOwnProfile()
        {
            foreach (string id in new[] { "region-lord", "gate-master", "frost-sovereign", "deep-lord", "last-lord" })
                BossEncounterProfile.Validate(BossEncounterProfile.Resolve(id));
            Assert.That(BossEncounterProfile.Resolve(""), Is.Null);
            Assert.Throws<InvalidOperationException>(() => BossEncounterProfile.Resolve("unknown"));
            for (int level = 1; level <= 50; level++)
                Assert.That(CampaignContent.Level(level).bossProfileId, Is.EqualTo(level == 20 ? "gate-master" : level == 30 ? "frost-sovereign" : level == 40 ? "deep-lord" : level == 50 ? "last-lord" : level % 10 == 0 ? "region-lord" : ""));
            var gate = BossEncounterProfile.Resolve("gate-master");
            Assert.That(gate.reinforcementEnemy, Is.EqualTo(EnemyArchetype.Vanguard));
            Assert.That(gate.reinforcementRoute, Is.EqualTo("first"));
            Assert.That(gate.reinforcementCount, Is.EqualTo(8));
            Assert.That(gate.reinforcementStartDelay, Is.EqualTo(2f));
            Assert.That(gate.reinforcementInterval, Is.EqualTo(1.2f));
            Assert.That(gate.silenceInterval, Is.EqualTo(5f));
            var frost = BossEncounterProfile.Resolve("frost-sovereign");
            Assert.That(frost.reinforcementEnemy, Is.EqualTo(EnemyArchetype.Raider));
            Assert.That(frost.reinforcementRoute, Is.EqualTo("first"));
            Assert.That(frost.reinforcementCount, Is.EqualTo(12));
            Assert.That(frost.reinforcementStartDelay, Is.EqualTo(3f));
            Assert.That(frost.reinforcementInterval, Is.EqualTo(.65f));
            Assert.That(frost.phaseThresholds, Is.EqualTo(new[] { .66f, .33f }));
            Assert.That(frost.phase3SpeedMultiplier, Is.EqualTo(1.5f));
            var legacy = BossEncounterProfile.Resolve("region-lord");
            Assert.That(legacy.reinforcementEnemy, Is.EqualTo(EnemyArchetype.Raider));
            Assert.That(legacy.reinforcementCount, Is.EqualTo(6));
            Assert.That(legacy.reinforcementRoute, Is.EqualTo("first"));
            Assert.That(legacy.phaseThresholds, Is.EqualTo(new[] { .66f, .33f }));
            Assert.That(legacy.phase3SpeedMultiplier, Is.EqualTo(1.44f));
            Assert.That(legacy.silenceInterval, Is.EqualTo(4f));
            var deep = BossEncounterProfile.Resolve("deep-lord");
            Assert.That(deep.reinforcementEnemy, Is.EqualTo(EnemyArchetype.Raider));
            Assert.That(deep.reinforcementRoute, Is.EqualTo("rotate"));
            Assert.That(deep.reinforcementCount, Is.EqualTo(10));
            Assert.That(deep.reinforcementStartDelay, Is.EqualTo(5f));
            Assert.That(deep.reinforcementInterval, Is.EqualTo(1.1f));
            Assert.That(deep.supportCount, Is.EqualTo(2));
            var last = BossEncounterProfile.Resolve("last-lord");
            Assert.That(last.reinforcementEnemy, Is.EqualTo(EnemyArchetype.Raider));
            Assert.That(last.reinforcementRoute, Is.EqualTo("all"));
            Assert.That((last.reinforcementCount + last.supportCount) * 3, Is.EqualTo(18));
            Assert.That(last.supportCount * 3, Is.EqualTo(3));
            Assert.That(last.reinforcementStartDelay, Is.EqualTo(12f));
            Assert.That(last.reinforcementInterval, Is.EqualTo(1.8f));
            Assert.That(last.phase3SpeedMultiplier, Is.EqualTo(1.25f));
        }
    }
}
