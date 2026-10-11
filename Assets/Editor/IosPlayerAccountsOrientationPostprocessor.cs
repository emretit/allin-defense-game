#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.iOS.Xcode;
using UnityEngine;

namespace DefenderChronicles.Editor
{
    public sealed class IosPlayerAccountsOrientationPostprocessor : IPostprocessBuildWithReport
    {
        private const string Portrait = "UIInterfaceOrientationPortrait";
        private const string LandscapeLeft = "UIInterfaceOrientationLandscapeLeft";
        private const string LandscapeRight = "UIInterfaceOrientationLandscapeRight";

        public int callbackOrder => 1000;

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.iOS) return;

            SetSupportedOrientationsInPlist(report.summary.outputPath);
            ReplacePlayerAccountsSafariView(report.summary.outputPath);
            Debug.Log("iOS supports portrait frontend and landscape battle; Player Accounts remains portrait.");
        }

        private static void SetSupportedOrientationsInPlist(string outputPath)
        {
            string plistPath = Path.Combine(outputPath, "Info.plist");
            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);
            SetSupportedOrientations(plist.root, "UISupportedInterfaceOrientations");
            SetSupportedOrientations(plist.root, "UISupportedInterfaceOrientations~ipad");
            plist.WriteToFile(plistPath);
        }

        private static void SetSupportedOrientations(PlistElementDict root, string key)
        {
            root.values.Remove(key);
            PlistElementArray orientations = root.CreateArray(key);
            orientations.AddString(Portrait);
            orientations.AddString(LandscapeLeft);
            orientations.AddString(LandscapeRight);
        }

        private static void ReplacePlayerAccountsSafariView(string outputPath)
        {
            const string relativeRoot = "Libraries/com.unity.services.authentication/Player Accounts/Runtime/Plugins/IOS/SafariView";
            // Current SDKs use ASWebAuthenticationSession with a native OAuth callback.
            // Keep that source intact; replacing it with the legacy browser drops its callback API.
            if (File.Exists(Path.Combine(outputPath, relativeRoot + ".m"))) return;
            string safariViewPath = Path.Combine(outputPath, relativeRoot + ".mm");
            if (!File.Exists(safariViewPath))
                throw new BuildFailedException($"Player Accounts SafariView source was not generated: {safariViewPath}");

            File.WriteAllText(safariViewPath, SafariViewSource);
            string projectPath = PBXProject.GetPBXProjectPath(outputPath);
            var project = new PBXProject();
            project.ReadFromFile(projectPath);
            project.AddFrameworkToProject(project.GetUnityFrameworkTargetGuid(), "SafariServices.framework", false);
            project.WriteToFile(projectPath);
        }

        private const string SafariViewSource = @"#import <SafariServices/SafariServices.h>

#ifdef __cplusplus
extern ""C"" UIViewController* UnityGetGLViewController();
#else
extern UIViewController* UnityGetGLViewController();
#endif
extern NSInteger _forceInterfaceOrientationMask;

static void DCRestoreUnityOrientation(UIViewController *unityViewController)
{
    _forceInterfaceOrientationMask = 0;
    if (@available(iOS 16.0, *))
        [unityViewController setNeedsUpdateOfSupportedInterfaceOrientations];
    [UIViewController attemptRotationToDeviceOrientation];
}

@interface DCPlayerAccountsSafariViewController : SFSafariViewController <SFSafariViewControllerDelegate>
@end

@implementation DCPlayerAccountsSafariViewController
- (instancetype)initWithURL:(NSURL *)URL
{
    self = [super initWithURL:URL];
    if (self)
        self.delegate = self;
    return self;
}

- (BOOL)shouldAutorotate
{
    return YES;
}

- (UIInterfaceOrientationMask)supportedInterfaceOrientations
{
    return UIInterfaceOrientationMaskPortrait;
}

- (UIInterfaceOrientation)preferredInterfaceOrientationForPresentation
{
    return UIInterfaceOrientationPortrait;
}

- (void)safariViewControllerDidFinish:(SFSafariViewController *)controller
{
    DCRestoreUnityOrientation(UnityGetGLViewController());
}
@end

#ifdef __cplusplus
extern ""C"" {
#endif
    void launchUnityPlayerAccountUrl(const char *url)
    {
        UIViewController *uvc = UnityGetGLViewController();
        NSURL *URL = [NSURL URLWithString:[[NSString alloc] initWithUTF8String:url]];
        DCPlayerAccountsSafariViewController *sfvc = [[DCPlayerAccountsSafariViewController alloc] initWithURL:URL];
        sfvc.modalPresentationStyle = UIModalPresentationFullScreen;
        _forceInterfaceOrientationMask |= UIInterfaceOrientationMaskPortrait;
        [uvc presentViewController:sfvc animated:YES completion:nil];
    }

    void dismissUnityPlayerAccount()
    {
        UIViewController *uvc = UnityGetGLViewController();
        [uvc dismissViewControllerAnimated:YES completion:^{
            DCRestoreUnityOrientation(uvc);
        }];
    }
#ifdef __cplusplus
}
#endif
";
    }
}
#endif
