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
        None,
        Guest,
        Apple,
        Google
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
        public const int CurrentVersion = 2;
        public const string FileName = "campaign-save.json";

        public CampaignSaveData Data { get; private set; }
        public string SavePath { get; }
        public event Action Changed;

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
            CampaignLevelProgress progress = Data.levels.FirstOrDefault(item => item.level == clampedLevel);
            if (progress == null)
            {
                progress = new CampaignLevelProgress { level = clampedLevel };
                Data.levels.Add(progress);
            }

            int previousStars = progress.stars;
            int earnedStars = Mathf.Clamp(stars, 1, 3);
            int improvedStars = Mathf.Max(0, earnedStars - previousStars);
            CampaignLevelDefinition definition = CampaignCatalog.Level(clampedLevel);
            int valorReward = improvedStars * 5 + (previousStars == 0 ? definition.valorReward : 0);
            int heroXpReward = previousStars == 0 ? definition.heroXpReward : 0;

            progress.stars = Mathf.Max(progress.stars, earnedStars);
            progress.bestLives = Mathf.Max(progress.bestLives, Mathf.Max(0, remainingLives));
            Data.unlockedLevel = Mathf.Max(Data.unlockedLevel, Mathf.Min(CampaignCatalog.LevelCount, clampedLevel + 1));
            Data.valor += valorReward;
            Data.heroXp += heroXpReward;
            Save();
            return new CampaignCompletionReward(valorReward, heroXpReward);
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
            data.unlockedLevel = Mathf.Clamp(data.unlockedLevel, 1, CampaignCatalog.LevelCount);
            data.lastSelectedLevel = Mathf.Clamp(data.lastSelectedLevel, 1, data.unlockedLevel);
            data.valor = Mathf.Max(0, data.valor);
            data.heroXp = Mathf.Max(0, data.heroXp);
            data.levels ??= new List<CampaignLevelProgress>();
            data.levels = data.levels.Where(item => item != null && item.level >= 1 && item.level <= CampaignCatalog.LevelCount)
                .GroupBy(item => item.level)
                .Select(group => new CampaignLevelProgress
                {
                    level = group.Key,
                    stars = Mathf.Clamp(group.Max(item => item.stars), 0, 3),
                    bestLives = Mathf.Max(0, group.Max(item => item.bestLives))
                })
                .OrderBy(item => item.level)
                .ToList();
            return data;
        }
    }
}
