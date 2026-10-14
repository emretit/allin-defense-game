using System.Linq;
using AllinDefense.Core;
using AllinDefense.Data;
using NUnit.Framework;
using UnityEngine;

namespace AllinDefense.Tests
{
    public sealed class FirstRegionDifficultyCurveTests
    {
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        [TestCase(8)]
        public void ConsecutiveLessonsHaveDifferentSpawnPatterns(int level)
        {
            string first = Pattern(FirstRegionCampaign.Level(level));
            string next = Pattern(FirstRegionCampaign.Level(level + 1));
            Assert.That(next, Is.Not.EqualTo(first), "A new lesson must change the incoming pressure, not only the map.");
        }

        private static string Pattern(FirstRegionLevel level) => string.Join("|", level.waves.Select(w =>
            string.Join(";", w.groups.Select(g => JsonUtility.ToJson(g)))));

        [Test]
        public void OnboardingAddsAStepBetweenBasicDefenceAndArmour()
        {
            Assert.That(FirstRegionCampaign.Level(3).waves.Length, Is.EqualTo(4));
            Assert.That(FirstRegionCampaign.Level(4).waves.Length, Is.EqualTo(5));
            Assert.That(FirstRegionCampaign.Level(4).targetMaxSeconds, Is.LessThanOrEqualTo(270));
        }

        [TestCase(4, EnemyArchetype.Vanguard)]
        [TestCase(6, EnemyArchetype.Regenerator)]
        [TestCase(7, EnemyArchetype.Support)]
        [TestCase(8, EnemyArchetype.Siege)]
        public void NewThreatGetsASmallDistinctIntroduction(int level, EnemyArchetype type)
        {
            var waves = FirstRegionCampaign.Level(level).waves;
            Assert.That(waves[0].groups.Any(g => g.enemy == type), Is.False);
            var introduction = waves[1].groups.Where(g => g.enemy == type).ToArray();
            Assert.That(introduction.Sum(g => g.count), Is.InRange(1, 2));
            foreach (var group in introduction)
                Assert.That(waves[1].groups.Any(g => g.enemy != type && g.enemy != EnemyArchetype.Raider &&
                    Mathf.Approximately(g.startDelay, group.startDelay)), Is.False);
        }

        [Test]
        public void LessonsUseValidSpawnsAndLeaveTimeForTheLastEnemy()
        {
            for (int level = 1; level <= 10; level++)
            {
                var definition = FirstRegionCampaign.Level(level);
                float scheduledSeconds = 0;
                foreach (var wave in definition.waves)
                {
                    Assert.That(wave.groups, Is.Not.Empty);
                    foreach (var group in wave.groups)
                    {
                        Assert.That(group.count, Is.GreaterThan(0));
                        Assert.That(group.startDelay, Is.GreaterThanOrEqualTo(0));
                        Assert.That(group.interval, Is.GreaterThanOrEqualTo(.1f));
                    }
                    scheduledSeconds += wave.groups.Max(g => g.startDelay + (g.count - 1) * g.interval);
                }
                Assert.That(scheduledSeconds, Is.LessThan(definition.targetMaxSeconds), $"Level {level} cannot finish inside its window.");
            }
        }

        [Test]
        public void ChangedWaveDataHasANewCheckpointVersion()
        {
            Assert.That(FirstRegionCampaign.Version, Is.EqualTo("first-region-v2"));
            Assert.That(new BattleCheckpoint().ruleVersion, Is.EqualTo(FirstRegionCampaign.Version));
        }
    }
}
