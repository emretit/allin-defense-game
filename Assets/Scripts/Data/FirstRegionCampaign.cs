using System;
using System.Linq;
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
        [Serializable] private sealed class Document { public string version; public FirstRegionLevel[] levels; }
        private static Document document;
        public static FirstRegionLevel Level(int number)
        {
            if (document == null)
            {
                var resource = Resources.Load<TextAsset>("FirstRegionCampaign");
                if (resource == null) throw new InvalidOperationException("First-region data missing.");
                document = JsonUtility.FromJson<Document>(resource.text);
                if (document.version != Version || document.levels.Length != LevelCount)
                    throw new InvalidOperationException("Incompatible first-region data.");
            }
            return document.levels[Mathf.Clamp(number, 1, LevelCount) - 1];
        }
        public static WaveDefinition[] CreateWaves(int number) => Level(number).waves.Select(w =>
            new WaveDefinition(w.id, string.Join(" • ", w.groups.Select(g => $"{EnemyName(g.enemy)} ×{g.count}")),
                w.clearBonus, w.bossWave, w.groups.Select(g => new SpawnGroupDefinition(g.enemy, g.count, g.startDelay, g.interval)).ToArray())).ToArray();
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
