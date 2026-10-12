using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AllinDefense.Core;
using UnityEngine;
using UnityEngine.Profiling;

namespace AllinDefense.UI
{
    public sealed partial class AppFlowController
    {
        [Serializable] private sealed class FrontendSample
        {
            public string route;
            public bool cold;
            public double feedbackMs;
            public double usableMs;
            public float longestFrameMs;
        }
        [Serializable] private sealed class FrontendAcceptanceReport
        {
            public string platform;
            public int width, height;
            public List<FrontendSample> samples = new();
            public List<long> memory = new();
            public List<int> uiObjects = new();
            public List<string> errors = new();
            public string status = "running";
        }
        private static FrontendAcceptanceReport acceptanceReport;
        private static int acceptanceStage;
        private static GameLanguage acceptanceOriginalLanguage;
        private static double reloadStarted;
        private static string AcceptanceRoot => Path.Combine(Application.persistentDataPath, "FrontendExperience");
        private static bool AcceptanceRequested => Debug.isDebugBuild &&
            Environment.GetEnvironmentVariable("DC_FRONTEND_ACCEPTANCE") == "1";

        private void StartFrontendAcceptance()
        {
            if (!AcceptanceRequested) return;
            if (acceptanceStage == 0) StartCoroutine(AcceptMenus());
            else StartCoroutine(AcceptReload());
        }

        private IEnumerator AcceptMenus()
        {
            while (CurrentScreen == AppScreen.Boot) yield return null;
            acceptanceOriginalLanguage = FrontendPreferences.Language;
            acceptanceReport = new FrontendAcceptanceReport { platform = Application.platform.ToString(), width = Screen.width, height = Screen.height };
            Directory.CreateDirectory(AcceptanceRoot);
            CompleteLocalGuestSignIn();
            yield return new WaitForSecondsRealtime(.3f);
            yield return CaptureAcceptance("main-menu");
            for (int i = 0; i < 31; i++)
            {
                yield return MeasureAcceptance("menu-map", i == 0, OpenCampaign);
                yield return MeasureAcceptance("map-menu", i == 0, () => ShowScreen(AppScreen.MainMenu));
                yield return MeasureAcceptance("menu-settings", i == 0, OpenSettings);
                CloseSettings();
                if (i % 5 == 0)
                {
                    GC.Collect();
                    acceptanceReport.memory.Add(Profiler.GetTotalAllocatedMemoryLong());
                    acceptanceReport.uiObjects.Add(GetComponentsInChildren<Transform>(true).Length);
                }
            }
            OpenCampaign();
            foreach (var language in new[] { GameLanguage.Turkish, GameLanguage.English })
            {
                FrontendPreferences.Language = language;
                yield return new WaitForSecondsRealtime(.3f);
                yield return CaptureAcceptance("map-" + language);
                OpenSettings(); yield return CaptureAcceptance("settings-" + language); CloseSettings();
                ShowScreen(AppScreen.Login); yield return new WaitForSecondsRealtime(.2f);
                yield return CaptureAcceptance("account-" + language); ReturnFromAccount();
            }
            yield return MeasureAcceptance("first-battle", true, () => LaunchCampaignLevel(1));
            yield return CaptureAcceptance("battle");
            ShowResult(false, 0, default);
            yield return CaptureAcceptance("defeat");
            acceptanceStage = 1;
            WriteAcceptance();
            reloadStarted = Time.realtimeSinceStartupAsDouble;
            ReloadCampaignLevel(1);
        }

        private IEnumerator AcceptReload()
        {
            double deadline = Time.realtimeSinceStartupAsDouble + 30;
            while ((CurrentScreen != AppScreen.Battle || launchingBattle) && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
            if (CurrentScreen != AppScreen.Battle || launchingBattle)
            {
                acceptanceReport.errors.Add("Battle reload did not complete");
                acceptanceReport.status = "failed"; WriteAcceptance();
                FrontendPreferences.Language = acceptanceOriginalLanguage; yield break;
            }
            acceptanceReport.samples.Add(new FrontendSample { route = acceptanceStage == 1 ? "retry" : "next-level",
                usableMs = (Time.realtimeSinceStartupAsDouble - reloadStarted) * 1000,
                feedbackMs = FrontendLoadingOverlay.FirstFeedbackMs, longestFrameMs = FrontendLoadingOverlay.LongestFrameMs });
            if (acceptanceStage == 1)
            {
                HandleSessionPhaseChanged(GamePhase.Victory);
                yield return CaptureAcceptance("victory");
                acceptanceStage = 2; WriteAcceptance();
                reloadStarted = Time.realtimeSinceStartupAsDouble;
                ReloadCampaignLevel(2);
                yield break;
            }
            yield return MeasureAcceptance("battle-map", false, OpenCampaign);
            yield return CaptureAcceptance("returned-map");
            acceptanceReport.status = acceptanceReport.errors.Count == 0 ? "passed" : "failed";
            WriteAcceptance();
            Debug.Log("[DC_FRONTEND_ACCEPTANCE] " + acceptanceReport.status + " " + AcceptanceRoot);
            FrontendPreferences.Language = acceptanceOriginalLanguage;
            acceptanceStage = 0;
        }

        private IEnumerator MeasureAcceptance(string route, bool cold, Action action)
        {
            double start = Time.realtimeSinceStartupAsDouble;
            action();
            yield return null;
            double feedback = (Time.realtimeSinceStartupAsDouble - start) * 1000;
            float maxFrame = Time.unscaledDeltaTime * 1000;
            while ((launchingBattle || orientationRoutine != null || navigation.IsTransitioning) && Time.realtimeSinceStartupAsDouble - start < 30)
            {
                yield return null;
                maxFrame = Mathf.Max(maxFrame, Time.unscaledDeltaTime * 1000);
            }
            if (launchingBattle || orientationRoutine != null || navigation.IsTransitioning) acceptanceReport.errors.Add(route + " timed out");
            acceptanceReport.samples.Add(new FrontendSample { route = route, cold = cold, feedbackMs = feedback,
                usableMs = (Time.realtimeSinceStartupAsDouble - start) * 1000, longestFrameMs = maxFrame });
        }

        private IEnumerator CaptureAcceptance(string name)
        {
            yield return new WaitForSecondsRealtime(.25f);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Application.isMobilePlatform
                ? Path.Combine("FrontendExperience", name + ".png") : Path.Combine(AcceptanceRoot, name + ".png"));
            foreach (var text in GetComponentsInChildren<TMPro.TMP_Text>())
            {
                text.ForceMeshUpdate();
                if (text.isTextOverflowing) acceptanceReport.errors.Add(name + " overflow: " + text.name + ": " + text.text);
            }
        }
        private static void WriteAcceptance() => File.WriteAllText(Path.Combine(AcceptanceRoot, "report.json"), JsonUtility.ToJson(acceptanceReport, true));
    }
}
