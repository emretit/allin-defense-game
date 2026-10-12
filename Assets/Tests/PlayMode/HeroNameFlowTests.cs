using System;
using System.Collections;
using System.IO;
using System.Linq;
using AllinDefense.Core;
using AllinDefense.Presentation;
using AllinDefense.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace AllinDefense.Tests
{
    public sealed class HeroNameFlowTests
    {
        private string directory;
        private GameObject sessionRoot, flowRoot;
        private GameLanguage previousLanguage;

        [UnityTest]
        public IEnumerator PlayerCanNameCancelResetAndSeeHeroInBattleWithoutChangingAccountName()
        {
            previousLanguage = GameLocalization.CurrentLanguage;
            GameLocalization.SetLanguage(GameLanguage.Turkish);
            directory = Path.Combine(Path.GetTempPath(), "dc-hero-ui-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, "campaign.json");
            var catalog = Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog");
            sessionRoot = new GameObject("Hero Name Test Session");
            var session = sessionRoot.AddComponent<GameSessionController>();
            session.ConfigurePresentation(catalog);
            flowRoot = new GameObject("Hero Name Test Flow");
            var flow = flowRoot.AddComponent<AppFlowController>();
            flow.Initialize(catalog, session, false, path);
            flow.StopAllCoroutines();
            flow.CompleteServiceSignIn(PlayerAuthProvider.Guest, "local-guest", "Player Name");
            yield return null;
            Click(flow, "Settings");
            Click(flow, "Settings Hero");
            var field = flow.GetComponentInChildren<TMP_InputField>();
            Assert.That(field, Is.Not.Null);
            Assert.That(field.placeholder.GetComponent<TMP_Text>().text, Is.EqualTo("Savaşçı"));
            Assert.That(field.textViewport.rect.width, Is.GreaterThan(0));
            field.text = "  Çağrı  ";
            Click(flow, "Save Hero Name");
            Assert.That(new CampaignProgressStore(path).HeroName, Is.EqualTo("Çağrı"));
            Assert.That(flow.Progress.Data.displayName, Is.EqualTo("Player Name"));
            field.text = "Unsaved Name";
            Click(flow, "Cancel Hero Name");
            Click(flow, "Settings Hero");
            Assert.That(field.text, Is.EqualTo("Çağrı"));
            field.text = "<b>Name</b>";
            Click(flow, "Save Hero Name");
            Assert.That(flow.Progress.HeroName, Is.EqualTo("Çağrı"));
            field.text = "";
            Click(flow, "Save Hero Name");
            Assert.That(flow.Progress.HeroName, Is.EqualTo("Savaşçı"));
            field.text = "Alp";
            Click(flow, "Save Hero Name");
            field.text = "Should Not Transfer";
            flow.Progress.SignIn(PlayerAuthProvider.PlayerAccount, "second-player", "Second Player");
            Assert.That(field.gameObject.activeInHierarchy, Is.False);
            Click(flow, "Settings Hero");
            Assert.That(field.text, Is.Empty);
            field.text = "AYARLAR";
            Click(flow, "Save Hero Name");
            GameLocalization.SetLanguage(GameLanguage.English);
            Assert.That(field.text, Is.EqualTo("AYARLAR"), "Player names must never be translated as UI copy.");
            Click(flow, "Cancel Hero Name");
            session.ConfigureCampaignStore(flow.Progress);
            session.Initialize();
            yield return null;
            var hud = Object.FindAnyObjectByType<DefenderHudController>();
            hud.Refresh(session);
            Assert.That(hud.HeroDisplayLabel, Is.EqualTo("AYARLAR"));
            flow.Progress.TrySetHeroName("");
            hud.Refresh(session);
            Assert.That(hud.HeroDisplayLabel, Is.EqualTo("Warrior"));
            GameLocalization.SetLanguage(GameLanguage.Turkish);
            hud.Refresh(session);
            Assert.That(hud.HeroDisplayLabel, Is.EqualTo("Savaşçı"));
        }

        private static void Click(AppFlowController flow, string name)
        {
            var button = UiQuery.FindById<Button>(flow, name);
            Assert.That(button.gameObject.activeInHierarchy, Is.True, name);
            button.onClick.Invoke();
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Object.Destroy(flowRoot);
            Object.Destroy(sessionRoot);
            yield return null;
            Time.timeScale = 1;
            GameLocalization.SetLanguage(previousLanguage);
            if (directory != null && Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }
}
