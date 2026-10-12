using AllinDefense.Combat;
using NUnit.Framework;

namespace AllinDefense.Tests
{
    public sealed class AuditBossVictoryRulesTests
    {
        [TestCase(20, 20, 5, 7)]
        [TestCase(12, 20, 5, 7)]
        [TestCase(7, 20, 5, 2)]
        [TestCase(5, 20, 5, 0)]
        [TestCase(20, 20, 20, 0)]
        [TestCase(10, 10, 2, 3)]
        [TestCase(25, 25, 5, 9)]
        [TestCase(8, 8, 1, 3)]
        [TestCase(7, 20, 0, 6)]
        [TestCase(0, 20, 5, 0)]
        [TestCase(20, 0, 5, 0)]
        public void BossBreachAppliesNormalDamageAndOneStarLifeCeiling(
            int lives, int startingLives, int bossDamage, int expectedLives)
        {
            int remaining = BattleRules.LivesAfterBossBreach(lives, startingLives, bossDamage);
            Assert.That(remaining, Is.EqualTo(expectedLives));
            Assert.That(BattleRules.StarsForLives(remaining, startingLives), Is.LessThanOrEqualTo(1));
        }

        [TestCase(1)]
        [TestCase(10)]
        [TestCase(20)]
        [TestCase(21)]
        [TestCase(25)]
        public void BossBreachCeilingIsTheHighestLifeCountBelowTwoStars(int startingLives)
        {
            int remaining = BattleRules.LivesAfterBossBreach(startingLives, startingLives, 1);
            Assert.That(BattleRules.StarsForLives(remaining, startingLives), Is.LessThanOrEqualTo(1));
            Assert.That(BattleRules.StarsForLives(remaining + 1, startingLives), Is.GreaterThanOrEqualTo(2));
        }
    }
}
