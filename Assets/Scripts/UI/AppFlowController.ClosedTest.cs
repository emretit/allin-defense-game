using System.Linq;
using DefenderChronicles.Core;
using DefenderChronicles.Data;
using UnityEngine;
using UnityEngine.UI;

namespace DefenderChronicles.UI
{
    public sealed partial class AppFlowController
    {
        private Button importGuestButton;
        private string SaveStatus() => progress.HasWriteError ? GameLocalization.Text("Yerel kayıt yazılamadı · Tekrar denenecek", "Local save failed · Retrying") : progress.Data.unreadableSavePreserved ? GameLocalization.Text("Eski kayıt okunamadı · Dosyaları kurtarma için korundu", "Old save unreadable · Files preserved for recovery") : progress.SyncStatus switch
        {
            CampaignSyncStatus.Current => GameLocalization.Text("Kaydedildi · Eşitlendi", "Saved · Synced"),
            CampaignSyncStatus.RequiresReview => GameLocalization.Text("Aktarım sorunu · Yerel kayıt korundu", "Transfer issue · Local save retained"),
            CampaignSyncStatus.Syncing => GameLocalization.Text("Kaydedildi · Eşitleniyor", "Saved · Syncing"),
            _ => GameLocalization.Text("Kaydedildi · Eşitleme bekliyor", "Saved · Sync pending")
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
                    var cancel = CreateButton("Cancel Import", modal.transform, GameLocalization.Text("İPTAL", "CANCEL"), new Vector2(340,130), theme.night, () => Destroy(modal));
                    SetRect(cancel.GetComponent<RectTransform>(), new Vector2(180,-135), new Vector2(340,130), Center(), Center());
                    modal.transform.SetAsLastSibling();
                });
            SetRect(importGuestButton.GetComponent<RectTransform>(), new Vector2(0,90), new Vector2(630,110), BottomCenter(), BottomCenter());
            importGuestButton.gameObject.SetActive(progress.CanImportGuest);
        }
        private static string LearningObjective(int level) => level switch
        {
            1 => GameLocalization.Text("Okçu kur · Yükselt · Kalkan Darbesi ile kaleyi koru", "Build an archer · Upgrade · Protect the keep with Shield Bash"),
            2 => GameLocalization.Text("Kışlayla düşmanı okçunun menzilinde tut", "Use barracks to hold enemies within archer range"),
            3 => GameLocalization.Text("Büyücüyle kalabalığı temizle", "Clear crowds with a mage"),
            4 => GameLocalization.Text("Öncü: %50 fiziksel zırh · Büyüye direnç yok", "Vanguard: 50% physical armour · No magic resistance"),
            5 => GameLocalization.Text("Üç kule rolünü birlikte kullan", "Combine all three tower roles"),
            6 => GameLocalization.Text("Yeni: Hızlı Atış / Keskin Nişancı · Yenilenmeyi aş", "New: Rapid Fire / Sharpshooter · Overcome regeneration"),
            7 => GameLocalization.Text("Yeni: Patlama / Buz · Önce şifacıyı durdur", "New: Blast / Frost · Stop the healer first"),
            8 => GameLocalization.Text("Yeni: Kalkan / Hücum Birliği · Kuşatmayı durdur", "New: Shield / Assault Guard · Interrupt siege attacks"),
            9 => GameLocalization.Text("Tek rotada karma baskıya hazırlan", "Prepare for mixed pressure on one route"),
            _ => GameLocalization.Text("Lord: %66 / %33 fazları · Kırmızı uyarıyı Kalkan Darbesi ile kes", "Lord: 66% / 33% phases · Interrupt the red warning with Shield Bash")
        };
        private static string DefenceAdvice(EnemyArchetype enemy) => enemy switch
        {
            EnemyArchetype.Vanguard => GameLocalization.Text("Zırhlı hedefe büyücü yerleştir.", "Place a mage against armoured targets."),
            EnemyArchetype.Support => GameLocalization.Text("Keskin Nişancı şifacıları önce hedefler.", "Sharpshooters target healers first."),
            EnemyArchetype.Siege or EnemyArchetype.Boss => GameLocalization.Text("Kırmızı uyarı sırasında menzildeyse Kalkan Darbesi kullan.", "Use Shield Bash during the red warning when the attacker is in range."),
            EnemyArchetype.Regenerator => GameLocalization.Text("Aynı hedefe iki kuleyle yoğunlaş.", "Focus two towers on the same target."),
            _ => GameLocalization.Text("Bir sonraki yol kıvrımına okçu ekle veya yükselt.", "Add or upgrade an archer at the next bend.")
        };
    }
}
