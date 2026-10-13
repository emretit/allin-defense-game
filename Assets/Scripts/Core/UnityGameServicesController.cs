using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AllinDefense.UI;
using Unity.Services.Authentication;
using Unity.Services.Authentication.PlayerAccounts;
using Unity.Services.CloudCode;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using UnityEngine;

namespace AllinDefense.Core
{
    public sealed class UnityGameServicesController : MonoBehaviour, ICampaignProgressionService
    {
        private const string CampaignModule = "CampaignProgression";
        private AppFlowController flow;
        private CampaignProgressStore progress;
        private PlayerAuthProvider requestedProvider;
        private bool initialized;
        private bool restoring;
        private bool syncing;
        private bool deletionSuspended;
        private int authenticationOperations;
        private bool playerAccountsSubscribed;
        private Task initialization;
        private bool syncRequested;
        private float nextAttempt;
        private int failures;
        public const string TestEnvironment = "closed-test";

        public void Configure(AppFlowController appFlow, CampaignProgressStore progressStore)
        {
            if (AppFlowController.AcceptanceRequested) return;
            if (flow != null) flow.AuthenticationRequested -= HandleAuthenticationRequested;
            if (progress != null) progress.Changed -= HandleProgressChanged;
            flow = appFlow;
            progress = progressStore;
            flow.AuthenticationRequested += HandleAuthenticationRequested;
            flow.AccountDeletion = DeleteAccountAsync;
            progress.Changed += HandleProgressChanged;
            RestoreSession();
        }

        private async void RestoreSession()
        {
            if (deletionSuspended || restoring || progress == null || string.IsNullOrWhiteSpace(Application.cloudProjectId)) return;
            restoring = true;
            try
            {
                await EnsureInitializedAsync();
                if (deletionSuspended) return;
                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    if (!AuthenticationService.Instance.SessionTokenExists &&
                        progress.Data.authProvider == PlayerAuthProvider.PlayerAccount)
                    { progress.FailSync(true); return; }
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                }
                if (progress.Data.authProvider == PlayerAuthProvider.Guest)
                {
                    var auth = AuthenticationService.Instance;
                    // A crash during account sign-in can leave a registered token beside the guest save.
                    if (!CanRestoreGuestOwner(progress.Data.playerId,auth.PlayerId,auth.PlayerInfo?.Identities?.Count > 0))
                    {
                        auth.SignOut(true);
                        await auth.SignInAnonymouslyAsync();
                    }
                    if (deletionSuspended) return;
                    progress.SignIn(PlayerAuthProvider.Guest, AuthenticationService.Instance.PlayerId, progress.Data.displayName);
                }
                RequestSync();
            }
            catch (Exception error)
            {
                if (!deletionSuspended) progress.FailSync();
                ScheduleRetry();
                Debug.LogWarning("Cloud session restore deferred: " + error.Message);
            }
            finally { restoring = false; }
        }

        public void RequestSync() { if (deletionSuspended) return; syncRequested = true; nextAttempt = 0f; }
        public async Task DeleteAccountAsync(bool localOnly)
        {
            if (deletionSuspended) throw new InvalidOperationException("Account deletion already in progress.");
            deletionSuspended = true;
            syncRequested = false;
            try
            {
                while (syncing || restoring || authenticationOperations > 0) await Task.Yield();
                if (!localOnly)
                {
                    await EnsureInitializedAsync();
                    await AuthenticationService.Instance.DeleteAccountAsync();
                }
                if (initialized)
                {
                    try { if (PlayerAccountService.Instance.IsSignedIn) PlayerAccountService.Instance.SignOut(); }
                    catch (Exception error) { Debug.LogWarning("Player Account sign-out: " + error.Message); }
                    AuthenticationService.Instance.SignOut(true);
                }
            }
            catch
            {
                deletionSuspended = false;
                throw;
            }
        }

        public static bool CanRestoreGuestOwner(string localOwner,string sessionOwner,bool registered) =>
            !registered && (string.IsNullOrEmpty(localOwner) || localOwner == "local-guest" || localOwner == sessionOwner);
        private void Update()
        {
            if (deletionSuspended || !syncRequested || syncing || Time.realtimeSinceStartup < nextAttempt) return;
            syncRequested = false;
            if (!initialized || !AuthenticationService.Instance.IsSignedIn) RestoreSession();
            else _ = SynchronizeCloudAsync();
        }
        private void ScheduleRetry()
        {
            if (deletionSuspended) return;
            failures = Math.Min(failures + 1, 6);
            nextAttempt = Time.realtimeSinceStartup + Math.Min(60f, (float)Math.Pow(2, failures));
            syncRequested = true;
        }
        private void OnApplicationFocus(bool focused) { if (focused) RequestSync(); }
        private void OnApplicationPause(bool paused) { if (!paused) RequestSync(); }

