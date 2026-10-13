#if UNITY_IOS
using System;
using System.IO;
using System.Linq;
using AllinDefense.Presentation;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.iOS.Xcode;
using UnityEngine;

namespace AllinDefense.Editor
{
    public sealed class IosExternalSignInPostprocessor : IPostprocessBuildWithReport
    {
        public int callbackOrder => 1001;

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.iOS) return;
            string output = report.summary.outputPath;
            string projectPath = PBXProject.GetPBXProjectPath(output);
            var project = new PBXProject();
            project.ReadFromFile(projectPath);
            string main = project.GetUnityMainTargetGuid();
            string entitlements = project.GetBuildPropertyForAnyConfig(main, "CODE_SIGN_ENTITLEMENTS");
            if (string.IsNullOrWhiteSpace(entitlements)) entitlements = "AllinDefense.entitlements";
            var capabilities = new ProjectCapabilityManager(projectPath, entitlements, null, main);
            capabilities.AddSignInWithApple();
            capabilities.WriteToFile();
            project.ReadFromFile(projectPath);
            project.AddFrameworkToProject(project.GetUnityFrameworkTargetGuid(), "AuthenticationServices.framework", false);
            project.AddFrameworkToProject(project.GetUnityFrameworkTargetGuid(), "Security.framework", false);
            project.WriteToFile(projectPath);

            var theme = Resources.Load<FrontendPresentationTheme>("Presentation/FrontendPresentationTheme");
            string clientId = theme != null ? theme.googleIosClientId?.Trim() : null;
            if (string.IsNullOrEmpty(clientId)) return;
            const string suffix = ".apps.googleusercontent.com";
            if (!clientId.EndsWith(suffix, StringComparison.Ordinal) || clientId.Length == suffix.Length)
                throw new BuildFailedException("Google iOS client ID must end with .apps.googleusercontent.com.");
            string scheme = "com.googleusercontent.apps." + clientId.Substring(0, clientId.Length - suffix.Length);
            string plistPath = Path.Combine(output, "Info.plist");
            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);
            var types = plist.root.values.TryGetValue("CFBundleURLTypes", out var existing)
                ? existing.AsArray() : plist.root.CreateArray("CFBundleURLTypes");
            bool present = types.values.Any(type => type.AsDict().values.TryGetValue("CFBundleURLSchemes", out var schemes) &&
                schemes.AsArray().values.Any(value => value.AsString() == scheme));
            if (!present)
            {
                var entry = types.AddDict();
                entry.SetString("CFBundleURLName", "Google Sign In");
                entry.CreateArray("CFBundleURLSchemes").AddString(scheme);
            }
            plist.WriteToFile(plistPath);
        }
    }
}
#endif
