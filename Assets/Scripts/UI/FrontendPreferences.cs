using System;
using UnityEngine;

namespace AllinDefense.UI
{
    public static class FrontendPreferences
    {
        private const string Prefix = "AllinDefense.Frontend.";
        private const string MusicKey = Prefix + "Music";
        private const string SfxKey = Prefix + "Sfx";
        private const string HapticsKey = Prefix + "Haptics";
        private const string CameraShakeKey = Prefix + "CameraShake";
        private const string ReducedMotionKey = Prefix + "ReducedMotion";

        static FrontendPreferences() => UiSystemAccessibility.Changed += SystemMotionChanged;
        private static void SystemMotionChanged()
        {
            if (!PlayerPrefs.HasKey(ReducedMotionKey)) MotionChanged?.Invoke();
        }

        public static event Action AudioVolumesChanged;
        public static event Action MotionChanged;

        public static float MusicVolume
        {
            get => PlayerPrefs.GetFloat(MusicKey, 1f);
            set => SetFloat(MusicKey, value);
        }

        public static float SfxVolume
        {
            get => PlayerPrefs.GetFloat(SfxKey, 1f);
            set => SetFloat(SfxKey, value);
        }

        public static bool Haptics
        {
            get => GetBool(HapticsKey, true);
            set => SetBool(HapticsKey, value);
        }

        public static bool CameraShake
        {
            get => GetBool(CameraShakeKey, !UiSystemAccessibility.ReducedMotion);
            set => SetBool(CameraShakeKey, value);
        }

        public static bool ReducedMotion
        {
            get => GetBool(ReducedMotionKey, UiSystemAccessibility.ReducedMotion);
            set { SetBool(ReducedMotionKey, value); MotionChanged?.Invoke(); }
        }

        public static GameLanguage Language
        {
            get => GameLocalization.CurrentLanguage;
            set => GameLocalization.SetLanguage(value);
        }

        public static bool HasSeenEnemy(AllinDefense.Data.EnemyArchetype enemy) =>
            GetBool(Prefix + "EnemySeen." + enemy, false);

        public static void MarkEnemySeen(AllinDefense.Data.EnemyArchetype enemy) =>
            SetBool(Prefix + "EnemySeen." + enemy, true);

        public static bool ReplayFirstBattleGuide
        {
            get => GetBool(Prefix + "ReplayFirstBattleGuide", false);
            set => SetBool(Prefix + "ReplayFirstBattleGuide", value);
        }

        public static void ResetLearningGuide()
        {
            PlayerPrefs.SetInt(Prefix + "ReplayFirstBattleGuide", 1);
            foreach (AllinDefense.Data.EnemyArchetype enemy in Enum.GetValues(typeof(AllinDefense.Data.EnemyArchetype)))
                PlayerPrefs.DeleteKey(Prefix + "EnemySeen." + enemy);
            PlayerPrefs.Save();
        }

        private static bool GetBool(string key, bool fallback) => PlayerPrefs.GetInt(key, fallback ? 1 : 0) != 0;

        private static void SetBool(string key, bool value)
        {
            PlayerPrefs.SetInt(key, value ? 1 : 0);
            PlayerPrefs.Save();
        }

        private static void SetFloat(string key, float value)
        {
            float previous = PlayerPrefs.GetFloat(key, 1f);
            float volume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(key, volume);
            PlayerPrefs.Save();
            if (previous != volume) AudioVolumesChanged?.Invoke();
        }
    }
}
