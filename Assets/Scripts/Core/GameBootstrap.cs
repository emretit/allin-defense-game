using UnityEngine;
using DefenderChronicles.Presentation;
using DefenderChronicles.UI;
using System;
using System.Linq;

namespace DefenderChronicles.Core
{
    /// <summary>
    /// Minimal production entry point. The scene must explicitly provide the
    /// validated presentation catalog before the gameplay session is initialized.
    /// </summary>
    [DefaultExecutionOrder(-500)]
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private DefenderPresentationCatalog presentationCatalog;
        public static bool RuntimeSceneReady { get; private set; }

        private void Awake()
        {
            RuntimeSceneReady = false;
            bool showFrontend = ShouldShowFrontend();
            bool resumePendingBattle = showFrontend && AppFlowController.HasPendingLevel;
            MobileOrientationController.Request(showFrontend && !resumePendingBattle
                ? MobileOrientationMode.PortraitFrontend
                : MobileOrientationMode.LandscapeBattle);

            var session = FindAnyObjectByType<GameSessionController>();
            if (session == null)
            {
                var root = new GameObject("DefenderChroniclesSession");
                session = root.AddComponent<GameSessionController>();
            }

            if (presentationCatalog == null)
                throw new System.InvalidOperationException("Production presentation catalog is missing from GameBootstrap.");
            session.ConfigurePresentation(presentationCatalog);

            if (showFrontend)
            {
                var appFlowObject = new GameObject("DefenderChroniclesAppFlow");
                var appFlow = appFlowObject.AddComponent<AppFlowController>();
                appFlow.Initialize(presentationCatalog, session);
                session.ConfigureMainMenuAction(appFlow.ReturnToMainMenu);
                var services = appFlowObject.AddComponent<UnityGameServicesController>();
                services.Configure(appFlow, appFlow.Progress);
                if (Environment.GetEnvironmentVariable("DC_UGS_GUEST_ACCEPTANCE") == "1")
                    appFlow.BeginAuthentication(PlayerAuthProvider.Guest);
                else
                {
                    string externalProvider = Environment.GetEnvironmentVariable("DC_UGS_EXTERNAL_ACCEPTANCE");
                    if (!string.IsNullOrWhiteSpace(externalProvider))
                        appFlow.BeginAuthentication(PlayerAuthProvider.PlayerAccount);
                }
                int pendingLevel = AppFlowController.ConsumePendingLevel();
                if (pendingLevel > 0) appFlow.LaunchCampaignLevel(pendingLevel);
            }
            else
            {
                string requestedLevel = Environment.GetEnvironmentVariable("DC_CAMPAIGN_LEVEL");
                if (int.TryParse(requestedLevel, out int levelNumber))
                    session.SetCampaignLevel(levelNumber);
                session.Initialize();
            }
            RuntimeSceneReady = true;
        }

        private static bool ShouldShowFrontend()
        {
            if (Application.isEditor && !AppFlowController.HasPendingLevel &&
                Environment.GetEnvironmentVariable("DC_FRONTEND_EDITOR") != "1") return false;
            string[] args = Environment.GetCommandLineArgs();
            bool commandLineAutomation = args.Any(item =>
                string.Equals(item, "-dc-smoke", StringComparison.OrdinalIgnoreCase));
            bool environmentAutomation = Environment.GetEnvironmentVariable("DC_SMOKE") == "1" ||
                                         Environment.GetEnvironmentVariable("DC_SMOKE_DEFEAT") == "1" ||
                                         Environment.GetEnvironmentVariable("DC_SLOT_REGRESSION") == "1" ||
                                         Environment.GetEnvironmentVariable("DC_ABILITY_CAPTURE") == "1" ||
                                         Environment.GetEnvironmentVariable("DC_PAUSE_CAPTURE") == "1" ||
                                         Environment.GetEnvironmentVariable("DC_ACTOR_CAPTURE") == "1" ||
                                         Environment.GetEnvironmentVariable("DC_PERFORMANCE_ACCEPTANCE") == "1" ||
                                         Environment.GetEnvironmentVariable("DC_SOAK_ACCEPTANCE") == "1" ||
                                         Environment.GetEnvironmentVariable("DC_AUDIO_ACCEPTANCE") == "1" ||
                                         Environment.GetEnvironmentVariable("DC_VIS812_CAPTURE") == "1" ||
                                         Environment.GetEnvironmentVariable("DC_VIS812_CAPTURE_DEFEAT") == "1";
            bool showFrontend = !commandLineAutomation && !environmentAutomation;
            Debug.Log($"[DC_FRONTEND_MODE] show={showFrontend} args={string.Join(",", args)}");
            return showFrontend;
        }
    }
}
