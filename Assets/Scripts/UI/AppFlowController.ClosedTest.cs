using System.Linq;
using AllinDefense.Core;
using AllinDefense.Data;
using UnityEngine;
using UnityEngine.UI;

namespace AllinDefense.UI
{
    public sealed partial class AppFlowController
    {
        // The closed test exposes only the first region; storage still keeps the full campaign.
        private const int VisibleRegionCount = 1;
        private Button importGuestButton;
        private int MaxPlayableLevel => Mathf.Min(FirstRegionCampaign.LevelCount, progress.Data.unlockedLevel);
        private string SaveStatus() => progress.HasWriteError ? FrontendCopy.SaveRetry.Value : progress.Data.unreadableSavePreserved ? FrontendCopy.SaveRecovery.Value : progress.SyncStatus switch
        {
            CampaignSyncStatus.RequiresReview => FrontendCopy.TransferReview.Value,
            CampaignSyncStatus.Pending when progress.Data.authProvider == PlayerAuthProvider.PlayerAccount => FrontendCopy.WaitingForConnection.Value,
            CampaignSyncStatus.Failed => FrontendCopy.WaitingForConnection.Value,
            _ => ""
        };
        private void OnSaveStatusChanged(CampaignSyncStatus status)
        {
            RefreshProfile();
            if (importGuestButton != null) importGuestButton.gameObject.SetActive(progress.CanImportGuest);
        }
        private void BuildGuestImportAction(Transform root)
        {
            importGuestButton = CreateButton("Import Guest Progress", root,
                FrontendCopy.ImportGuestProgress.Value, new Vector2(630, 110),
                theme.defenderBlue, () =>
                {
                    var modal = CreatePanel("Confirm Guest Import", canvas.transform, theme.panel);
                    SetRect(modal.GetComponent<RectTransform>(), Vector2.zero, new Vector2(860, 500), Center(), Center());
                    var label = CreateText("Explanation", modal.transform,
                        FrontendCopy.GuestImportExplanation.Value,
                        28, catalog.bodyFont, TMPro.FontStyles.Normal, TMPro.TextAlignmentOptions.Center, theme.textPrimary);
                    SetRect(label.rectTransform, new Vector2(0,65), new Vector2(760,200), Center(), Center());
                    var accept = CreateButton("Confirm Import", modal.transform, FrontendCopy.Import.Value, new Vector2(340,130), theme.commandGold,
                        () => { progress.ImportGuestProgress(); RefreshProfile(); importGuestButton.gameObject.SetActive(progress.CanImportGuest); Destroy(modal); });
                    SetRect(accept.GetComponent<RectTransform>(), new Vector2(-180,-135), new Vector2(340,130), Center(), Center());
                    var cancel = CreateButton("Cancel Import", modal.transform, FrontendCopy.Cancel, new Vector2(340,130), theme.night, () => Destroy(modal));
                    SetRect(cancel.GetComponent<RectTransform>(), new Vector2(180,-135), new Vector2(340,130), Center(), Center());
                    modal.transform.SetAsLastSibling();
                });
            AllinDefense.UI.Kit.UiKit.Size(importGuestButton.GetComponent<RectTransform>(), height: TouchHeight);
            importGuestButton.gameObject.SetActive(progress.CanImportGuest);
        }
        private static string LearningObjective(int level) => level switch
        {
            1 => FrontendCopy.ObjectiveArcher.Value,
            2 => FrontendCopy.ObjectiveBarracks.Value,
            3 => FrontendCopy.ObjectiveMage.Value,
            4 => FrontendCopy.ObjectiveArmour.Value,
            5 => FrontendCopy.ObjectiveTowerRoles.Value,
            6 => FrontendCopy.ObjectiveRegeneration.Value,
            7 => FrontendCopy.ObjectiveHealer.Value,
            8 => FrontendCopy.ObjectiveSiege.Value,
            9 => FrontendCopy.ObjectiveMixedPressure.Value,
            _ => FrontendCopy.ObjectiveLord.Value
        };
        private static string DefenceAdvice(EnemyArchetype enemy) => enemy switch
        {
            EnemyArchetype.Vanguard => FrontendCopy.PlaceAMageAgainstArmouredTargets.Value,
            EnemyArchetype.Support => FrontendCopy.SharpshootersTargetHealersFirst.Value,
            EnemyArchetype.Siege or EnemyArchetype.Boss => FrontendCopy.UseStunningStrikeDuringTheRedWarning.Value,
            EnemyArchetype.Regenerator => FrontendCopy.FocusTwoTowersOnTheSameTarget.Value,
            _ => FrontendCopy.AddOrUpgradeAnArcherAtThe.Value
        };
    }
}
