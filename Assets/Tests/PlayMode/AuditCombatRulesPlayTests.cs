using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using AllinDefense.Combat;
using AllinDefense.Core;
using AllinDefense.Data;
using AllinDefense.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AllinDefense.Tests
{
    public sealed class AuditCombatRulesPlayTests
    {
        private GameSessionController session;
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        private void Create()
        {
            session = new GameObject("Audit combat rules regression").AddComponent<GameSessionController>();
            session.ConfigurePresentation(Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog"));
            session.SetCampaignLevel(10);
            session.Initialize();
            Time.timeScale = 0f;
        }

        private GreyboxEnemy Spawn(EnemyArchetype type, int health = 10000, float speed = 0f, int baseDamage = 1)
        {
            typeof(GameSessionController).GetMethod("SpawnEnemy", PrivateInstance, null, new[] { typeof(EnemyArchetype) }, null)
                .Invoke(session, new object[] { type });
            var enemy = session.Enemies[session.Enemies.Count - 1];
            Set(enemy, "stats", new EnemyStats(health, speed, 17, baseDamage, 0, Color.white));
            Set(enemy, "health", health);
            return enemy;
        }

        private static void Set(object actor, string field, object value) =>
            actor.GetType().GetField(field, PrivateInstance).SetValue(actor, value);
        private static object Get(object actor, string field) => actor.GetType().GetField(field, PrivateInstance).GetValue(actor);
        private static void Tick(object actor, float delta) =>
            actor.GetType().GetMethod("Tick", PrivateInstance).Invoke(actor, new object[] { delta });
        private void Advance(float seconds)
        {
            for (int i = 0; i < Mathf.RoundToInt(seconds / BattleRules.StepSeconds); i++)
                session.AdvanceBattle(BattleRules.StepSeconds);
        }

        [UnityTest]
        public IEnumerator KnightWaitingTwentySecondsDoesNotBurstWhenTargetArrives()
        {
            Create();
            Advance(20f);
            var hero = session.GetComponentInChildren<GreyboxHero>();
            var enemy = Spawn(EnemyArchetype.Raider);
            enemy.transform.position = hero.Position;
            enemy.Stun(2f);
            int attacks = 0;
            session.PresentationSignaled += signal => { if (signal.cue == PresentationCue.HeroAttacked) attacks++; };
            Advance(1f);
            Assert.That(attacks, Is.InRange(2, 3));
            Assert.That(enemy.Health, Is.EqualTo(10000 - attacks * 20));
            yield return null;
        }

        [UnityTest]
        public IEnumerator ArcherWaitingEightSecondsUsesNormalAttackRate()
        {
            Create();
            Assert.That(session.TryBuildOutpost(0, OutpostKind.Archer), Is.True);
            var tower = session.Outposts[0];
            Advance(8f);
            var enemy = Spawn(EnemyArchetype.Raider);
            enemy.transform.position = tower.Position + Vector3.forward;
            enemy.Stun(2f);
            int attacks = 0;
            session.PresentationSignaled += signal =>
            {
                if (signal.cue == PresentationCue.OutpostAttacked && signal.source == tower.gameObject) attacks++;
            };
            Advance(1f);
            Debug.Log($"[AUDIT_ARCHER_IDLE] wait_seconds=8 encounter_seconds=1 attacks={attacks} rate={tower.AttackRateValue:F3}");
            Assert.That(attacks, Is.InRange(1, Mathf.CeilToInt(tower.AttackRateValue)));
            yield return null;
        }

        [UnityTest]
        public IEnumerator BossHealingCannotRepeatPhaseTwoReinforcements()
        {
            Create();
            var boss = Spawn(EnemyArchetype.Boss, 1000);
            var phases = new List<int>();
            session.PresentationSignaled += signal => { if (signal.cue == PresentationCue.BossPhase) phases.Add(signal.value); };
            boss.TakeDamage(350);
            Tick(boss, BattleRules.StepSeconds);
            boss.Heal(350);
            Tick(boss, BattleRules.StepSeconds);
            boss.TakeDamage(350);
            Tick(boss, BattleRules.StepSeconds);
            Assert.That(phases, Is.EqualTo(new[] { 2 }));
            Assert.That(boss.GetComponent<BossPhaseVisual>().CurrentPhase, Is.EqualTo(2));
            Assert.That(((ICollection)Get(session, "scheduledSpawns")).Count, Is.EqualTo(6));
            yield return null;
        }

        [UnityTest]
        public IEnumerator BossSkippingToPhaseThreeEmitsBothTransitionsInOrder()
        {
            Create();
            var boss = Spawn(EnemyArchetype.Boss, 1000);
            var phases = new List<int>();
            var visualPhases = new List<int>();
            session.PresentationSignaled += signal =>
            {
                if (signal.cue != PresentationCue.BossPhase) return;
                phases.Add(signal.value);
                visualPhases.Add(boss.GetComponent<BossPhaseVisual>().CurrentPhase);
            };
            boss.TakeDamage(700);
            Tick(boss, BattleRules.StepSeconds);
            Tick(boss, BattleRules.StepSeconds);
            Assert.That(phases, Is.EqualTo(new[] { 2, 3 }));
            Assert.That(visualPhases, Is.EqualTo(new[] { 2, 3 }));
            Assert.That(((ICollection)Get(session, "scheduledSpawns")).Count, Is.EqualTo(6));
            yield return null;
        }

        [UnityTest]
        public IEnumerator BossKeepsPhaseThreeSpeedAfterHealingAboveThreshold()
        {
            Create();
            var boss = Spawn(EnemyArchetype.Boss, 1000, 1f);
            Set(boss, "path", new[] { Vector3.zero, Vector3.forward * 100f });
            boss.transform.position = Vector3.zero;
            boss.TakeDamage(700);
            Tick(boss, .1f);
            Vector3 before = boss.Position;
            boss.Heal(700);
            Tick(boss, .1f);
            Assert.That(Vector3.Distance(before, boss.Position), Is.EqualTo(.144f).Within(.001f));
            Assert.That(Get(boss, "bossPhase"), Is.EqualTo(3));
            yield return null;
        }

        [UnityTest]
        public IEnumerator DisruptedBarracksStopsAttacksAndMovementButStillBlocks()
        {
            Create();
            Assert.That(session.TryBuildOutpost(0, OutpostKind.Barracks), Is.True);
            Advance(5f);
            var tower = session.Outposts[0];
            var guard = tower.Soldiers[0];
            var enemy = Spawn(EnemyArchetype.Raider, speed: 1f);
            enemy.transform.position = guard.Position;
            var positions = new List<Vector3>();
            foreach (var soldier in tower.Soldiers) positions.Add(soldier.Position);
            Vector3 enemyPosition = enemy.Position;
            tower.DisableTemporarily(2f);
            Advance(1f);
            Assert.That(enemy.Health, Is.EqualTo(10000));
            Assert.That(enemy.Position, Is.EqualTo(enemyPosition));
            Assert.That(enemy.State, Is.EqualTo(EnemyCombatState.Attacking));
            for (int i = 0; i < tower.Soldiers.Count; i++)
            {
                Assert.That(tower.Soldiers[i].CanBlock, Is.True);
                Assert.That(tower.Soldiers[i].Position, Is.EqualTo(positions[i]));
            }
            Advance(1.2f);
            Assert.That(tower.IsDisrupted, Is.False);
            Assert.That(tower.Soldiers.Count, Is.EqualTo(3));
            Assert.That(enemy.Health, Is.LessThan(10000));
            yield return null;
        }

        [UnityTest]
        public IEnumerator LastLifeLeakStopsRemainingActorsAndDeathRewardsInSameStep()
        {
            Create();
            var hero = session.GetComponentInChildren<GreyboxHero>();
            var victim = Spawn(EnemyArchetype.Raider, 1);
            victim.transform.position = hero.Position;
            victim.Stun(2f);
            var follower = Spawn(EnemyArchetype.Raider, speed: 1f);
            var leaker = Spawn(EnemyArchetype.Raider, baseDamage: session.Lives);
            Set(leaker, "targetWaypoint", session.Path.Count);
            leaker.transform.position = session.Path[0];
            int gold = session.Gold, deaths = 0;
            Vector3 followerPosition = follower.Position;
            session.PresentationSignaled += signal => { if (signal.cue == PresentationCue.EnemyDefeated) deaths++; };
            session.AdvanceBattle(.1f);
            Assert.That(session.Phase, Is.EqualTo(GamePhase.Defeat));
            Assert.That(session.Gold, Is.EqualTo(gold));
            Assert.That(deaths, Is.Zero);
            Assert.That(victim.IsAlive, Is.True);
            Assert.That(follower.Position, Is.EqualTo(followerPosition));
            Assert.That(Get(session, "stepAccumulator"), Is.EqualTo(0d));
            yield return null;
        }

        [UnityTest]
        public IEnumerator VictoryDuringOutpostTickStopsRemainingActorsAndClockRemainder()
        {
            Create();
            Assert.That(session.TryBuildOutpost(0, OutpostKind.Archer), Is.True);
            var enemy = Spawn(EnemyArchetype.Raider, 1);
            enemy.transform.position = session.Outposts[0].Position;
            enemy.Stun(2f);
            typeof(GameSessionController).GetMethod("SetPhase", PrivateInstance).Invoke(session, new object[] { GamePhase.Wave });
            Set(session, "currentWaveIndex", ((System.Array)Get(session, "waves")).Length - 1);
            Set(session, "spawning", false);
            var hero = session.GetComponentInChildren<GreyboxHero>();
            Set(hero, "attackTimer", 1f);
            int goldAtVictory = -1;
            session.PhaseChanged += phase => { if (phase == GamePhase.Victory) goldAtVictory = session.Gold; };
            session.AdvanceBattle(.1f);
            Assert.That(session.Phase, Is.EqualTo(GamePhase.Victory));
            Assert.That(session.Gold, Is.EqualTo(goldAtVictory));
            Assert.That(Get(hero, "attackTimer"), Is.EqualTo(1f));
            Assert.That(Get(session, "stepAccumulator"), Is.EqualTo(0d));
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (session != null) Object.Destroy(session.gameObject);
            Time.timeScale = 1f;
            yield return null;
        }
    }
}
