#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace AllinDefense.Editor
{
    public static class AndroidBuildSettings
    {
        private const string BundleIdentifier = "com.emreaydin.allindefense";

        [MenuItem("ALLIN: Defense/Apply Android Settings")]
        public static void Apply()
        {
            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
                throw new InvalidOperationException("Android build target could not be activated.");

            PlayerSettings.companyName = "Emre Aydin";
            PlayerSettings.productName = "ALLIN: Defense";
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, BundleIdentifier);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.bundleVersionCode = Mathf.Max(1, PlayerSettings.Android.bundleVersionCode);
            MobileOrientationBuildSettings.ApplySupportedOrientations();
            MobileBrandingSettings.ApplyAndroidAppIcon();
            Debug.Log("ALLIN: Defense Android ARM64/IL2CPP settings applied.");
        }

        [MenuItem("ALLIN: Defense/Build Android Development APK")]
        public static void BuildDevelopmentApk()
        {
            Apply();
            EditorUserBuildSettings.buildAppBundle = false;
            Build("Builds/Android/AllinDefense-Development.apk",
                BuildOptions.Development | BuildOptions.AllowDebugging, "Android Development APK");
        }

        [MenuItem("ALLIN: Defense/Build Android Release AAB")]
        public static void BuildReleaseAab()
        {
            Apply();
            EditorUserBuildSettings.buildAppBundle = true;
            Build("Builds/Android/AllinDefense-Release.aab", BuildOptions.None, "Android Release AAB");
        }

        private static void Build(string relativePath, BuildOptions buildOptions, string label)
        {
            string outputPath = Path.GetFullPath(relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? "Builds/Android");
            string[] scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
            if (scenes.Length == 0) scenes = new[] { "Assets/Scenes/Game.unity" };

            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                locationPathName = outputPath,
                options = buildOptions
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException($"{label} failed: {report.summary.result}");
            Debug.Log($"{label} built at: {outputPath}");
        }
    }
}
#endif
