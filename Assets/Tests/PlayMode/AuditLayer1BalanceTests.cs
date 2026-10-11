using System.Collections;
using System.Collections.Generic;
using DefenderChronicles.Core;
using DefenderChronicles.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Data = DefenderChronicles.Data;

namespace DefenderChronicles.Tests
{
    public sealed class AuditLayer1BalanceTests
    {
        [UnityTest]
        public IEnumerator ArcherHeavyKillsBossWithMoreThanOneStarLives() => RunStrategy("Archer-heavy", ArcherHeavyPlan());

        [UnityTest]
        public IEnumerator BarracksHeavyKillsBossWithMoreThanOneStarLives() => RunStrategy("Barracks-heavy", BarracksHeavyPlan());

        [UnityTest]
        public IEnumerator MageHeavyStillKillsBoss() => RunStrategy("Mage-heavy", MageHeavyPlan());

        [UnityTest]
        public IEnumerator PrimitiveArcherHeavyKillsBossWithMoreThanOneStarLives() => RunStrategy("Primitive Archer-heavy", ArcherHeavyPlan(), false);

        [UnityTest]
        public IEnumerator PrimitiveBarracksHeavyKillsBossWithMoreThanOneStarLives() => RunStrategy("Primitive Barracks-heavy", BarracksHeavyPlan(), false);

        [UnityTest]
        public IEnumerator PrimitiveMageHeavyStillKillsBoss() => RunStrategy("Primitive Mage-heavy", MageHeavyPlan(), false);

        private static IEnumerator RunStrategy(string name, IReadOnlyList<StrategyAction> plan, bool production = true)
        {
            Time.timeScale = 0f;
            var session = new GameObject("Audit balance " + name).AddComponent<GameSessionController>();
            if (production) session.ConfigurePresentation(Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog"));
            session.SetCampaignLevel(10);
            session.Initialize();
            Time.timeScale = 0f;
            yield return null;
            int actionIndex = 0, knightAttacks = 0, bossKills = 0;
            session.PresentationSignaled += signal =>
            {
                if (signal.cue == PresentationCue.HeroAttacked) knightAttacks++;
            };
            session.StartNextWave();
            int steps = 0;
            while (session.Phase != GamePhase.Victory && session.Phase != GamePhase.Defeat && steps < 16500)
            {
                ExecuteAffordableActions(session, plan, ref actionIndex);
                if (session.Phase == GamePhase.Intermission) session.StartNextWave();
                if (session.Hero != null && session.Hero.IsAlive && session.Hero.AbilityRemaining <= 0f)
                    session.Hero.TryUseShieldBash();
                GreyboxEnemy boss = null;
                foreach (var enemy in session.Enemies)
                    if (enemy != null && enemy.Archetype == Data.EnemyArchetype.Boss) boss = enemy;
                session.AdvanceBattle(1f / 30f);
                if (boss != null && !boss.IsAlive)
                {
                    if (boss.Health == 0) bossKills++;
                    Debug.Log($"[AUDIT_BOSS] strategy={name} remainingHealth={boss.Health}");
                }
                steps++;
                if (steps % 300 == 0) yield return null;
            }
            Debug.Log($"[AUDIT_BALANCE] strategy={name} phase={session.Phase} wave={session.CurrentWaveIndex}/{session.WaveCount} lives={session.Lives} seconds={session.BattleElapsedSeconds:F2} knightAttacks={knightAttacks} bossKills={bossKills} worstLeak={session.MostDamagingLeak} actions={actionIndex}/{plan.Count}");
            Assert.That(session.Phase, Is.EqualTo(GamePhase.Victory));
            Assert.That(session.Lives, Is.GreaterThan(7));
            Assert.That(bossKills, Is.EqualTo(1), "A balance victory must defeat the boss rather than accept a breach.");
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            foreach (var session in Object.FindObjectsByType<GameSessionController>(FindObjectsSortMode.None))
                Object.Destroy(session.gameObject);
            Time.timeScale = 1f;
            yield return null;
        }

        private static void ExecuteAffordableActions(GameSessionController session,
            IReadOnlyList<StrategyAction> plan, ref int actionIndex)
        {
            while (actionIndex < plan.Count)
            {
                StrategyAction action = plan[actionIndex];
                bool completed = action.upgrade ? session.TryUpgradeOutpost(action.slotIndex)
                    : session.TryBuildOutpost(action.slotIndex, action.kind);
                if (!completed) return;
                actionIndex++;
            }
        }

        private static StrategyAction[] ArcherHeavyPlan() => new[]
        {
            Build(0, Data.OutpostKind.Archer),
            Build(2, Data.OutpostKind.Archer),
            Upgrade(0),
            Build(4, Data.OutpostKind.Archer),
            Upgrade(2),
            Build(6, Data.OutpostKind.Archer),
            Build(7, Data.OutpostKind.Archer),
            Upgrade(4),
            Upgrade(6),
            Upgrade(7),
            Build(1, Data.OutpostKind.Barracks),
            Build(3, Data.OutpostKind.Barracks),
            Build(5, Data.OutpostKind.Mage),
            Upgrade(1),
            Upgrade(3),
            Upgrade(5)
        };

        private static StrategyAction[] BarracksHeavyPlan() => new[]
        {
            Build(1, Data.OutpostKind.Barracks),
            Build(0, Data.OutpostKind.Archer),
            Build(2, Data.OutpostKind.Mage),
            Build(3, Data.OutpostKind.Barracks),
            Upgrade(1),
            Build(5, Data.OutpostKind.Barracks),
            Upgrade(3),
            Build(7, Data.OutpostKind.Barracks),
            Build(6, Data.OutpostKind.Barracks),
            Upgrade(5),
            Upgrade(7),
            Upgrade(6),
            Build(4, Data.OutpostKind.Archer),
            Upgrade(0),
            Upgrade(2),
            Upgrade(4)
        };

        private static StrategyAction[] MageHeavyPlan() => new[]
        {
            Build(2, Data.OutpostKind.Mage),
            Build(0, Data.OutpostKind.Archer),
            Upgrade(2),
            Build(4, Data.OutpostKind.Mage),
            Build(6, Data.OutpostKind.Mage),
            Upgrade(4),
            Build(7, Data.OutpostKind.Mage),
            Upgrade(6),
            Build(5, Data.OutpostKind.Mage),
            Upgrade(7),
            Upgrade(5),
            Build(1, Data.OutpostKind.Barracks),
            Build(3, Data.OutpostKind.Archer),
            Upgrade(0),
            Upgrade(1),
            Upgrade(3)
        };

        private static StrategyAction Build(int slotIndex, Data.OutpostKind kind) =>
            new(slotIndex, kind, false);

        private static StrategyAction Upgrade(int slotIndex) =>
            new(slotIndex, default, true);

        private readonly struct StrategyAction
        {
            public readonly int slotIndex;
            public readonly Data.OutpostKind kind;
            public readonly bool upgrade;

            public StrategyAction(int slotIndex, Data.OutpostKind kind, bool upgrade)
            {
                this.slotIndex = slotIndex;
                this.kind = kind;
                this.upgrade = upgrade;
            }
        }
    }
}
