#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using DefenderChronicles.Core;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
namespace DefenderChronicles.Editor
{
    public static class ClosedTestBuilds
    {
        public static void Ios()
        {
            IosFirstBuildSettings.Apply();
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS,"com.emreaydin.defenderchronicles.closedtest");
            PlayerSettings.iOS.buildNumber="2026090601";
            Build(BuildTarget.iOS,"Builds/ClosedTest-iOS");
        }
        public static void Android()
        {
            AndroidBuildSettings.Apply();
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android,"com.emreaydin.defenderchronicles.closedtest");
            EditorUserBuildSettings.buildAppBundle=false;
            Build(BuildTarget.Android,"Builds/ClosedTest-Android/DefenderChronicles-ct-20260906-01.apk");
        }
        public static void IosSimulator()
        {
            IosFirstBuildSettings.ApplySimulator();
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS,"com.emreaydin.defenderchronicles.closedtest");
            PlayerSettings.iOS.buildNumber="2026090601";
            Build(BuildTarget.iOS,"Builds/ClosedTest-iOS-Simulator","iOS-Simulator");
        }
        private static void Build(BuildTarget target,string destination,string evidenceName=null)
        {
            PlayerSettings.productName="Defender Chronicles Test";
            PlayerSettings.bundleVersion="0.4.0";
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).DefaultIfEmpty("Assets/Scenes/Game.unity").ToArray(),
                target=target, locationPathName=destination, options=BuildOptions.Development|BuildOptions.AllowDebugging
            });
            File.WriteAllText($"docs/closed-test/evidence/build-{evidenceName ?? target.ToString()}.txt",$"build={ClosedTestBuild.Id}\nresult={report.summary.result}\nbytes={report.summary.totalSize}\nseconds={report.summary.totalTime.TotalSeconds}\nerrors={report.summary.totalErrors}\noutput={destination}\n");
            if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Closed-test build failed: "+report.summary.result);
        }
    }
}
#endif
