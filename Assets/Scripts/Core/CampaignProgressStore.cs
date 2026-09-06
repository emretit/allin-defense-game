using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DefenderChronicles.Data;
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
        public const int CurrentVersion = 3;
        public const string FileName = "campaign-save.json";

        public CampaignSaveData Data { get; private set; }
        public string SavePath { get; }
        public CampaignSyncStatus SyncStatus { get; private set; } = CampaignSyncStatus.LocalOnly;
        public int PendingClaimCount => Data.pendingClaims?.Count ?? 0;
        public event Action Changed;
        public event Action<CampaignSyncStatus> SyncStatusChanged;

        public CampaignProgressStore(string savePath = null)
        {
            SavePath = string.IsNullOrWhiteSpace(savePath)
                ? Path.Combine(Application.persistentDataPath, FileName)
                : savePath;
            Data = Load();
        }

        public void SignIn(PlayerAuthProvider provider, string playerId, string displayName)
        {
            Data.authProvider = provider;
            Data.playerId = string.IsNullOrWhiteSpace(playerId) ? Guid.NewGuid().ToString("N") : playerId;
            Data.displayName = string.IsNullOrWhiteSpace(displayName) ? "Muhafız" : displayName;
            Save();
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
            var claim = new CampaignCompletionClaim
            {
                claimId = Guid.NewGuid().ToString("N"),
                sequence = Data.nextClaimSequence++,
                levelId = definition.id,
                level = clampedLevel,
                stars = Mathf.Clamp(stars, 1, 3),
                remainingLives = Mathf.Clamp(remainingLives, 0, definition.startingLives),
                createdAtUtc = DateTime.UtcNow.ToString("O")
            };
            Data.pendingClaims.Add(claim);
            CampaignCompletionReward reward = ApplyCompletion(Data, claim);
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
            if (!string.Equals(result.rulesVersion, CampaignRulesVersion.Current, StringComparison.Ordinal))
                throw new InvalidOperationException($"Campaign rules mismatch: {result.rulesVersion}.");

            var processed = new HashSet<string>(
                result.decisions?.Where(item => item != null && !string.IsNullOrWhiteSpace(item.claimId))
                    .Select(item => item.claimId) ?? Enumerable.Empty<string>(),
                StringComparer.Ordinal);
            List<CampaignCompletionClaim> remaining = Data.pendingClaims
                .Where(item => item != null && !processed.Contains(item.claimId))
                .OrderBy(item => item.sequence)
                .ToList();

            string playerId = Data.playerId;
            string displayName = Data.displayName;
            PlayerAuthProvider authProvider = Data.authProvider;
            int lastSelectedLevel = Data.lastSelectedLevel;
            long nextSequence = Data.nextClaimSequence;

            Data = new CampaignSaveData
            {
                playerId = playerId,
                displayName = displayName,
                authProvider = authProvider,
                unlockedLevel = result.state.unlockedLevel,
                lastSelectedLevel = lastSelectedLevel,
                valor = result.state.valor,
                heroXp = result.state.heroXp,
                levels = result.state.levels ?? new List<CampaignLevelProgress>(),
                serverRevision = result.state.revision,
                nextClaimSequence = nextSequence,
                pendingClaims = remaining
            };
            foreach (CampaignCompletionClaim pending in remaining) ApplyCompletion(Data, pending);
            Save();
            SetSyncStatus(remaining.Count == 0 ? CampaignSyncStatus.Current : CampaignSyncStatus.Pending);
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
            string directory = Path.GetDirectoryName(SavePath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            string temporaryPath = SavePath + ".tmp";
            File.WriteAllText(temporaryPath, JsonUtility.ToJson(Data, true));
            if (File.Exists(SavePath)) File.Delete(SavePath);
            File.Move(temporaryPath, SavePath);
            Changed?.Invoke();
        }

        private CampaignSaveData Load()
        {
            if (!File.Exists(SavePath)) return Normalize(new CampaignSaveData());
            try
            {
                return Normalize(JsonUtility.FromJson<CampaignSaveData>(File.ReadAllText(SavePath)));
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Campaign save could not be read; a clean local save was created. {exception.Message}");
                return Normalize(new CampaignSaveData());
            }
        }

        public static CampaignSaveData Normalize(CampaignSaveData data)
        {
            data ??= new CampaignSaveData();
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
                claim.levelId = definition.id;
                claim.stars = Mathf.Clamp(claim.stars, 1, 3);
                claim.remainingLives = Mathf.Clamp(claim.remainingLives, 0, definition.startingLives);
                claim.rulesVersion = CampaignRulesVersion.Current;
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
            if (SyncStatus == status) return;
            SyncStatus = status;
            SyncStatusChanged?.Invoke(status);
        }
    }
}
