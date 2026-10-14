using System.Linq;
using AllinDefense.Core;
using AllinDefense.Data;
using UnityEngine;
using UnityEngine.UI;

namespace AllinDefense.UI
{
    public sealed partial class AppFlowController
    {
        // Released builds expose only the balanced regions; storage still keeps the full campaign.
        private const int ReleasedRegionCount = 5;
        private static int VisibleRegionCount
        {
            get
            {
#if UNITY_EDITOR || UNITY_INCLUDE_TESTS
                return visibleRegionCountForTests ?? ReleasedRegionCount;
#else
                return ReleasedRegionCount;
#endif
            }
        }
#if UNITY_EDITOR || UNITY_INCLUDE_TESTS
        private static int? visibleRegionCountForTests;
        internal static void SetVisibleRegionCountForTests(int? count) =>
            visibleRegionCountForTests = count.HasValue ? Mathf.Clamp(count.Value, 1, CampaignCatalog.RegionCount) : null;
#endif
        private static int PlayableLevelCount => VisibleRegionCount * CampaignCatalog.LevelsPerRegion;
        private Button importGuestButton;
        private int MaxPlayableLevel => Mathf.Min(PlayableLevelCount, progress.Data.unlockedLevel);
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
        private static string LearningObjective(int level)
        {
            var definition = CampaignCatalog.Level(level);
            if (definition.isBoss) return FrontendCopy.ObjectiveBoss.Format(definition.localizedName);
            if (level > 5 * CampaignCatalog.LevelsPerRegion)
                return FrontendCopy.ObjectiveRegion.Format(GameLocalization.Translate(CampaignCatalog.Regions[definition.region - 1].subtitle));
            return level switch
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
                11 => FrontendCopy.ObjectiveAshVanguard.Value,
                12 => FrontendCopy.ObjectiveHealerEscort.Value,
                13 => FrontendCopy.ObjectiveLongTurn.Value,
                14 => FrontendCopy.ObjectiveSecondEntrance.Value,
                15 => FrontendCopy.ObjectiveAlternatingRoutes.Value,
                16 => FrontendCopy.ObjectiveSupportConvoy.Value,
                17 => FrontendCopy.ObjectiveShortSidePath.Value,
                18 => FrontendCopy.ObjectiveSiegeTiming.Value,
                19 => FrontendCopy.ObjectiveHoldBothRoutes.Value,
                21 => FrontendCopy.ObjectiveFastPack.Value,
                22 => FrontendCopy.ObjectiveArrivalTiming.Value,
                23 => FrontendCopy.ObjectiveFastThenRegen.Value,
                24 => FrontendCopy.ObjectiveHeroBetweenRoutes.Value,
                25 => FrontendCopy.ObjectiveRegroup.Value,
                26 => FrontendCopy.ObjectiveDelayedPack.Value,
                27 => FrontendCopy.ObjectiveSpreadDefence.Value,
                28 => FrontendCopy.ObjectiveEarlyHealer.Value,
                29 => FrontendCopy.ObjectivePhaseRehearsal.Value,
                31 => FrontendCopy.ObjectiveQuietRouteShift.Value,
                32 => FrontendCopy.ObjectiveMergeCoverage.Value,
                33 => FrontendCopy.ObjectiveSplitBudget.Value,
                34 => FrontendCopy.ObjectiveSideSiege.Value,
                35 => FrontendCopy.ObjectiveQuietUpgrade.Value,
                36 => FrontendCopy.ObjectiveRoleSwap.Value,
                37 => FrontendCopy.ObjectiveRegenHealerSplit.Value,
                38 => FrontendCopy.ObjectiveDoublePush.Value,
                39 => FrontendCopy.ObjectiveSpecialisationTest.Value,
                41 => FrontendCopy.ObjectiveAllLessons.Value,
                42 => FrontendCopy.ObjectiveWatchSidePath.Value,
                43 => FrontendCopy.ObjectiveThirdEntrance.Value,
                44 => FrontendCopy.ObjectiveRoutesInTurn.Value,
                45 => FrontendCopy.ObjectiveRebuild.Value,
                46 => FrontendCopy.ObjectiveThirdRouteDelay.Value,
                47 => FrontendCopy.ObjectiveHeroToSiege.Value,
                48 => FrontendCopy.ObjectiveKeepCorridor.Value,
                49 => FrontendCopy.ObjectiveFinalRehearsal.Value,
                _ => FrontendCopy.ObjectiveLord.Value
            };
        }
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
