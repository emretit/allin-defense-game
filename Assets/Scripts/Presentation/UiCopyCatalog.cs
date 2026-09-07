using System;
using System.Collections.Generic;
using System.Linq;
using DefenderChronicles.UI;
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
            if (GameLocalization.IsEnglish) return English(key);
            return entries.FirstOrDefault(entry => entry != null && entry.key == key)?.value ?? $"[{key}]";
        }

        private static string English(UiTextKey key) => key switch
        {
            UiTextKey.Gold => "GOLD",
            UiTextKey.Keep => "KEEP",
            UiTextKey.Wave => "WAVE",
            UiTextKey.Preparing => "PREPARING",
            UiTextKey.Combat => "COMBAT",
            UiTextKey.Intermission => "INTERMISSION",
            UiTextKey.Paused => "PAUSED",
            UiTextKey.Loading => "LOADING",
            UiTextKey.Pause => "PAUSE",
            UiTextKey.Continue => "RESUME",
            UiTextKey.Archer => "ARCHER",
            UiTextKey.Barracks => "BARRACKS",
            UiTextKey.Mage => "MAGE",
            UiTextKey.ShieldBash => "STUNNING STRIKE",
            UiTextKey.Cancel => "CANCEL",
            UiTextKey.FirstWave => "START FIRST WAVE",
            UiTextKey.EarlyWave => "CALL WAVE EARLY",
            UiTextKey.NextWave => "NEXT WAVE",
            UiTextKey.Upgrade => "UPGRADE",
            UiTextKey.Sell => "SELL",
            UiTextKey.Knight => "Warrior",
            UiTextKey.AbilityReady => "READY",
            UiTextKey.KnightReturning => "Warrior returns",
            UiTextKey.Victory => "VICTORY\nGREENWATCH DEFENDED",
            UiTextKey.Defeat => "DEFEAT\nGREENWATCH HAS FALLEN",
            UiTextKey.VictoryPhase => "VICTORY",
            UiTextKey.DefeatPhase => "DEFEAT",
            UiTextKey.Restart => "TRY AGAIN",
            UiTextKey.SlotInstruction => "Choose an outpost below.",
            UiTextKey.BuildInstruction => "Tap an outpost platform and build your defence.",
            _ => $"[{key}]"
        };

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
