using System.Collections;
using System.Reflection;
using AllinDefense.Core;
using AllinDefense.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AllinDefense.Tests
{
    public sealed class HeroFacingTests
    {
        private GameSessionController session;
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        [UnitySetUp] public IEnumerator Setup()
        {
            session = new GameObject("Hero facing fixture").AddComponent<GameSessionController>();
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
            yield return null;
        }

        private static Vector3 Flat(Vector3 value) => new(value.x, 0f, value.z);

        private Vector3 NearestRoadPoint(Vector3 origin)
        {
            Vector3 nearest = Flat(session.Path[0]);
            float best = float.MaxValue;
            for (int i = 0; i < session.Path.Count - 1; i++)
            {
                Vector3 a = Flat(session.Path[i]), ab = Flat(session.Path[i + 1]) - a;
                float t = Mathf.Clamp01(Vector3.Dot(Flat(origin) - a, ab) / Mathf.Max(ab.sqrMagnitude, .0001f));
                Vector3 point = a + ab * t;
                if ((point - Flat(origin)).sqrMagnitude < best) { best = (point - Flat(origin)).sqrMagnitude; nearest = point; }
            }
            return nearest;
        }

        [UnityTest] public IEnumerator IdleHeroFacesTheRoadAndTurnsBackAfterLookingAway()
        {
            var hero = session.Hero;
            Assert.That(hero, Is.Not.Null);
            Vector3 toRoad = NearestRoadPoint(hero.Position) - Flat(hero.Position);
            Vector3 forward = Flat(hero.transform.forward).normalized;
            Assert.That(Vector3.Dot(forward, hero.RoadFacingDirection), Is.GreaterThan(.99f));
            if (toRoad.magnitude > .5f)
                Assert.That(Vector3.Angle(forward, toRoad), Is.LessThan(60f), "Hero must look at the road, not away from it.");

            hero.transform.rotation = Quaternion.LookRotation(-hero.RoadFacingDirection);
            var tick = typeof(GreyboxHero).GetMethod("Tick", Private);
            for (int i = 0; i < 10; i++) tick.Invoke(hero, new object[] { .1f });
            Assert.That(Vector3.Dot(Flat(hero.transform.forward).normalized, hero.RoadFacingDirection), Is.GreaterThan(.99f));
            yield return null;
        }
    }
}
