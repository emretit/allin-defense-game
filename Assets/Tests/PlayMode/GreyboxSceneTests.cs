using System.Collections;
using System.Linq;
using DefenderChronicles.Core;
using DefenderChronicles.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DefenderChronicles.Tests
{
    public sealed class GreyboxSceneTests
    {
        [UnityTearDown]
        public IEnumerator TearDownSession()
        {
            Time.timeScale = 1f;
            foreach (var session in Object.FindObjectsByType<GameSessionController>(FindObjectsSortMode.None))
                if (session != null) Object.Destroy(session.gameObject);
            foreach (var hud in GameObject.FindGameObjectsWithTag("Untagged"))
                if (hud != null && hud.name == "Game HUD") Object.Destroy(hud);
            yield return null;
        }

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
            Assert.That(session.Hero, Is.Not.Null);
            for (int i = 1; i <= 8; i++)
            {
                GameObject slot = GameObject.Find($"Outpost Slot {i}");
                Assert.That(slot, Is.Not.Null, $"Fixed outpost slot {i} must be visible.");
                Assert.That(slot.GetComponent<Collider>(), Is.Not.Null, $"Fixed outpost slot {i} must be selectable.");
            }

            Assert.That(session.TryBuildOutpost(0, Data.OutpostKind.Archer), Is.True);
            Assert.That(session.Gold, Is.EqualTo(125));
            Assert.That(session.TryBuildOutpost(0, Data.OutpostKind.Mage), Is.False,
                "An occupied fixed slot must reject another outpost.");
            Assert.That(session.TryBuildOutpost(-1, Data.OutpostKind.Mage), Is.False,
                "A tap outside the fixed slot range must not build.");
            Assert.That(session.TryBuildOutpost(8, Data.OutpostKind.Mage), Is.False,
                "A tap outside the fixed slot range must not build.");
            Assert.That(session.Gold, Is.EqualTo(125));
            Assert.That(session.TryUpgradeOutpost(0), Is.True);
            Assert.That(session.Gold, Is.EqualTo(70));
            Assert.That(session.TrySellOutpost(0), Is.True);
            Assert.That(session.Gold, Is.EqualTo(148));
            Assert.That(session.TryBuildOutpost(0, Data.OutpostKind.Mage), Is.True,
                "Selling must make the same fixed slot reusable.");
            Assert.That(session.Gold, Is.EqualTo(23));
            Assert.That(session.SlotCount, Is.EqualTo(8));

            Vector3 guardianPosition = session.Hero.Position;
            yield return null;
            Assert.That(session.Hero.Position, Is.EqualTo(guardianPosition),
                "The keep guardian must remain stationary.");
            Assert.That(session.Hero.TryUseShieldBash(), Is.True);
            Assert.That(session.Hero.AbilityRemaining, Is.GreaterThan(0f));
            session.Hero.TakeDamage(10000);
            Assert.That(session.Hero.IsAlive, Is.False);
            Time.timeScale = 20f;
            yield return new WaitForSeconds(10.1f);
            Time.timeScale = 1f;
            Assert.That(session.Hero.IsAlive, Is.True);
            Assert.That(session.Hero.Position, Is.EqualTo(guardianPosition),
                "The keep guardian must respawn at the fixed guard position.");

            session.TogglePause();
            Assert.That(session.Phase, Is.EqualTo(GamePhase.Paused));
            session.TogglePause();
            Assert.That(session.Phase, Is.EqualTo(GamePhase.Preparing));
            LogAssert.NoUnexpectedReceived();
            Object.Destroy(gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ProductionPresentationPreservesEconomyAndEmitsTypedSignals()
        {
            var catalog = Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog");
            Assert.That(catalog, Is.Not.Null);
            var gameObject = new GameObject("Art Session Test");
            var session = gameObject.AddComponent<GameSessionController>();
            session.ConfigurePresentation(catalog);
            int buildSignals = 0;
            session.PresentationSignaled += signal =>
            {
                if (signal.cue == PresentationCue.OutpostBuilt) buildSignals++;
            };
            session.Initialize();
            yield return null;

            Assert.That(session.HasProductionPresentation, Is.True);
            Assert.That(session.TryBuildOutpost(0, Data.OutpostKind.Archer), Is.True);
            Assert.That(session.Gold, Is.EqualTo(125));
            Assert.That(buildSignals, Is.EqualTo(1));
            Assert.That(session.TryBuildOutpost(0, Data.OutpostKind.Mage), Is.False);
            Assert.That(session.Gold, Is.EqualTo(125));
            Assert.That(buildSignals, Is.EqualTo(1));
            Assert.That(GameObject.Find("Greenwatch Environment"), Is.Not.Null);
            Assert.That(GameObject.Find("Licensed Nature 01 trees_A_medium"), Is.Not.Null);
            Assert.That(GameObject.Find("Scenic Battlefield Dressing"), Is.Not.Null);
            Assert.That(GameObject.Find("Road Hand-Laid Stone Borders"), Is.Not.Null);
            Assert.That(GameObject.Find("Moon Disc"), Is.Not.Null);
            for (int i = 1; i <= 8; i++)
            {
                GameObject slot = GameObject.Find($"Outpost Slot {i}");
                Assert.That(slot, Is.Not.Null, $"Production fixed slot {i} must be visible.");
                Assert.That(slot.GetComponent<Collider>(), Is.Not.Null,
                    $"Production fixed slot {i} must be selectable.");
            }
            Assert.That(GameObject.Find("Free Placement 9"), Is.Null);
            Assert.That(GameObject.Find("Stationary Defender Anchor 1"), Is.Null);
            Assert.That(GameObject.Find("Defender HUD"), Is.Not.Null);
            Assert.That(session.Hero.GetComponent<StylizedActorView>(), Is.Not.Null);
            Assert.That(session.Hero.GetComponentInChildren<StylizedGroundingAura>(), Is.Not.Null);
            Assert.That(session.Hero.GetComponentInChildren<Animator>(), Is.Not.Null);
            int heroMaterials = session.Hero.GetComponentsInChildren<Renderer>()
                .SelectMany(renderer => renderer.sharedMaterials).Where(material => material != null).Distinct().Count();
            Assert.That(heroMaterials, Is.LessThanOrEqualTo(2));
            LogAssert.NoUnexpectedReceived();
            Object.Destroy(gameObject);
            yield return null;
        }
    }
}
