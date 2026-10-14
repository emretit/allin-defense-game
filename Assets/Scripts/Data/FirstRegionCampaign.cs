using System;
using AllinDefense.UI;
using UnityEngine;

namespace AllinDefense.Data
{
    [Serializable] public sealed class FirstRegionLevel
    {
        public int number, targetMinSeconds, targetMaxSeconds, startingGold;
        public float healthMultiplier;
        public int[] killRewards;
        public WaveDefinition[] waves;
    }
    public static class FirstRegionCampaign
    {
        public const int LevelCount = 10;
        public const string Version = "first-region-v2";
        public static FirstRegionLevel Level(int number)
        {
            var data = CampaignContent.Level(Mathf.Clamp(number, 1, LevelCount));
            return new FirstRegionLevel { number = data.level, targetMinSeconds = data.targetMinSeconds,
                targetMaxSeconds = data.targetMaxSeconds, startingGold = data.startingGold,
                healthMultiplier = data.healthMultiplier, killRewards = data.killRewards, waves = data.waves };
        }
        public static WaveDefinition[] CreateWaves(int number) => CampaignContent.CreateWaves(Mathf.Clamp(number, 1, LevelCount));
        public static string EnemyName(EnemyArchetype enemy) => enemy switch
        {
            EnemyArchetype.Raider => GameLocalization.Text("Yağmacı", "Raider"),
            EnemyArchetype.Vanguard => GameLocalization.Text("Öncü", "Vanguard"),
            EnemyArchetype.Regenerator => GameLocalization.Text("Yenilenen", "Regenerator"),
            EnemyArchetype.Support => GameLocalization.Text("Şifacı", "Healer"),
            EnemyArchetype.Siege => GameLocalization.Text("Kuşatma", "Siege"),
            EnemyArchetype.Boss => GameLocalization.Text("Bölge Lordu", "Region Lord"),
            _ => enemy.ToString()
        };
    }
}
