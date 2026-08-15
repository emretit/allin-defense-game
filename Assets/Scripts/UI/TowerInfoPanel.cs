using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DefenderChronicles.Core;
using DefenderChronicles.Grid;
using DefenderChronicles.Towers;

namespace DefenderChronicles.UI
{
    public class TowerInfoPanel : MonoBehaviour
    {
        public static TowerInfoPanel Instance { get; private set; }

        [Header("UI Elements")]
        [SerializeField] private GameObject panel;
        [SerializeField] private TextMeshProUGUI towerNameText;
        [SerializeField] private TextMeshProUGUI towerLevelText;
        [SerializeField] private TextMeshProUGUI damageText;
        [SerializeField] private TextMeshProUGUI rangeText;
        [SerializeField] private TextMeshProUGUI attackSpeedText;
        [SerializeField] private Button upgradeButton;
        [SerializeField] private TextMeshProUGUI upgradeButtonText;
        [SerializeField] private Button sellButton;
        [SerializeField] private TextMeshProUGUI sellButtonText;

        private TowerBase selectedTower;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>Wires UI built at runtime by GameBootstrap.</summary>
        public void BindRuntime(
            GameObject panelRoot,
            TextMeshProUGUI nameTmp,
            TextMeshProUGUI levelTmp,
            TextMeshProUGUI dmgTmp,
            TextMeshProUGUI rangeTmp,
            TextMeshProUGUI speedTmp,
            Button upgradeBtn,
            TextMeshProUGUI upgradeLabel,
            Button sellBtn,
            TextMeshProUGUI sellLabel,
            Button closeBtn)
        {
            panel = panelRoot;
            towerNameText = nameTmp;
            towerLevelText = levelTmp;
            damageText = dmgTmp;
            rangeText = rangeTmp;
            attackSpeedText = speedTmp;
            upgradeButton = upgradeBtn;
            upgradeButtonText = upgradeLabel;
            sellButton = sellBtn;
            sellButtonText = sellLabel;

            if (upgradeButton != null)
            {
                upgradeButton.onClick.RemoveAllListeners();
                upgradeButton.onClick.AddListener(OnUpgradeButtonPressed);
            }
            if (sellButton != null)
            {
                sellButton.onClick.RemoveAllListeners();
                sellButton.onClick.AddListener(OnSellButtonPressed);
            }
            if (closeBtn != null)
            {
                closeBtn.onClick.RemoveAllListeners();
                closeBtn.onClick.AddListener(HidePanel);
            }
        }

        private void Start()
        {
            panel?.SetActive(false);
        }

        public void ShowTowerInfo(TowerBase tower)
        {
            selectedTower = tower;
            panel?.SetActive(true);
            RefreshUI();
        }

        public void HidePanel()
        {
            panel?.SetActive(false);
            selectedTower = null;
        }

        private void RefreshUI()
        {
            if (selectedTower == null) return;
            var data = selectedTower.Data;

            if (towerNameText != null) towerNameText.text = data.towerName;
            if (towerLevelText != null) towerLevelText.text = $"Level {data.level}";
            if (damageText != null) damageText.text = $"Damage: {data.damage}";
            if (rangeText != null) rangeText.text = $"Range: {data.attackRange:F1}";
            if (attackSpeedText != null) attackSpeedText.text = $"Speed: {data.attackSpeed:F1}/s";

            bool canUpgrade = selectedTower.CanUpgrade();
            if (upgradeButton != null) upgradeButton.interactable = canUpgrade;
            if (upgradeButtonText != null)
                upgradeButtonText.text = canUpgrade ? $"Upgrade ({data.upgradeCost}g)" : "Max Level";

            if (sellButtonText != null)
                sellButtonText.text = $"Sell ({selectedTower.GetSellValue()}g)";
        }

        public void OnUpgradeButtonPressed()
        {
            if (selectedTower == null) return;
            if (selectedTower.Upgrade())
                RefreshUI();
        }

        public void OnSellButtonPressed()
        {
            if (selectedTower == null) return;
            GameManager.Instance?.EarnGold(selectedTower.GetSellValue());
            OutpostSlotManager.Instance?.TryReleaseOutpost(selectedTower.gameObject);
            GridManager.Instance?.TryReleaseOccupant(selectedTower.gameObject);
            Destroy(selectedTower.gameObject);
            HidePanel();
        }
    }
}
