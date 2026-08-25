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
        private const string AppIconPath =
            "Assets/Art/AppIcon/BrandV2/DefenderChronicles-AppIcon-v2-1024.png";
        private const string LaunchArtworkPath =
            "Assets/Art/Brand/Launch/GreenwatchLaunch-v2.png";

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
            BuildSimulator("Builds/iOS-Simulator", true);
        }

        [MenuItem("Defender Chronicles/Build iOS Simulator Release Xcode Project")]
        public static void BuildSimulatorReleaseProject()
        {
            BuildSimulator("Builds/iOS-Simulator-Release", false);
        }

        [MenuItem("Defender Chronicles/Build iOS Device Development Xcode Project")]
        public static void BuildDeviceDevelopmentProject()
        {
            Apply();
            BuildXcodeProject("Builds/iOS-Device",
                BuildOptions.Development | BuildOptions.AllowDebugging | BuildOptions.ConnectWithProfiler,
                "iOS Device Development");
        }

        private static void BuildSimulator(string relativeOutputPath, bool development)
        {
            ApplySimulator();

            BuildXcodeProject(relativeOutputPath, development ? BuildOptions.Development : BuildOptions.None,
                $"iOS Simulator {(development ? "Development" : "Release")}");
        }

        private static void BuildXcodeProject(string relativeOutputPath, BuildOptions buildOptions, string label)
        {

            string outputPath = Path.GetFullPath(relativeOutputPath);
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
                options = buildOptions
            };

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new System.Exception($"iOS Simulator build failed: {report.summary.result}");

            Debug.Log($"{label} Xcode project built at: {outputPath}");
        }

        private static void ApplySharedIosSettings()
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS);
            PlayerSettings.companyName = "Emre Aydin";
            PlayerSettings.productName = "Defender Chronicles";
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, BundleIdentifier);
            PlayerSettings.iOS.targetOSVersionString = "15.0";
            PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneAndiPad;
            MobileOrientationBuildSettings.ApplySupportedOrientations();
            PlayerSettings.iOS.requiresFullScreen = true;
            ApplyAppIcon();
            ApplyLaunchScreen();
        }

        private static void ApplyAppIcon()
        {
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(AppIconPath);
            if (icon == null)
                throw new FileNotFoundException($"Required 1024x1024 app icon is missing: {AppIconPath}");

            int[] sizes = PlayerSettings.GetIconSizesForTargetGroup(BuildTargetGroup.iOS);
            if (sizes == null || sizes.Length == 0)
                throw new System.InvalidOperationException("Unity did not provide any iOS application icon slots.");

            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.iOS,
                Enumerable.Repeat(icon, sizes.Length).ToArray());
        }

        private static void ApplyLaunchScreen()
        {
            var artwork = AssetDatabase.LoadAssetAtPath<Texture2D>(LaunchArtworkPath);
            if (artwork == null)
                throw new FileNotFoundException($"Required iOS launch artwork is missing: {LaunchArtworkPath}");

            PlayerSettings.iOS.SetiPhoneLaunchScreenType(iOSLaunchScreenType.ImageAndBackgroundRelative);
            PlayerSettings.iOS.SetiPadLaunchScreenType(iOSLaunchScreenType.ImageAndBackgroundRelative);
            PlayerSettings.iOS.SetLaunchScreenImage(artwork, iOSLaunchScreenImageType.iPhoneLandscapeImage);
            PlayerSettings.iOS.SetLaunchScreenImage(artwork, iOSLaunchScreenImageType.iPhonePortraitImage);
            PlayerSettings.iOS.SetLaunchScreenImage(artwork, iOSLaunchScreenImageType.iPadImage);
        }
    }
}
#endif
