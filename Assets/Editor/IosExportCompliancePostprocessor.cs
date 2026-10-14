#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.iOS.Xcode;

namespace AllinDefense.Editor
{
    // The game only uses OS-provided encryption (HTTPS to Unity Gaming Services, Sign in with Apple),
    // which is exempt; declaring it skips App Store Connect's export compliance prompt on every upload.
    public sealed class IosExportCompliancePostprocessor : IPostprocessBuildWithReport
    {
        public int callbackOrder => 1002;

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.iOS) return;

            string plistPath = Path.Combine(report.summary.outputPath, "Info.plist");
            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);
            plist.root.SetBoolean("ITSAppUsesNonExemptEncryption", false);
            plist.WriteToFile(plistPath);
        }
    }
}
#endif
