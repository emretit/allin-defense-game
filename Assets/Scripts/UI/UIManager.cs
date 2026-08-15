using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DefenderChronicles.Core;
using DefenderChronicles.Towers;
using DefenderChronicles.Data;
using DefenderChronicles.Heroes;
using DefenderChronicles.Utils;

namespace DefenderChronicles.UI
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("HUD")]
        [SerializeField] private TextMeshProUGUI goldText;
        [SerializeField] private TextMeshProUGUI livesText;
        [SerializeField] private TextMeshProUGUI waveText;
        [SerializeField] private Button nextWaveButton;
        [SerializeField] private Button pauseButton;
        [SerializeField] private TextMeshProUGUI pauseButtonText;
        [SerializeField] private Button cancelPlacementButton;

        [Header("Panels")]
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private GameObject victoryPanel;
        [SerializeField] private GameObject towerShopPanel;
        [SerializeField] private GameObject heroShopPanel;

        [Header("Wave Info")]
        [SerializeField] private Slider waveProgressBar;
        [SerializeField] private TextMeshProUGUI waveCountText;

        [Header("Audio UI")]
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;

        [Header("Hero")]
        [SerializeField] private Button heroSpecialButton;
        [SerializeField] private TextMeshProUGUI heroSpecialCooldownText;

        private bool _listenersRegistered;

        public bool HasHudBound => goldText != null && nextWaveButton != null;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            RegisterHudListeners();

            gameOverPanel?.SetActive(false);
            victoryPanel?.SetActive(false);

            if (nextWaveButton != null)
                nextWaveButton.interactable = true;
        }

        /// <summary>Used by <see cref="GameBootstrap"/> when HUD is built at runtime.</summary>
        public void ApplyRuntimeBindings(
            TextMeshProUGUI gold,
            TextMeshProUGUI lives,
            TextMeshProUGUI wave,
            Button nextWave,
            Button pause,
            TextMeshProUGUI pauseLabel,
            Button cancelPlacement,
            TextMeshProUGUI waveCount,
            GameObject gameOver,
            GameObject victory,
            Button btnArcher,
            Button btnMage,
            Button btnWarrior,
            Button btnCatapult,
            Slider waveProgress,
            Slider musicVol,
            Slider sfxVol,
            Button heroSpecial,
            TextMeshProUGUI heroCooldownLabel)
        {
            goldText = gold;
            livesText = lives;
            waveText = wave;
            nextWaveButton = nextWave;
            pauseButton = pause;
            pauseButtonText = pauseLabel;
            cancelPlacementButton = cancelPlacement;
            waveCountText = waveCount;
            gameOverPanel = gameOver;
            victoryPanel = victory;
            waveProgressBar = waveProgress;
            musicVolumeSlider = musicVol;
            sfxVolumeSlider = sfxVol;
            heroSpecialButton = heroSpecial;
            heroSpecialCooldownText = heroCooldownLabel;

            RegisterHudListeners();
            RegisterVolumeSliders();
            RegisterHeroSpecialButton();

            if (btnArcher != null)
                btnArcher.onClick.AddListener(() => TowerPlacer.Instance?.SelectTower(TowerType.Archer));
            if (btnMage != null)
                btnMage.onClick.AddListener(() => TowerPlacer.Instance?.SelectTower(TowerType.Mage));
            if (btnWarrior != null)
                btnWarrior.onClick.AddListener(() => TowerPlacer.Instance?.SelectTower(TowerType.Warrior));
            if (btnCatapult != null)
                btnCatapult.onClick.AddListener(() => TowerPlacer.Instance?.SelectTower(TowerType.Catapult));
            if (cancelPlacement != null)
                cancelPlacement.onClick.AddListener(() => TowerPlacer.Instance?.CancelPlacement());
        }

        private void RegisterHudListeners()
        {
            if (_listenersRegistered)
                return;
            _listenersRegistered = true;

            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.OnGoldChanged.AddListener(UpdateGold);
                gm.OnLivesChanged.AddListener(UpdateLives);
                gm.OnGameOver.AddListener(ShowGameOver);
                gm.OnVictory.AddListener(ShowVictory);
                UpdateGold(gm.CurrentGold);
                UpdateLives(gm.CurrentLives);
            }

            var wm = WaveManager.Instance;
            if (wm != null)
            {
                wm.OnWaveStarted.AddListener(UpdateWaveUI);
                wm.OnWaveCompleted.AddListener(OnWaveCompleted);
                wm.OnAllWavesCompleted.AddListener(OnAllWavesCompleted);
                wm.OnNextWaveUnlocked.AddListener(OnNextWaveUnlocked);
                wm.OnWaveClearProgress.AddListener(UpdateWaveProgressBar);
            }

            if (nextWaveButton != null)
                nextWaveButton.onClick.AddListener(OnNextWaveButtonPressed);
            if (pauseButton != null)
                pauseButton.onClick.AddListener(OnPauseButtonPressed);
        }

        private void RegisterVolumeSliders()
        {
            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.minValue = 0f;
                musicVolumeSlider.maxValue = 1f;
                musicVolumeSlider.SetValueWithoutNotify(0.4f);
                musicVolumeSlider.onValueChanged.AddListener(v => AudioManager.Instance?.SetMusicVolume(v));
                AudioManager.Instance?.SetMusicVolume(musicVolumeSlider.value);
            }
            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.minValue = 0f;
                sfxVolumeSlider.maxValue = 1f;
                sfxVolumeSlider.SetValueWithoutNotify(0.8f);
                sfxVolumeSlider.onValueChanged.AddListener(v => AudioManager.Instance?.SetSFXVolume(v));
                AudioManager.Instance?.SetSFXVolume(sfxVolumeSlider.value);
            }
        }

        private void RegisterHeroSpecialButton()
        {
            if (heroSpecialButton == null) return;
            heroSpecialButton.onClick.RemoveAllListeners();
            heroSpecialButton.onClick.AddListener(OnHeroSpecialAbilityPressed);
        }

        public void OnHeroSpecialAbilityPressed()
        {
            var hero = Object.FindAnyObjectByType<HeroBase>();
            hero?.UseSpecialAbility();
        }

        private void LateUpdate()
        {
            if (heroSpecialButton == null && heroSpecialCooldownText == null) return;
            var hero = Object.FindAnyObjectByType<HeroBase>();
            if (hero == null)
            {
                if (heroSpecialCooldownText != null) heroSpecialCooldownText.text = "";
                return;
            }
            if (!hero.IsAlive)
            {
                if (heroSpecialCooldownText != null) heroSpecialCooldownText.text = "";
                if (heroSpecialButton != null) heroSpecialButton.interactable = false;
                return;
            }
            var gm = GameManager.Instance;
            bool blockSpecial = gm != null && gm.IsGameOver;
            if (heroSpecialButton != null)
                heroSpecialButton.interactable = hero.SpecialAbilityReady && !blockSpecial;
            if (heroSpecialCooldownText != null)
            {
                heroSpecialCooldownText.text = hero.SpecialAbilityReady
                    ? "Yetenek hazır"
                    : $"{Mathf.Ceil(hero.SpecialCooldownRemaining)} sn";
            }
        }

        private void UpdateWaveProgressBar(float normalized)
        {
            if (waveProgressBar != null)
                waveProgressBar.normalizedValue = Mathf.Clamp01(normalized);
        }

        private void UpdateGold(int amount)
        {
            if (goldText != null)
                goldText.text = $"{amount}";
        }

        private void UpdateLives(int amount)
        {
            if (livesText != null)
                livesText.text = $"{amount}";

            if (livesText != null && amount <= 5)
                livesText.color = Color.red;
            else if (livesText != null)
                livesText.color = Color.white;
        }

        private void UpdateWaveUI(int waveNumber)
        {
            if (waveText != null)
                waveText.text = $"Wave {waveNumber}";

            if (nextWaveButton != null)
                nextWaveButton.interactable = false;

            if (waveProgressBar != null)
                waveProgressBar.normalizedValue = 0f;

            ShowWaveStartBanner(waveNumber);
        }

        private void ShowWaveStartBanner(int waveNumber)
        {
            Debug.Log($"--- WAVE {waveNumber} ---");
        }

        private void OnAllWavesCompleted()
        {
            if (nextWaveButton != null)
                nextWaveButton.interactable = false;
        }

        private void OnNextWaveUnlocked()
        {
            if (nextWaveButton == null || WaveManager.Instance == null) return;
            if (WaveManager.Instance.AllWavesCompleted) return;
            nextWaveButton.interactable = true;
        }

        public void OnNextWaveButtonPressed()
        {
            WaveManager.Instance?.StartNextWave();
            if (nextWaveButton != null)
                nextWaveButton.interactable = false;
        }

        public void OnPauseButtonPressed()
        {
            GameManager.Instance?.TogglePause();
            bool isPaused = GameManager.Instance?.IsPaused ?? false;
            if (pauseButtonText != null)
                pauseButtonText.text = isPaused ? "Resume" : "Pause";
        }

        public void OnWaveCompleted(int waveNumber)
        {
            PlayerProgress.RecordWaveCompleted(waveNumber);

            if (nextWaveButton != null)
                nextWaveButton.interactable = false;

            if (waveCountText != null && WaveManager.Instance != null)
                waveCountText.text = $"{waveNumber} / {WaveManager.Instance.TotalWaves}";
        }

        private void ShowGameOver()
        {
            gameOverPanel?.SetActive(true);
            AudioManager.Instance?.PlayGameOver();
        }

        private void ShowVictory()
        {
            victoryPanel?.SetActive(true);
            AudioManager.Instance?.PlayVictory();
        }

        public void OnRestartButton()
        {
            GameManager.Instance?.RestartGame();
        }

        public void ToggleTowerShop()
        {
            if (towerShopPanel != null)
                towerShopPanel.SetActive(!towerShopPanel.activeSelf);
        }

        public void ToggleHeroShop()
        {
            if (heroShopPanel != null)
                heroShopPanel.SetActive(!heroShopPanel.activeSelf);
        }
    }
}
