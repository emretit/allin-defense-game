using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DefenderChronicles.UI;
using Unity.Services.Authentication;
using Unity.Services.Authentication.PlayerAccounts;
using Unity.Services.CloudSave;
using Unity.Services.Core;
using UnityEngine;

namespace DefenderChronicles.Core
{
    public sealed class UnityGameServicesController : MonoBehaviour
    {
        private const string CampaignKey = "campaign_save_v1";
        private AppFlowController flow;
        private CampaignProgressStore progress;
        private PlayerAuthProvider requestedProvider;
        private bool initialized;
        private bool syncing;
        private bool playerAccountsSubscribed;

        public void Configure(AppFlowController appFlow, CampaignProgressStore progressStore)
        {
            flow = appFlow;
            progress = progressStore;
            flow.AuthenticationRequested += HandleAuthenticationRequested;
            progress.Changed += HandleProgressChanged;
        }

        private async void HandleAuthenticationRequested(PlayerAuthProvider provider)
        {
            requestedProvider = provider;
            flow.SetLoginStatus(provider == PlayerAuthProvider.Guest
                ? "Misafir hesabı hazırlanıyor..."
                : "Güvenli hesap penceresi hazırlanıyor...");

            if (string.IsNullOrWhiteSpace(Application.cloudProjectId))
            {
                CompleteUnlinkedProjectSignIn(provider);
                return;
            }

            try
            {
                await EnsureInitializedAsync();
                if (provider == PlayerAuthProvider.Guest)
                {
                    if (AuthenticationService.Instance.IsSignedIn &&
                        progress.Data.authProvider != PlayerAuthProvider.Guest)
                    {
                        AuthenticationService.Instance.SignOut(true);
                    }
                    if (!AuthenticationService.Instance.IsSignedIn)
                        await AuthenticationService.Instance.SignInAnonymouslyAsync();
                    await CompleteAuthenticationAsync(PlayerAuthProvider.Guest);
                    return;
                }

                PlayerAccountService.Instance.SignedIn -= HandlePlayerAccountSignedIn;
                PlayerAccountService.Instance.SignedIn += HandlePlayerAccountSignedIn;
                playerAccountsSubscribed = true;
                if (PlayerAccountService.Instance.IsSignedIn &&
                    progress.Data.authProvider != PlayerAuthProvider.Guest &&
                    progress.Data.authProvider != provider)
                {
                    if (AuthenticationService.Instance.IsSignedIn)
                        AuthenticationService.Instance.SignOut(true);
                    PlayerAccountService.Instance.SignOut();
                }
                if (PlayerAccountService.Instance.IsSignedIn)
                    await SignInWithPlayerAccountAsync();
                else
                    await PlayerAccountService.Instance.StartSignInAsync();
            }
            catch (Exception exception)
            {
                HandleSignInFailure(provider, exception);
            }
        }

        private async void HandlePlayerAccountSignedIn()
        {
            try
            {
                await SignInWithPlayerAccountAsync();
            }
            catch (Exception exception)
            {
                HandleSignInFailure(requestedProvider, exception);
            }
        }

        private async Task SignInWithPlayerAccountAsync()
        {
            if (AuthenticationService.Instance.IsSignedIn && progress.Data.authProvider == PlayerAuthProvider.Guest)
            {
                try
                {
                    await AuthenticationService.Instance.LinkWithUnityAsync(PlayerAccountService.Instance.AccessToken);
                }
                catch (AuthenticationException exception)
                    when (exception.ErrorCode == AuthenticationErrorCodes.AccountAlreadyLinked)
                {
                    AuthenticationService.Instance.SignOut(true);
                    await AuthenticationService.Instance.SignInWithUnityAsync(PlayerAccountService.Instance.AccessToken);
                }
            }
            else if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInWithUnityAsync(PlayerAccountService.Instance.AccessToken);
            }
            await CompleteAuthenticationAsync(requestedProvider);
        }

        private async Task CompleteAuthenticationAsync(PlayerAuthProvider provider)
        {
            await SynchronizeCloudAsync();
            string displayName = provider switch
            {
                PlayerAuthProvider.Google => "Google Muhafızı",
                PlayerAuthProvider.Apple => "Apple Muhafızı",
                _ => "Misafir Muhafız"
            };
            flow.CompleteServiceSignIn(provider, AuthenticationService.Instance.PlayerId, displayName);
            flow.SetLoginStatus("İlerleme Cloud Save ile eşitlendi.");
            Debug.Log($"[DC_UGS_AUTH_PASS] provider={provider} cloud_sync=true");
        }

        private async Task EnsureInitializedAsync()
        {
            if (initialized) return;
            await UnityServices.InitializeAsync();
            initialized = true;
        }

        private async Task SynchronizeCloudAsync()
        {
            if (!AuthenticationService.Instance.IsSignedIn) return;
            syncing = true;
            try
            {
                var cloudData = await CloudSaveService.Instance.Data.Player.LoadAsync(
                    new HashSet<string> { CampaignKey });
                if (cloudData.TryGetValue(CampaignKey, out var cloudItem))
                {
                    string json = cloudItem.Value.GetAs<string>();
                    CampaignSaveData incoming = JsonUtility.FromJson<CampaignSaveData>(json);
                    progress.Merge(incoming);
                }
                await SaveCloudAsync();
            }
            finally
            {
                syncing = false;
            }
        }

        private async void HandleProgressChanged()
        {
            if (syncing || !initialized || !AuthenticationService.Instance.IsSignedIn) return;
            try
            {
                await SaveCloudAsync();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Cloud Save update will be retried on the next sign-in. {exception.Message}");
            }
        }

        private Task SaveCloudAsync()
        {
            var values = new Dictionary<string, object>
            {
                { CampaignKey, JsonUtility.ToJson(progress.Data) }
            };
            return CloudSaveService.Instance.Data.Player.SaveAsync(values);
        }

        private void HandleSignInFailure(PlayerAuthProvider provider, Exception exception)
        {
            Debug.LogWarning($"Unity Authentication is unavailable: {exception.Message}");
            if (provider == PlayerAuthProvider.Guest)
            {
                flow.CompleteGuestFallback();
                Debug.Log("[DC_UGS_GUEST_FALLBACK_PASS] screen=MainMenu provider=Guest");
                return;
            }
            flow.SetLoginStatus("Çevrimiçi hesap henüz yapılandırılmadı. Misafir olarak devam edebilirsin.");
        }

        private void CompleteUnlinkedProjectSignIn(PlayerAuthProvider provider)
        {
            if (provider == PlayerAuthProvider.Guest)
            {
                flow.CompleteGuestFallback();
                Debug.Log("[DC_UGS_GUEST_FALLBACK_PASS] screen=MainMenu provider=Guest reason=project-unlinked");
                return;
            }
            flow.SetLoginStatus("Çevrimiçi hesap bağlantısı hazırlanıyor. Şimdilik misafir olarak devam edebilirsin.");
        }

        private void OnDestroy()
        {
            if (flow != null) flow.AuthenticationRequested -= HandleAuthenticationRequested;
            if (progress != null) progress.Changed -= HandleProgressChanged;
            if (playerAccountsSubscribed) PlayerAccountService.Instance.SignedIn -= HandlePlayerAccountSignedIn;
        }
    }
}
