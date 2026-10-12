using System;
using System.Collections.Generic;
using System.Linq;
using AllinDefense.UI;
using UnityEngine;

namespace AllinDefense.Presentation
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

    [CreateAssetMenu(menuName = "ALLIN: Defense/UI Copy", fileName = "UiCopyTr")]
    public sealed class UiCopyCatalog : ScriptableObject
    {
        public string locale = "tr-TR";
        public UiCopyEntry[] entries = Array.Empty<UiCopyEntry>();

        public string Get(UiTextKey key) => key switch
        {
            UiTextKey.Gold => FrontendCopy.HudGold.Value,
            UiTextKey.Keep => FrontendCopy.HudKeep.Value,
            UiTextKey.Wave => FrontendCopy.HudWave.Value,
            UiTextKey.Preparing => FrontendCopy.HudPreparing.Value,
            UiTextKey.Combat => FrontendCopy.HudCombat.Value,
            UiTextKey.Intermission => FrontendCopy.HudIntermission.Value,
            UiTextKey.Paused => FrontendCopy.HudPaused.Value,
            UiTextKey.Loading => FrontendCopy.HudLoading.Value,
            UiTextKey.Pause => FrontendCopy.HudPause.Value,
            UiTextKey.Continue => FrontendCopy.HudContinue.Value,
            UiTextKey.Archer => FrontendCopy.HudArcher.Value,
            UiTextKey.Barracks => FrontendCopy.HudBarracks.Value,
            UiTextKey.Mage => FrontendCopy.HudMage.Value,
            UiTextKey.ShieldBash => FrontendCopy.HudShieldBash.Value,
            UiTextKey.Cancel => FrontendCopy.HudCancel.Value,
            UiTextKey.FirstWave => FrontendCopy.HudFirstWave.Value,
            UiTextKey.EarlyWave => FrontendCopy.HudEarlyWave.Value,
            UiTextKey.NextWave => FrontendCopy.HudNextWave.Value,
            UiTextKey.Upgrade => FrontendCopy.HudUpgrade.Value,
            UiTextKey.Sell => FrontendCopy.HudSell.Value,
            UiTextKey.Knight => FrontendCopy.HudKnight.Value,
            UiTextKey.AbilityReady => FrontendCopy.HudAbilityReady.Value,
            UiTextKey.KnightReturning => FrontendCopy.HudKnightReturning.Value,
            UiTextKey.Victory => FrontendCopy.HudVictory.Value,
            UiTextKey.Defeat => FrontendCopy.HudDefeat.Value,
            UiTextKey.VictoryPhase => FrontendCopy.HudVictoryPhase.Value,
            UiTextKey.DefeatPhase => FrontendCopy.HudDefeatPhase.Value,
            UiTextKey.Restart => FrontendCopy.HudRestart.Value,
            UiTextKey.SlotInstruction => FrontendCopy.HudSlotInstruction.Value,
            UiTextKey.BuildInstruction => FrontendCopy.HudBuildInstruction.Value,
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
