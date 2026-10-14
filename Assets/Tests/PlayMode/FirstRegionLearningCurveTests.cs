using System.Collections;
using AllinDefense.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AllinDefense.Tests
{
    public sealed class FirstRegionLearningCurveTests
    {
        private UnityEngine.Random.State randomState;
        private CampaignBalanceBot.Outcome outcome;
        [SetUp] public void Setup() => randomState = UnityEngine.Random.state;

        [UnityTest]
        public IEnumerator ArmourAndLaterLessonsAllowDifferentDefencesWithRealBudgets()
        {
            for (int level = 4; level <= 9; level++)
            {
                int victories = 0;
                for (int strategy = 0; strategy < CampaignBalanceBot.plans.Length; strategy++)
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

        private IEnumerator Run(int level, int strategy, float reactionSeconds, bool delayed, bool passive = false) =>
            CampaignBalanceBot.RunLearning(level, strategy, reactionSeconds, delayed, value => outcome = value, passive);

        [UnityTearDown] public IEnumerator Cleanup()
        {
            foreach (var session in Object.FindObjectsByType<GameSessionController>(FindObjectsSortMode.None)) Object.Destroy(session.gameObject);
            UnityEngine.Random.state = randomState;
            Time.timeScale = 1;
            yield return null;
        }
    }
}
