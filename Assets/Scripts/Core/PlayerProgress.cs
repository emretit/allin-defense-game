using UnityEngine;

namespace AllinDefense.Core
{
    /// <summary>Yerel ilerleme (PlayerPrefs). İnternet / bulut gerekmez.</summary>
    public static class PlayerProgress
    {
        private const string HighWaveKey = "AllinDefense_HighWave";
        private const string TotalGoldKey = "AllinDefense_TotalGold";
        private const string MapIndexKey = "AllinDefense_MapIndex";
        private const string UnlockedTowersKey = "AllinDefense_UnlockedTowers";

        public static int HighWaveReached => PlayerPrefs.GetInt(HighWaveKey, 0);
        public static int TotalGoldEarned => PlayerPrefs.GetInt(TotalGoldKey, 0);
        public static int SelectedMapIndex => Mathf.Clamp(PlayerPrefs.GetInt(MapIndexKey, 0), 0, MapPathPresets.Count - 1);

        /// <summary>Bitmask: bit 0 = Archer, 1 = Mage, 2 = Warrior, 3 = Catapult. Default all unlocked for TD MVP.</summary>
        public static int UnlockedTowersMask
        {
            get => PlayerPrefs.GetInt(UnlockedTowersKey, 0xF);
            set
            {
                PlayerPrefs.SetInt(UnlockedTowersKey, value);
                PlayerPrefs.Save();
            }
        }

        public static void Reset()
        {
            foreach (string key in new[] { HighWaveKey, TotalGoldKey, MapIndexKey, UnlockedTowersKey })
                PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }

        public static void RecordWaveCompleted(int waveNumberCompleted)
        {
            int prev = HighWaveReached;
            if (waveNumberCompleted > prev)
            {
                PlayerPrefs.SetInt(HighWaveKey, waveNumberCompleted);
                PlayerPrefs.Save();
            }
        }

        public static void RecordGoldEarned(int amount)
        {
            if (amount <= 0) return;
            int t = PlayerPrefs.GetInt(TotalGoldKey, 0);
            PlayerPrefs.SetInt(TotalGoldKey, t + amount);
            PlayerPrefs.Save();
        }

        public static void SetSelectedMapIndex(int index)
        {
            PlayerPrefs.SetInt(MapIndexKey, Mathf.Clamp(index, 0, MapPathPresets.Count - 1));
            PlayerPrefs.Save();
        }

        public static bool IsTowerUnlocked(int towerBitIndex)
        {
            if (towerBitIndex < 0 || towerBitIndex > 3) return true;
            return (UnlockedTowersMask & (1 << towerBitIndex)) != 0;
        }

        public static void UnlockTower(int towerBitIndex)
        {
            if (towerBitIndex < 0 || towerBitIndex > 3) return;
            UnlockedTowersMask |= 1 << towerBitIndex;
        }
    }
}
