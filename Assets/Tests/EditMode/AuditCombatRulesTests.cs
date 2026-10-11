using System.Reflection;
using DefenderChronicles.Core;
using DefenderChronicles.Data;
using NUnit.Framework;
using UnityEngine;

namespace DefenderChronicles.Tests
{
    public sealed class AuditCombatRulesTests
    {
        [Test]
        public void IdleKnightCooldownCannotBecomeNegative()
        {
            var root = new GameObject("Idle knight cooldown regression");
            try
            {
                var hero = root.AddComponent<GreyboxHero>();
                hero.Configure(null, Vector3.zero);
                typeof(GreyboxHero).GetMethod("Tick", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(hero, new object[] { 20f });
                Assert.That(typeof(GreyboxHero).GetField("attackTimer", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(hero), Is.EqualTo(0f));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void IdleOutpostCooldownCannotBecomeNegative()
        {
            var root = new GameObject("Idle outpost cooldown regression");
            try
            {
                var session = root.AddComponent<GameSessionController>();
                var tower = root.AddComponent<GreyboxOutpost>();
                tower.Configure(session, null, OutpostKind.Archer, new OutpostStats(50, 50, 10, 4f, 1f));
                typeof(GreyboxOutpost).GetMethod("Tick", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(tower, new object[] { 20f });
                Assert.That(typeof(GreyboxOutpost).GetField("attackTimer", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(tower), Is.EqualTo(0f));
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
