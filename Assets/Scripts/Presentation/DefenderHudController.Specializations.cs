using DefenderChronicles.Core;
using DefenderChronicles.Data;
using DefenderChronicles.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DefenderChronicles.Presentation
{
    public sealed partial class DefenderHudController
    {
        private GameObject specializationModal;
        private void ApplySpecializationSelection(GreyboxOutpost outpost)
        {
            var session = GameSessionController.Instance;
            selectedBadgeText.text = GameLocalization.Text($"SV {outpost.Level}", $"LV {outpost.Level}");
            selectedText.text = GameLocalization.Text(
                $"HASAR {outpost.DamageValue} · {(outpost.Kind == OutpostKind.Mage ? "Büyü" : "Fiziksel")}\nMENZİL {outpost.RangeValue:0.0}\nATIŞ {outpost.AttackRateValue:0.0}/s",
                $"DAMAGE {outpost.DamageValue} · {(outpost.Kind == OutpostKind.Mage ? "Magic" : "Physical")}\nRANGE {outpost.RangeValue:0.0}\nRATE {outpost.AttackRateValue:0.0}/s");
            selectedHintText.text = GameLocalization.Text($"Satışta {outpost.SellValue} altın geri alırsın.", $"Selling returns {outpost.SellValue} gold.");
            SetButtonLabel(sellButton, GameLocalization.Text($"SAT\n+{outpost.SellValue}", $"SELL\n+{outpost.SellValue}"));
            SetButtonLabel(upgradeButton, outpost.CanUpgrade ? GameLocalization.Text($"YÜKSELT\n{outpost.UpgradeCost}", $"UPGRADE\n{outpost.UpgradeCost}") : GameLocalization.Text("TAMAM", "MAX"));
            if (outpost.CanUpgrade && session != null && session.Gold < outpost.UpgradeCost)
                selectedHintText.text = GameLocalization.Text($"Yükseltme için {outpost.UpgradeCost-session.Gold} altın eksik.", $"Need {outpost.UpgradeCost-session.Gold} more gold to upgrade.");
            if (outpost.Specialization != OutpostSpecialization.None)
            {
                selectedTitleText.text = TowerProgression.Name(outpost.Specialization);
                selectedRoleText.text = TowerProgression.Description(outpost.Specialization);
            }
            if (outpost.CanSpecialize)
            {
                bool unlocked = session != null && session.UnlockedCampaignLevel >= TowerProgression.UnlockLevel(outpost.Kind);
                upgradeButton.interactable = unlocked;
                SetButtonLabel(upgradeButton, unlocked
                    ? GameLocalization.Text($"UZMANLAŞ\n{TowerProgression.Cost(outpost.Kind)} ALTIN", $"SPECIALIZE\n{TowerProgression.Cost(outpost.Kind)} GOLD")
                    : GameLocalization.Text($"BÖLÜM {TowerProgression.UnlockLevel(outpost.Kind) - 1} SONRASI", $"AFTER LEVEL {TowerProgression.UnlockLevel(outpost.Kind) - 1}"));
            }
        }

        public void ShowSpecializationChoices(GreyboxOutpost outpost, GameSessionController session)
        {
            if (specializationModal != null) Destroy(specializationModal);
            specializationModal = new GameObject("Specialization choices", typeof(RectTransform), typeof(Image));
            var parent = selectedPanel.transform.parent;
            specializationModal.transform.SetParent(parent, false);
            var rect = (RectTransform)specializationModal.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            specializationModal.GetComponent<Image>().color = new Color(.02f, .04f, .08f, .96f);
            for (int i = 0; i < 2; i++)
            {
                var branch = TowerProgression.Branch(outpost.Kind, i);
                var button = Instantiate(upgradeButton, specializationModal.transform);
                button.name = branch.ToString();
                button.onClick = new Button.ButtonClickedEvent();
                var br = (RectTransform)button.transform;
                br.anchorMin = br.anchorMax = new Vector2(.5f, .5f);
                br.pivot = new Vector2(.5f,.5f);
                br.anchoredPosition = new Vector2(i == 0 ? -235 : 235, 40);
                br.sizeDelta = new Vector2(440, 260);
                int cost = TowerProgression.Cost(outpost.Kind);
                SetButtonLabel(button, $"{TowerProgression.Name(branch)}\n{TowerProgression.Description(branch)}\n" +
                    GameLocalization.Text($"{cost} ALTIN", $"{cost} GOLD") +
                    (session.Gold < cost ? GameLocalization.Text($"\n{cost-session.Gold} altın eksik", $"\nNeed {cost-session.Gold} more gold") : ""));
                foreach (var label in button.GetComponentsInChildren<TMP_Text>()) { label.fontSize = 32; label.enableAutoSizing = true; label.fontSizeMin = 28; label.fontSizeMax = 34; }
                button.interactable = session.UnlockedCampaignLevel >= TowerProgression.UnlockLevel(outpost.Kind) && session.Gold >= cost;
                button.onClick.AddListener(() => { if (session.TrySpecializeOutpost(outpost.Slot.Index, branch)) Destroy(specializationModal); });
            }
            var close = Instantiate(sellButton, specializationModal.transform);
            close.onClick = new Button.ButtonClickedEvent();
            var cr = (RectTransform)close.transform;
            cr.anchorMin = cr.anchorMax = cr.pivot = new Vector2(.5f,.5f);
            cr.anchoredPosition = new Vector2(0,-190); cr.sizeDelta = new Vector2(440,130);
            SetButtonLabel(close, GameLocalization.Text("GERİ • Dal değiştirmek için satıp yeniden kur", "BACK • Sell and rebuild to change branch"));
            close.interactable = true;
            close.onClick.AddListener(() => Destroy(specializationModal));
        }
    }
}
