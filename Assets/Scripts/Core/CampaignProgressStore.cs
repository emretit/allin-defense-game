using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DefenderChronicles.Data;
using DefenderChronicles.Combat;
using UnityEngine;

namespace DefenderChronicles.Core
{
    public enum PlayerAuthProvider
    {
        None = 0,
        Guest = 1,
        [Obsolete("Use PlayerAccount. Kept only for save migration.")]
        Apple = 2,
        [Obsolete("Use PlayerAccount. Kept only for save migration.")]
        Google = 3,
        PlayerAccount = 4
    }

    [Serializable]
    public sealed class CampaignLevelProgress
    {
        public int level;
        public int stars;
        public int bestLives;
    }

    [Serializable]
    public sealed class RejectedCompletion
    {
        public CampaignCompletionClaim claim;
        public string reason;
    }

    [Serializable]
    public sealed class CampaignSaveData
    {
        public int version = CampaignProgressStore.CurrentVersion;
        public string playerId;
        public string displayName;
        public PlayerAuthProvider authProvider;
        public int unlockedLevel = 1;
        public int lastSelectedLevel = 1;
        public int valor;
        public int heroXp;
        public List<CampaignLevelProgress> levels = new();
        public long serverRevision;
        public long nextClaimSequence = 1;
        public bool migrationNeedsReview;
        public bool unreadableSavePreserved;
        public BattleCheckpoint checkpoint;
        public List<RejectedCompletion> rejectedClaims = new();
        public List<string> guestImports = new();
        public List<CampaignCompletionClaim> pendingClaims = new();

        public int TotalStars => levels?.Sum(item => Mathf.Clamp(item.stars, 0, 3)) ?? 0;
    }

    public readonly struct CampaignCompletionReward
    {
        public CampaignCompletionReward(int valor, int heroXp)
        {
            Valor = valor;
            HeroXp = heroXp;
        }

        public int Valor { get; }
        public int HeroXp { get; }
    }

    public sealed class CampaignProgressStore
    {
        public const int CurrentVersion = 4;
        public const string FileName = "campaign-save.json";

        [Serializable] private sealed class ActiveProfile { public string key; }
        private readonly string rootPath;
        public CampaignSaveData Data { get; private set; }
        public string SavePath { get; private set; }
        public string ProfileKey { get; private set; }
        public bool HasWriteError { get; private set; }
        public string LocalError { get; private set; }
        public bool RecoveredFromBackup { get; private set; }
        public CampaignSyncStatus SyncStatus { get; private set; } = CampaignSyncStatus.LocalOnly;
        public int PendingClaimCount => Data.pendingClaims?.Count ?? 0;
        public event Action Changed;
        public event Action<CampaignSyncStatus> SyncStatusChanged;
        public event Action ProfileChanged;

