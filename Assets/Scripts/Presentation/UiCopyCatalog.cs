using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DefenderChronicles.Presentation
{
    public enum UiTextKey
    {
        Gold,
        Keep,
        Wave,
        Preparing,
        Combat,
        Intermission,
        Paused,
        Loading,
        Pause,
        Continue,
        Archer,
        Barracks,
        Mage,
        ShieldBash,
        Cancel,
        FirstWave,
        EarlyWave,
        NextWave,
        Upgrade,
        Sell,
        Knight,
        AbilityReady,
        KnightReturning,
        Victory,
        Defeat,
        VictoryPhase,
        DefeatPhase,
        Restart,
        SlotInstruction,
        BuildInstruction
    }

    [Serializable]
    public sealed class UiCopyEntry
    {
        public UiTextKey key;
        [TextArea] public string value;
    }

    [CreateAssetMenu(menuName = "Defender Chronicles/UI Copy", fileName = "UiCopyTr")]
    public sealed class UiCopyCatalog : ScriptableObject
    {
        public string locale = "tr-TR";
        public UiCopyEntry[] entries = Array.Empty<UiCopyEntry>();

        public string Get(UiTextKey key)
        {
            return entries.FirstOrDefault(entry => entry != null && entry.key == key)?.value ?? $"[{key}]";
        }

        public IReadOnlyList<string> ValidationErrors()
        {
            var errors = new List<string>();
            foreach (UiTextKey key in Enum.GetValues(typeof(UiTextKey)))
                if (string.IsNullOrWhiteSpace(Get(key)) || Get(key).StartsWith("[", StringComparison.Ordinal))
                    errors.Add($"Missing UI copy: {key}.");
            return errors;
        }
    }
}
