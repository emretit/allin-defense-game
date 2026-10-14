using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AllinDefense.Combat;
using AllinDefense.Core;
using AllinDefense.Data;
using AllinDefense.Presentation;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AllinDefense.Tests
{
    internal static class CampaignBalanceBot
    {
        [Serializable] internal sealed class Outcome
        {
            public bool victory;
            public string phase;
            public int lives, peak, stars, hardestWave, bossPhaseLivesLost;
            public float seconds, firstUpgrade, firstWaveEnd;
            public int[] leaksByEntrance, livesLostByWave, peakByWave;
        }
        internal static readonly OutpostKind[][] plans =
        {
            new[] { OutpostKind.Archer, OutpostKind.Barracks, OutpostKind.Archer, OutpostKind.Barracks,
                OutpostKind.Archer, OutpostKind.Mage, OutpostKind.Archer, OutpostKind.Archer },
            new[] { OutpostKind.Archer, OutpostKind.Barracks, OutpostKind.Mage, OutpostKind.Barracks,
                OutpostKind.Archer, OutpostKind.Barracks, OutpostKind.Barracks, OutpostKind.Barracks },
            new[] { OutpostKind.Archer, OutpostKind.Barracks, OutpostKind.Mage, OutpostKind.Archer,
                OutpostKind.Mage, OutpostKind.Mage, OutpostKind.Mage, OutpostKind.Mage }
        };
        private static OutpostKind Kind(int level, int strategy, int slot, bool balance)
        {
            if (level == 1) return OutpostKind.Archer;
            if (level == 2) return slot % 3 == 2 ? OutpostKind.Barracks : OutpostKind.Archer;
            if (!balance) return plans[strategy][slot % plans[strategy].Length];
            return strategy == 0 ? (OutpostKind)(slot % 3) :
                new[] { OutpostKind.Archer, OutpostKind.Mage, OutpostKind.Archer, OutpostKind.Barracks,
                    OutpostKind.Mage, OutpostKind.Archer, OutpostKind.Barracks, OutpostKind.Mage }[slot % 8];
        }

        private static void Act(GameSessionController session, int level, int strategy, float elapsed,
            int[] order, bool balance, bool delayed, Outcome outcome)
        {
            for (int index = 0; index < order.Length; index++)
            {
                int slot = order[index];
                var tower = session.Outposts.FirstOrDefault(o => o != null && o.Slot.Index == slot);
                bool purchased;
                if (tower == null) purchased = session.TryBuildOutpost(slot, Kind(level, strategy, slot, balance));
                else if (tower.CanUpgrade)
                {
                    purchased = session.TryUpgradeOutpost(slot);
                    if (purchased && outcome.firstUpgrade < 0) outcome.firstUpgrade = elapsed;
                }
                else purchased = tower.CanSpecialize && elapsed > 60 &&
                    session.TrySpecializeOutpost(slot, TowerProgression.Branch(tower.Kind, strategy % 2));
                if (delayed && purchased) break;
            }
        }

        internal static int[] SlotOrder(GameSessionController session, bool distribute)
        {
            if (!distribute || session.EntranceCount == 1) return Enumerable.Range(0, session.SlotCount).ToArray();
            var map = CampaignLevelFactory.Create(session.CampaignLevelNumber);
            var remaining = Enumerable.Range(0, session.SlotCount).ToList();
            var result = new List<int>();
            for (int index = 0; remaining.Count > 0; index++)
            {
                var route = session.Routes[index % session.EntranceCount];
                // Prefer coverage before routes merge; round robin spends across all entrances.
                var approach = route.Take(Math.Max(2, route.Length / 2)).ToArray();
                int slot = remaining.OrderBy(i => CampaignLevelFactory.DistanceToRoute(map.outpostSlots[i], approach)).ThenBy(i => i).First();
                result.Add(slot); remaining.Remove(slot);
            }
            return result.ToArray();
        }

        internal static IEnumerator RunBalance(int level, int strategy,
            Action<GameSessionController, Outcome> completed, bool measurement = false)
        {
            var session = new GameObject("Closed test balance").AddComponent<GameSessionController>();
            session.ConfigurePresentation(Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog"));
            session.SetCampaignLevel(level); session.Initialize();
            var outcome = new Outcome { firstUpgrade = -1, firstWaveEnd = -1 };
            var metrics = measurement ? new Metrics(session, outcome) : null;
            int[] order = SlotOrder(session, measurement);
            float limit = measurement ? CampaignContent.Level(level).targetMaxSeconds + 180 : 550;
            float nextAction = 0, spentTime = 0;
            session.StartNextWave();
            while (session.Phase != GamePhase.Victory && session.Phase != GamePhase.Defeat && spentTime < limit)
            {
                if (spentTime >= nextAction)
                {
                    Act(session, level, strategy, spentTime, order, true, false, outcome);
                    if (session.Hero != null) session.Hero.TryUseShieldBash();
                    nextAction += 1;
                }
                int wave = session.CurrentWaveIndex;
                session.AdvanceBattle(1f / 30f); spentTime += 1f / 30f;
                metrics?.Observe(wave);
                if (outcome.firstWaveEnd < 0 && session.CurrentWaveIndex > 0) outcome.firstWaveEnd = spentTime;
                // Drain Unity's deferred destroys without adding battle time.
                if ((int)(spentTime * 30) % 300 == 0) { Time.timeScale = 0; yield return null; }
            }
            metrics?.Finish();
            outcome.phase = session.Phase.ToString();
            outcome.victory = session.Phase == GamePhase.Victory;
            outcome.lives = session.Lives;
            outcome.seconds = session.BattleElapsedSeconds;
            outcome.peak = session.PeakEnemyCount;
            outcome.stars = outcome.victory ? BattleRules.StarsForLives(session.Lives, session.StartingLivesForLevel) : 0;
            completed(session, outcome);
            Object.Destroy(session.gameObject); Time.timeScale = 0; yield return null;
        }

        internal static IEnumerator RunLearning(int level, int strategy, float reactionSeconds, bool delayed, System.Action<Outcome> completed, bool passive = false, bool measurement = false)
        {
            UnityEngine.Random.InitState(2300 + level);
            Time.timeScale = 0;
            var session = new GameObject("First region learning curve").AddComponent<GameSessionController>();
            session.ConfigurePresentation(Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog"));
            session.SetCampaignLevel(level);
            session.Initialize();
            Time.timeScale = 0;
            int[] opening = SlotOrder(session, measurement);
            bool firstBuilt = session.TryBuildOutpost(opening[0], OutpostKind.Archer);
            if (!measurement) Assert.That(firstBuilt, Is.True);
            OutpostKind second = passive ? OutpostKind.Archer : delayed ? level == 1 ? OutpostKind.Archer : level == 2 ? OutpostKind.Barracks : OutpostKind.Mage : plans[strategy][2];
            bool secondBuilt = session.TryBuildOutpost(opening[measurement && session.EntranceCount > 1 ? 1 : 2], second);
            if (!measurement) Assert.That(secondBuilt, Is.True, "Two useful opening towers must fit the real starting budget.");
            if (passive)
            {
                bool thirdBuilt = session.TryBuildOutpost(opening[measurement && session.EntranceCount > 1 ? 2 : 4], OutpostKind.Archer);
                if (!measurement) Assert.That(thirdBuilt, Is.True);
            }
            session.StartNextWave();
            float elapsed = 0, nextAction = passive ? float.PositiveInfinity : delayed ? reactionSeconds : 0;
            var outcome = new Outcome { firstUpgrade = -1, firstWaveEnd = -1 };
            var metrics = measurement ? new Metrics(session, outcome) : null;
            int[] order = SlotOrder(session, measurement);
            int frameLimit = measurement ? Mathf.CeilToInt((CampaignContent.Level(level).targetMaxSeconds + 180) * 30) : 18000;
            for (int frame = 0; frame < frameLimit && session.Phase is not (GamePhase.Victory or GamePhase.Defeat); frame++)
            {
                if (elapsed >= nextAction)
                {
                    Act(session, level, strategy, elapsed, order, false, delayed, outcome);
                    session.Hero.TryUseShieldBash();
                    nextAction += reactionSeconds;
                }
                int wave = session.CurrentWaveIndex;
                session.AdvanceBattle(1f / 30f);
                metrics?.Observe(wave);
                elapsed += 1f / 30f;
                if (outcome.firstWaveEnd < 0 && session.CurrentWaveIndex > 0) outcome.firstWaveEnd = elapsed;
                if (frame % 300 == 0) yield return null;
            }
            metrics?.Finish();
            outcome.phase = session.Phase.ToString();
            outcome.victory = session.Phase == GamePhase.Victory;
            outcome.lives = session.Lives;
            outcome.stars = outcome.victory ? BattleRules.StarsForLives(session.Lives, session.StartingLivesForLevel) : 0;
            outcome.seconds = session.BattleElapsedSeconds;
            outcome.peak = session.PeakEnemyCount;
            if (!measurement) Debug.Log($"[LEARNING_CURVE] level={level} strategy={strategy} delayed={delayed} passive={passive} phase={session.Phase} lives={outcome.lives} seconds={outcome.seconds:F2} gold={session.Gold} peak={outcome.peak} upgrade={outcome.firstUpgrade:F2} firstWave={outcome.firstWaveEnd:F2}");
            if (!measurement) Assert.That(outcome.peak, Is.LessThanOrEqualTo(60));
            completed(outcome);
            Object.Destroy(session.gameObject); yield return null;
        }

        private sealed class Metrics
        {
            private static readonly FieldInfo EnemyPath = typeof(GreyboxEnemy).GetField("path", BindingFlags.Instance | BindingFlags.NonPublic);
            private readonly GameSessionController session;
            private readonly Outcome outcome;
            private int previousLives;

            internal Metrics(GameSessionController session, Outcome outcome)
            {
                this.session = session; this.outcome = outcome; previousLives = session.Lives;
                outcome.leaksByEntrance = new int[session.EntranceCount];
                outcome.livesLostByWave = new int[session.WaveCount];
                outcome.peakByWave = new int[session.WaveCount];
                session.LivesChanged += LivesChanged;
            }

            private void LivesChanged(int lives)
            {
                int lost = previousLives - lives; previousLives = lives;
                if (lost <= 0) return;
                int wave = Mathf.Clamp(session.CurrentWaveIndex, 0, session.WaveCount - 1);
                outcome.livesLostByWave[wave] += lost;
                if (CampaignContent.Level(session.CampaignLevelNumber).waves[wave].bossWave) outcome.bossPhaseLivesLost += lost;
                // ReachKeep marks the enemy removed before LivesChanged, then removes it from Enemies.
                var breached = session.Enemies.FirstOrDefault(e => e != null && !e.IsAlive && e.Health > 0);
                if (breached == null) throw new InvalidOperationException("Life loss without a breached enemy.");
                var route = EnemyPath.GetValue(breached);
                int entrance = Enumerable.Range(0, session.EntranceCount).First(i => ReferenceEquals(session.Routes[i], route));
                outcome.leaksByEntrance[entrance]++;
            }

            internal void Observe(int wave)
            {
                if (wave < outcome.peakByWave.Length) outcome.peakByWave[wave] = Math.Max(outcome.peakByWave[wave], session.Enemies.Count);
            }

            internal void Finish()
            {
                session.LivesChanged -= LivesChanged;
                outcome.hardestWave = Enumerable.Range(0, session.WaveCount).OrderByDescending(i => outcome.livesLostByWave[i])
                    .ThenByDescending(i => outcome.peakByWave[i]).ThenBy(i => i).First() + 1;
            }
        }
    }
}
