using System.Linq;
using System.Reflection;
using AllinDefense.Combat;
using AllinDefense.Core;
using AllinDefense.Data;
using NUnit.Framework;
using UnityEngine;

namespace AllinDefense.Tests
{
    public sealed class CampaignDifficultyCurveTests
    {
        private static readonly int[] ApprovedRegions = { 1, 2, 3, 4, 5 };
        private static readonly MethodInfo BaseStats = typeof(GameSessionController).GetMethod("GetStats", BindingFlags.Static | BindingFlags.NonPublic);

        [TestCaseSource(nameof(ApprovedRegions))]
        public void ApprovedRegionHasDistinctLessonsAndIncreasingHealth(int region)
        {
            var levels = CampaignContent.Region(region).levels;
            for (int index = 0; index < levels.Length; index++)
            {
                var level = levels[index];
                if (index > 0)
                {
                    Assert.That(Pattern(level), Is.Not.EqualTo(Pattern(levels[index - 1])), $"Level {level.level} repeats the previous spawn pattern.");
                    if (level.level is 15 or 19 or 25 or 29 or 35 or 45 or 49)
                    {
                        Assert.That(TotalHealth(level), Is.LessThan(TotalHealth(levels[index - 1])), "Authored recovery chapters must ease pressure.");
                        Assert.That(TotalHealth(level), Is.GreaterThanOrEqualTo(TotalHealth(levels[index - 2])), "Recovery must retain the earlier lesson pressure.");
                    }
                    else
                        Assert.That(TotalHealth(level), Is.GreaterThanOrEqualTo(TotalHealth(levels[index - 1])), $"Level {level.level} reduces total incoming health.");
                }
                // Only the first chapter after a regional boss may reset the health curve.
                if (index == 0 && region > 1) Assert.That(TotalHealth(level), Is.GreaterThan(0));
                var runtime = CampaignLevelFactory.Create(level.level);
                float scheduledSeconds = 0;
                foreach (var wave in level.waves)
                {
                    Assert.That(wave.groups, Is.Not.Empty);
                    foreach (var group in wave.groups)
                    {
                        Assert.That(group.count, Is.GreaterThan(0));
                        Assert.That(group.startDelay, Is.GreaterThanOrEqualTo(0));
                        Assert.That(group.interval, Is.GreaterThanOrEqualTo(.1f));
                        Assert.That(group.routeId, Is.InRange(0, runtime.routes.Length - 1));
                    }
                    scheduledSeconds += wave.groups.Max(g => g.startDelay + (g.count - 1) * g.interval);
                }
                float finalSpawn = level.waves[^1].groups.Max(g => g.startDelay + (g.count - 1) * g.interval);
                float finalArrival = level.waves[^1].groups.Max(g => g.startDelay + (g.count - 1) * g.interval +
                    Length(runtime.routes[g.routeId]) / (Stats(g.enemy).speed * level.speedMultiplier * BattleRules.LegacyPace));
                Assert.That(scheduledSeconds - finalSpawn + finalArrival, Is.LessThan(level.targetMaxSeconds),
                    $"Level {level.level}: final spawns need enough time to reach the keep without blocking or slowing.");
                if (region == 1)
                {
                    int[] minimum = { 120, 120, 180, 210, 240, 240, 240, 240, 240, 360 };
                    int[] maximum = { 180, 180, 240, 270, 360, 360, 360, 360, 360, 480 };
                    Assert.That(level.targetMinSeconds, Is.EqualTo(minimum[index]));
                    Assert.That(level.targetMaxSeconds, Is.EqualTo(maximum[index]));
                }
                else
                {
                    bool boss = level.level % CampaignCatalog.LevelsPerRegion == 0;
                    Assert.That(level.targetMinSeconds, Is.EqualTo(boss ? 420 : 240));
                    Assert.That(level.targetMaxSeconds, Is.EqualTo(boss ? 540 : 420));
                }
            }
        }

        [Test]
        public void AshPassTeachesItsSecondEntranceBeforeArmoredConvoys()
        {
            foreach (int level in new[] { 11, 12, 13 })
                Assert.That(CampaignLevelFactory.Create(level).entranceCount, Is.EqualTo(1));
            var introduction = CampaignContent.Level(14);
            var sideGroups = introduction.waves.SelectMany(w => w.groups).Where(g => g.routeId == 1).ToArray();
            Assert.That(sideGroups, Is.Not.Empty);
            Assert.That(sideGroups.All(g => g.enemy == EnemyArchetype.Raider && g.count <= 9), Is.True);
            Assert.That(sideGroups[0].startDelay, Is.LessThanOrEqualTo(10));
            foreach (int level in new[] { 12, 16 })
                Assert.That(CampaignContent.Level(level).waves.All(w =>
                    w.groups.Any(g => g.enemy == EnemyArchetype.Vanguard) &&
                    w.groups.Any(g => g.enemy == EnemyArchetype.Support)), Is.True);
            Assert.That(CampaignContent.Level(13).waves.All(w => w.groups.Any(g => g.enemy == EnemyArchetype.Regenerator)), Is.True);
            Assert.That(CampaignContent.Level(18).waves.All(w => w.groups.Any(g => g.enemy == EnemyArchetype.Siege)), Is.True);
            Assert.That(TotalHealth(CampaignContent.Level(11)), Is.InRange(
                TotalHealth(CampaignContent.Level(8)) * .8, TotalHealth(CampaignContent.Level(9)) * 1.1));
            Assert.That(TotalHealth(CampaignContent.Level(20)) / (double)TotalHealth(CampaignContent.Level(10)), Is.InRange(1.2, 1.4));
        }

