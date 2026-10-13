using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using AllinDefense.Core;
using AllinDefense.Presentation;
using AllinDefense.UI;
using AllinDefense.UI.Kit;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace AllinDefense.Tests
{
    public sealed class AccountDeletionTests
    {
        private string directory;
        private GameObject sessionRoot, flowRoot;
        private AppFlowController flow;
        private bool reducedMotion;
        private GameLanguage language;
        private int gold, wave, map, towers;
        private float music, sfx;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            reducedMotion = FrontendPreferences.ReducedMotion;
            language = GameLocalization.CurrentLanguage;
            music = FrontendPreferences.MusicVolume; sfx = FrontendPreferences.SfxVolume;
            gold = PlayerProgress.TotalGoldEarned; wave = PlayerProgress.HighWaveReached;
            map = PlayerProgress.SelectedMapIndex; towers = PlayerProgress.UnlockedTowersMask;
            FrontendPreferences.ReducedMotion = true;
            GameLocalization.SetLanguage(GameLanguage.Turkish);
            directory = Path.Combine(Path.GetTempPath(), "dc-delete-ui-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var catalog = Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog");
            sessionRoot = new GameObject("Account Deletion Test Session");
            var session = sessionRoot.AddComponent<GameSessionController>();
            session.ConfigurePresentation(catalog);
            flowRoot = new GameObject("Account Deletion Test Flow");
            flow = flowRoot.AddComponent<AppFlowController>();
            flow.Initialize(catalog, session, false, Path.Combine(directory, "campaign.json"));
            flow.StopAllCoroutines();
            flow.CompleteServiceSignIn(PlayerAuthProvider.PlayerAccount, "delete-player", "Account Name");
            flow.Progress.CompleteLevel(1, 3, 20);
            flow.Progress.Data.guestImports.Add("guest");
            flow.Progress.Save();
            yield return null;
            UiQuery.FindById<Button>(flow, "Settings").onClick.Invoke();
            yield return null;
            UiQuery.FindById<Button>(flow, "Settings Account").onClick.Invoke();
            yield return null;
            Assert.That(flow.CurrentScreen, Is.EqualTo(AppScreen.Login));
        }

        private void Click(string id) => UiQuery.FindById<Button>(flow, id).onClick.Invoke();

        [UnityTest]
        public IEnumerator ButtonIsAccessibleAndCancelPreservesAccount()
        {
            var button = UiQuery.FindById<Button>(flow, "Delete Account");
            Assert.That(button.gameObject.activeInHierarchy, Is.True);
            Assert.That(button.GetComponent<UiAccessibleButton>().Label, Is.EqualTo("HESABI SİL"));
            int calls = 0;
            flow.AccountDeletion = _ => { calls++; return Task.CompletedTask; };
            string before = JsonUtility.ToJson(flow.Progress.Data);
            Click("Delete Account");
            yield return null;
            Click("Cancel Account Deletion");
            Assert.That(calls, Is.Zero);
            Assert.That(JsonUtility.ToJson(flow.Progress.Data), Is.EqualTo(before));
            Assert.That(flow.CurrentScreen, Is.EqualTo(AppScreen.Login));
        }

        [UnityTest]
        public IEnumerator SuccessfulDeletionClearsProgressAndKeepsPreferences()
        {
            int calls = 0;
            var gate = new TaskCompletionSource<bool>();
            flow.AccountDeletion = local => { Assert.That(local, Is.False); calls++; return gate.Task; };
            PlayerProgress.RecordGoldEarned(77);
            FrontendPreferences.MusicVolume = .35f;
            FrontendPreferences.SfxVolume = .65f;
            Click("Delete Account"); yield return null;
            Click("Confirm Account Deletion Button");
            Assert.That(UiQuery.FindById<Button>(flow, "Cancel Account Deletion").interactable, Is.False);
            Assert.That(flow.Progress.Data.TotalStars, Is.GreaterThan(0));
            gate.SetResult(true); yield return null;
            Assert.That(calls, Is.EqualTo(1));
            Assert.That(flow.Progress.Data.TotalStars, Is.Zero);
            Assert.That(flow.Progress.Data.unlockedLevel, Is.EqualTo(1));
            Assert.That(flow.Progress.Data.playerId, Is.EqualTo("local-guest"));
            Assert.That(flow.Progress.Data.authProvider, Is.EqualTo(PlayerAuthProvider.None));
            Assert.That(flow.Progress.Data.displayName, Is.Null.Or.Empty);
            Assert.That(flow.Progress.PendingClaimCount, Is.Zero);
            Assert.That(flow.Progress.Data.guestImports, Is.Empty);
            Assert.That(PlayerProgress.TotalGoldEarned, Is.Zero);
            Assert.That(FrontendPreferences.ReducedMotion, Is.True);
            Assert.That(GameLocalization.CurrentLanguage, Is.EqualTo(GameLanguage.Turkish));
            Assert.That(FrontendPreferences.MusicVolume, Is.EqualTo(.35f));
            Assert.That(FrontendPreferences.SfxVolume, Is.EqualTo(.65f));
            Assert.That(flow.CurrentScreen, Is.EqualTo(AppScreen.Welcome));
            var reopened = new CampaignProgressStore(Path.Combine(directory, "campaign.json"));
            Assert.That(reopened.Data.TotalStars, Is.Zero);
            Assert.That(reopened.PendingClaimCount, Is.Zero);
            Assert.That(UiQuery.FindById<TMP_Text>(flow, "Account Deleted Status").text, Is.EqualTo(FrontendCopy.AccountDeleted.Value));
        }

        [UnityTest]
        public IEnumerator FailedDeletionKeepsIdentityAndShowsSafeError()
        {
            string before = JsonUtility.ToJson(flow.Progress.Data);
            flow.AccountDeletion = _ => Task.FromException(new IOException("private backend detail"));
            Click("Delete Account"); yield return null;
            LogAssert.Expect(LogType.Warning, "Account deletion failed: private backend detail");
            Click("Confirm Account Deletion Button"); yield return null;
            Assert.That(JsonUtility.ToJson(flow.Progress.Data), Is.EqualTo(before));
            Assert.That(flow.CurrentScreen, Is.EqualTo(AppScreen.Login));
            Assert.That(UiQuery.FindById<TMP_Text>(flow, "Login Status").text, Is.EqualTo(FrontendCopy.AccountDeleteFailed.Value));
            Assert.That(UiQuery.FindById<Button>(flow, "Delete Account").interactable, Is.True);
        }

        [UnityTest]
        public IEnumerator LocalGuestDeletesWithoutRemoteService()
        {
            flow.Progress.SignIn(PlayerAuthProvider.Guest, "local-guest", "Guest");
            flow.Progress.CompleteLevel(1, 3, 20);
            int remoteCalls = 0;
            flow.AccountDeletion = local => { if (!local) remoteCalls++; return Task.CompletedTask; };
            Click("Delete Account"); yield return null;
            Click("Confirm Account Deletion Button"); yield return null;
            Assert.That(remoteCalls, Is.Zero);
            Assert.That(flow.Progress.Data.TotalStars, Is.Zero);
            Assert.That(flow.CurrentScreen, Is.EqualTo(AppScreen.Welcome));
        }

        [UnityTest]
        public IEnumerator ConfirmationFitsBothLanguages()
        {
            Click("Delete Account"); yield return null;
            foreach (var value in new[] { GameLanguage.Turkish, GameLanguage.English })
            {
                GameLocalization.SetLanguage(value); yield return null;
                Canvas.ForceUpdateCanvases();
                CaptureConfirmation(value);
                foreach (var text in UiQuery.FindById<RectTransform>(flow, "Confirm Account Deletion").GetComponentsInChildren<TMP_Text>())
                    {
                        text.ForceMeshUpdate();
                        Assert.That(text.isTextOverflowing, Is.False, text.name + ": " + text.text);
                    }
            }
        }

        private void CaptureConfirmation(GameLanguage value)
        {
            var canvas = flow.GetComponentInChildren<Canvas>();
            var cameraRoot = new GameObject("Account Deletion Evidence Camera");
            var camera = cameraRoot.AddComponent<Camera>();
            var target = new RenderTexture(1080, 1920, 24);
            var texture = new Texture2D(1080, 1920, TextureFormat.RGB24, false);
            var previousTarget = RenderTexture.active;
            var previousMode = canvas.renderMode;
            var previousCamera = canvas.worldCamera;
            try
            {
                camera.targetTexture = target;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1;
                Canvas.ForceUpdateCanvases();
                camera.Render();
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, 1080, 1920), 0, 0);
                texture.Apply();
                string output = Path.Combine(Application.dataPath, "../Builds/account-deletion");
                Directory.CreateDirectory(output);
                File.WriteAllBytes(Path.Combine(output, "confirmation-" + value + ".png"), texture.EncodeToPNG());
            }
            finally
            {
                canvas.renderMode = previousMode;
                canvas.worldCamera = previousCamera;
                RenderTexture.active = previousTarget;
                camera.targetTexture = null;
                Object.Destroy(cameraRoot); Object.Destroy(texture); Object.Destroy(target);
            }
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Object.Destroy(flowRoot); Object.Destroy(sessionRoot); yield return null;
            Time.timeScale = 1;
            FrontendPreferences.MusicVolume = music; FrontendPreferences.SfxVolume = sfx;
            FrontendPreferences.ReducedMotion = reducedMotion;
            GameLocalization.SetLanguage(language);
            PlayerProgress.Reset(); PlayerProgress.RecordGoldEarned(gold); PlayerProgress.RecordWaveCompleted(wave);
            PlayerProgress.SetSelectedMapIndex(map); PlayerProgress.UnlockedTowersMask = towers;
            if (directory != null && Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }
}
