using System;
using System.Collections;
using System.IO;
using System.Reflection;
using DefenderChronicles.Core;
using DefenderChronicles.Data;
using DefenderChronicles.Presentation;
using DefenderChronicles.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DefenderChronicles.Tests
{
    public sealed class AuditBossVictoryTests
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private GameSessionController session;
        private CampaignProgressStore store;
        private string directory;
        private GameLanguage previousLanguage;

        [SetUp]
        public void Setup()
        {
            previousLanguage = GameLocalization.CurrentLanguage;
            directory = Path.Combine(Path.GetTempPath(), "dc-boss-victory-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            store = new CampaignProgressStore(Path.Combine(directory, "campaign.json"));
            store.Data.unlockedLevel = 10;
            session = new GameObject("Boss victory regression").AddComponent<GameSessionController>();
            session.ConfigureCampaignStore(store);
            session.SetCampaignLevel(10);
            session.Initialize();
            Time.timeScale = 0f;
        }

        [TestCase(20, GameLanguage.Turkish)]
        [TestCase(20, GameLanguage.English)]
        [TestCase(1, GameLanguage.Turkish)]
        public void FinalWaveBossBreachDefeatsWithoutGoldStarsRewardsOrClaims(int lives, GameLanguage language)
        {
            GameLocalization.SetLanguage(language);
            PrepareWave(session.WaveCount - 1, EnemyArchetype.Boss);
            typeof(GameSessionController).GetProperty(nameof(GameSessionController.Lives)).SetValue(session, lives);
            Invoke("SaveWaveCheckpoint");
            Assert.That(store.Data.checkpoint, Is.Not.Null);
            int gold = session.Gold;
            int reportedLives = -1;
            int defeats = 0, victories = 0;
            session.LivesChanged += value => reportedLives = value;
            session.PhaseChanged += phase =>
            {
                if (phase == GamePhase.Defeat) defeats++;
                if (phase == GamePhase.Victory) victories++;
            };

            Invoke("EnemyReachedKeep", session.Enemies[0], 5);

            Assert.That(session.Phase, Is.EqualTo(GamePhase.Defeat));
            Assert.That(session.Lives, Is.Zero);
            Assert.That(reportedLives, Is.Zero);
            Assert.That(defeats, Is.EqualTo(1));
            Assert.That(victories, Is.Zero);
            Assert.That(session.Enemies, Is.Empty);
            Assert.That(session.Gold, Is.EqualTo(gold));
            Assert.That(session.CurrentWaveIndex, Is.EqualTo(session.WaveCount - 1));
            Assert.That(session.CompletionReward.Valor, Is.Zero);
            Assert.That(session.CompletionReward.HeroXp, Is.Zero);
            Assert.That(store.Data.TotalStars, Is.Zero);
            Assert.That(store.Data.valor, Is.Zero);
            Assert.That(store.Data.heroXp, Is.Zero);
            Assert.That(store.PendingClaimCount, Is.Zero);
            Assert.That(store.Data.checkpoint, Is.Null);
            var persisted = new CampaignProgressStore(Path.Combine(directory, "campaign.json"));
            Assert.That(persisted.PendingClaimCount, Is.Zero);
            Assert.That(persisted.Data.TotalStars, Is.Zero);
            Assert.That(persisted.Data.checkpoint, Is.Null);
            var message = (Text)typeof(GameSessionController).GetField("messageText", PrivateInstance).GetValue(session);
            Assert.That(message.text, Is.EqualTo(GameLocalization.Text(
                "Bölge Lordu kaleyi ele geçirdi.", "The Region Lord breached the keep.")));
        }

        [Test]
        public void FinalWaveNormalBreachStillCompletesWithRemainingLivesAndClearBonus()
        {
            PrepareWave(session.WaveCount - 1, EnemyArchetype.Raider);
            var waves = (WaveDefinition[])typeof(GameSessionController).GetField("waves", PrivateInstance).GetValue(session);
            int gold = session.Gold;
            Invoke("EnemyReachedKeep", session.Enemies[0], 5);
            Assert.That(session.Phase, Is.EqualTo(GamePhase.Victory));
            Assert.That(session.Lives, Is.EqualTo(15));
            Assert.That(session.Gold, Is.EqualTo(gold + waves[waves.Length - 1].clearBonus));
            Assert.That(store.Data.TotalStars, Is.EqualTo(3));
            Assert.That(store.PendingClaimCount, Is.EqualTo(1));
        }

        [TestCase(2, 18)]
        [TestCase(0, 19)]
        public void NormalBreachKeepsExistingLifeDamageAndIntermission(int damage, int remainingLives)
        {
            PrepareWave(0, EnemyArchetype.Raider);
            Invoke("EnemyReachedKeep", session.Enemies[0], damage);
            Assert.That(session.Phase, Is.EqualTo(GamePhase.Intermission));
            Assert.That(session.Lives, Is.EqualTo(remainingLives));
            Assert.That(store.PendingClaimCount, Is.Zero);
        }

        [Test]
        public void SelectedArcherRangeUpdatesImmediatelyAfterUpgrade()
        {
            BuildArcher();
            float previous = session.Outposts[0].RangeValue;
            AssertSelectionRadius(session.Outposts[0]);
            Assert.That(session.TryUpgradeOutpost(0), Is.True);
            Assert.That(session.Outposts[0].RangeValue, Is.GreaterThan(previous));
            AssertSelectionRadius(session.Outposts[0]);
        }

        [Test]
        public void SelectedArcherRangeUpdatesImmediatelyAfterSpecialization()
        {
            BuildArcher();
            Assert.That(session.TryUpgradeOutpost(0), Is.True);
            Invoke("SelectOutpost", session.Outposts[0]);
            float previous = session.Outposts[0].RangeValue;
            Assert.That(session.TrySpecializeOutpost(0, OutpostSpecialization.Sharpshooter), Is.True);
            Assert.That(session.Outposts[0].RangeValue, Is.GreaterThan(previous));
            AssertSelectionRadius(session.Outposts[0]);
        }

        [Test]
        public void UpgradingAnotherArcherPreservesTheSelectedRange()
        {
            BuildArcher();
            Assert.That(session.TryBuildOutpost(1, OutpostKind.Archer), Is.True);
            GreyboxOutpost selected = session.Outposts[1];
            Assert.That(session.TryUpgradeOutpost(0), Is.True);
            AssertSelectionRadius(selected);
            Assert.That(typeof(GameSessionController).GetField("selectedOutpost", PrivateInstance).GetValue(session),
                Is.SameAs(selected));
        }

        private void BuildArcher()
        {
            Invoke("EarnGold", 1000);
            Assert.That(session.TryBuildOutpost(0, OutpostKind.Archer), Is.True);
            Invoke("SelectOutpost", session.Outposts[0]);
        }

        private void AssertSelectionRadius(GreyboxOutpost outpost)
        {
            var visual = (CombatRangeVisual)typeof(GameSessionController).GetField("selectionRange", PrivateInstance).GetValue(session);
            var ring = visual.GetComponent<LineRenderer>();
            Assert.That(ring.enabled, Is.True);
            for (int i = 0; i < ring.positionCount; i++)
            {
                Vector3 offset = ring.GetPosition(i) - outpost.Slot.transform.position;
                offset.y = 0f;
                Assert.That(offset.magnitude, Is.EqualTo(outpost.RangeValue).Within(.001f), $"Ring point {i}");
            }
        }

        private void PrepareWave(int index, EnemyArchetype archetype)
        {
            typeof(GameSessionController).GetField("currentWaveIndex", PrivateInstance).SetValue(session, index);
            Invoke("SetPhase", GamePhase.Wave);
            typeof(GameSessionController).GetMethod("SpawnEnemy", PrivateInstance, null,
                new[] { typeof(EnemyArchetype) }, null).Invoke(session, new object[] { archetype });
        }

        private void Invoke(string method, params object[] arguments) =>
            typeof(GameSessionController).GetMethod(method, PrivateInstance).Invoke(session, arguments);

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (session != null) Object.Destroy(session.gameObject);
            yield return null;
            Time.timeScale = 1f;
            GameLocalization.SetLanguage(previousLanguage);
            if (directory != null && Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }
}
