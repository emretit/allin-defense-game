using DefenderChronicles.Core;
using DefenderChronicles.Data;
using NUnit.Framework;
using UnityEngine;

namespace DefenderChronicles.Tests
{
    public sealed class CoreRulesTests
    {
        [TestCase(100, 25, 75)]
        [TestCase(10, 50, 0)]
        [TestCase(10, -2, 10)]
        public void DamageNeverCreatesNegativeHealth(int health, int damage, int expected)
        {
            Assert.That(CombatMath.ApplyDamage(health, damage), Is.EqualTo(expected));
        }

        [Test]
        public void EconomyRefundIsSixtyPercent()
        {
            Assert.That(CombatMath.Refund(175), Is.EqualTo(105));
        }

        [Test]
        public void RouteProgressUsesXzSegmentProjection()
        {
            float progress = CombatMath.RouteProgress(Vector3.zero, new Vector3(10f, 0f, 0f),
                new Vector3(4f, 9f, 2f));
            Assert.That(progress, Is.EqualTo(4f).Within(0.001f));
        }

        [Test]
        public void CooldownStopsAtZero()
        {
            Assert.That(CombatMath.TickCooldown(0.25f, 1f), Is.Zero);
        }

        [Test]
        public void SpawnGroupsRejectInvalidCountsAndIntervals()
        {
            var group = new SpawnGroupDefinition(EnemyArchetype.Raider, -4, -1f, 0f);
            Assert.That(group.count, Is.EqualTo(1));
            Assert.That(group.startDelay, Is.Zero);
            Assert.That(group.interval, Is.EqualTo(0.1f));
        }
    }
}
