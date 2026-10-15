using System.Collections;
using System.Linq;
using System.Reflection;
using AllinDefense.Core;
using AllinDefense.Data;
using AllinDefense.Presentation;
using AllinDefense.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AllinDefense.Tests
{
    public sealed class WorldHealthBarTests
    {
        private GameSessionController session;
        private bool reducedMotion;
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        [UnitySetUp] public IEnumerator Setup()
        {
            reducedMotion = FrontendPreferences.ReducedMotion;
            session = new GameObject("World health fixture").AddComponent<GameSessionController>();
            session.ConfigurePresentation(Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog"));
            session.SetCampaignLevel(1);
            session.ConfigureProgress(1);
            session.Initialize();
            Time.timeScale = 0;
            yield return null;
        }

        [UnityTearDown] public IEnumerator Cleanup()
        {
            Object.Destroy(session.gameObject);
            Time.timeScale = 1;
            FrontendPreferences.ReducedMotion = reducedMotion;
            yield return null;
        }

        private static Transform Root(Component actor) => actor.transform.Find("Health Bar/Outline");
        private static Transform Fill(Component actor) => Root(actor).Find("Background/Health Backing/Fill");
        private static void AssertBar(Component actor, float fraction, bool visible)
        {
            Assert.That(Root(actor), Is.Not.Null);
            Assert.That(Root(actor).gameObject.activeInHierarchy, Is.EqualTo(visible));
            Assert.That(Fill(actor).localScale.x, Is.EqualTo(fraction).Within(.0001f));
            Assert.That(actor.transform.Find("Health Bar").GetComponent<RectTransform>().sizeDelta, Is.EqualTo(new Vector2(110, 17)));
            Assert.That(Fill(actor).GetComponent<Image>().color, Is.EqualTo(UiTokens.Gold));
        }

        [UnityTest] public IEnumerator GuardsShowActualHealthOnlyAfterDamageAndHideOnDeath()
        {
            Assert.That(session.TryBuildOutpost(0, OutpostKind.Barracks), Is.True);
            var guard = session.Outposts[0].Soldiers.First();
            int maximum = guard.Health;
            AssertBar(guard, 1, false);
            guard.TakeDamage(30);
            AssertBar(guard, guard.Health / (float)maximum, true);
            guard.SetUpgradeLevel(1);
            AssertBar(guard, 1, false);
            guard.TakeDamage(20);
            AssertBar(guard, guard.Health / (float)maximum, true);
            guard.TakeDamage(int.MaxValue);
            Assert.That(Root(guard).gameObject.activeInHierarchy, Is.False);
            yield return null;
        }

        [UnityTest] public IEnumerator HeroShowsActualHealthAndHidesOnDeathAndRespawn()
        {
            var hero = session.Hero;
            AssertBar(hero, 1, false);
            hero.TakeDamage(60);
            AssertBar(hero, hero.HealthFraction, true);
            hero.TakeDamage(int.MaxValue);
            AssertBar(hero, 0, false);
            typeof(GreyboxHero).GetMethod("Tick", Private).Invoke(hero, new object[] { 5f });
            Assert.That(hero.IsAlive, Is.True);
            AssertBar(hero, 1, false);
            yield return null;
        }

        [UnityTest] public IEnumerator ArmourIconFollowsHealthVisibilityAndMeetsPhoneWorldSize()
        {
            typeof(GameSessionController).GetMethod("SpawnEnemy", Private, null, new[] { typeof(EnemyArchetype) }, null)
                .Invoke(session, new object[] { EnemyArchetype.Vanguard });
            var enemy = session.Enemies.Last();
            var icon = enemy.GetComponentInChildren<ShieldStatusIcon>(true);
            Assert.That(icon, Is.Not.Null);
            Assert.That(icon.transform.parent, Is.SameAs(Root(enemy)));
            Assert.That(icon.gameObject.activeInHierarchy, Is.False);
            Assert.That(icon.rectTransform.rect.width * icon.transform.lossyScale.x, Is.GreaterThanOrEqualTo(.5f));
            Assert.That(icon.rectTransform.rect.height * icon.transform.lossyScale.y, Is.GreaterThanOrEqualTo(.5759f));
            enemy.TakeDamage(20);
            Assert.That(enemy.HealthBarVisible, Is.True);
            Assert.That(icon.gameObject.activeInHierarchy, Is.True);
            yield return null;
        }


        [UnityTest] public IEnumerator AffordableEmptyPreparationSlotStillBreathesUnlessMotionIsReduced()
        {
            FrontendPreferences.ReducedMotion = false;
            var visual = session.GetComponentsInChildren<DefenderSlotVisual>().First();
            var ring = visual.transform.Find("SlotGoldRing").GetComponent<Renderer>();
            var properties = new MaterialPropertyBlock();
            yield return null;
            ring.GetPropertyBlock(properties);
            Color before = properties.GetColor("_EmissionColor");
            yield return new WaitForSecondsRealtime(.12f);
            ring.GetPropertyBlock(properties);
            Assert.That(properties.GetColor("_EmissionColor"), Is.Not.EqualTo(before));
            FrontendPreferences.ReducedMotion = true;
            yield return null;
            int writes = visual.RingPropertyWriteCount;
            ring.GetPropertyBlock(properties);
            Color steady = properties.GetColor("_EmissionColor");
            yield return new WaitForSecondsRealtime(.12f);
            ring.GetPropertyBlock(properties);
            Assert.That(properties.GetColor("_EmissionColor"), Is.EqualTo(steady));
            Assert.That(visual.RingPropertyWriteCount, Is.EqualTo(writes));
        }

        [UnityTest] public IEnumerator StableSlotInputsDoNotRewritePropertyBlocksAcrossFrames()
        {
            FrontendPreferences.ReducedMotion = true;
            var visual = session.GetComponentsInChildren<DefenderSlotVisual>().First();
            yield return null;
            int writes = visual.RingPropertyWriteCount;
            for (int frame = 0; frame < 4; frame++) yield return null;
            Assert.That(visual.RingPropertyWriteCount, Is.EqualTo(writes));
            visual.SetSelected(true);
            Assert.That(visual.RingPropertyWriteCount, Is.EqualTo(writes + 1));
            visual.SetSelected(true);
            yield return null;
            Assert.That(visual.RingPropertyWriteCount, Is.EqualTo(writes + 1));
            visual.SetSelected(false);
            visual.SetOccupied(true);
            FrontendPreferences.ReducedMotion = false;
            int occupiedWrites = visual.RingPropertyWriteCount;
            yield return null;
            Assert.That(visual.RingPropertyWriteCount, Is.EqualTo(occupiedWrites));
            FrontendPreferences.ReducedMotion = true;
            visual.SetOccupied(false);
            typeof(GameSessionController).GetMethod("SpendGold", Private).Invoke(session, new object[] { session.Gold });
            yield return null;
            Assert.That(visual.RingPropertyWriteCount, Is.EqualTo(occupiedWrites + 2));
            int unaffordableWrites = visual.RingPropertyWriteCount;
            yield return null;
            Assert.That(visual.RingPropertyWriteCount, Is.EqualTo(unaffordableWrites));
        }
    }
}
