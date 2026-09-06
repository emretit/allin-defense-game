using System.Collections;
using System.Reflection;
using DefenderChronicles.Core;
using DefenderChronicles.Data;
using DefenderChronicles.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace DefenderChronicles.Tests
{
    public class FirstRegionRegressionTests
    {
        [UnityTest] public IEnumerator NoDefencesCannotWinFirstLevel() => ObserveFirstLevel(false);
        [UnityTest] public IEnumerator OneIdleArcherCannotEarnThreeStars() => ObserveFirstLevel(true);

        private IEnumerator ObserveFirstLevel(bool build)
        {
            var session = Create(1);
            if (build) Assert.That(session.TryBuildOutpost(0, OutpostKind.Archer), Is.True);
            Time.timeScale = 8f;
            session.StartNextWave();
            float deadline = Time.realtimeSinceStartup + 100f;
            while (session.Phase != GamePhase.Victory && session.Phase != GamePhase.Defeat &&
                   Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(session.Phase == GamePhase.Victory || session.Phase == GamePhase.Defeat, Is.True);
            Debug.Log($"[FIRST_REGION_IDLE] build={build} phase={session.Phase} lives={session.Lives} seconds={session.BattleElapsedSeconds}");
            if (!build) Assert.That(session.Phase, Is.EqualTo(GamePhase.Defeat));
            else Assert.That(session.Phase != GamePhase.Victory ||
                             session.Lives < Mathf.CeilToInt(session.StartingLivesForLevel * .75f), Is.True);
        }

        [UnityTest]
        public IEnumerator StunnedSiegeCannotDisruptAnOutpost()
        {
            var session = Create(10);
            Assert.That(session.TryBuildOutpost(0, OutpostKind.Archer), Is.True);
            typeof(GameSessionController).GetMethod("SpawnEnemy", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(session, new object[] { EnemyArchetype.Siege });
            GreyboxEnemy enemy = session.Enemies[0];
            enemy.transform.position = session.Outposts[0].Position + Vector3.right;
            enemy.Stun(20f);
            yield return null;
            yield return null;
            Assert.That(session.Outposts[0].IsDisrupted, Is.False);
        }

        [UnityTest]
        public IEnumerator GuardsCanReachTheRouteInEveryFirstRegionLayout()
        {
            for (int level = 1; level <= 10; level++)
            {
                var session = Create(level);
                session.ConfigureProgress(50);
                typeof(GameSessionController).GetMethod("EarnGold", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(session, new object[] { 2000 }); // Geometry only.
                for (int slot = 0; slot < session.SlotCount; slot++)
                    Assert.That(session.TryBuildOutpost(slot, OutpostKind.Barracks), Is.True);
                foreach (var guard in Object.FindObjectsByType<GreyboxFriendlyUnit>(FindObjectsSortMode.None))
                {
                    float minimum = float.MaxValue;
                    for (int i = 0; i + 1 < session.Path.Count; i++)
                    {
                        Vector3 a = session.Path[i], d = session.Path[i + 1] - a;
                        float t = Mathf.Clamp01(Vector3.Dot(guard.Position - a, d) / d.sqrMagnitude);
                        minimum = Mathf.Min(minimum, Vector3.Distance(guard.Position, a + d * t));
                    }
                    Assert.That(minimum, Is.LessThan(1.2f), $"Level {level}: guard cannot block route");
                }
                Object.Destroy(session.gameObject);
                yield return null;
            }
        }

        private static GameSessionController Create(int level)
        {
            var session = new GameObject("First region regression").AddComponent<GameSessionController>();
            session.ConfigurePresentation(Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog"));
            session.SetCampaignLevel(level);
            session.Initialize();
            return session;
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.timeScale = 1f;
            foreach (var session in Object.FindObjectsByType<GameSessionController>(FindObjectsSortMode.None))
                Object.Destroy(session.gameObject);
            yield return null;
        }
    }
}
