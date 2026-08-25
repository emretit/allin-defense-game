using System;
using UnityEngine;

namespace DefenderChronicles.Core
{
    public static class MobileRuntimeSettings
    {
        public const int TargetFrameRate = 60;
        public const string DefaultQualityName = "Medium";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ApplyBeforeSceneLoad()
        {
            Apply();
        }

        public static bool Apply()
        {
            if (!Application.isMobilePlatform) return false;

            int qualityIndex = ResolveQualityIndex(QualitySettings.names, DefaultQualityName);
            if (qualityIndex >= 0 && QualitySettings.GetQualityLevel() != qualityIndex)
                QualitySettings.SetQualityLevel(qualityIndex, applyExpensiveChanges: true);

            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = TargetFrameRate;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            Debug.Log($"[DC_MOBILE_RUNTIME] quality={CurrentQualityName()} targetFps={TargetFrameRate} safeArea={Screen.safeArea}");
            return true;
        }

        public static int ResolveQualityIndex(string[] qualityNames, string preferredName)
        {
            if (qualityNames == null || qualityNames.Length == 0) return -1;

            for (int index = 0; index < qualityNames.Length; index++)
            {
                if (string.Equals(qualityNames[index], preferredName, StringComparison.OrdinalIgnoreCase))
                    return index;
            }

            return Mathf.Clamp(qualityNames.Length / 2, 0, qualityNames.Length - 1);
        }

        private static string CurrentQualityName()
        {
            string[] names = QualitySettings.names;
            if (names == null || names.Length == 0) return "Unavailable";
            return names[Mathf.Clamp(QualitySettings.GetQualityLevel(), 0, names.Length - 1)];
        }
    }
}
