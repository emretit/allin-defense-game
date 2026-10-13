using System.Threading.Tasks;

namespace AllinDefense.Core
{
    public enum ExternalSignInStatus { Success, Cancelled, Failed, NotConfigured }

    public readonly struct ExternalSignInResult
    {
        public ExternalSignInStatus Status { get; }
        public string IdToken { get; }
        public string DisplayName { get; }
        public ExternalSignInResult(ExternalSignInStatus status, string idToken = null, string displayName = null)
        { Status = status; IdToken = idToken; DisplayName = displayName; }
    }

    public interface IExternalSignInProvider
    {
        Task<ExternalSignInResult> RequestIdTokenAsync();
        void ClearSession();
    }

    public interface IExternalTokenAuthentication
    {
        Task<string> SignInAsync(PlayerAuthProvider provider, string idToken);
        Task<string> ResolvePlayerNameAsync();
        Task DeleteAccountAsync(bool localOnly);
    }
}
