#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;

namespace AllinDefense.Editor
{
    public static class IosFirstBuildSettings
    {
        private const string BundleIdentifier = "com.emreaydin.allindefense";
        private const string AppIconPath =
            "Assets/Art/AppIcon/BrandV2/AllinDefense-AppIcon-v2-1024.png";
        private const string LaunchArtworkPath =
            "Assets/Art/Brand/Launch/GreenwatchLaunch-v2.png";

        [MenuItem("ALLIN: Defense/Apply iOS First Settings")]
        public static void Apply()
        {
            ApplySharedIosSettings();
            PlayerSettings.iOS.sdkVersion = iOSSdkVersion.DeviceSDK;

            Debug.Log("ALLIN: Defense iOS device settings applied.");
        }

        [MenuItem("ALLIN: Defense/Apply iOS Simulator Settings")]
        public static void ApplySimulator()
        {
            ApplySharedIosSettings();
            PlayerSettings.iOS.sdkVersion = iOSSdkVersion.SimulatorSDK;
            PlayerSettings.iOS.simulatorSdkArchitecture = AppleMobileArchitectureSimulator.Universal;

            Debug.Log("ALLIN: Defense iOS Simulator settings applied.");
        }

        [MenuItem("ALLIN: Defense/Build iOS Simulator Xcode Project")]
        public static void BuildSimulatorProject()
        {
            BuildSimulator("Builds/iOS-Simulator", true);
        }

        [MenuItem("ALLIN: Defense/Build iOS Simulator Release Xcode Project")]
        public static void BuildSimulatorReleaseProject()
        {
            BuildSimulator("Builds/iOS-Simulator-Release", false);
        }

        [MenuItem("ALLIN: Defense/Build iOS Device Development Xcode Project")]
        public static void BuildDeviceDevelopmentProject()
        {
            Apply();
            BuildOptions options = BuildOptions.Development | BuildOptions.AllowDebugging;
            if (System.Environment.GetEnvironmentVariable("DC_IOS_CONNECT_PROFILER") == "1")
                options |= BuildOptions.ConnectWithProfiler;

            BuildXcodeProject("Builds/iOS-Device", options, "iOS Device Development");
        }

        // tools/build_ios_appstore_ipa.sh sets ALLIN_IOS_VERSION / ALLIN_IOS_BUILD; the menu keeps current values.
        [MenuItem("ALLIN: Defense/Build iOS App Store Xcode Project")]
        public static void BuildDeviceAppStoreProject()
        {
            Apply();
            string version = System.Environment.GetEnvironmentVariable("ALLIN_IOS_VERSION");
            string build = System.Environment.GetEnvironmentVariable("ALLIN_IOS_BUILD");
            if (!string.IsNullOrWhiteSpace(version))
                PlayerSettings.bundleVersion = version.Trim();
            if (!string.IsNullOrWhiteSpace(build))
                PlayerSettings.iOS.buildNumber = build.Trim();

            BuildXcodeProject("Builds/iOS-AppStore", BuildOptions.None,
                $"iOS App Store {PlayerSettings.bundleVersion} ({PlayerSettings.iOS.buildNumber})");
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
                throw new System.Exception($"{label} build failed: {report.summary.result}");

            Debug.Log($"{label} Xcode project built at: {outputPath}");
        }

        private static void ApplySharedIosSettings()
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS);
            PlayerSettings.companyName = "Emre Aydin";
            PlayerSettings.productName = "ALLIN: Defense";
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
