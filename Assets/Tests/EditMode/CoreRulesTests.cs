using DefenderChronicles.Core;
using DefenderChronicles.Data;
using DefenderChronicles.Presentation;
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

        [Test]
        public void ProductionPresentationCatalogIsComplete()
        {
            var catalog = Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog");
            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.ValidationErrors(), Is.Empty);
            Assert.That(catalog.actorVisuals, Has.Length.EqualTo(8));
            Assert.That(catalog.outpostVisuals, Has.Length.EqualTo(6));
            Assert.That(catalog.uiMixer, Is.Not.SameAs(catalog.sfxMixer));
            Assert.That(catalog.uiMixer, Is.Not.SameAs(catalog.musicMixer));
            Assert.That(catalog.sfxMixer, Is.Not.SameAs(catalog.musicMixer));
            Assert.That(catalog.provenance, Has.Length.EqualTo(7));
            Assert.That(catalog.audioLicense, Is.Not.Null);
        }

        [Test]
        public void TurkishUiCopyHasEveryPublicKey()
        {
            var copy = Resources.Load<UiCopyCatalog>("Presentation/UiCopyTr");
            Assert.That(copy, Is.Not.Null);
            Assert.That(copy.locale, Is.EqualTo("tr-TR"));
            Assert.That(copy.ValidationErrors(), Is.Empty);
        }
    }
}
