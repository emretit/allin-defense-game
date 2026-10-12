using System;
using System.IO;
using AllinDefense.Core;
using NUnit.Framework;

namespace AllinDefense.Tests
{
    public class SaveRecoveryTests
    {
        [TestCase("local-guest","anonymous-new",false,true)]
        [TestCase("guest-a","guest-a",false,true)]
        [TestCase("guest-a","account-b",true,false)]
        [TestCase("local-guest","account-b",true,false)]
        [TestCase("guest-a","guest-b",false,false)]
        public void InterruptedAccountSignInCannotAttachGuestClaimsToAnotherOwner(string local,string cached,bool registered,bool expected)
        {
            Assert.That(UnityGameServicesController.CanRestoreGuestOwner(local,cached,registered),Is.EqualTo(expected));
        }
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
        public void VictoryCommitsCheckpointRemovalAndClaimTogether()
        {
            var store = new CampaignProgressStore(path);
            store.Data.checkpoint = new BattleCheckpoint { level=1,nextWave=2,gold=95,lives=20 };
            store.Save(); store.CompleteLevel(1,3,20);
            var reloaded = new CampaignProgressStore(path);
            Assert.That(reloaded.Data.checkpoint,Is.Null);
            Assert.That(reloaded.PendingClaimCount,Is.EqualTo(1));
            Assert.That(reloaded.ProgressFor(1).stars,Is.EqualTo(3));
        }

        [Test]
        public void FailedWriteKeepsInMemoryProgressAndRecoversOnRetry()
        {
            var store = new CampaignProgressStore(path);
            Directory.CreateDirectory(store.SavePath);
            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Warning,new System.Text.RegularExpressions.Regex("Local save retry required:"));
            store.CompleteLevel(1,3,20);
            Assert.That(store.HasWriteError,Is.True);
            Assert.That(store.ProgressFor(1).stars,Is.EqualTo(3));
            Directory.Delete(store.SavePath);
            store.Save();
            Assert.That(store.HasWriteError,Is.False);
            Assert.That(new CampaignProgressStore(path).ProgressFor(1).stars,Is.EqualTo(3));
        }

        [Test]
        public void UnreadableVersionsAreQuarantinedBeforeNewProgressCanWrite()
        {
            File.WriteAllText(path,"broken original"); File.WriteAllText(path+".bak","broken backup");
            var store = new CampaignProgressStore(path);
            Assert.That(store.Data.migrationNeedsReview,Is.True);
            string recovery = Directory.GetDirectories(directory,"*.unreadable-*")[0];
            store.CompleteLevel(1,3,20);
            Assert.That(File.ReadAllText(Path.Combine(recovery,"save")),Is.EqualTo("broken original"));
            Assert.That(File.ReadAllText(Path.Combine(recovery,"save.bak")),Is.EqualTo("broken backup"));
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
