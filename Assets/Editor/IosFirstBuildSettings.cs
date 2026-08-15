#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;

namespace DefenderChronicles.Editor
{
    public static class IosFirstBuildSettings
    {
        private const string BundleIdentifier = "com.emreaydin.defenderchronicles";

        [MenuItem("Defender Chronicles/Apply iOS First Settings")]
        public static void Apply()
        {
            ApplySharedIosSettings();
            PlayerSettings.iOS.sdkVersion = iOSSdkVersion.DeviceSDK;

            Debug.Log("Defender Chronicles iOS device settings applied.");
        }

        [MenuItem("Defender Chronicles/Apply iOS Simulator Settings")]
        public static void ApplySimulator()
        {
            ApplySharedIosSettings();
            PlayerSettings.iOS.sdkVersion = iOSSdkVersion.SimulatorSDK;
            PlayerSettings.iOS.simulatorSdkArchitecture = AppleMobileArchitectureSimulator.Universal;

            Debug.Log("Defender Chronicles iOS Simulator settings applied.");
        }

        [MenuItem("Defender Chronicles/Build iOS Simulator Xcode Project")]
        public static void BuildSimulatorProject()
        {
            ApplySimulator();

            string outputPath = Path.GetFullPath("Builds/iOS-Simulator");
            Directory.CreateDirectory(outputPath);

            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            if (scenes.Length == 0)
                scenes = new[] { "Assets/Scenes/Game.unity" };

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                target = BuildTarget.iOS,
                targetGroup = BuildTargetGroup.iOS,
                locationPathName = outputPath,
                options = BuildOptions.Development
            };

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new System.Exception($"iOS Simulator build failed: {report.summary.result}");

            Debug.Log($"iOS Simulator Xcode project built at: {outputPath}");
        }

        private static void ApplySharedIosSettings()
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS);
            PlayerSettings.companyName = "Emre Aydin";
            PlayerSettings.productName = "Defender Chronicles";
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, BundleIdentifier);
            PlayerSettings.iOS.targetOSVersionString = "15.0";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeRight;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.iOS.requiresFullScreen = true;
        }
    }
}
#endif