        private async void HandleAuthenticationRequested(PlayerAuthProvider provider)
        {
            if ((int)provider is 2 or 3)
                provider = PlayerAuthProvider.PlayerAccount;
            if (deletionSuspended && progress.Data.playerId != "local-guest") return;
            deletionSuspended = false;
            requestedProvider = provider;
            bool wasGuest = progress.Data.authProvider == PlayerAuthProvider.Guest;
            if (provider == PlayerAuthProvider.Guest)
            {
                // Local play is ready immediately; authentication and merging continue separately.
                bool wasSyncing = syncing;
                syncing = true;
                try { flow?.CompleteGuestFallback(); }
                finally { syncing = wasSyncing; }
            }
            flow?.SetLoginStatus(provider == PlayerAuthProvider.Guest
                ? "Misafir hesabı hazırlanıyor..."
                : "Güvenli hesap penceresi hazırlanıyor...");

            if (string.IsNullOrWhiteSpace(Application.cloudProjectId))
            {
                CompleteUnlinkedProjectSignIn(provider);
                return;
            }

            authenticationOperations++;
            try
            {
                await EnsureInitializedAsync();
                if (deletionSuspended) return;
                if (provider == PlayerAuthProvider.Guest)
                {
                    if (AuthenticationService.Instance.IsSignedIn &&
                        !wasGuest)
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
            finally { authenticationOperations--; }
        }

        private async void HandlePlayerAccountSignedIn()
        {
            if (deletionSuspended) return;
            authenticationOperations++;
            try
            {
                await SignInWithPlayerAccountAsync();
            }
            catch (Exception exception)
            {
                HandleSignInFailure(requestedProvider, exception);
            }
            finally { authenticationOperations--; }
        }

        private async Task SignInWithPlayerAccountAsync()
        {
            if (deletionSuspended) return;
            // Account sign-in changes identity only. Guest progression needs the separate explicit import action.
            if (AuthenticationService.Instance.IsSignedIn) AuthenticationService.Instance.SignOut();
            await AuthenticationService.Instance.SignInWithUnityAsync(PlayerAccountService.Instance.AccessToken);
            await CompleteAuthenticationAsync(PlayerAuthProvider.PlayerAccount);
        }

        private async Task CompleteAuthenticationAsync(PlayerAuthProvider provider)
        {
            if (deletionSuspended) return;
            string displayName = provider == PlayerAuthProvider.PlayerAccount
                ? await ResolvePlayerNameAsync()
                : "Misafir Muhafız";
            if (deletionSuspended) return;
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
            if (deletionSuspended) return;
            flow?.SetLoginStatus(synchronized
                ? "İlerleme güvenli bulut kaydıyla eşitlendi."
                : "Çevrimdışı ilerleme korunuyor; bağlantı gelince eşitlenecek.");
            Debug.Log($"[DC_UGS_AUTH_PASS] provider={provider} cloud_sync={synchronized.ToString().ToLowerInvariant()}");
        }

        private async Task EnsureInitializedAsync()
        {
            if (initialized) return;
            initialization ??= UnityServices.InitializeAsync(new InitializationOptions().SetEnvironmentName(TestEnvironment));
            try { await initialization; initialized = true; }
            catch { initialization = null; throw; }
        }

        private async Task<bool> SynchronizeCloudAsync()
        {
            if (deletionSuspended) return false;
            if (!initialized || !AuthenticationService.Instance.IsSignedIn) { ScheduleRetry(); return false; }
            if (progress.Data.playerId != AuthenticationService.Instance.PlayerId)
            { progress.FailSync(true); return false; }
            if (syncing) return false;
            syncing = true;
            progress.BeginSync();
            string owner = progress.Data.playerId;
            try
            {
                CampaignSyncResult result = await GetStateAsync(progress.Data);
                if (deletionSuspended || progress.Data.playerId != owner) return false;
                progress.ApplyServerSync(result);
                while (progress.PendingClaimCount > 0)
                {
                    IReadOnlyList<CampaignCompletionClaim> pending = progress.PendingClaims();
                    result = await SubmitPendingAsync(pending);
                    if (deletionSuspended || progress.Data.playerId != owner) return false;
                    progress.ApplyServerSync(result);
                }
                failures = 0;
                return progress.SyncStatus == CampaignSyncStatus.Current;
            }
            catch (Exception exception)
            {
                if (!deletionSuspended) progress.FailSync();
                ScheduleRetry();
                Debug.LogWarning($"Campaign sync is pending: {exception.Message}");
                return false;
            }
            finally
            {
                syncing = false;
            }
        }

        private void HandleProgressChanged()
        {
            if (!syncing) RequestSync();
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
            if (deletionSuspended) return;
            Debug.LogWarning($"Unity Authentication is unavailable: {exception.Message}");
            if (provider == PlayerAuthProvider.Guest)
            {
                flow?.CompleteGuestFallback();
                Debug.Log("[DC_UGS_GUEST_FALLBACK_PASS] screen=MainMenu provider=Guest");
                return;
            }
            flow?.SetLoginStatus("Çevrimiçi hesap henüz yapılandırılmadı. Misafir olarak devam edebilirsin.");
        }

        private void CompleteUnlinkedProjectSignIn(PlayerAuthProvider provider)
        {
            if (provider == PlayerAuthProvider.Guest)
            {
                flow?.CompleteGuestFallback();
                Debug.Log("[DC_UGS_GUEST_FALLBACK_PASS] screen=MainMenu provider=Guest reason=project-unlinked");
                return;
            }
            flow?.SetLoginStatus("Çevrimiçi hesap bağlantısı hazırlanıyor. Şimdilik misafir olarak devam edebilirsin.");
        }

        private void OnDestroy()
        {
            if (flow != null) flow.AuthenticationRequested -= HandleAuthenticationRequested;
            if (progress != null) progress.Changed -= HandleProgressChanged;
            if (playerAccountsSubscribed) PlayerAccountService.Instance.SignedIn -= HandlePlayerAccountSignedIn;
        }
    }
}
