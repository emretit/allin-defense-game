using System;
using System.IO;
using System.Text.RegularExpressions;
using AllinDefense.Core;
using AllinDefense.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AllinDefense.Tests
{
    public sealed class HeroIdentityTests
    {
        private string directory;
        private string path;
        private GameLanguage language;
        [SetUp] public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "dc-hero-name-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            path = Path.Combine(directory, "campaign.json");
            language = GameLocalization.CurrentLanguage;
            GameLocalization.SetLanguage(GameLanguage.Turkish);
        }
        [TearDown] public void TearDown()
        {
            GameLocalization.SetLanguage(language);
            Directory.Delete(directory, true);
        }

        [TestCase(3)] [TestCase(4)]
        public void ExistingSaveWithoutHeroNameUsesWarriorAndKeepsLateCampaign(int version)
        {
            File.WriteAllText(path, $"{{\"version\":{version},\"unlockedLevel\":50,\"lastSelectedLevel\":50}}");
            var store = new CampaignProgressStore(path);
            Assert.That(store.HeroName, Is.EqualTo("Savaşçı"));
            Assert.That(store.TrySetHeroName("Alp"), Is.True);
            var reloaded = new CampaignProgressStore(path);
            Assert.That(reloaded.Data.unlockedLevel, Is.EqualTo(50));
            Assert.That(reloaded.Data.lastSelectedLevel, Is.EqualTo(50));
        }

        [Test]
        public void CustomNameSurvivesRestartLanguageChangeAndCampaignSync()
        {
            var store = new CampaignProgressStore(path);
            Assert.That(store.TrySetHeroName("  Çağrı  "), Is.True);
            store = new CampaignProgressStore(path);
            Assert.That(store.HeroName, Is.EqualTo("Çağrı"));
            GameLocalization.SetLanguage(GameLanguage.English);
            Assert.That(store.HeroName, Is.EqualTo("Çağrı"));
            store.ApplyServerSync(new CampaignSyncResult { state = new CampaignServerState { revision = 1 } });
            Assert.That(new CampaignProgressStore(path).HeroName, Is.EqualTo("Çağrı"));
            Assert.That(store.TrySetHeroName("  "), Is.True);
            Assert.That(new CampaignProgressStore(path).HeroName, Is.EqualTo("Warrior"));
            GameLocalization.SetLanguage(GameLanguage.Turkish);
            Assert.That(store.HeroName, Is.EqualTo("Savaşçı"));
        }

        [Test]
        public void AccountsKeepSeparateNamesAndAccountDisplayNameDoesNotRenameHero()
        {
            var store = new CampaignProgressStore(path);
            store.TrySetHeroName("Misafir Alp");
            store.SignIn(PlayerAuthProvider.PlayerAccount, "a", "Account A");
            Assert.That(store.HeroName, Is.EqualTo("Savaşçı"));
            store.TrySetHeroName("Demir");
            store.SignIn(PlayerAuthProvider.PlayerAccount, "b", "Account B");
            Assert.That(store.HeroName, Is.EqualTo("Savaşçı"));
            store.TrySetHeroName("Efe");
            store.SignIn(PlayerAuthProvider.PlayerAccount, "a", "New Account Display Name");
            Assert.That(new CampaignProgressStore(path).HeroName, Is.EqualTo("Demir"));
            store.SignIn(PlayerAuthProvider.Guest, "local-guest", "Guest");
            Assert.That(new CampaignProgressStore(path).HeroName, Is.EqualTo("Misafir Alp"));
        }

        [TestCase("<b>Alp</b>")]
        [TestCase("Alp\nEfe")]
        [TestCase("abcdefghijklmnopqrstu")]
        [TestCase("Alp\u202eEfe")]
        public void InvalidNameCannotOverwriteSavedName(string invalid)
        {
            var store = new CampaignProgressStore(path);
            store.TrySetHeroName("Alp");
            Assert.That(store.TrySetHeroName(invalid), Is.False);
            Assert.That(store.HeroName, Is.EqualTo("Alp"));
            Assert.That(new CampaignProgressStore(path).HeroName, Is.EqualTo("Alp"));
        }

        [Test]
        public void FailedNameWriteReportsFailureAndCanRetry()
        {
            var store = new CampaignProgressStore(path);
            Directory.CreateDirectory(store.SavePath);
            LogAssert.Expect(LogType.Warning, new Regex("Local save retry required:"));
            Assert.That(store.TrySetHeroName("Efe"), Is.False);
            Assert.That(store.HasWriteError, Is.True);
            Assert.That(store.HeroName, Is.EqualTo("Efe"));
            Directory.Delete(store.SavePath);
            store.Save();
            Assert.That(new CampaignProgressStore(path).HeroName, Is.EqualTo("Efe"));
        }
    }
}
