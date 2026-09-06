using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DefenderChronicles.UI;
using Unity.Services.Authentication;
using Unity.Services.Authentication.PlayerAccounts;
using Unity.Services.CloudCode;
using Unity.Services.Core;
using UnityEngine;

namespace DefenderChronicles.Core
{
    public sealed class UnityGameServicesController : MonoBehaviour, ICampaignProgressionService
    {
        private const string CampaignModule = "CampaignProgression";
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
            if ((int)provider is 2 or 3)
                provider = PlayerAuthProvider.PlayerAccount;
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
            await CompleteAuthenticationAsync(PlayerAuthProvider.PlayerAccount);
        }

        private async Task CompleteAuthenticationAsync(PlayerAuthProvider provider)
        {
            string displayName = provider == PlayerAuthProvider.PlayerAccount
                ? await ResolvePlayerNameAsync()
                : "Misafir Muhafız";
            syncing = true;
            try
            {
                flow.CompleteServiceSignIn(provider, AuthenticationService.Instance.PlayerId, displayName);
            }
            finally
            {
                syncing = false;
            }

            bool synchronized = await SynchronizeCloudAsync();
            flow.SetLoginStatus(synchronized
                ? "İlerleme güvenli bulut kaydıyla eşitlendi."
                : "Çevrimdışı ilerleme korunuyor; bağlantı gelince eşitlenecek.");
            Debug.Log($"[DC_UGS_AUTH_PASS] provider={provider} cloud_sync={synchronized.ToString().ToLowerInvariant()}");
        }

        private async Task EnsureInitializedAsync()
        {
            if (initialized) return;
            await UnityServices.InitializeAsync();
            initialized = true;
        }

        private async Task<bool> SynchronizeCloudAsync()
        {
            if (!AuthenticationService.Instance.IsSignedIn) return false;
            if (syncing) return false;
            syncing = true;
            progress.BeginSync();
            try
            {
                CampaignSyncResult result = await GetStateAsync(progress.Data);
                progress.ApplyServerSync(result);
                while (progress.PendingClaimCount > 0)
                {
                    IReadOnlyList<CampaignCompletionClaim> pending = progress.PendingClaims();
                    result = await SubmitPendingAsync(pending);
                    progress.ApplyServerSync(result);
                }
                return true;
            }
            catch (Exception exception)
            {
                progress.FailSync();
                Debug.LogWarning($"Campaign sync is pending: {exception.Message}");
                return false;
            }
            finally
            {
                syncing = false;
            }
        }

        private async void HandleProgressChanged()
        {
            if (syncing || !initialized || !AuthenticationService.Instance.IsSignedIn) return;
            await SynchronizeCloudAsync();
        }

        public Task<CampaignSyncResult> GetStateAsync(CampaignSaveData legacySnapshot = null)
        {
            var arguments = new Dictionary<string, object>
            {
                { "legacySnapshotJson", legacySnapshot == null ? string.Empty : JsonUtility.ToJson(legacySnapshot) }
            };
            return CloudCodeService.Instance.CallModuleEndpointAsync<CampaignSyncResult>(
                CampaignModule, "GetState", arguments);
        }

        public Task<CampaignSyncResult> SubmitPendingAsync(IReadOnlyList<CampaignCompletionClaim> claims)
        {
            var arguments = new Dictionary<string, object>
            {
                { "claims", claims?.Take(20).ToList() ?? new List<CampaignCompletionClaim>() }
            };
            return CloudCodeService.Instance.CallModuleEndpointAsync<CampaignSyncResult>(
                CampaignModule, "SubmitCompletionBatch", arguments);
        }

        private static async Task<string> ResolvePlayerNameAsync()
        {
            try
            {
                string playerName = await AuthenticationService.Instance.GetPlayerNameAsync(true);
                return string.IsNullOrWhiteSpace(playerName) ? "Muhafız" : playerName;
            }
            catch (Exception)
            {
                return "Muhafız";
            }
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
