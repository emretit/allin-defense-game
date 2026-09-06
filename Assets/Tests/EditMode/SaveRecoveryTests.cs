using System;
using System.IO;
using DefenderChronicles.Core;
using NUnit.Framework;

namespace DefenderChronicles.Tests
{
    public class SaveRecoveryTests
    {
        private string directory;
        private string path;
        [SetUp] public void Setup()
        {
            directory = Path.Combine(Path.GetTempPath(), "dc-save-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            path = Path.Combine(directory, "campaign.json");
        }
        [TearDown] public void Cleanup() => Directory.Delete(directory, true);

        [TestCase(SaveWriteStage.TemporaryFlushed)] [TestCase(SaveWriteStage.BeforeReplace)]
        [TestCase(SaveWriteStage.Committed)]
        public void InterruptedWritesRecoverAnEntireValidVersion(SaveWriteStage failure)
        {
            AtomicSaveFile.Write(path, "old");
            Assert.Throws<IOException>(() => AtomicSaveFile.Write(path, "new", stage =>
            { if (stage == failure) throw new IOException("Simulated interruption"); }));
            Assert.That(AtomicSaveFile.TryRead(path, x => x is "old" or "new", out string data, out _), Is.True);
            Assert.That(data, Is.EqualTo(failure == SaveWriteStage.Committed ? "new" : "old"));
        }

        [Test]
        public void CorruptMainRecoversBackup()
        {
            AtomicSaveFile.Write(path, "old"); AtomicSaveFile.Write(path, "new");
            File.WriteAllText(path, "broken");
            Assert.That(AtomicSaveFile.TryRead(path, x => x is "old" or "new", out string data, out bool recovered), Is.True);
            Assert.That(recovered, Is.True); Assert.That(data, Is.EqualTo("old"));
        }

        [Test]
        public void AccountsAndGuestKeepIndependentProgressAcrossRestart()
        {
            var store = new CampaignProgressStore(path);
            store.CompleteLevel(1, 3, 20);
            store.SignIn(PlayerAuthProvider.PlayerAccount, "account-a", "A");
            Assert.That(store.Data.levels, Is.Empty);
            Assert.That(store.CanImportGuest, Is.True);
            Assert.That(store.ImportGuestProgress(), Is.True);
            Assert.That(store.PendingClaims()[0].ownerPlayerId, Is.EqualTo("account-a"));
            store.SignIn(PlayerAuthProvider.PlayerAccount, "account-b", "B");
            Assert.That(store.PendingClaimCount, Is.Zero);
            Assert.That(store.Data.levels, Is.Empty);
            store.SignIn(PlayerAuthProvider.PlayerAccount, "account-a", "A");
            var reloaded = new CampaignProgressStore(path);
            Assert.That(reloaded.Data.playerId, Is.EqualTo("account-a"));
            Assert.That(reloaded.ProgressFor(1).stars, Is.EqualTo(3));
            Assert.That(reloaded.ImportGuestProgress(), Is.False);
        }

        [Test]
        public void LegacyLateCampaignIsRetainedWhenMigrationIsIncomplete()
        {
            File.WriteAllText(path, "{\"version\":3,\"unlockedLevel\":50,\"lastSelectedLevel\":50,\"levels\":[{\"level\":50,\"stars\":3,\"bestLives\":12}]}");
            var store = new CampaignProgressStore(path);
            store.ApplyServerSync(new CampaignSyncResult { state = new CampaignServerState
                { revision = 1, migrationIncomplete = true } });
            Assert.That(store.Data.unlockedLevel, Is.EqualTo(50));
            Assert.That(store.ProgressFor(50).stars, Is.EqualTo(3));
            Assert.That(store.SyncStatus, Is.EqualTo(CampaignSyncStatus.RequiresReview));
            Assert.That(File.Exists(path + ".legacy-v3"), Is.True);
        }

        [Test]
        public void RejectedClaimKeepsReasonAndCannotPretendToBeSynchronized()
        {
            var store = new CampaignProgressStore(path); store.CompleteLevel(1, 3, 20);
            string id = store.PendingClaims()[0].claimId;
            var response = new CampaignSyncResult(); response.state.revision = 1;
            response.decisions.Add(new CampaignClaimDecision { claimId = id, accepted = false, reason = "rules_mismatch" });
            store.ApplyServerSync(response);
            Assert.That(store.PendingClaimCount, Is.Zero);
            Assert.That(store.Data.rejectedClaims[0].reason, Is.EqualTo("rules_mismatch"));
            Assert.That(store.SyncStatus, Is.EqualTo(CampaignSyncStatus.RequiresReview));
        }

        [Test]
        public void NormalizingOldQueueDoesNotRelabelItsRulesVersion()
        {
            var data = new CampaignSaveData();
            data.pendingClaims.Add(new CampaignCompletionClaim { claimId = "old", sequence = 1,
                level = 1, stars = 3, remainingLives = 20, rulesVersion = "legacy-rules" });
            CampaignProgressStore.Normalize(data);
            Assert.That(data.pendingClaims[0].rulesVersion, Is.EqualTo("legacy-rules"));
        }
    }
}
