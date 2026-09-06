using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using DefenderChronicles.Core;
using DefenderChronicles.Data;
using DefenderChronicles.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DefenderChronicles.Tests
{
    public sealed class BarracksSquadTests
    {
        private string folder;
        private GameSessionController Create(CampaignProgressStore store = null, bool build = true)
        {
            var session = new GameObject("Barracks squad regression").AddComponent<GameSessionController>();
            session.ConfigurePresentation(Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog"));
            session.SetCampaignLevel(5);
            if (store != null) session.ConfigureCampaignStore(store);
            session.Initialize();
            Time.timeScale = 0;
            if (build) Assert.That(session.TryBuildOutpost(0, OutpostKind.Barracks), Is.True);
            return session;
        }

        [UnityTest]
        public IEnumerator ThreeSoldiersLeaveTheBarracksAndReachSeparateRoadPositions()
        {
            var session = Create();
            var soldiers = Object.FindObjectsByType<GreyboxFriendlyUnit>(FindObjectsSortMode.None);
            Assert.That(soldiers.Length, Is.EqualTo(3));
            var starts = soldiers.Select(s => s.Position).ToArray();
            for (int frame = 0; frame < 300; frame++) session.AdvanceBattle(1f / 60);
            for (int i = 0; i < soldiers.Length; i++)
            {
                Assert.That(Vector3.Distance(starts[i], soldiers[i].Position), Is.GreaterThan(.3f), "Soldiers must walk out, not appear on the road.");
                Assert.That(DistanceToRoad(session, soldiers[i].Position), Is.LessThan(.45f));
                Assert.That(CombatMath.ClosestRouteDistance(session.Path, soldiers[i].RallyPoint),
                    Is.GreaterThan(CombatMath.ClosestRouteDistance(session.Path, session.Outposts[0].Position) + .5f),
                    "The squad must form up beyond the building so all three soldiers remain visible.");
                for (int j = 0; j < i; j++)
                    Assert.That(Vector3.Distance(soldiers[i].Position, soldiers[j].Position), Is.GreaterThan(.5f));
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator SoldiersMeetAnEnemyOnTheRoadAndTradeMeleeDamage()
        {
            var session = Create();
            var guards = session.Outposts[0].Soldiers.ToArray();
            Assert.That(guards.All(g => !g.CanBlock), Is.True, "Walking out of the building must not block the road remotely.");
            Advance(session, 5);
            var formation = guards.Select(g => g.Position).ToArray();
            typeof(GameSessionController).GetMethod("SpawnEnemy", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(session, new object[] { EnemyArchetype.Vanguard });
            var enemy = session.Enemies.Single();
            // Place a durable, physically armoured opponent on the same road segment as the squad.
            enemy.transform.position = guards[0].RallyPoint;
            int enemyHealth = enemy.Health;
            Advance(session, .8f);
            Assert.That(enemy.Health, Is.LessThan(enemyHealth), "The squad must damage its opponent.");
            Assert.That(guards.Any(g => g.Health < session.Outposts[0].CombatStats.guardHealth), Is.True, "The enemy must fight back.");
            Assert.That(enemy.State, Is.EqualTo(EnemyCombatState.Attacking), "Melee engagement must hold the enemy on the road.");
            Assert.That(guards.Any(g => Vector3.Distance(g.Position, enemy.Position) <= .85f), Is.True);
            Assert.That(guards.Where((g, i) => Vector3.Distance(g.Position, formation[i]) > .2f).Any(), Is.True,
                "Soldiers must close the distance rather than attack from their idle position.");
            foreach (var guard in guards) Assert.That(DistanceToRoad(session, guard.Position), Is.LessThan(.45f));
            enemy.TakeDamage(100000);
            Advance(session, 5);
            foreach (var guard in guards) Assert.That(Vector3.Distance(guard.Position, guard.RallyPoint), Is.LessThan(.05f));
            yield return null;
        }

        [UnityTest]
        public IEnumerator EnemyCannotStoreInstantAttacksWhileWaitingForAnOpponent()
        {
            var session = Create();
            Advance(session, 5);
            typeof(GameSessionController).GetMethod("SpawnEnemy", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(session, new object[] { EnemyArchetype.Vanguard });
            var enemy = session.Enemies.Single();
            // A stationary opponent isolates waiting time from encounter placement.
            typeof(GreyboxEnemy).GetField("stats", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(enemy, new EnemyStats(230, 0, 0, 1, 13, Color.white));
            Advance(session, 3);
            enemy.transform.position = session.Outposts[0].Soldiers[0].RallyPoint;
            int before = session.Outposts[0].Soldiers.Sum(g => g.Health);
            Advance(session, 1f / 60);
            int firstHit = session.Outposts[0].Soldiers.Sum(g => g.Health);
            Assert.That(firstHit, Is.EqualTo(before - 13));
            Advance(session, 1f / 60);
            Assert.That(session.Outposts[0].Soldiers.Sum(g => g.Health), Is.EqualTo(firstHit),
                "Walking or waiting must not accumulate a burst of overdue attacks.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator MeleeReachUsesTheRoadPlaneRegardlessOfVisualPivotHeight()
        {
            var session = Create();
            Advance(session, 5);
            typeof(GameSessionController).GetMethod("SpawnEnemy", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(session, new object[] { EnemyArchetype.Vanguard });
            var enemy = session.Enemies.Single();
            enemy.transform.position = session.Outposts[0].Soldiers[0].RallyPoint + Vector3.up * 1.02f;
            enemy.Stun(5);
            int before = enemy.Health;
            Advance(session, 1);
            Assert.That(enemy.Health, Is.LessThan(before), "An elevated model pivot must not make a road enemy immune to melee.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator ReplacementKeepsTheMissingFormationSlotAndSellingRemovesTheSquad()
        {
            var session = Create();
            var tower = session.Outposts[0];
            Advance(session, 5);
            var survivors = tower.Soldiers.Where(g => g.FormationIndex != 1).ToArray();
            tower.Soldiers.Single(g => g.FormationIndex == 1).TakeDamage(100000);
            yield return null;
            Advance(session, 1f / 60);
            Assert.That(tower.Soldiers.Count, Is.EqualTo(3));
            Assert.That(tower.Soldiers.Select(g => g.FormationIndex), Is.EquivalentTo(new[] { 0, 1, 2 }));
            foreach (var survivor in survivors) Assert.That(tower.Soldiers, Does.Contain(survivor));
            var replacement = tower.Soldiers.Single(g => g.FormationIndex == 1);
            Assert.That(replacement.IsDeployed, Is.False);
            Assert.That(Vector3.Distance(replacement.Position, tower.Position), Is.LessThan(1));
            Advance(session, 5);
            Assert.That(replacement.CanBlock, Is.True);
            Assert.That(Vector3.Distance(replacement.Position, replacement.RallyPoint), Is.LessThan(.05f));
            Assert.That(session.TrySellOutpost(0), Is.True);
            Assert.That(survivors.All(g => !g.CanBlock) && !replacement.CanBlock, Is.True);
            yield return null;
            Assert.That(Object.FindObjectsByType<GreyboxFriendlyUnit>(FindObjectsSortMode.None), Is.Empty);
        }

        [UnityTest]
        public IEnumerator BothSpecializationsKeepThreeSoldiers()
        {
            foreach (var branch in new[] { OutpostSpecialization.ShieldGuard, OutpostSpecialization.AssaultGuard })
            {
                var session = Create();
                session.ConfigureProgress(10);
                typeof(GameSessionController).GetMethod("EarnGold", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(session, new object[] { 1000 });
                Assert.That(session.TryUpgradeOutpost(0), Is.True);
                Assert.That(session.TrySpecializeOutpost(0, branch), Is.True);
                Assert.That(session.Outposts[0].CombatStats.guardCount, Is.EqualTo(3));
                Assert.That(session.Outposts[0].Soldiers.Count, Is.EqualTo(3));
                Object.Destroy(session.gameObject);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator CheckpointPreservesMarchingPositionsAndMissingFormationSlots()
        {
            var store = CreateStore();
            var session = Create(store);
            Advance(session, 1f / 60);
            session.Outposts[0].Soldiers.Single(g => g.FormationIndex == 0).TakeDamage(13);
            session.Outposts[0].Soldiers.Single(g => g.FormationIndex == 1).TakeDamage(100000);
            session.StartNextWave();
            var checkpoint = store.Data.checkpoint;
            var saved = checkpoint.outposts.Single().guards;
            Assert.That(saved.Count, Is.EqualTo(2));
            Assert.That(saved.All(g => !g.deployed), Is.True);
            Object.Destroy(session.gameObject);
            yield return null;
            session = Create(new CampaignProgressStore(Path.Combine(folder, "save.json")), false);
            Assert.That(session.CheckpointRestored, Is.True);
            Assert.That(session.Gold, Is.EqualTo(checkpoint.gold));
            Assert.That(session.Lives, Is.EqualTo(checkpoint.lives));
            foreach (var guard in session.Outposts[0].Soldiers)
            {
                var expected = saved.Single(g => g.formationIndex == guard.FormationIndex);
                Assert.That(guard.Health, Is.EqualTo(expected.health));
                Assert.That(guard.IsDeployed, Is.False);
                Assert.That(Vector3.Distance(guard.Position, expected.position), Is.LessThan(.001f));
            }
            Advance(session, 5);
            Assert.That(session.Outposts[0].Soldiers.Select(g => g.FormationIndex), Is.EquivalentTo(new[] { 0, 1, 2 }));
            Assert.That(session.Outposts[0].Soldiers.All(g => g.CanBlock), Is.True);
        }

        [UnityTest]
        public IEnumerator LegacyTwoSoldierCheckpointRestoresAndRecruitsThirdSoldier()
        {
            var store = CreateStore();
            var session = Create(store);
            Advance(session, 5);
            session.StartNextWave();
            var saved = store.Data.checkpoint.outposts.Single().guards;
            saved.RemoveAt(2);
            foreach (var guard in saved) { guard.hasPosition = false; guard.formationIndex = 0; }
            store.Save();
            Object.Destroy(session.gameObject);
            yield return null;
            store = new CampaignProgressStore(Path.Combine(folder, "save.json"));
            session = Create(store, false);
            Assert.That(session.CheckpointRestored, Is.True);
            Assert.That(session.Outposts[0].Soldiers.Count, Is.EqualTo(2));
            Assert.That(store.Data.unlockedLevel, Is.EqualTo(50));
            Advance(session, 5);
            Assert.That(session.Outposts[0].Soldiers.Count, Is.EqualTo(3));
            Assert.That(session.Outposts[0].Soldiers.All(g => g.CanBlock), Is.True);
        }

        private CampaignProgressStore CreateStore()
        {
            folder = Path.Combine(Application.temporaryCachePath, "barracks-squad-" + System.Guid.NewGuid());
            var store = new CampaignProgressStore(Path.Combine(folder, "save.json"));
            store.Data.unlockedLevel = 50;
            store.Save();
            return store;
        }

        private static void Advance(GameSessionController session, float seconds)
        {
            for (int frame = 0; frame < Mathf.RoundToInt(seconds * 60); frame++) session.AdvanceBattle(1f / 60);
        }

        private static float DistanceToRoad(GameSessionController session, Vector3 position)
        {
            float best = float.MaxValue;
            for (int i = 0; i < session.Path.Count - 1; i++)
            {
                Vector3 a = session.Path[i], d = session.Path[i + 1] - a;
                if (d.sqrMagnitude < .0001f) continue;
                best = Mathf.Min(best, Vector3.Distance(position, a + d * Mathf.Clamp01(Vector3.Dot(position - a, d) / d.sqrMagnitude)));
            }
            return best;
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            foreach (var session in Object.FindObjectsByType<GameSessionController>(FindObjectsSortMode.None)) Object.Destroy(session.gameObject);
            Time.timeScale = 1;
            yield return null;
            if (folder != null && Directory.Exists(folder)) Directory.Delete(folder, true);
        }
    }
}
