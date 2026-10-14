using System.Collections;
using System.Linq;
using AllinDefense.Core;
using AllinDefense.Combat;
using AllinDefense.Data;
using AllinDefense.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AllinDefense.Tests
{
    public sealed class FirstRegionLearningCurveTests
    {
        private GameSessionController session;
        private UnityEngine.Random.State randomState;
        private Outcome outcome;
        private struct Outcome
        {
            public bool victory;
            public int lives, peak, stars;
            public float seconds, firstUpgrade, firstWaveEnd;
        }
        private static readonly OutpostKind[][] plans =
        {
            new[] { OutpostKind.Archer, OutpostKind.Barracks, OutpostKind.Archer, OutpostKind.Barracks,
                OutpostKind.Archer, OutpostKind.Mage, OutpostKind.Archer, OutpostKind.Archer },
            new[] { OutpostKind.Archer, OutpostKind.Barracks, OutpostKind.Mage, OutpostKind.Barracks,
                OutpostKind.Archer, OutpostKind.Barracks, OutpostKind.Barracks, OutpostKind.Barracks },
            new[] { OutpostKind.Archer, OutpostKind.Barracks, OutpostKind.Mage, OutpostKind.Archer,
                OutpostKind.Mage, OutpostKind.Mage, OutpostKind.Mage, OutpostKind.Mage }
        };
        [SetUp] public void Setup() => randomState = UnityEngine.Random.state;

        [UnityTest]
        public IEnumerator ArmourAndLaterLessonsAllowDifferentDefencesWithRealBudgets()
        {
            for (int level = 4; level <= 9; level++)
            {
                int victories = 0;
                for (int strategy = 0; strategy < plans.Length; strategy++)
                {
                    yield return Run(level, strategy, 1, false);
                    if (outcome.victory && outcome.lives >= 8) victories++;
                    if (strategy == 0)
                        Assert.That(outcome.victory, Is.True, $"Level {level}: armour must leave an archer-heavy mixed defence viable.");
                    Assert.That(outcome.firstUpgrade, Is.InRange(0, outcome.firstWaveEnd), $"Level {level}: first upgrade must be affordable in wave one.");
                }
                Assert.That(victories, Is.GreaterThanOrEqualTo(2), $"Level {level}: at least two different defences must earn two stars.");
            }
        }

        [UnityTest]
        public IEnumerator SlowerDecisionsCanLearnEachLessonWithoutPerfectTiming()
        {
            for (int level = 1; level <= 9; level++)
            {
                yield return Run(level, 1, 3, true);
                Assert.That(outcome.victory, Is.True, $"Level {level}: a three-second decision cadence must remain playable.");
                Assert.That(outcome.lives, Is.GreaterThanOrEqualTo(level <= 3 ? 15 : 8), $"Level {level}: delayed decisions should leave room to recover.");
            }
        }

        [UnityTest]
        public IEnumerator StaticUnupgradedArchersCannotFarmThreeStarLaterLessons()
        {
            for (int level = 4; level <= 9; level++)
            {
                yield return Run(level, 0, float.PositiveInfinity, false, true);
                Assert.That(outcome.stars, Is.LessThan(3), $"Level {level}: a static three-archer defence must need further decisions for three stars.");
            }
        }

        private IEnumerator Run(int level, int strategy, float reactionSeconds, bool delayed, bool passive = false)
        {
            UnityEngine.Random.InitState(2300 + level);
            Time.timeScale = 0;
            session = new GameObject("First region learning curve").AddComponent<GameSessionController>();
            session.ConfigurePresentation(Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog"));
            session.SetCampaignLevel(level);
            session.Initialize();
            Time.timeScale = 0;
            Assert.That(session.TryBuildOutpost(0, OutpostKind.Archer), Is.True);
            OutpostKind second = passive ? OutpostKind.Archer : delayed ? level == 1 ? OutpostKind.Archer : level == 2 ? OutpostKind.Barracks : OutpostKind.Mage : plans[strategy][2];
            Assert.That(session.TryBuildOutpost(2, second), Is.True, "Two useful opening towers must fit the real starting budget.");
            if (passive) Assert.That(session.TryBuildOutpost(4, OutpostKind.Archer), Is.True);
            session.StartNextWave();
            float elapsed = 0, nextAction = passive ? float.PositiveInfinity : delayed ? reactionSeconds : 0;
            outcome = new Outcome { firstUpgrade = -1, firstWaveEnd = -1 };
            for (int frame = 0; frame < 18000 && session.Phase is not (GamePhase.Victory or GamePhase.Defeat); frame++)
            {
                if (elapsed >= nextAction)
                {
                    for (int slot = 0; slot < session.SlotCount; slot++)
                    {
                        var tower = session.Outposts.FirstOrDefault(o => o != null && o.Slot.Index == slot);
                        bool purchased;
                        if (tower == null)
                        {
                            OutpostKind kind = level == 1 ? OutpostKind.Archer : level == 2
                                ? (slot % 3 == 2 ? OutpostKind.Barracks : OutpostKind.Archer) : plans[strategy][slot];
                            purchased = session.TryBuildOutpost(slot, kind);
                        }
                        else if (tower.CanUpgrade)
                        {
                            purchased = session.TryUpgradeOutpost(slot);
                            if (purchased && outcome.firstUpgrade < 0) outcome.firstUpgrade = elapsed;
                        }
                        else purchased = tower.CanSpecialize && elapsed > 60 &&
                            session.TrySpecializeOutpost(slot, TowerProgression.Branch(tower.Kind, strategy % 2));
                        if (delayed && purchased) break;
                    }
                    session.Hero.TryUseShieldBash();
                    nextAction += reactionSeconds;
                }
                session.AdvanceBattle(1f / 30f);
                elapsed += 1f / 30f;
                if (outcome.firstWaveEnd < 0 && session.CurrentWaveIndex > 0) outcome.firstWaveEnd = elapsed;
                if (frame % 300 == 0) yield return null;
            }
            outcome.victory = session.Phase == GamePhase.Victory;
            outcome.lives = session.Lives;
            outcome.stars = outcome.victory ? BattleRules.StarsForLives(session.Lives, session.StartingLivesForLevel) : 0;
            outcome.seconds = session.BattleElapsedSeconds;
            outcome.peak = session.PeakEnemyCount;
            Debug.Log($"[LEARNING_CURVE] level={level} strategy={strategy} delayed={delayed} passive={passive} phase={session.Phase} lives={outcome.lives} seconds={outcome.seconds:F2} gold={session.Gold} peak={outcome.peak} upgrade={outcome.firstUpgrade:F2} firstWave={outcome.firstWaveEnd:F2}");
            Assert.That(outcome.peak, Is.LessThanOrEqualTo(60));
            Object.Destroy(session.gameObject); yield return null;
        }

        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (session != null) Object.Destroy(session.gameObject);
            UnityEngine.Random.state = randomState;
            Time.timeScale = 1;
            yield return null;
        }
    }
}
