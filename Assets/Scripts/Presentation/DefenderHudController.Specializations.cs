using AllinDefense.Core;
using AllinDefense.Data;
using AllinDefense.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AllinDefense.Presentation
{
    public sealed partial class DefenderHudController
    {
        private GameObject specializationModal;
        private GameSessionController specializationSession;
        private void ApplySpecializationSelection(GreyboxOutpost outpost)
        {
            var session = GameSessionController.Instance;
            selectedBadgeText.text = FrontendCopy.TowerLevel.Format(outpost.Level);
            selectedText.text = FrontendCopy.TowerDamageStats.Format(outpost.DamageValue,
                (outpost.Kind == OutpostKind.Mage ? FrontendCopy.MagicDamage : FrontendCopy.PhysicalDamage).Value,
                outpost.RangeValue, outpost.AttackRateValue);
            selectedHintText.text = FrontendCopy.SellingReturnsGold.Format(outpost.SellValue);
            SetButtonLabel(sellButton, FrontendCopy.Sell.Format(outpost.SellValue));
            SetButtonLabel(upgradeButton, outpost.CanUpgrade ? FrontendCopy.Upgrade.Format(outpost.UpgradeCost) : FrontendCopy.Max.Value);
            if (outpost.CanUpgrade && session != null && session.Gold < outpost.UpgradeCost)
                selectedHintText.text = FrontendCopy.NeedMoreGoldToUpgrade.Format(outpost.UpgradeCost-session.Gold);
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
                    ? FrontendCopy.SpecializeGold.Format(TowerProgression.Cost(outpost.Kind))
                    : FrontendCopy.AfterLevel.Format(TowerProgression.UnlockLevel(outpost.Kind) - 1));
            }
        }

        public void ShowSpecializationChoices(GreyboxOutpost outpost, GameSessionController session)
        {
            if (outpost == null || session == null || !outpost.CanSpecialize) return;
            CloseSpecializationChoices();
            if (!session.BeginSpecializationModal()) return;
            CancelSaleConfirmation();
            specializationSession = session;
            session.PhaseChanged += HandleSpecializationPhaseChanged;
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
                UiId.Assign(button.transform, branch.ToString());
                button.onClick = new Button.ButtonClickedEvent();
                var br = (RectTransform)button.transform;
                br.anchorMin = br.anchorMax = new Vector2(.5f, .5f);
                br.pivot = new Vector2(.5f,.5f);
                br.anchoredPosition = new Vector2(i == 0 ? -235 : 235, 40);
                br.sizeDelta = new Vector2(440, 260);
                int cost = TowerProgression.Cost(outpost.Kind);
                SetButtonLabel(button, $"{TowerProgression.Name(branch)}\n{TowerProgression.Description(branch)}\n" +
                    FrontendCopy.Gold.Format(cost) +
                    (session.Gold < cost ? FrontendCopy.NeedMoreGold.Format(cost-session.Gold) : ""));
                foreach (var label in button.GetComponentsInChildren<TMP_Text>()) { label.fontSize = 32; label.enableAutoSizing = true; label.fontSizeMin = 28; label.fontSizeMax = 34; }
                button.interactable = session.UnlockedCampaignLevel >= TowerProgression.UnlockLevel(outpost.Kind) && session.Gold >= cost;
                button.onClick.AddListener(() =>
                {
                    CloseSpecializationChoices();
                    session.TrySpecializeOutpost(outpost.Slot.Index, branch);
                });
            }
            var close = Instantiate(sellButton, specializationModal.transform);
            close.onClick = new Button.ButtonClickedEvent();
            var cr = (RectTransform)close.transform;
            cr.anchorMin = cr.anchorMax = cr.pivot = new Vector2(.5f,.5f);
            cr.anchoredPosition = new Vector2(0,-190); cr.sizeDelta = new Vector2(440,130);
            SetButtonLabel(close, FrontendCopy.BackSellAndRebuildToChangeBranch.Value);
            close.interactable = true;
            close.onClick.AddListener(CloseSpecializationChoices);
            UiId.AssignHierarchy(specializationModal.transform);
        }

        private void HandleSpecializationPhaseChanged(GamePhase phase)
        {
            if (phase != GamePhase.Paused) CloseSpecializationChoices();
        }

        public void CloseSpecializationChoices()
        {
            if (specializationModal != null)
            {
                specializationModal.SetActive(false);
                Destroy(specializationModal);
                specializationModal = null;
            }
            var session = specializationSession;
            specializationSession = null;
            if (session != null)
            {
                session.PhaseChanged -= HandleSpecializationPhaseChanged;
                session.EndSpecializationModal();
            }
        }
    }
}
