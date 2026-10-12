using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using AllinDefense.Data;
using AllinDefense.Presentation;
using UnityEngine.UI;
using AllinDefense.Core;
using AllinDefense.Combat;
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
            public List<string> screenshots = new();
            public List<string> errors = new();
            public List<string> knownIssues = new();
            public float minimumMenuTouchPt = 999f;
            public int menuTouchTargetsChecked;
            public string status = "running";
        }
        private static readonly HashSet<string> acceptanceErrors = new();
        private static readonly HashSet<string> knownLegacyHudOverflowKeys = new(StringComparer.Ordinal);
        private static FrontendAcceptanceReport acceptanceReport;
        private static int acceptanceStage;
        private static string acceptanceSavePath;
        private static GameLanguage acceptanceOriginalLanguage;
        private static double reloadStarted;
        private static string AcceptanceRoot => Path.Combine(Application.persistentDataPath, "FrontendExperience");
        internal static bool AcceptanceRequested => Debug.isDebugBuild &&
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
            acceptanceErrors.Clear();
            if (progress.Data.TotalStars != 0 || progress.Data.unlockedLevel != 1)
                acceptanceReport.errors.Add("welcome: acceptance guest is not new");
            ShowScreen(AppScreen.Boot);
            yield return CaptureAcceptance("boot");
            ShowScreen(AppScreen.Welcome);
            yield return CaptureAcceptance("welcome");
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
                ShowScreen(AppScreen.MainMenu);
                yield return CaptureAcceptance("main-menu-" + language);
                OpenCampaign();
                yield return CaptureAcceptance("map-" + language);
                OpenBriefing();
                yield return CaptureAcceptance("briefing-" + language);
                regionScrolls[0].verticalNormalizedPosition = 0;
                yield return CaptureAcceptance("map-last-node-" + language);
                regionScrolls[0].verticalNormalizedPosition = 1;
                OpenSettings(); yield return CaptureAcceptance("settings-" + language); CloseSettings();
                ShowScreen(AppScreen.Login); yield return new WaitForSecondsRealtime(.2f);
                yield return CaptureAcceptance("account-" + language); ReturnFromAccount();
            }
            yield return MeasureAcceptance("first-battle", true, () => LaunchCampaignLevel(1));
            yield return CaptureAcceptance("battle");
            FrontendPreferences.Language = GameLanguage.Turkish;
            AcceptanceSet("tutorialStep", -1);
            AcceptanceInvoke("RefreshFirstBattleGuide");
            yield return CaptureAcceptance("battle-Turkish");
            FrontendPreferences.Language = GameLanguage.English;
            AcceptanceSet("tutorialStep", -1);
            AcceptanceInvoke("RefreshFirstBattleGuide");
            var hud = session.GetComponentInChildren<DefenderHudController>();
            AcceptanceInvoke("SelectSlot", session.GetComponentsInChildren<GreyboxOutpostSlot>().First(slot => slot.Index == 0));
            yield return CaptureAcceptance("battle-build-menu");
            hud.BattleRadial.Choose(0);
            yield return CaptureAcceptance("battle-build-preview");
            AcceptanceInvoke("EarnGold", 10000);
            AcceptanceSet("unlockedCampaignLevel", 10);
            session.TryBuildOutpost(0, OutpostKind.Archer);
            AcceptanceInvoke("SelectOutpost", session.Outposts[0]);
            yield return CaptureAcceptance("battle-selected-tower");
            hud.BattleRadial.Choose(0);
            yield return CaptureAcceptance("battle-upgrade-preview");
            session.TryUpgradeOutpost(0);
            hud.ShowSpecializationChoices(session.Outposts[0], session);
            hud.BattleRadial.Choose(0);
            yield return CaptureAcceptance("specialization");
            hud.CloseSpecializationChoices();
            session.StartNextWave();
            yield return new WaitForSecondsRealtime(3f);
            yield return CaptureAcceptance("battle-active-wave");
            session.TogglePause();
            yield return CaptureAcceptance("pause");
            AcceptanceInvoke("CancelSelection");
            hud.GetComponentsInChildren<UiId>(true).First(id => id.Id == "MainMenu").GetComponent<Button>().onClick.Invoke();
            yield return CaptureAcceptance("pause-map-confirmation");
            hud.GetComponentsInChildren<UiId>(true).First(id => id.Id == "KeepPlaying").GetComponent<Button>().onClick.Invoke();
            session.TogglePause();
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
                yield return WinAcceptanceBattle();
                if (session.Phase != GamePhase.Victory || BattleRules.StarsForLives(session.Lives, session.StartingLivesForLevel) != 3)
                    acceptanceReport.errors.Add("victory: battle did not produce a real three-star result");
                yield return new WaitForSecondsRealtime(1.2f);
                yield return CaptureAcceptance("victory");
                acceptanceStage = 2; WriteAcceptance();
                reloadStarted = Time.realtimeSinceStartupAsDouble;
                ReloadCampaignLevel(2);
                yield break;
            }
            if (acceptanceStage == 2)
            {
                acceptanceStage = 3; WriteAcceptance();
                reloadStarted = Time.realtimeSinceStartupAsDouble;
                progress.Data.unlockedLevel = 10;
                progress.SelectLevel(10);
                ReloadCampaignLevel(10);
                yield break;
            }
            yield return PrepareAcceptanceBoss();
            yield return CaptureAcceptance("battle-level-10-boss");
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
            acceptanceReport.screenshots.Add(name + ".png");
            ScreenCapture.CaptureScreenshot(Application.isMobilePlatform
                ? Path.Combine("FrontendExperience", name + ".png") : Path.Combine(AcceptanceRoot, name + ".png"));
            if (name.StartsWith("main-menu", StringComparison.Ordinal) || name.StartsWith("map-", StringComparison.Ordinal) ||
                name.StartsWith("briefing-", StringComparison.Ordinal))
            {
                if (CurrentScreen == AppScreen.Campaign && briefingSheet != null)
                {
                    RectTransform card = (RectTransform)briefingSheet.Body.parent;
                    var corners = new Vector3[4]; briefingSheet.Body.GetWorldCorners(corners);
                    foreach (Vector3 corner in corners)
                        if (!card.rect.Contains(card.InverseTransformPoint(corner)))
                        {
                            acceptanceReport.errors.Add(name + " | briefing content outside bottom sheet");
                            break;
                        }
                }
                foreach (var button in screens[CurrentScreen].GetComponentsInChildren<Button>())
                {
                    if (!button.interactable) continue;
                    var corners = new Vector3[4];
                    ((RectTransform)button.transform).GetWorldCorners(corners);
                    Vector2 logical = UiMetrics.LogicalScreenSize;
                    float width = Vector3.Distance(corners[0], corners[3]) * logical.x / Screen.width;
                    float height = Vector3.Distance(corners[0], corners[1]) * logical.y / Screen.height;
                    float minimum = Mathf.Min(width, height);
                    acceptanceReport.minimumMenuTouchPt = Mathf.Min(acceptanceReport.minimumMenuTouchPt, minimum);
                    acceptanceReport.menuTouchTargetsChecked++;
                    if (minimum < UiMetrics.MinTouchPt - .1f)
                        acceptanceReport.errors.Add(name + " | touch target below 44 pt: " + button.GetComponent<UiId>()?.Id + " | " + minimum);
                }
            }
            foreach (var text in FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None))
            {
                if (!AcceptanceTextVisible(text)) continue;
                text.ForceMeshUpdate();
                if (!text.textInfo.characterInfo.Take(text.textInfo.characterCount).Any(character => character.isVisible) || !text.isTextOverflowing) continue;
                string path = text.name;
                for (Transform parent = text.transform.parent; parent != null; parent = parent.parent) path = parent.name + "/" + path;
                string key = name + " | " + path + " | " + GameLocalization.CurrentLanguage;
                if (!acceptanceErrors.Add(key)) continue;
                var issues = knownLegacyHudOverflowKeys.Contains(key) ? acceptanceReport.knownIssues : acceptanceReport.errors;
                issues.Add(key + " | overflow: " + text.text);
            }
        }

        private static bool AcceptanceTextVisible(TMPro.TMP_Text text)
        {
            if (text is not TMPro.TextMeshProUGUI || !text.isActiveAndEnabled || text.color.a <= 0f || text.canvasRenderer.cull ||
                text.GetComponentsInParent<CanvasGroup>().Any(group => group.alpha <= 0f)) return false;
            var corners = new Vector3[4];
            text.rectTransform.GetWorldCorners(corners);
            var canvas = text.canvas;
            Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            Vector2 min = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
            Vector2 max = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
            if (!new Rect(0, 0, Screen.width, Screen.height).Overlaps(Rect.MinMaxRect(min.x, min.y, max.x, max.y))) return false;
            foreach (var mask in text.GetComponentsInParent<RectMask2D>())
            {
                mask.rectTransform.GetWorldCorners(corners);
                Vector2 maskMin = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
                Vector2 maskMax = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
                if (!Rect.MinMaxRect(maskMin.x, maskMin.y, maskMax.x, maskMax.y).Overlaps(Rect.MinMaxRect(min.x, min.y, max.x, max.y))) return false;
            }
            return true;
        }

        private object AcceptanceInvoke(string method, params object[] arguments) =>
            typeof(GameSessionController).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(session, arguments);
        private void AcceptanceSet(string field, object value) =>
            typeof(GameSessionController).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(session, value);

        private void BuildAcceptanceDefense()
        {
            AcceptanceInvoke("EarnGold", 100000);
            AcceptanceSet("unlockedCampaignLevel", 10);
            for (int i = 0; i < session.SlotCount; i++)
            {
                session.TryBuildOutpost(i, OutpostKind.Archer);
                session.TryUpgradeOutpost(i);
                session.TrySpecializeOutpost(i, OutpostSpecialization.Sharpshooter);
            }
            AcceptanceInvoke("CancelSelection");
        }

        private IEnumerator WinAcceptanceBattle()
        {
            BuildAcceptanceDefense();
            double deadline = Time.realtimeSinceStartupAsDouble + 30;
            while (session.Phase != GamePhase.Victory && session.Phase != GamePhase.Defeat && Time.realtimeSinceStartupAsDouble < deadline)
            {
                if (session.Phase is GamePhase.Preparing or GamePhase.Intermission) session.StartNextWave();
                for (int i = 0; i < 100 && session.Phase == GamePhase.Wave; i++) session.AdvanceBattle(.05f);
                yield return null;
            }
        }

        private IEnumerator PrepareAcceptanceBoss()
        {
            BuildAcceptanceDefense();
            double deadline = Time.realtimeSinceStartupAsDouble + 30;
            while (session.CurrentWaveIndex < session.WaveCount - 1 && session.Phase != GamePhase.Defeat && Time.realtimeSinceStartupAsDouble < deadline)
            {
                if (session.Phase is GamePhase.Preparing or GamePhase.Intermission) session.StartNextWave();
                for (int i = 0; i < 100 && session.Phase == GamePhase.Wave; i++) session.AdvanceBattle(.05f);
                yield return null;
            }
            if (session.Phase is GamePhase.Preparing or GamePhase.Intermission) session.StartNextWave();
            deadline = Time.realtimeSinceStartupAsDouble + 30;
            while (session.Phase == GamePhase.Wave && !session.Enemies.Any(enemy => enemy.IsAlive && enemy.Archetype == EnemyArchetype.Boss) &&
                   Time.realtimeSinceStartupAsDouble < deadline)
            {
                for (int step = 0; step < 20 && !session.Enemies.Any(enemy => enemy.IsAlive && enemy.Archetype == EnemyArchetype.Boss); step++)
                    session.AdvanceBattle(.05f);
                yield return null;
            }
            session.GetComponentInChildren<DefenderHudController>()?.Refresh(session);
            if (session.Phase != GamePhase.Wave || !session.NextWaveHasBoss ||
                !session.Enemies.Any(enemy => enemy.IsAlive && enemy.Archetype == EnemyArchetype.Boss))
                acceptanceReport.errors.Add("level-10-boss: live boss is not on the battlefield");
        }
        private static void WriteAcceptance() => File.WriteAllText(Path.Combine(AcceptanceRoot, "report.json"), JsonUtility.ToJson(acceptanceReport, true));
    }
}
