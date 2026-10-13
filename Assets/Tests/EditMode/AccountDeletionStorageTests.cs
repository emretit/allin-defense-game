using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using AllinDefense.Core;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AllinDefense.Tests
{
    public sealed class AccountDeletionStorageTests
    {
        [Test]
        public void DeletedAccountCannotRecoverClaimsOrIdentityFromBackup()
        {
            string directory = Path.Combine(Path.GetTempPath(), "dc-account-delete-" + Guid.NewGuid().ToString("N"));
            try
            {
                string path = Path.Combine(directory, "campaign.json");
                var store = new CampaignProgressStore(path);
                store.SignIn(PlayerAuthProvider.PlayerAccount, "deleted-player", "Deleted Account");
                store.CompleteLevel(1, 3, 20);
                store.TrySetHeroName("Old Hero");
                store.Data.guestImports.Add("guest");
                store.Save();
                string previousPath = store.SavePath;
                Assert.That(File.Exists(previousPath + ".bak"), Is.True);
                store.DeleteLocalAccount();
                Assert.That(File.Exists(previousPath), Is.False);
                Assert.That(File.Exists(previousPath + ".bak"), Is.False);
                var reopened = new CampaignProgressStore(path);
                Assert.That(reopened.Data.authProvider, Is.EqualTo(PlayerAuthProvider.None));
                Assert.That(reopened.Data.playerId, Is.EqualTo("local-guest"));
                Assert.That(reopened.Data.heroName, Is.Empty);
                Assert.That(reopened.Data.TotalStars, Is.Zero);
                Assert.That(reopened.PendingClaimCount, Is.Zero);
                Assert.That(reopened.Data.guestImports, Is.Empty);
                Assert.That(reopened.Data.checkpoint, Is.Null);
            }
            finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        }

        [Test]
        public async Task LocalDeletionCancelsQueuedSyncAndKeepsBackgroundSyncSuspended()
        {
            var root = new GameObject("Account Deletion Sync Test");
            try
            {
                var controller = root.AddComponent<UnityGameServicesController>();
                controller.RequestSync();
                var queued = typeof(UnityGameServicesController).GetField("syncRequested", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(queued.GetValue(controller), Is.True);
                await controller.DeleteAccountAsync(true);
                Assert.That(queued.GetValue(controller), Is.False);
                controller.RequestSync();
                Assert.That(queued.GetValue(controller), Is.False);
                await controller.DeleteAccountAsync(true).ContinueWith(task => Assert.That(task.IsFaulted, Is.True));
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
