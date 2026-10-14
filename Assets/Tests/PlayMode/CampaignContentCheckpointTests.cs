using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using AllinDefense.Core;
using AllinDefense.Data;
using AllinDefense.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace AllinDefense.Tests
{
    public sealed class CampaignContentCheckpointTests
    {
        private string directory;
        private string SavePath => Path.Combine(directory, "campaign.json");
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        [SetUp] public void Setup() => directory = Path.Combine(Application.temporaryCachePath, "campaign-content-" + Guid.NewGuid());

        private GameSessionController Create(int level, CampaignProgressStore store)
        {
            var session = new GameObject("Campaign content checkpoint").AddComponent<GameSessionController>();
            session.SetCampaignLevel(level);
            session.ConfigureCampaignStore(store);
            session.Initialize();
            Time.timeScale = 0;
            return session;
        }

        [UnityTest]
        public IEnumerator LevelElevenRestoresPersistedWaveAndPaidInventory()
        {
            var store = new CampaignProgressStore(SavePath);
            store.Data.unlockedLevel = 11;
            var session = Create(11, store);
            Assert.That(session.TryBuildOutpost(0, OutpostKind.Archer), Is.True);
            session.Hero.TakeDamage(70);
            session.StartNextWave();
            int gold = session.Gold;
            Assert.That(store.Data.checkpoint.ruleVersion, Is.EqualTo("region-02-v1"));
            Object.Destroy(session.gameObject); yield return null;
            store = new CampaignProgressStore(SavePath);
            session = Create(11, store);
            Assert.That(session.CheckpointRestored, Is.True);
            Assert.That(session.Gold, Is.EqualTo(gold));
            Assert.That(session.CurrentWaveIndex, Is.Zero);
            Assert.That(session.Outposts.Single().Kind, Is.EqualTo(OutpostKind.Archer));
            Assert.That(session.Hero.HealthFraction, Is.EqualTo(170f / 240f));
        }

        [UnityTest]
        public IEnumerator RegionTwoVersionChangePreservesRegionOneCheckpoint()
        {
            var store = new CampaignProgressStore(SavePath);
            store.Data.unlockedLevel = 50;
            var session = Create(1, store);
            session.StartNextWave();
            Object.Destroy(session.gameObject); yield return null;
            var region = CampaignContent.Region(2);
            string previous = region.contentVersion;
            try
            {
                region.contentVersion = "region-02-test-update";
                store = new CampaignProgressStore(SavePath);
                Assert.That(GameSessionController.IsValidCheckpoint(store, store.Data.checkpoint), Is.True);
                session = Create(1, store);
                Assert.That(session.CheckpointRestored, Is.True);
                Assert.That(store.Data.checkpoint.ruleVersion, Is.EqualTo("first-region-v2"));
            }
            finally { region.contentVersion = previous; }
        }

        [UnityTest]
        public IEnumerator ChangedRegionVersionRestartsOnlyBattleWithBalanceMessage()
        {
            var store = new CampaignProgressStore(SavePath);
            store.Data.unlockedLevel = 50;
            var session = Create(11, store);
            session.StartNextWave();
            Object.Destroy(session.gameObject); yield return null;
            var region = CampaignContent.Region(2);
            string previous = region.contentVersion;
            try
            {
                region.contentVersion = "region-02-test-update";
                store = new CampaignProgressStore(SavePath);
                session = Create(11, store);
                yield return null;
                Assert.That(session.CheckpointRestored, Is.False);
                Assert.That(store.Data.checkpoint, Is.Null);
                Assert.That(store.Data.unlockedLevel, Is.EqualTo(50));
                Assert.That(session.GetComponentsInChildren<UnityEngine.UI.Text>(true).Any(t =>
                    t.text == FrontendCopy.BalanceUpdatedBattleRestart.Value), Is.True);
            }
            finally { region.contentVersion = previous; }
        }

        [TestCase(40, "deep-lord", 12, 2)]
        [TestCase(50, "last-lord", 18, 3)]
        [TestCase(20, "gate-master", 8, 1)]
        public void BossReinforcementsConsumeSelectedProfile(int level, string profileId, int count, int entrances)
        {
            var content = CampaignContent.Level(level);
            string previous = content.bossProfileId;
            try
            {
                content.bossProfileId = profileId;
                var store = new CampaignProgressStore(SavePath);
                store.Data.unlockedLevel = 50;
                var session = Create(level, store);
                typeof(GameSessionController).GetMethod("SpawnBossReinforcements", Private).Invoke(session, null);
                var spawns = ((IEnumerable)typeof(GameSessionController).GetField("scheduledSpawns", Private).GetValue(session)).Cast<object>().ToArray();
                Assert.That(spawns.Length, Is.EqualTo(count));
                Assert.That(spawns.Select(s => (int)s.GetType().GetField("routeId").GetValue(s)).Distinct().Count(), Is.EqualTo(entrances));
                var enemies = spawns.Select(s => (EnemyArchetype)s.GetType().GetField("archetype").GetValue(s)).ToArray();
                if (profileId == "deep-lord") Assert.That(enemies.Count(e => e == EnemyArchetype.Support), Is.EqualTo(2));
                if (profileId == "gate-master") Assert.That(enemies, Is.All.EqualTo(EnemyArchetype.Vanguard));
            }
            finally { content.bossProfileId = previous; }
        }

        [UnityTearDown] public IEnumerator Cleanup()
        {
            foreach (var session in Object.FindObjectsByType<GameSessionController>(FindObjectsSortMode.None)) Object.Destroy(session.gameObject);
            Time.timeScale = 1;
            yield return null;
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }
}
