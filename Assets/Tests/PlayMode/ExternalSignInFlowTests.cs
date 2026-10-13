using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
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
    public sealed class ExternalSignInFlowTests
    {
        private sealed class Provider : IExternalSignInProvider
        {
            public ExternalSignInResult Result = new(ExternalSignInStatus.Success, "fake-id-token");
            public TaskCompletionSource<ExternalSignInResult> Gate;
            public int Calls, Clears;
            public Task<ExternalSignInResult> RequestIdTokenAsync() { Calls++; return Gate?.Task ?? Task.FromResult(Result); }
            public void ClearSession() { Clears++; }
        }
        private sealed class Authentication : IExternalTokenAuthentication
        {
            public int Calls, Deletes, NameCalls;
            public PlayerAuthProvider Provider;
            public string Token;
            public Exception Failure;
            public Task<string> SignInAsync(PlayerAuthProvider provider, string token)
            {
                Calls++; Provider = provider; Token = token;
                return Failure == null ? Task.FromResult("external-player") : Task.FromException<string>(Failure);
            }
            public Task<string> ResolvePlayerNameAsync() { NameCalls++; return Task.FromResult("Resolved Name"); }
            public Task DeleteAccountAsync(bool localOnly) { Deletes++; return Task.CompletedTask; }
        }
        private string directory;
        private GameObject flowRoot, sessionRoot;
        private AppFlowController flow;
        private UnityGameServicesController services;
        private Provider apple, google;
        private Authentication authentication;
        private GameLanguage language;
        private bool reducedMotion;
        private int gold, wave, map, towers;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            gold = PlayerProgress.TotalGoldEarned; wave = PlayerProgress.HighWaveReached;
            map = PlayerProgress.SelectedMapIndex; towers = PlayerProgress.UnlockedTowersMask;
            language = GameLocalization.CurrentLanguage;
            reducedMotion = FrontendPreferences.ReducedMotion;
            FrontendPreferences.ReducedMotion = true;
            GameLocalization.SetLanguage(GameLanguage.Turkish);
            directory = Path.Combine(Path.GetTempPath(), "dc-external-ui-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var catalog = Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog");
            sessionRoot = new GameObject("External Sign In Test Session");
            var session = sessionRoot.AddComponent<GameSessionController>(); session.ConfigurePresentation(catalog);
            flowRoot = new GameObject("External Sign In Test Flow");
            flow = flowRoot.AddComponent<AppFlowController>();
            flow.Initialize(catalog, session, false, Path.Combine(directory, "campaign.json"));
            flow.StopAllCoroutines();
            Show(AppScreen.Welcome);
            apple = new Provider(); google = new Provider(); authentication = new Authentication();
            services = flowRoot.AddComponent<UnityGameServicesController>();
            services.Configure(flow, flow.Progress, apple, google, authentication);
            yield return null;
        }

        private void Show(AppScreen screen) => typeof(AppFlowController).GetMethod("ShowScreen", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(flow, new object[] { screen });
        private void Click(string id) => UiQuery.FindById<Button>(flow, id).onClick.Invoke();
        private string Status => UiQuery.FindById<TMP_Text>(flow, "Welcome Login Status").text;

        [UnityTest]
        public IEnumerator AppleSuccessLeavesWelcomeAndUsesAppleNameWithoutImportingGuestProgress()
        {
            flow.Progress.CompleteLevel(1, 3, 20);
            apple.Result = new ExternalSignInResult(ExternalSignInStatus.Success, "apple-token", "Çağrı Apple");
            Click("Welcome Apple Sign In"); yield return null;
            Assert.That(flow.Progress.Data.authProvider, Is.EqualTo(PlayerAuthProvider.Apple));
            Assert.That(flow.CurrentScreen, Is.EqualTo(AppScreen.MainMenu));
            Assert.That(authentication.Provider, Is.EqualTo(PlayerAuthProvider.Apple));
            Assert.That(authentication.Token, Is.EqualTo("apple-token"));
            Assert.That(flow.Progress.Data.displayName, Is.EqualTo("Çağrı Apple"));
            Assert.That(authentication.NameCalls, Is.Zero);
            Assert.That(flow.Progress.Data.TotalStars, Is.Zero);
            Assert.That(flow.Progress.CanImportGuest, Is.True);
        }

        [UnityTest]
        public IEnumerator GoogleSuccessUsesGoogleTokenAndResolvedName()
        {
            google.Result = new ExternalSignInResult(ExternalSignInStatus.Success, "google-token");
            Click("Welcome Google Sign In"); yield return null;
            Assert.That(flow.Progress.Data.authProvider, Is.EqualTo(PlayerAuthProvider.Google));
            Assert.That(flow.CurrentScreen, Is.EqualTo(AppScreen.MainMenu));
            Assert.That(authentication.Provider, Is.EqualTo(PlayerAuthProvider.Google));
            Assert.That(authentication.Token, Is.EqualTo("google-token"));
            Assert.That(flow.Progress.Data.displayName, Is.EqualTo("Resolved Name"));
        }

        [UnityTest]
        public IEnumerator CancelledAppleLeavesScreenIdentityAndProgressUnchangedWithoutError()
        {
            apple.Result = new ExternalSignInResult(ExternalSignInStatus.Cancelled);
            string before = JsonUtility.ToJson(flow.Progress.Data);
            Click("Welcome Apple Sign In"); yield return null;
            Assert.That(flow.CurrentScreen, Is.EqualTo(AppScreen.Welcome));
            Assert.That(JsonUtility.ToJson(flow.Progress.Data), Is.EqualTo(before));
            Assert.That(Status, Is.Empty);
            Assert.That(authentication.Calls, Is.Zero);
        }

        [UnityTest]
        public IEnumerator CancelledGoogleOnAccountScreenStaysOnAccountScreenWithoutError()
        {
            Show(AppScreen.Login);
            google.Result = new ExternalSignInResult(ExternalSignInStatus.Cancelled);
            string before = JsonUtility.ToJson(flow.Progress.Data);
            Click("Account Google Sign In"); yield return null;
            Assert.That(flow.CurrentScreen, Is.EqualTo(AppScreen.Login));
            Assert.That(JsonUtility.ToJson(flow.Progress.Data), Is.EqualTo(before));
            Assert.That(UiQuery.FindById<TMP_Text>(flow, "Login Status").text, Is.Empty);
            Assert.That(authentication.Calls, Is.Zero);
        }

        [UnityTest]
        public IEnumerator ProviderFailureShowsOnlySafeCopy()
        {
            google.Result = new ExternalSignInResult(ExternalSignInStatus.Failed);
            Click("Welcome Google Sign In"); yield return null;
            Assert.That(Status, Is.EqualTo(FrontendCopy.SignInFailed.Value));
            Assert.That(flow.CurrentScreen, Is.EqualTo(AppScreen.Welcome));
            Assert.That(authentication.Calls, Is.Zero);
        }

        [UnityTest]
        public IEnumerator ServiceFailureKeepsLocalIdentityAndHidesExceptionDetails()
        {
            authentication.Failure = new IOException("private provider details");
            LogAssert.Expect(LogType.Warning, "Unity Authentication is unavailable: private provider details");
            Click("Welcome Apple Sign In"); yield return null;
            Assert.That(Status, Is.EqualTo(FrontendCopy.SignInFailed.Value));
            Assert.That(Status, Does.Not.Contain("private"));
            Assert.That(flow.Progress.Data.playerId, Is.EqualTo("local-guest"));
        }

        [UnityTest]
        public IEnumerator EmptyGoogleClientIdKeepsVisibleButtonAndDoesNotCallServices()
        {
            var bridge = flowRoot.AddComponent<IosExternalSignInBridge>();
            services.Configure(flow, flow.Progress, apple, bridge.Provider(PlayerAuthProvider.Google, ""), authentication);
            var button = UiQuery.FindById<Button>(flow, "Welcome Google Sign In");
            Assert.That(button.gameObject.activeInHierarchy, Is.True);
            Click("Welcome Google Sign In"); yield return null;
            Assert.That(Status, Is.EqualTo(FrontendCopy.GoogleSignInNotConfigured.Value));
            Assert.That(authentication.Calls, Is.Zero);
        }

        [UnityTest]
        public IEnumerator DeletionWaitsForNativeSignInAndDiscardsItsLateToken()
        {
            apple.Gate = new TaskCompletionSource<ExternalSignInResult>();
            Click("Welcome Apple Sign In");
            Task deletion = services.DeleteAccountAsync(true);
            Assert.That(deletion.IsCompleted, Is.False);
            apple.Gate.SetResult(new ExternalSignInResult(ExternalSignInStatus.Success, "late-token"));
            while (!deletion.IsCompleted) yield return null;
            Assert.That(deletion.IsFaulted, Is.False);
            Assert.That(authentication.Calls, Is.Zero);
            Assert.That(authentication.Deletes, Is.EqualTo(1));
            Assert.That(apple.Clears, Is.EqualTo(1)); Assert.That(google.Clears, Is.EqualTo(1));
            Assert.That(flow.Progress.Data.playerId, Is.EqualTo("local-guest"));
        }

        [UnityTest]
        public IEnumerator ExternalAccountsDeleteThroughSameServiceAndClearProviders()
        {
            Click("Welcome Google Sign In"); yield return null;
            Show(AppScreen.Login);
            Click("Delete Account"); yield return null;
            Click("Confirm Account Deletion Button"); yield return null;
            Assert.That(authentication.Deletes, Is.EqualTo(1));
            Assert.That(apple.Clears, Is.EqualTo(1)); Assert.That(google.Clears, Is.EqualTo(1));
            Assert.That(flow.CurrentScreen, Is.EqualTo(AppScreen.Welcome));
            Assert.That(flow.Progress.Data.playerId, Is.EqualTo("local-guest"));
        }

        [UnityTest]
        public IEnumerator ExternalTokenWaitsForExistingSyncBeforeChangingIdentity()
        {
            var field = typeof(UnityGameServicesController).GetField("syncing", BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(services, true);
            Click("Welcome Apple Sign In"); yield return null;
            Assert.That(apple.Calls, Is.EqualTo(1));
            Assert.That(authentication.Calls, Is.Zero);
            Assert.That(flow.Progress.Data.playerId, Is.EqualTo("local-guest"));
            field.SetValue(services, false); yield return null;
            Assert.That(authentication.Calls, Is.EqualTo(1));
            Assert.That(flow.Progress.Data.authProvider, Is.EqualTo(PlayerAuthProvider.Apple));
        }

        [UnityTest]
        public IEnumerator RepeatedSignInRequestsCannotRaceProviders()
        {
            apple.Gate = new TaskCompletionSource<ExternalSignInResult>();
            Click("Welcome Apple Sign In"); Click("Welcome Google Sign In");
            Assert.That(google.Calls, Is.Zero);
            apple.Gate.SetResult(new ExternalSignInResult(ExternalSignInStatus.Cancelled)); yield return null;
            Click("Welcome Google Sign In"); yield return null;
            Assert.That(google.Calls, Is.EqualTo(1));
            Assert.That(flow.Progress.Data.authProvider, Is.EqualTo(PlayerAuthProvider.Google));
        }

        [UnityTest]
        public IEnumerator ButtonsKeepOfficialLogosAccessibleLabelsAndEqualTouchSizesInBothLanguages()
        {
            foreach (var value in new[] { GameLanguage.Turkish, GameLanguage.English })
            {
                GameLocalization.SetLanguage(value); yield return null; Canvas.ForceUpdateCanvases();
                var a = UiQuery.FindById<Button>(flow, "Welcome Apple Sign In");
                var g = UiQuery.FindById<Button>(flow, "Welcome Google Sign In");
                Assert.That(a.GetComponent<UiAccessibleButton>().Label, Is.EqualTo(FrontendCopy.ContinueWithApple.Value));
                Assert.That(g.GetComponent<UiAccessibleButton>().Label, Is.EqualTo(FrontendCopy.ContinueWithGoogle.Value));
                Assert.That(a.GetComponent<UiPointSize>().Points, Is.GreaterThanOrEqualTo(44));
                Assert.That(a.GetComponent<MobileTouchTarget>(), Is.Not.Null);
                Assert.That(g.GetComponent<MobileTouchTarget>(), Is.Not.Null);
                foreach (var button in new[] { a, g })
                {
                    var hit = UiQuery.FindById<RectTransform>(button, "Touch Target");
                    var points = UiMetrics.PointsPerCanvasUnit(flow.GetComponentInChildren<Canvas>());
                    Assert.That(hit.rect.width * points.x, Is.GreaterThanOrEqualTo(43.999f));
                    Assert.That(hit.rect.height * points.y, Is.GreaterThanOrEqualTo(43.999f));
                }
                Assert.That(g.GetComponent<RectTransform>().rect.size, Is.EqualTo(a.GetComponent<RectTransform>().rect.size));
                Assert.That(UiQuery.FindById<Image>(flow, "Welcome Google Sign In Logo").sprite, Is.Not.Null);
                Assert.That(UiQuery.FindById<Image>(flow, "Welcome Apple Sign In Logo").sprite, Is.Not.Null);
                Assert.That(UiQuery.FindById<Image>(flow, "Welcome Google Sign In Logo").preserveAspect, Is.True);
                foreach (var screen in new[] { AppScreen.Welcome, AppScreen.Login })
                {
                    Show(screen); yield return null; Canvas.ForceUpdateCanvases();
                    foreach (var text in flow.GetComponentsInChildren<TMP_Text>().Where(text => text.gameObject.activeInHierarchy))
                    { text.ForceMeshUpdate(); Assert.That(text.isTextOverflowing, Is.False, text.name + ": " + text.text); }
                    Capture(screen + "-" + value);
                }
            }
        }

        [UnityTest]
        public IEnumerator InjectedAndroidPlatformHidesAppleInWelcomeAndAccount()
        {
            Object.Destroy(services); Object.Destroy(flowRoot); yield return null;
            var catalog = Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog");
            flowRoot = new GameObject("Android External Sign In Flow");
            flow = flowRoot.AddComponent<AppFlowController>();
            flow.AppleSignInAvailable = () => false;
            flow.Initialize(catalog, sessionRoot.GetComponent<GameSessionController>(), false, Path.Combine(directory, "android.json"));
            flow.StopAllCoroutines(); Show(AppScreen.Welcome); yield return null;
            Assert.That(UiQuery.FindById<Button>(flow, "Welcome Apple Sign In").gameObject.activeSelf, Is.False);
            Show(AppScreen.Login); yield return null;
            Assert.That(UiQuery.FindById<Button>(flow, "Account Apple Sign In").gameObject.activeSelf, Is.False);
            Assert.That(UiQuery.FindById<Button>(flow, "Welcome Google Sign In").gameObject.activeSelf, Is.True);
        }

        private void Capture(string name)
        {
            var canvas = flow.GetComponentInChildren<Canvas>();
            var cameraRoot = new GameObject("External Sign In Evidence Camera");
            var camera = cameraRoot.AddComponent<Camera>();
            var target = new RenderTexture(1080, 1920, 24);
            var texture = new Texture2D(1080, 1920, TextureFormat.RGB24, false);
            var previousTarget = RenderTexture.active; var previousMode = canvas.renderMode; var previousCamera = canvas.worldCamera;
            try
            {
                camera.targetTexture = target; canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
                Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, 1080, 1920), 0, 0); texture.Apply();
                string output = Path.Combine(Application.dataPath, "../Builds/auth"); Directory.CreateDirectory(output);
                File.WriteAllBytes(Path.Combine(output, name + ".png"), texture.EncodeToPNG());
            }
            finally
            {
                canvas.renderMode = previousMode; canvas.worldCamera = previousCamera; RenderTexture.active = previousTarget;
                camera.targetTexture = null; Object.Destroy(cameraRoot); Object.Destroy(texture); Object.Destroy(target);
            }
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Object.Destroy(flowRoot); Object.Destroy(sessionRoot); yield return null;
            Time.timeScale = 1; FrontendPreferences.ReducedMotion = reducedMotion; GameLocalization.SetLanguage(language);
            PlayerProgress.Reset(); PlayerProgress.RecordGoldEarned(gold); PlayerProgress.RecordWaveCompleted(wave);
            PlayerProgress.SetSelectedMapIndex(map); PlayerProgress.UnlockedTowersMask = towers;
            if (directory != null && Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }
}
