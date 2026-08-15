using System.Collections;
using DefenderChronicles.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DefenderChronicles.Tests
{
    public sealed class GreyboxSceneTests
    {
        [UnityTest]
        public IEnumerator SessionOpensWithCompleteGreyboxContent()
        {
            var gameObject = new GameObject("Test Game Session");
            var session = gameObject.AddComponent<GameSessionController>();
            session.Initialize();
            yield return null;

            Assert.That(session.Phase, Is.EqualTo(GamePhase.Preparing));
            Assert.That(session.Gold, Is.EqualTo(200));
            Assert.That(session.Lives, Is.EqualTo(20));
            Assert.That(session.WaveCount, Is.EqualTo(7));
            Assert.That(session.SlotCount, Is.EqualTo(8));
            Assert.That(session.CommandPointCount, Is.EqualTo(4));
            Assert.That(session.Hero, Is.Not.Null);

            Assert.That(session.TryBuildOutpost(0, Data.OutpostKind.Archer), Is.True);
            Assert.That(session.Gold, Is.EqualTo(125));
            Assert.That(session.TryUpgradeOutpost(0), Is.True);
            Assert.That(session.Gold, Is.EqualTo(70));
            Assert.That(session.TrySellOutpost(0), Is.True);
            Assert.That(session.Gold, Is.EqualTo(148));

            session.TogglePause();
            Assert.That(session.Phase, Is.EqualTo(GamePhase.Paused));
            session.TogglePause();
            Assert.That(session.Phase, Is.EqualTo(GamePhase.Preparing));
            LogAssert.NoUnexpectedReceived();
        }
    }
}
