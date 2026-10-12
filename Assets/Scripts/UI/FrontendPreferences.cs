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

        public static event Action AudioVolumesChanged;

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
            get => GetBool(CameraShakeKey, true);
            set => SetBool(CameraShakeKey, value);
        }

        public static bool ReducedMotion
        {
            get => GetBool(ReducedMotionKey, false);
            set => SetBool(ReducedMotionKey, value);
        }

        public static GameLanguage Language
        {
            get => GameLocalization.CurrentLanguage;
            set => GameLocalization.SetLanguage(value);
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
