using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEngine;

namespace AllinDefense.Core
{
    public sealed class IosExternalSignInBridge : MonoBehaviour
    {
        [Serializable] private sealed class NativeResult
        {
            public string requestId;
            public int status;
            public string token;
            public string displayName;
        }
        private TaskCompletionSource<ExternalSignInResult> pending;
        private string requestId;

        public IExternalSignInProvider Provider(PlayerAuthProvider provider, string googleIosClientId = null) =>
            new NativeProvider(this, provider, googleIosClientId);

        private Task<ExternalSignInResult> RequestAsync(PlayerAuthProvider provider, string clientId)
        {
            if (provider == PlayerAuthProvider.Google && string.IsNullOrWhiteSpace(clientId))
                return Task.FromResult(new ExternalSignInResult(ExternalSignInStatus.NotConfigured));
            if (pending != null) return Task.FromResult(new ExternalSignInResult(ExternalSignInStatus.Cancelled));
#if UNITY_IOS && !UNITY_EDITOR
            requestId = Guid.NewGuid().ToString("N");
            pending = new TaskCompletionSource<ExternalSignInResult>();
            var task = pending.Task;
            AllinAuthRequest(gameObject.name, requestId, (int)provider, clientId ?? string.Empty);
            return task;
#else
            return Task.FromResult(new ExternalSignInResult(ExternalSignInStatus.Failed));
#endif
        }

        public void OnNativeSignIn(string json)
        {
            if (pending == null) return;
            NativeResult result;
            try { result = JsonUtility.FromJson<NativeResult>(json); }
            catch (ArgumentException) { return; }
            if (result == null || result.requestId != requestId) return;
            var completion = pending;
            pending = null; requestId = null;
            var status = result.status is >= 0 and <= 3 ? (ExternalSignInStatus)result.status : ExternalSignInStatus.Failed;
            if (status == ExternalSignInStatus.Success && string.IsNullOrWhiteSpace(result.token)) status = ExternalSignInStatus.Failed;
            completion.TrySetResult(new ExternalSignInResult(status, status == ExternalSignInStatus.Success ? result.token : null,
                status == ExternalSignInStatus.Success ? result.displayName : null));
        }

        public void ClearSession()
        {
#if UNITY_IOS && !UNITY_EDITOR
            AllinAuthClearSession();
#endif
            var completion = pending;
            pending = null; requestId = null;
            completion?.TrySetResult(new ExternalSignInResult(ExternalSignInStatus.Cancelled));
        }
        private void OnDestroy() => ClearSession();

        private sealed class NativeProvider : IExternalSignInProvider
        {
            private readonly IosExternalSignInBridge bridge;
            private readonly PlayerAuthProvider provider;
            private readonly string clientId;
            public NativeProvider(IosExternalSignInBridge bridge, PlayerAuthProvider provider, string clientId)
            { this.bridge = bridge; this.provider = provider; this.clientId = clientId; }
            public Task<ExternalSignInResult> RequestIdTokenAsync() => bridge.RequestAsync(provider, clientId);
            public void ClearSession() => bridge.ClearSession();
        }
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void AllinAuthRequest(string receiver, string requestId, int provider, string clientId);
        [DllImport("__Internal")] private static extern void AllinAuthClearSession();
#endif
    }
}
