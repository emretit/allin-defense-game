using System;
using System.Collections.Generic;
using UnityEngine;

namespace DefenderChronicles.Data
{
    [Serializable]
    public sealed class CampaignLevelDefinition
    {
        public int number;
        public int region;
        public int indexInRegion;
        public string id;
        public string displayName;
        public string localizedName => DefenderChronicles.UI.GameLocalization.Translate(displayName);
        public int waveCount;
        public int startingGold;
        public int startingLives;
        public float targetMinutes;
        public float difficulty;
        public int valorReward;
        public int heroXpReward;
        public bool isBoss;
        public int pathPreset;
    }

    [Serializable]
    public sealed class CampaignRegionDefinition
    {
        public int number;
        public string id;
        public string displayName;
        public string localizedName => DefenderChronicles.UI.GameLocalization.Translate(displayName);
        public string subtitle;
        public Color primaryColor;
        public Color accentColor;
        public List<CampaignLevelDefinition> levels = new();
    }

    public static class CampaignCatalog
    {
        public const int RegionCount = 5;
        public const int LevelsPerRegion = 10;
        public const int LevelCount = RegionCount * LevelsPerRegion;

        private static readonly string[] RegionNames =
        {
            "Yeşil Nöbet", "Kül Geçidi", "Buz Sınırı", "Batık Krallık", "Son Hisar"
        };

        private static readonly string[] RegionSubtitles =
        {
            "Savunmanın temelleri", "Zırh ve kuşatma", "Hız ve don etkileri",
            "Çoklu baskı hatları", "Ustalık sınavı"
        };

        private static readonly Color[] RegionColors =
        {
            new(0.08f, 0.29f, 0.24f), new(0.30f, 0.15f, 0.12f), new(0.10f, 0.25f, 0.34f),
            new(0.19f, 0.13f, 0.31f), new(0.25f, 0.08f, 0.10f)
        };

        private static readonly Color[] RegionAccents =
        {
            new(0.25f, 0.82f, 0.55f), new(1f, 0.48f, 0.22f), new(0.30f, 0.78f, 1f),
            new(0.76f, 0.48f, 1f), new(1f, 0.76f, 0.22f)
        };

        private static readonly string[,] LevelNames =
        {
            { "İlk Nöbet", "Dar Patika", "Çifte Baskı", "Eski Köprü", "Sessiz Koruluk", "Kırık Yol", "Gece Akını", "Taş Çember", "Kapı Önü", "Yeşil Muhafız" },
            { "Kızıl Toprak", "Maden Yolu", "Ağır Adımlar", "İki Cephe", "Kül Yağmuru", "Demir Konvoy", "Yarık Geçit", "Kuşatma Kampı", "Kor Ateşi", "Geçit Efendisi" },
            { "İlk Ayaz", "Donmuş Dere", "Beyaz Sis", "Keskin Rüzgar", "Buz Köprüsü", "Kurtuluş Yolu", "Soğuk Pusu", "Kristal Kapı", "Uzun Gece", "Buz Hükümdarı" },
            { "Kayıp Kıyı", "Batık Yol", "Gelgit", "İkiz Liman", "Derin Sular", "Korsan Baskını", "Yıkık Saray", "Boğaz Savunması", "Son Fener", "Derinliklerin Lordu" },
            { "Dönüş Yolu", "Kara Ordu", "Üç Koldan", "Son Erzak", "Kırılan Hat", "Şafak Öncesi", "Büyük Kuşatma", "İç Kale", "Son Kapı", "Lordrush" }
        };

        private static IReadOnlyList<CampaignRegionDefinition> regions;

        public static IReadOnlyList<CampaignRegionDefinition> Regions => regions ??= Build();

        public static CampaignLevelDefinition Level(int number)
        {
            int clamped = Mathf.Clamp(number, 1, LevelCount) - 1;
            return Regions[clamped / LevelsPerRegion].levels[clamped % LevelsPerRegion];
        }

        private static IReadOnlyList<CampaignRegionDefinition> Build()
        {
            var result = new List<CampaignRegionDefinition>(RegionCount);
            float[] localDifficulty = { 0f, 0.12f, 0.28f, 0.48f, 0.35f, 0.62f, 0.86f, 1.12f, 0.94f, 1.55f };

            for (int regionIndex = 0; regionIndex < RegionCount; regionIndex++)
            {
                var region = new CampaignRegionDefinition
                {
                    number = regionIndex + 1,
                    id = $"region-{regionIndex + 1:00}",
                    displayName = RegionNames[regionIndex],
                    subtitle = RegionSubtitles[regionIndex],
                    primaryColor = RegionColors[regionIndex],
                    accentColor = RegionAccents[regionIndex]
                };

                for (int localIndex = 0; localIndex < LevelsPerRegion; localIndex++)
                {
                    int number = regionIndex * LevelsPerRegion + localIndex + 1;
                    float progress = (number - 1f) / (LevelCount - 1f);
                    region.levels.Add(new CampaignLevelDefinition
                    {
                        number = number,
                        region = regionIndex + 1,
                        indexInRegion = localIndex + 1,
                        id = $"level-{number:00}",
                        displayName = LevelNames[regionIndex, localIndex],
                        waveCount = Mathf.Clamp(3 + regionIndex + localIndex / 2 +
                                                (localIndex == 9 && regionIndex > 0 ? 1 : 0), 3, 12),
                        startingGold = Mathf.RoundToInt(Mathf.Lerp(230f, 155f, progress) / 5f) * 5,
                        startingLives = Mathf.RoundToInt(Mathf.Lerp(20f, 12f, progress)),
                        targetMinutes = Mathf.Round(Mathf.Lerp(4f, 10f, progress) * 2f) / 2f,
                        difficulty = 1f + regionIndex * 1.55f + localDifficulty[localIndex],
                        valorReward = 12 + regionIndex * 4 + localIndex * 2 + (localIndex == 9 ? 25 : 0),
                        heroXpReward = 60 + number * 8 + (localIndex == 9 ? 100 : 0),
                        isBoss = localIndex == 9,
                        pathPreset = (regionIndex + localIndex) % 5
                    });
                }
                if (regionIndex == 0)
                {
                    region.levels[9].startingGold = 200;
                    region.levels[9].startingLives = 20;
                }
                if (regionIndex == 0)
                    foreach (var level in region.levels)
                    {
                        var authored = FirstRegionCampaign.Level(level.number);
                        level.waveCount = authored.waves.Length;
                        level.startingGold = authored.startingGold;
                        level.targetMinutes = (authored.targetMinSeconds + authored.targetMaxSeconds) / 120f;
                    }
                result.Add(region);
            }
            return result;
        }
    }
}
