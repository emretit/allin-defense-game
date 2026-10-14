using System;
using System.Linq;
using UnityEngine;

namespace AllinDefense.Data
{
    [Serializable] public sealed class BossEncounterProfile
    {
        public string id;
        public EnemyArchetype reinforcementEnemy;
        public int reinforcementCount;
        public string reinforcementRoute;
        public float[] phaseThresholds;
        public float phase3SpeedMultiplier, silenceInterval;
        public float reinforcementStartDelay, reinforcementInterval;
        public int supportCount;

        [Serializable] private sealed class Document { public BossEncounterProfile[] profiles; }
        private static Document document;

        public static BossEncounterProfile Resolve(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (document == null)
            {
                var asset = Resources.Load<TextAsset>("Campaign/boss-profiles");
                if (asset == null) throw new InvalidOperationException("Boss profiles are missing.");
                var loaded = JsonUtility.FromJson<Document>(asset.text);
                if (loaded?.profiles == null || loaded.profiles.Length == 0)
                    throw new InvalidOperationException("Boss profiles are empty.");
                foreach (var profile in loaded.profiles) Validate(profile);
                if (loaded.profiles.Select(p => p.id).Distinct().Count() != loaded.profiles.Length)
                    throw new InvalidOperationException("Duplicate boss profile id.");
                document = loaded;
            }
            return document.profiles.FirstOrDefault(p => p.id == id)
                ?? throw new InvalidOperationException($"Unknown boss profile '{id}'.");
        }

        public static void Validate(BossEncounterProfile profile)
        {
            if (profile == null || string.IsNullOrWhiteSpace(profile.id) ||
                !Enum.IsDefined(typeof(EnemyArchetype), profile.reinforcementEnemy) || profile.reinforcementEnemy == EnemyArchetype.Boss ||
                profile.reinforcementCount <= 0 || profile.supportCount < 0 ||
                (profile.reinforcementRoute != "first" && profile.reinforcementRoute != "rotate" && profile.reinforcementRoute != "all") ||
                profile.phaseThresholds == null || profile.phaseThresholds.Length != 2 ||
                !float.IsFinite(profile.phaseThresholds[0]) || !float.IsFinite(profile.phaseThresholds[1]) ||
                profile.phaseThresholds[0] >= 1 || profile.phaseThresholds[1] <= 0 || profile.phaseThresholds[0] <= profile.phaseThresholds[1] ||
                !float.IsFinite(profile.phase3SpeedMultiplier) || profile.phase3SpeedMultiplier <= 0 ||
                !float.IsFinite(profile.silenceInterval) || profile.silenceInterval <= 0 ||
                !float.IsFinite(profile.reinforcementStartDelay) || profile.reinforcementStartDelay < 0 ||
                !float.IsFinite(profile.reinforcementInterval) || profile.reinforcementInterval <= 0)
                throw new InvalidOperationException("Invalid boss profile.");
        }
    }
}
