using UnityEngine;
using UnityEngine.Events;

namespace AllinDefense.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Game Settings")]
        [SerializeField] private int startingGold = 200;
        [SerializeField] private int startingLives = 20;

        [Header("Events")]
        public UnityEvent<int> OnGoldChanged;
        public UnityEvent<int> OnLivesChanged;
        public UnityEvent OnGameOver;
        public UnityEvent OnVictory;

        private int currentGold;
        private int currentLives;
        private bool isGameOver;
        private bool isPaused;
        private float currentGameSpeed = 1f;

        public int CurrentGold => currentGold;
        public int CurrentLives => currentLives;
        public bool IsGameOver => isGameOver;
        public bool IsPaused => isPaused;
        public float CurrentGameSpeed => currentGameSpeed;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            currentGold = startingGold;
            currentLives = startingLives;
            isGameOver = false;
            isPaused = false;
            currentGameSpeed = 1f;
            if (OnGoldChanged == null) OnGoldChanged = new UnityEvent<int>();
            if (OnLivesChanged == null) OnLivesChanged = new UnityEvent<int>();
            if (OnGameOver == null) OnGameOver = new UnityEvent();
            if (OnVictory == null) OnVictory = new UnityEvent();
        }

        private void Start()
        {
            OnGoldChanged?.Invoke(currentGold);
            OnLivesChanged?.Invoke(currentLives);
        }

        public bool SpendGold(int amount)
        {
            if (currentGold < amount) return false;
            currentGold -= amount;
            OnGoldChanged?.Invoke(currentGold);
            return true;
        }

        public void EarnGold(int amount)
        {
            currentGold += amount;
            PlayerProgress.RecordGoldEarned(amount);
            OnGoldChanged?.Invoke(currentGold);
        }

        public void LoseLife(int amount = 1)
        {
            if (isGameOver) return;
            currentLives -= amount;
            currentLives = Mathf.Max(0, currentLives);
            OnLivesChanged?.Invoke(currentLives);

            if (currentLives <= 0)
                TriggerGameOver();
        }

        public void TriggerVictory()
        {
            if (isGameOver) return;
            isGameOver = true;
            Time.timeScale = 0f;
            OnVictory?.Invoke();
        }

        private void TriggerGameOver()
        {
            isGameOver = true;
            Time.timeScale = 0f;
            OnGameOver?.Invoke();
        }

        public void TogglePause()
        {
            isPaused = !isPaused;
            Time.timeScale = isPaused ? 0f : currentGameSpeed;
        }

        public void SetGameSpeed(float speed)
        {
            currentGameSpeed = Mathf.Approximately(speed, 2f) ? 2f : 1f;
            if (!isPaused && !isGameOver)
                Time.timeScale = currentGameSpeed;
        }

        public void RestartGame()
        {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }
    }
}
