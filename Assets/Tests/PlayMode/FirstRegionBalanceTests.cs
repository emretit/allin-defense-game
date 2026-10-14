using System.Collections;
using System.Linq;
using AllinDefense.Combat;
using AllinDefense.Core;
using AllinDefense.Data;
using AllinDefense.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AllinDefense.Tests
{
    public sealed class FirstRegionBalanceTests
    {
        [UnityTest]
        public IEnumerator RealResourceCampaign()
        {
            var failures = new System.Collections.Generic.List<string>();
            for (int level = 1; level <= 10; level++)
            for (int strategy = 0; strategy < (level >= 4 ? 2 : 1); strategy++)
            {
                var session = new GameObject("Closed test balance").AddComponent<GameSessionController>();
                session.ConfigurePresentation(Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog"));
                session.SetCampaignLevel(level); session.Initialize();
                // Every purchase goes through the production resource and ownership checks.
                float nextAction = 0, spentTime = 0;
                session.StartNextWave();
                while (session.Phase != GamePhase.Victory && session.Phase != GamePhase.Defeat && spentTime < 550)
                {
                    if (spentTime >= nextAction)
                    {
                        for (int slot = 0; slot < session.SlotCount; slot++)
                        {
                            var tower = session.Outposts.FirstOrDefault(o => o != null && o.Slot.Index == slot);
                            OutpostKind kind = level == 1 ? OutpostKind.Archer : level == 2
                                ? (slot % 3 == 2 ? OutpostKind.Barracks : OutpostKind.Archer)
                                : strategy == 0 ? (OutpostKind)(slot % 3) : new[] { OutpostKind.Archer, OutpostKind.Mage, OutpostKind.Archer, OutpostKind.Barracks, OutpostKind.Mage, OutpostKind.Archer, OutpostKind.Barracks, OutpostKind.Mage }[slot];
                            if (tower == null) session.TryBuildOutpost(slot, kind);
                            else if (tower.CanUpgrade) session.TryUpgradeOutpost(slot);
                            else if (tower.CanSpecialize && spentTime > 60) session.TrySpecializeOutpost(slot,
                                TowerProgression.Branch(tower.Kind, strategy));
                        }
                        if (session.Hero != null) session.Hero.TryUseShieldBash();
                        nextAction += 1;
                    }
                    session.AdvanceBattle(1f / 30f); spentTime += 1f / 30f;
                    // Drain Unity's deferred destroys without adding battle time.
                    if ((int)(spentTime * 30) % 300 == 0) { Time.timeScale = 0; yield return null; }
                }
                Assert.That(session.PeakEnemyCount, Is.LessThanOrEqualTo(60), "Concurrent enemy budget");
                var target = FirstRegionCampaign.Level(level);
                Debug.Log($"[BALANCE] level={level} strategy={strategy} phase={session.Phase} lives={session.Lives} seconds={session.BattleElapsedSeconds:F2} gold={session.Gold} peak={session.PeakEnemyCount} towers={session.Outposts.Count} specialized={session.Outposts.Count(o => o.Specialization != OutpostSpecialization.None)} target={target.targetMinSeconds}-{target.targetMaxSeconds}");
                int minimumLives = level <= 3 ? 15 : 8;
                if (session.Lives < minimumLives) failures.Add($"level {level} strategy {strategy}: only {session.Lives} lives, expected >= {minimumLives}");
                if (session.Phase != GamePhase.Victory || session.BattleElapsedSeconds < target.targetMinSeconds || session.BattleElapsedSeconds > target.targetMaxSeconds) failures.Add($"level {level} strategy {strategy}: {session.Phase}, {session.BattleElapsedSeconds:F2}s");
                Object.Destroy(session.gameObject); Time.timeScale = 0; yield return null;
            }
            Assert.That(failures, Is.Empty, string.Join("; ", failures));
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            foreach(var session in Object.FindObjectsByType<GameSessionController>(FindObjectsSortMode.None)) Object.Destroy(session.gameObject);
            Time.timeScale = 1; yield return null;
        }
    }
}
