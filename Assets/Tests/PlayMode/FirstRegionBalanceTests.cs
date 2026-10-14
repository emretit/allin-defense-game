using System.Collections;
using System.Linq;
using AllinDefense.Combat;
using AllinDefense.Core;
using AllinDefense.Data;
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
                yield return CampaignBalanceBot.RunBalance(level, strategy, (session, outcome) =>
                {
                    Assert.That(session.PeakEnemyCount, Is.LessThanOrEqualTo(60), "Concurrent enemy budget");
                    var target = FirstRegionCampaign.Level(level);
                    Debug.Log($"[BALANCE] level={level} strategy={strategy} phase={session.Phase} lives={session.Lives} seconds={session.BattleElapsedSeconds:F2} gold={session.Gold} peak={session.PeakEnemyCount} towers={session.Outposts.Count} specialized={session.Outposts.Count(o => o.Specialization != OutpostSpecialization.None)} target={target.targetMinSeconds}-{target.targetMaxSeconds}");
                    int minimumLives = level <= 3 ? 15 : 8;
                    if (session.Lives < minimumLives) failures.Add($"level {level} strategy {strategy}: only {session.Lives} lives, expected >= {minimumLives}");
                    if (session.Phase != GamePhase.Victory || session.BattleElapsedSeconds < target.targetMinSeconds || session.BattleElapsedSeconds > target.targetMaxSeconds) failures.Add($"level {level} strategy {strategy}: {session.Phase}, {session.BattleElapsedSeconds:F2}s");
                });
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
