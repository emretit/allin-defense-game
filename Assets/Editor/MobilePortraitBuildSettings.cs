#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace AllinDefense.Editor
{
    public static class MobileBrandingSettings
    {
        private const string AppIconPath =
            "Assets/Art/AppIcon/BrandV2/AllinDefense-AppIcon-v2-1024.png";

        public static void ApplyAndroidAppIcon()
        {
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(AppIconPath);
            if (icon == null)
                throw new FileNotFoundException($"Required Android application icon is missing: {AppIconPath}");

            int[] sizes = PlayerSettings.GetIconSizesForTargetGroup(BuildTargetGroup.Android);
            if (sizes == null || sizes.Length == 0)
                throw new System.InvalidOperationException("Unity did not provide any Android application icon slots.");

            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Android,
                Enumerable.Repeat(icon, sizes.Length).ToArray());
        }
    }

    public static class MobileOrientationBuildSettings
    {
        public static void ApplySupportedOrientations()
        {
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
        }
    }

    public sealed class MobileOrientationBuildPreprocessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => -1000;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.iOS && report.summary.platform != BuildTarget.Android)
                return;

            MobileOrientationBuildSettings.ApplySupportedOrientations();
            if (report.summary.platform == BuildTarget.Android)
                MobileBrandingSettings.ApplyAndroidAppIcon();
            Debug.Log($"ALLIN: Defense {report.summary.platform} build supports portrait frontend and landscape battle.");
        }
    }
}
#endif
