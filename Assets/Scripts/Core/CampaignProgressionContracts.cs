using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AllinDefense.Core
{
    public enum CampaignSyncStatus
    {
        LocalOnly,
        Pending,
        Syncing,
        Current,
        RequiresAccount,
        Failed,
        RequiresReview
    }

    [Serializable]
    public sealed class CampaignCompletionClaim
    {
        public string claimId;
        public string ownerPlayerId;
        public long sequence;
        public string levelId;
        public int level;
        public int stars;
        public int remainingLives;
        public string rulesVersion = CampaignRulesVersion.Current;
        public string createdAtUtc;
    }

    [Serializable]
    public sealed class CampaignServerState
    {
        public int schemaVersion = CampaignProgressStore.CurrentVersion;
        public long revision;
        public bool migrationIncomplete;
        public int unlockedLevel = 1;
        public int valor;
        public int heroXp;
        public List<CampaignLevelProgress> levels = new();
        public List<string> processedClaimIds = new();
    }

    [Serializable]
    public sealed class CampaignClaimDecision
    {
        public string claimId;
        public bool accepted;
        public string reason;
        public int awardedValor;
        public int awardedHeroXp;
    }

    [Serializable]
    public sealed class CampaignSyncResult
    {
        public CampaignServerState state = new();
        public List<CampaignClaimDecision> decisions = new();
        public string rulesVersion = CampaignRulesVersion.Current;
    }

    public interface ICampaignProgressionService
    {
        Task<CampaignSyncResult> GetStateAsync(CampaignSaveData legacySnapshot = null);
        Task<CampaignSyncResult> SubmitPendingAsync(IReadOnlyList<CampaignCompletionClaim> claims);
    }

    public static class CampaignRulesVersion
    {
        public const string Current = "campaign-rules-v2";
    }
}
