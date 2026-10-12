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
        private void BuildGuestImportAction()
        {
            var root = screens[AppScreen.MainMenu].transform;
            importGuestButton = CreateButton("Import Guest Progress", root,
                GameLocalization.Text("MİSAFİR İLERLEMESİNİ AKTAR", "IMPORT GUEST PROGRESS"), new Vector2(630, 110),
                theme.defenderBlue, () =>
                {
                    var modal = CreatePanel("Confirm Guest Import", canvas.transform, theme.panel);
                    SetRect(modal.GetComponent<RectTransform>(), Vector2.zero, new Vector2(860, 500), Center(), Center());
                    var label = CreateText("Explanation", modal.transform,
                        GameLocalization.Text("Bu cihazın misafir bölüm başarıları açık hesaba aktarılacak. Misafir kaydı saklanır; hesap değiştirmek otomatik aktarım yapmaz.",
                            "This device's guest level results will be imported into the current account. The guest save stays on this device; switching accounts never imports automatically."),
                        28, catalog.bodyFont, TMPro.FontStyles.Normal, TMPro.TextAlignmentOptions.Center, theme.textPrimary);
                    SetRect(label.rectTransform, new Vector2(0,65), new Vector2(760,200), Center(), Center());
                    var accept = CreateButton("Confirm Import", modal.transform, GameLocalization.Text("AKTAR", "IMPORT"), new Vector2(340,130), theme.commandGold,
                        () => { progress.ImportGuestProgress(); RefreshProfile(); importGuestButton.gameObject.SetActive(progress.CanImportGuest); Destroy(modal); });
                    SetRect(accept.GetComponent<RectTransform>(), new Vector2(-180,-135), new Vector2(340,130), Center(), Center());
                    var cancel = CreateButton("Cancel Import", modal.transform, FrontendCopy.Cancel, new Vector2(340,130), theme.night, () => Destroy(modal));
                    SetRect(cancel.GetComponent<RectTransform>(), new Vector2(180,-135), new Vector2(340,130), Center(), Center());
                    modal.transform.SetAsLastSibling();
                });
            SetRect(importGuestButton.GetComponent<RectTransform>(), new Vector2(0,716), new Vector2(630,110), BottomCenter(), BottomCenter());
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
            EnemyArchetype.Vanguard => GameLocalization.Text("Zırhlı hedefe büyücü yerleştir.", "Place a mage against armoured targets."),
            EnemyArchetype.Support => GameLocalization.Text("Keskin Nişancı şifacıları önce hedefler.", "Sharpshooters target healers first."),
            EnemyArchetype.Siege or EnemyArchetype.Boss => GameLocalization.Text("Kırmızı uyarı sırasında menzildeyse Sarsıcı Darbe kullan.", "Use Stunning Strike during the red warning when the attacker is in range."),
            EnemyArchetype.Regenerator => GameLocalization.Text("Aynı hedefe iki kuleyle yoğunlaş.", "Focus two towers on the same target."),
            _ => GameLocalization.Text("Bir sonraki yol kıvrımına okçu ekle veya yükselt.", "Add or upgrade an archer at the next bend.")
        };
    }
}