        public CampaignProgressStore(string savePath = null)
        {
            rootPath = string.IsNullOrWhiteSpace(savePath)
                ? Path.Combine(Application.persistentDataPath, "closed-test", FileName) : savePath;
            if (savePath == null && !File.Exists(rootPath) && !File.Exists(rootPath + ".active"))
            {
                string legacy = Path.Combine(Application.persistentDataPath, FileName);
                if (File.Exists(legacy))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(rootPath));
                    File.Copy(legacy, rootPath, false); // Import a copy; never mutate the original profile.
                }
            }
            if (AtomicSaveFile.TryRead(rootPath + ".active", ValidPointer, out string pointer, out _))
                ProfileKey = JsonUtility.FromJson<ActiveProfile>(pointer).key;
            if (ProfileKey != null)
            {
                SavePath = ProfilePath(ProfileKey);
                Data = LoadAt(SavePath);
            }
            else
            {
                Data = LoadAt(rootPath);
                ProfileKey = Data.authProvider == PlayerAuthProvider.PlayerAccount && !string.IsNullOrEmpty(Data.playerId)
                    ? AtomicSaveFile.Hash(Data.playerId) : "guest";
                SavePath = ProfilePath(ProfileKey);
            }
            if (string.IsNullOrEmpty(Data.playerId))
            {
                Data.playerId = "local-guest";
                Data.authProvider = PlayerAuthProvider.Guest;
            }
        }

        private string ProfilePath(string key) => Path.Combine(rootPath + ".profiles", key + ".json");
        private static bool ValidPointer(string json)
        {
            try
            {
                string key = JsonUtility.FromJson<ActiveProfile>(json)?.key;
                return key == "guest" || (key != null && key.Length == 64 && key.All(Uri.IsHexDigit));
            }
            catch (ArgumentException) { return false; }
        }

        public void SignIn(PlayerAuthProvider provider, string playerId, string displayName)
        {
            string id = string.IsNullOrWhiteSpace(playerId) ? "local-guest" : playerId;
            string nextKey = provider == PlayerAuthProvider.Guest ? "guest" : AtomicSaveFile.Hash(id);
            Save();
            if (HasWriteError) throw new IOException("Account switch deferred until the current save can be written.");
            if (nextKey != ProfileKey)
            {
                var nextData = LoadAt(ProfilePath(nextKey));
                ProfileKey = nextKey;
                SavePath = ProfilePath(nextKey);
                Data = nextData;
            }
            string oldOwner = Data.playerId;
            Data.authProvider = provider;
            Data.playerId = id;
            Data.displayName = string.IsNullOrWhiteSpace(displayName) ? "Muhafız" : displayName;
            // Guest authentication attaches the existing device guest. Registered profiles never inherit it.
            if (provider == PlayerAuthProvider.Guest)
                foreach (var claim in Data.pendingClaims) claim.ownerPlayerId = id;
            Save();
            SetSyncStatus(Data.migrationNeedsReview ? CampaignSyncStatus.RequiresReview : CampaignSyncStatus.LocalOnly);
            ProfileChanged?.Invoke();
        }

        public bool CanImportGuest => ProfileKey != "guest" && File.Exists(ProfilePath("guest")) &&
                                      !Data.guestImports.Contains("guest");

        public bool ImportGuestProgress()
        {
            if (!CanImportGuest) return false;
            CampaignSaveData guest = LoadAt(ProfilePath("guest"));
            // Deliberate user action: create fresh account-owned claims, never move the guest's queue.
            foreach (var result in guest.levels.OrderBy(x => x.level))
            {
                if (result.bestLives <= 0 || result.stars <= 0) continue;
                if (result.level > Data.unlockedLevel) { Data.migrationNeedsReview = true; break; }
                CompleteLevel(result.level, result.stars, result.bestLives);
            }
            Data.guestImports.Add("guest");
            Save();
            return true;
        }

        public int NextUnfinishedLevel(int available = 10)
        {
            for (int level = 1; level <= Mathf.Min(available, Data.unlockedLevel); level++)
                if (ProgressFor(level)?.stars is not > 0) return level;
            return Mathf.Min(available, Data.unlockedLevel);
        }

        public void SelectLevel(int level)
        {
            Data.lastSelectedLevel = Mathf.Clamp(level, 1, CampaignCatalog.LevelCount);
            Save();
        }

        public CampaignCompletionReward CompleteLevel(int level, int stars, int remainingLives)
        {
            int clampedLevel = Mathf.Clamp(level, 1, CampaignCatalog.LevelCount);
            CampaignLevelDefinition definition = CampaignCatalog.Level(clampedLevel);
            if (remainingLives <= 0 || remainingLives > definition.startingLives)
                throw new ArgumentOutOfRangeException(nameof(remainingLives), "Victory requires valid remaining lives.");
            var claim = new CampaignCompletionClaim
            {
                claimId = Guid.NewGuid().ToString("N"),
                ownerPlayerId = Data.playerId,
                sequence = Data.nextClaimSequence++,
                levelId = definition.id,
                level = clampedLevel,
                stars = BattleRules.StarsForLives(remainingLives, definition.startingLives),
                remainingLives = Mathf.Clamp(remainingLives, 0, definition.startingLives),
                createdAtUtc = DateTime.UtcNow.ToString("O")
            };
            Data.pendingClaims.Add(claim);
            Data.checkpoint = null; // Completion and checkpoint removal commit in the same atomic save.
            CampaignCompletionReward reward = ApplyCompletion(Data, claim);
            Data.lastSelectedLevel = NextUnfinishedLevel();
            Save();
            SetSyncStatus(CampaignSyncStatus.Pending);
            return reward;
        }

        public IReadOnlyList<CampaignCompletionClaim> PendingClaims(int limit = 20) =>
            Data.pendingClaims.OrderBy(item => item.sequence).Take(Mathf.Max(1, limit)).ToArray();

        public void BeginSync() => SetSyncStatus(CampaignSyncStatus.Syncing);

        public void FailSync(bool requiresAccount = false) =>
            SetSyncStatus(requiresAccount ? CampaignSyncStatus.RequiresAccount : CampaignSyncStatus.Failed);

        public void ApplyServerSync(CampaignSyncResult result)
        {
            if (result?.state == null) throw new ArgumentNullException(nameof(result));
            if (result.rulesVersion != CampaignRulesVersion.Current)
            {
                SetSyncStatus(CampaignSyncStatus.RequiresReview);
                throw new InvalidOperationException("Campaign rules require an update; pending progress is retained.");
            }
            if (result.state.revision < Data.serverRevision) return;
            var processed = new HashSet<string>();
            foreach (var decision in result.decisions ?? new List<CampaignClaimDecision>())
            {
                if (decision == null || string.IsNullOrEmpty(decision.claimId)) continue;
                var claim = Data.pendingClaims.FirstOrDefault(x => x.claimId == decision.claimId);
                if (claim == null) continue;
                processed.Add(claim.claimId);
                if (!decision.accepted)
                    Data.rejectedClaims.Add(new RejectedCompletion { claim = claim, reason = decision.reason });
            }
            Data.pendingClaims.RemoveAll(x => processed.Contains(x.claimId));
            var confirmed = result.state.levels ?? new List<CampaignLevelProgress>();
            bool legacyAhead = (Data.migrationNeedsReview || result.state.migrationIncomplete) &&
                Data.levels.Any(local => local.stars > (confirmed.FirstOrDefault(x => x.level == local.level)?.stars ?? 0) &&
                    !Data.pendingClaims.Any(x => x.level == local.level));
            Data.serverRevision = result.state.revision;
            if (legacyAhead)
            {
                // Keep incompatible legacy progress playable locally; never overwrite it with an empty cloud state.
                Data.migrationNeedsReview = true;
                Save();
                SetSyncStatus(CampaignSyncStatus.RequiresReview);
                return;
            }
            Data.migrationNeedsReview = false;
            Data.levels = confirmed.Select(x => new CampaignLevelProgress
                { level = x.level, stars = x.stars, bestLives = x.bestLives }).ToList();
            Data.unlockedLevel = result.state.unlockedLevel;
            Data.valor = result.state.valor;
            Data.heroXp = result.state.heroXp;
            foreach (var pending in Data.pendingClaims) ApplyCompletion(Data, pending);
            Save();
            SetSyncStatus(Data.rejectedClaims.Count > 0 ? CampaignSyncStatus.RequiresReview :
                Data.pendingClaims.Count > 0 ? CampaignSyncStatus.Pending : CampaignSyncStatus.Current);
        }

        public CampaignLevelProgress ProgressFor(int level) =>
            Data.levels.FirstOrDefault(item => item.level == level);

        public void Merge(CampaignSaveData incoming)
        {
            incoming = Normalize(incoming);
            Data.unlockedLevel = Mathf.Max(Data.unlockedLevel, incoming.unlockedLevel);
            Data.valor = Mathf.Max(Data.valor, incoming.valor);
            Data.heroXp = Mathf.Max(Data.heroXp, incoming.heroXp);
            foreach (CampaignLevelProgress incomingLevel in incoming.levels)
            {
                CampaignLevelProgress local = Data.levels.FirstOrDefault(item => item.level == incomingLevel.level);
                if (local == null)
                {
                    Data.levels.Add(new CampaignLevelProgress
                    {
                        level = incomingLevel.level,
                        stars = incomingLevel.stars,
                        bestLives = incomingLevel.bestLives
                    });
                    continue;
                }
                local.stars = Mathf.Max(local.stars, incomingLevel.stars);
                local.bestLives = Mathf.Max(local.bestLives, incomingLevel.bestLives);
            }
            Data.lastSelectedLevel = Mathf.Clamp(
                Mathf.Max(Data.lastSelectedLevel, incoming.lastSelectedLevel), 1, Data.unlockedLevel);
            Save();
        }

        public void Save()
        {
            Normalize(Data);
            try
            {
                AtomicSaveFile.Write(SavePath, JsonUtility.ToJson(Data));
                AtomicSaveFile.Write(rootPath + ".active", JsonUtility.ToJson(new ActiveProfile { key = ProfileKey }));
                HasWriteError = false; LocalError = null;
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
            {
                HasWriteError = true; LocalError = error.Message;
                SetSyncStatus(CampaignSyncStatus.RequiresReview);
                Debug.LogWarning("Local save retry required: " + error.Message);
            }
            Changed?.Invoke();
        }

        private CampaignSaveData LoadAt(string path)
        {
            bool Valid(string json)
            {
                try
                {
                    if (!json.Contains("\"version\"") || !json.Contains("\"unlockedLevel\"")) return false;
                    var candidate = JsonUtility.FromJson<CampaignSaveData>(json);
                    return candidate != null && candidate.version > 0 && candidate.version <= CurrentVersion;
                }
                catch (ArgumentException) { return false; }
            }
            if (!AtomicSaveFile.TryRead(path, Valid, out string payload, out bool recovered))
            {
                if (File.Exists(path) || File.Exists(path + ".bak") || File.Exists(path + ".tmp"))
                {
                    // Preserve every byte before allowing a fresh local session to write anything.
                    string recovery = path + ".unreadable-" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
                    Directory.CreateDirectory(recovery);
                    foreach (string suffix in new[] { "", ".bak", ".tmp" })
                        if (File.Exists(path+suffix)) File.Copy(path+suffix,Path.Combine(recovery,"save"+suffix));
                    LocalError = "Unreadable save preserved at " + recovery;
                    SetSyncStatus(CampaignSyncStatus.RequiresReview);
                    return Normalize(new CampaignSaveData { migrationNeedsReview = true, unreadableSavePreserved = true });
                }
                return Normalize(new CampaignSaveData());
            }
            RecoveredFromBackup |= recovered;
            var data = JsonUtility.FromJson<CampaignSaveData>(payload);
            if (data.version < CurrentVersion)
            {
                if (!File.Exists(path + ".legacy-v3")) AtomicSaveFile.Write(path + ".legacy-v3", payload);
                data.migrationNeedsReview = data.levels != null && data.levels.Any(x => x != null && x.stars > 0);
            }
            return Normalize(data);
        }

        public static CampaignSaveData Normalize(CampaignSaveData data)
        {
            data ??= new CampaignSaveData();
            // JsonUtility materializes null serializable classes as empty objects on some players.
            if (data.checkpoint != null && string.IsNullOrEmpty(data.checkpoint.owner)) data.checkpoint = null;
            data.version = CurrentVersion;
            if ((int)data.authProvider is 2 or 3)
                data.authProvider = PlayerAuthProvider.PlayerAccount;
            data.unlockedLevel = Mathf.Clamp(data.unlockedLevel, 1, CampaignCatalog.LevelCount);
            data.lastSelectedLevel = Mathf.Clamp(data.lastSelectedLevel, 1, data.unlockedLevel);
            data.valor = Mathf.Max(0, data.valor);
            data.heroXp = Mathf.Max(0, data.heroXp);
            data.serverRevision = Math.Max(0, data.serverRevision);
            data.levels ??= new List<CampaignLevelProgress>();
            data.levels = data.levels.Where(item => item != null && item.level >= 1 && item.level <= CampaignCatalog.LevelCount)
                .GroupBy(item => item.level)
                .Select(group => new CampaignLevelProgress
                {
                    level = group.Key,
                    stars = Mathf.Clamp(group.Max(item => item.stars), 0, 3),
                    bestLives = Mathf.Clamp(group.Max(item => item.bestLives), 0,
                        CampaignCatalog.Level(group.Key).startingLives)
                })
                .OrderBy(item => item.level)
                .ToList();
            data.rejectedClaims ??= new List<RejectedCompletion>();
            data.guestImports ??= new List<string>();
            data.pendingClaims ??= new List<CampaignCompletionClaim>();
            data.pendingClaims = data.pendingClaims
                .Where(item => item != null && !string.IsNullOrWhiteSpace(item.claimId) &&
                               item.sequence > 0 && item.level >= 1 && item.level <= CampaignCatalog.LevelCount)
                .GroupBy(item => item.claimId, StringComparer.Ordinal)
                .Select(group => group.OrderBy(item => item.sequence).First())
                .OrderBy(item => item.sequence)
                .ToList();
            foreach (CampaignCompletionClaim claim in data.pendingClaims)
            {
                CampaignLevelDefinition definition = CampaignCatalog.Level(claim.level);
                if (string.IsNullOrWhiteSpace(claim.levelId)) claim.levelId = definition.id;
                claim.stars = Mathf.Clamp(claim.stars, 1, 3);
                claim.remainingLives = Mathf.Clamp(claim.remainingLives, 0, definition.startingLives);
                if (string.IsNullOrWhiteSpace(claim.rulesVersion)) claim.rulesVersion = "campaign-rules-v1";
                if (string.IsNullOrEmpty(claim.ownerPlayerId)) claim.ownerPlayerId = data.playerId;
            }
            long lastSequence = data.pendingClaims.Count == 0 ? 0 : data.pendingClaims.Max(item => item.sequence);
            data.nextClaimSequence = Math.Max(Math.Max(1, data.nextClaimSequence), lastSequence + 1);
            return data;
        }

        private static CampaignCompletionReward ApplyCompletion(CampaignSaveData target,
            CampaignCompletionClaim claim)
        {
            int clampedLevel = Mathf.Clamp(claim.level, 1, CampaignCatalog.LevelCount);
            CampaignLevelProgress progress = target.levels.FirstOrDefault(item => item.level == clampedLevel);
            if (progress == null)
            {
                progress = new CampaignLevelProgress { level = clampedLevel };
                target.levels.Add(progress);
            }

            int previousStars = progress.stars;
            int earnedStars = Mathf.Clamp(claim.stars, 1, 3);
            int improvedStars = Mathf.Max(0, earnedStars - previousStars);
            CampaignLevelDefinition definition = CampaignCatalog.Level(clampedLevel);
            int valorReward = improvedStars * 5 + (previousStars == 0 ? definition.valorReward : 0);
            int heroXpReward = previousStars == 0 ? definition.heroXpReward : 0;

            progress.stars = Mathf.Max(progress.stars, earnedStars);
            progress.bestLives = Mathf.Max(progress.bestLives,
                Mathf.Clamp(claim.remainingLives, 0, definition.startingLives));
            target.unlockedLevel = Mathf.Max(target.unlockedLevel,
                Mathf.Min(CampaignCatalog.LevelCount, clampedLevel + 1));
            target.valor += valorReward;
            target.heroXp += heroXpReward;
            return new CampaignCompletionReward(valorReward, heroXpReward);
        }

        private void SetSyncStatus(CampaignSyncStatus status)
        {
            if (HasWriteError || Data?.unreadableSavePreserved == true) status = CampaignSyncStatus.RequiresReview;
            if (SyncStatus == status) return;
            SyncStatus = status;
            SyncStatusChanged?.Invoke(status);
        }
    }
}
