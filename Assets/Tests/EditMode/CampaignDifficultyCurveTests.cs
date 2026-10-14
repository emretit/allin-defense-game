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
        private static readonly int[] ApprovedRegions = { 1 };
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

        private static string Pattern(CampaignLevelContent level) => string.Join("|", level.waves.Select(w =>
            string.Join(";", w.groups.Select(g => JsonUtility.ToJson(g)))));
        private static EnemyStats Stats(EnemyArchetype enemy) => (EnemyStats)BaseStats.Invoke(null, new object[] { enemy });
        private static long TotalHealth(CampaignLevelContent level) => level.waves.Sum(w => w.groups.Sum(g =>
            (long)g.count * Mathf.RoundToInt(Stats(g.enemy).maxHealth * level.healthMultiplier)));
        private static float Length(Vector3[] route) => Enumerable.Range(1, route.Length - 1).Sum(i => Vector3.Distance(route[i - 1], route[i]));
    }
}
