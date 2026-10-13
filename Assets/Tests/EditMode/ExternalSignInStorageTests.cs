using System;
using System.IO;
using AllinDefense.Core;
using NUnit.Framework;
using UnityEngine;

namespace AllinDefense.Tests
{
    public sealed class ExternalSignInStorageTests
    {
        [TestCase(3, PlayerAuthProvider.Apple)]
        [TestCase(3, PlayerAuthProvider.Google)]
        [TestCase(4, PlayerAuthProvider.Apple)]
        [TestCase(4, PlayerAuthProvider.Google)]
        public void OldProviderValuesKeepUnityAccountIdentity(int version, PlayerAuthProvider provider)
        {
            var data = CampaignProgressStore.Normalize(new CampaignSaveData
                { version = version, authProvider = provider, playerId = "old-account", displayName = "Old Name" });
            Assert.That(data.authProvider, Is.EqualTo(PlayerAuthProvider.PlayerAccount));
            Assert.That(data.playerId, Is.EqualTo("old-account"));
            Assert.That(data.displayName, Is.EqualTo("Old Name"));
            Assert.That(data.version, Is.EqualTo(CampaignProgressStore.CurrentVersion));
        }

        [TestCase(PlayerAuthProvider.Apple)]
        [TestCase(PlayerAuthProvider.Google)]
        public void ExternalAccountsKeepProviderAndIndependentProgressAcrossRestart(PlayerAuthProvider provider)
        {
            string directory = Path.Combine(Path.GetTempPath(), "dc-external-save-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                string path = Path.Combine(directory, "campaign.json");
                var store = new CampaignProgressStore(path);
                store.CompleteLevel(1, 3, 20);
                store.SignIn(provider, "external-player", "External Name");
                Assert.That(store.Data.TotalStars, Is.Zero);
                Assert.That(store.CanImportGuest, Is.True);
                var reopened = new CampaignProgressStore(path);
                Assert.That(reopened.Data.authProvider, Is.EqualTo(provider));
                Assert.That(reopened.ProfileKey, Is.Not.EqualTo("guest"));
                Assert.That(reopened.Data.TotalStars, Is.Zero);
                Assert.That(reopened.ImportGuestProgress(), Is.True);
                Assert.That(reopened.PendingClaims()[0].ownerPlayerId, Is.EqualTo("external-player"));
            }
            finally { Directory.Delete(directory, true); }
        }

        [TestCase(PlayerAuthProvider.Apple)]
        [TestCase(PlayerAuthProvider.Google)]
        public void LegacyProviderSaveMigratesWithoutLosingGuestOwnershipRules(PlayerAuthProvider provider)
        {
            string directory = Path.Combine(Path.GetTempPath(), "dc-external-legacy-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                string path = Path.Combine(directory, "campaign.json");
                var legacy = new CampaignSaveData { version = 4, authProvider = provider, playerId = "legacy-owner" };
                legacy.levels.Add(new CampaignLevelProgress { level = 1, stars = 3, bestLives = 20 });
                File.WriteAllText(path, JsonUtility.ToJson(legacy));
                var store = new CampaignProgressStore(path);
                Assert.That(store.Data.authProvider, Is.EqualTo(PlayerAuthProvider.PlayerAccount));
                Assert.That(store.ProfileKey, Is.EqualTo(AtomicSaveFile.Hash("legacy-owner")));
                Assert.That(store.Data.TotalStars, Is.EqualTo(3));
                Assert.That(store.Data.migrationNeedsReview, Is.False, "A v4-to-v5 provider migration must not relabel progression.");
                Assert.That(UnityGameServicesController.CanRestoreGuestOwner("local-guest", "legacy-owner", true), Is.False);
            }
            finally { Directory.Delete(directory, true); }
        }

        [TestCase(PlayerAuthProvider.Apple, true)]
        [TestCase(PlayerAuthProvider.Google, true)]
        [TestCase(PlayerAuthProvider.PlayerAccount, true)]
        [TestCase(PlayerAuthProvider.Guest, false)]
        public void RegisteredProvidersRequireAnExistingSessionToken(PlayerAuthProvider provider, bool expected) =>
            Assert.That(UnityGameServicesController.RequiresRegisteredSession(provider), Is.EqualTo(expected));
    }
}