        [Test]
        public void FrostBorderTeachesArrivalTimingBeforeItsBoss()
        {
            foreach (var level in CampaignContent.Region(3).levels)
            {
                Assert.That(CampaignLevelFactory.Create(level.level).entranceCount, Is.EqualTo(2));
                Assert.That(level.speedMultiplier, Is.InRange(1.10f, 1.20f));
            }
            var introduction = CampaignContent.Level(21);
            Assert.That(introduction.waves[0].groups.All(g => g.enemy == EnemyArchetype.Raider && g.routeId == 0), Is.True);
            Assert.That(TotalHealth(introduction) / (double)TotalHealth(CampaignContent.Level(20)), Is.InRange(.75, .85));
            foreach (var wave in CampaignContent.Level(23).waves)
                Assert.That(wave.groups.Where(g => g.enemy == EnemyArchetype.Regenerator).Min(g => g.startDelay),
                    Is.GreaterThan(wave.groups.Where(g => g.enemy == EnemyArchetype.Raider).Min(g => g.startDelay)));
            foreach (var wave in CampaignContent.Level(26).waves)
                Assert.That(wave.groups.Where(g => g.enemy == EnemyArchetype.Vanguard).Min(g => g.startDelay),
                    Is.LessThan(wave.groups.Where(g => g.enemy == EnemyArchetype.Raider).Min(g => g.startDelay)));
            Assert.That(CampaignContent.Level(28).waves.All(w =>
                w.groups.Any(g => g.enemy == EnemyArchetype.Support) && w.groups.Any(g => g.enemy == EnemyArchetype.Vanguard)), Is.True);
            Assert.That(TotalHealth(CampaignContent.Level(30)) / (double)TotalHealth(CampaignContent.Level(20)), Is.InRange(1.2, 1.4));
        }

        [Test]
        public void SunkenKingdomAlternatesEntrancesAndSeparatesSupportLessons()
        {
            foreach (var level in CampaignContent.Region(4).levels)
            {
                Assert.That(CampaignLevelFactory.Create(level.level).entranceCount, Is.EqualTo(2));
                foreach (var wave in level.waves)
                {
                    var raiders = wave.groups.Where(g => g.enemy == EnemyArchetype.Raider).ToArray();
                    Assert.That(raiders.Select(g => g.routeId).Distinct().Count(), Is.EqualTo(2));
                    Assert.That(raiders.Max(g => g.startDelay), Is.GreaterThan(raiders.Min(g => g.startDelay)));
                }
            }
            var introduction = CampaignContent.Level(31);
            Assert.That(TotalHealth(introduction) / (double)TotalHealth(CampaignContent.Level(30)), Is.InRange(.75, .85));
            foreach (int number in new[] { 31, 33, 35 })
            foreach (var wave in CampaignContent.Level(number).waves)
            {
                var raiders = wave.groups.Where(g => g.enemy == EnemyArchetype.Raider).ToArray();
                int firstRoute = raiders.OrderBy(g => g.startDelay).First().routeId;
                float firstEnd = raiders.Where(g => g.routeId == firstRoute).Max(g => g.startDelay + (g.count - 1) * g.interval);
                float nextStart = raiders.Where(g => g.routeId != firstRoute).Min(g => g.startDelay);
                Assert.That(nextStart, Is.GreaterThan(firstEnd), $"Level {number} needs a gap between entrance packets.");
            }
            Assert.That(CampaignContent.Level(34).waves.All(w => w.groups.Any(g => g.enemy == EnemyArchetype.Siege && g.routeId == 1)), Is.True);
            foreach (var wave in CampaignContent.Level(36).waves)
                Assert.That(wave.groups.Where(g => g.enemy == EnemyArchetype.Vanguard).Select(g => g.routeId).Distinct(),
                    Is.Not.EquivalentTo(wave.groups.Where(g => g.enemy == EnemyArchetype.Support).Select(g => g.routeId).Distinct()));
            foreach (var wave in CampaignContent.Level(37).waves)
                Assert.That(wave.groups.Any(g => g.enemy == EnemyArchetype.Regenerator) && wave.groups.Any(g => g.enemy == EnemyArchetype.Support), Is.False);
            Assert.That(TotalHealth(CampaignContent.Level(40)) / (double)TotalHealth(CampaignContent.Level(30)), Is.InRange(1.2, 1.4));
        }

