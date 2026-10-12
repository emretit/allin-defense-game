using AllinDefense.Combat;
using NUnit.Framework;

namespace AllinDefense.Tests
{
    public class BattleRulesTests
    {
        [TestCase(0, 20, 0)] [TestCase(1, 20, 1)] [TestCase(8, 20, 2)]
        [TestCase(14, 20, 2)] [TestCase(15, 20, 3)] [TestCase(20, 20, 3)]
        public void StarsComeFromLives(int lives, int starting, int expected) =>
            Assert.That(BattleRules.StarsForLives(lives, starting), Is.EqualTo(expected));

        [Test]
        public void VanguardResistsPhysicalButNotMagic()
        {
            Assert.That(BattleRules.ResolveDamage(100, DamageType.Physical, .5f), Is.EqualTo(50));
            Assert.That(BattleRules.ResolveDamage(100, DamageType.Magic, .5f), Is.EqualTo(100));
        }

        [Test]
        public void SlowUsesStrongestEffectAndRefreshesMatchingDuration()
        {
            var slow = new SlowEffect();
            slow.Apply(.5f, 2f); slow.Tick(1f);
            slow.Apply(.8f, 10f);
            Assert.That(slow.Factor, Is.EqualTo(.5f));
            Assert.That(slow.Remaining, Is.EqualTo(1f));
            slow.Apply(.5f, 2f); slow.Tick(1.5f);
            Assert.That(slow.Factor, Is.EqualTo(.5f));
            slow.Tick(.5f);
            Assert.That(slow.Factor, Is.EqualTo(1f));
        }
    }
}