        [Test]
        public void LastBastionIntroducesItsThirdEntranceBeforeMasteryPackets()
        {
            foreach (var level in CampaignContent.Region(5).levels)
            {
                Assert.That(CampaignLevelFactory.Create(level.level).entranceCount, Is.EqualTo(level.level < 43 ? 2 : 3));
                Assert.That(level.speedMultiplier, Is.InRange(1.10f, 1.20f));
            }
            long previousBossHealth = TotalHealth(CampaignContent.Level(40));
            Assert.That(TotalHealth(CampaignContent.Level(41)) / (double)previousBossHealth, Is.InRange(.75, .85));
            Assert.That(TotalHealth(CampaignContent.Level(50)) / (double)previousBossHealth, Is.InRange(1.2, 1.3));
            var introduction = CampaignContent.Level(43);
            Assert.That(TotalHealth(introduction), Is.LessThanOrEqualTo(TotalHealth(CampaignContent.Level(42)) * 1.05));
            foreach (var wave in introduction.waves)
            {
                var third = wave.groups.Where(g => g.routeId == 2).ToArray();
                Assert.That(third, Is.Not.Empty);
                Assert.That(third.All(g => (g.enemy is EnemyArchetype.Raider or EnemyArchetype.Vanguard) && g.count <= 9), Is.True);
                Assert.That(third.Sum(g => g.count), Is.LessThanOrEqualTo(10));
                Assert.That(third.Where(g => g.enemy == EnemyArchetype.Vanguard).Sum(g => g.count), Is.LessThanOrEqualTo(1));
            }
            foreach (int number in new[] { 44, 45 })
            foreach (var wave in CampaignContent.Level(number).waves)
            {
                var packets = wave.groups.Where(g => g.enemy == EnemyArchetype.Raider).GroupBy(g => g.routeId)
                    .OrderBy(g => g.Min(p => p.startDelay)).ToArray();
                Assert.That(packets.Length, Is.EqualTo(3));
                for (int index = 1; index < packets.Length; index++)
                    Assert.That(packets[index].Min(g => g.startDelay), Is.GreaterThan(packets[index - 1]
                        .Max(g => g.startDelay + (g.count - 1) * g.interval)), $"Level {number} needs a gap between entrance packets.");
            }
            foreach (var wave in CampaignContent.Level(49).waves)
            {
                float siegeWarning = wave.groups.Where(g => g.enemy == EnemyArchetype.Siege).Min(g => g.startDelay);
                float secondArm = wave.groups.Where(g => g.enemy == EnemyArchetype.Raider).GroupBy(g => g.routeId)
                    .Select(g => g.Min(p => p.startDelay)).OrderBy(t => t).Skip(1).First();
                Assert.That(siegeWarning, Is.LessThan(secondArm));
                foreach (var support in wave.groups.Where(g => g.enemy == EnemyArchetype.Support))
                    Assert.That(support.startDelay, Is.GreaterThan(secondArm));
            }
            Assert.That(TotalHealth(CampaignContent.Level(45)), Is.LessThan(TotalHealth(CampaignContent.Level(44))));
            Assert.That(TotalHealth(CampaignContent.Level(45)), Is.GreaterThanOrEqualTo(TotalHealth(introduction)));
            foreach (var wave in CampaignContent.Level(47).waves)
            {
                var siege = wave.groups.Where(g => g.enemy == EnemyArchetype.Siege).ToArray();
                Assert.That(siege, Is.Not.Empty);
                Assert.That(siege.Select(g => g.routeId).Distinct(), Is.EquivalentTo(new[] { 1 }));
                foreach (int route in new[] { 0, 2 })
                    Assert.That(wave.groups.Any(g => g.enemy == EnemyArchetype.Raider && g.routeId == route), Is.True);
            }
        }

        private static string Pattern(CampaignLevelContent level) => string.Join("|", level.waves.Select(w =>
            string.Join(";", w.groups.Select(g => JsonUtility.ToJson(g)))));
        private static EnemyStats Stats(EnemyArchetype enemy) => (EnemyStats)BaseStats.Invoke(null, new object[] { enemy });
        private static long TotalHealth(CampaignLevelContent level) => level.waves.Sum(w => w.groups.Sum(g =>
            (long)g.count * Mathf.RoundToInt(Stats(g.enemy).maxHealth * level.healthMultiplier)));
        private static float Length(Vector3[] route) => Enumerable.Range(1, route.Length - 1).Sum(i => Vector3.Distance(route[i - 1], route[i]));
    }
}
