using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using AllinDefense.Data;
using AllinDefense.Enemies;

namespace AllinDefense.Core
{
    public class WaveManager : MonoBehaviour
    {
        public static WaveManager Instance { get; private set; }

        [Header("Wave Settings")]
        [SerializeField] private WaveData[] waves;
        [SerializeField] private float timeBetweenWaves = 10f;

        [Header("Enemy Prefabs")]
        [SerializeField] private GameObject goblinPrefab;
        [SerializeField] private GameObject orcPrefab;
        [SerializeField] private GameObject trollPrefab;
        [SerializeField] private GameObject dragonPrefab;

        [Header("Events")]
        public UnityEvent<int> OnWaveStarted;
        public UnityEvent<int> OnWaveCompleted;
        public UnityEvent OnAllWavesCompleted;
        /// <summary>Fired after <see cref="timeBetweenWaves"/> when more waves remain (enables Next Wave UI).</summary>
        public UnityEvent OnNextWaveUnlocked;
        /// <summary>Normalized cleared fraction of the current wave (enemies removed / planned spawns).</summary>
        public UnityEvent<float> OnWaveClearProgress;

        private int currentWaveIndex = 0;
        private int activeEnemyCount = 0;
        private bool isSpawning = false;
        private bool allWavesCompleted = false;

        public int CurrentWave => currentWaveIndex + 1;
        public int TotalWaves => waves != null ? waves.Length : 0;
        public bool IsSpawning => isSpawning;
        public bool AllWavesCompleted => allWavesCompleted;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (OnWaveStarted == null) OnWaveStarted = new UnityEvent<int>();
            if (OnWaveCompleted == null) OnWaveCompleted = new UnityEvent<int>();
            if (OnAllWavesCompleted == null) OnAllWavesCompleted = new UnityEvent();
            if (OnNextWaveUnlocked == null) OnNextWaveUnlocked = new UnityEvent();
            if (OnWaveClearProgress == null) OnWaveClearProgress = new UnityEvent<float>();
        }

        private int _waveSpawnPlanned;
        private int _waveSpawnRemoved;

        public int ActiveEnemyCount => activeEnemyCount;

        private void TryLoadWavesFromWaveSet()
        {
            if (waves != null && waves.Length > 0) return;
            var set = Resources.Load<WaveSet>("Data/DefaultWaveSet");
            if (set != null && set.Waves != null && set.Waves.Length > 0)
                waves = set.Waves;
        }

        /// <summary>Loads enemy prefabs from Resources and default waves if Inspector is empty.</summary>
        public void ApplyRuntimeDefaultsFromResources()
        {
            TryLoadWavesFromWaveSet();

            if (goblinPrefab == null)
                goblinPrefab = Resources.Load<GameObject>("Prefabs/Enemies/GoblinEnemy");
            if (orcPrefab == null)
                orcPrefab = Resources.Load<GameObject>("Prefabs/Enemies/OrcEnemy");
            if (trollPrefab == null)
                trollPrefab = Resources.Load<GameObject>("Prefabs/Enemies/TrollEnemy");
            if (dragonPrefab == null)
                dragonPrefab = Resources.Load<GameObject>("Prefabs/Enemies/DragonEnemy");

            if (waves != null && waves.Length > 0) return;

            waves = new[]
            {
                new WaveData
                {
                    waveNumber = 1,
                    timeBetweenSpawns = 0.55f,
                    goldBonus = 25,
                    enemies = new[]
                    {
                        new EnemySpawnInfo { enemyType = EnemyType.Goblin, count = 8, spawnDelay = 0f }
                    }
                },
                new WaveData
                {
                    waveNumber = 2,
                    timeBetweenSpawns = 0.45f,
                    goldBonus = 40,
                    enemies = new[]
                    {
                        new EnemySpawnInfo { enemyType = EnemyType.Goblin, count = 5, spawnDelay = 0f },
                        new EnemySpawnInfo { enemyType = EnemyType.Orc, count = 3, spawnDelay = 0.2f }
                    }
                },
                new WaveData
                {
                    waveNumber = 3,
                    timeBetweenSpawns = 0.4f,
                    goldBonus = 60,
                    enemies = new[]
                    {
                        new EnemySpawnInfo { enemyType = EnemyType.Orc, count = 4, spawnDelay = 0f },
                        new EnemySpawnInfo { enemyType = EnemyType.Troll, count = 1, spawnDelay = 0.5f },
                        new EnemySpawnInfo { enemyType = EnemyType.Goblin, count = 6, spawnDelay = 0.3f }
                    }
                },
                new WaveData
                {
                    waveNumber = 4,
                    timeBetweenSpawns = 0.35f,
                    goldBonus = 100,
                    enemies = new[]
                    {
                        new EnemySpawnInfo { enemyType = EnemyType.Troll, count = 1, spawnDelay = 0f },
                        new EnemySpawnInfo { enemyType = EnemyType.Dragon, count = 1, spawnDelay = 1f }
                    }
                }
            };
        }

        public void StartNextWave()
        {
            if (waves == null || waves.Length == 0) return;
            if (currentWaveIndex >= waves.Length || isSpawning) return;
            StartCoroutine(SpawnWave(waves[currentWaveIndex]));
        }

        private IEnumerator SpawnWave(WaveData wave)
        {
            if (wave == null)
            {
                Debug.LogError("Cannot start wave: WaveData is missing.");
                yield break;
            }

            isSpawning = true;
            ResetWaveSpawnTracking(wave);
            OnWaveStarted?.Invoke(wave.waveNumber);

            if (wave.enemies != null)
            {
                foreach (var spawnInfo in wave.enemies)
                {
                    if (spawnInfo == null || spawnInfo.count <= 0) continue;
                    yield return new WaitForSeconds(Mathf.Max(0f, spawnInfo.spawnDelay));

                    for (int i = 0; i < spawnInfo.count; i++)
                    {
                        if (SpawnEnemy(spawnInfo.enemyType))
                            activeEnemyCount++;
                        else
                            NotifyEnemyRemovedFromWave();
                        yield return new WaitForSeconds(Mathf.Max(0f, wave.timeBetweenSpawns));
                    }
                }
            }

            isSpawning = false;
            TryCompleteWaveIfClear();
        }

        private bool SpawnEnemy(EnemyType type)
        {
            var path = PathManager.Instance?.GetPath();
            if (path == null || path.Count == 0) return false;

            GameObject prefab = type switch
            {
                EnemyType.Goblin  => goblinPrefab,
                EnemyType.Orc     => orcPrefab,
                EnemyType.Troll   => trollPrefab,
                EnemyType.Dragon  => dragonPrefab,
                _                 => goblinPrefab
            };

            if (prefab == null) return false;

            var enemy = Instantiate(prefab, path[0], Quaternion.identity);
            var enemyComp = enemy.GetComponent<EnemyBase>();
            if (enemyComp == null)
            {
                Destroy(enemy);
                return false;
            }

            enemyComp.OnEnemyDied += HandleEnemyDied;
            enemyComp.OnEnemyReachedEnd += HandleEnemyReachedEnd;
            return true;
        }

        public void HandleEnemyDied(EnemyBase enemy)
        {
            enemy.OnEnemyDied -= HandleEnemyDied;
            enemy.OnEnemyReachedEnd -= HandleEnemyReachedEnd;
            activeEnemyCount = Mathf.Max(0, activeEnemyCount - 1);
            NotifyEnemyRemovedFromWave();

            TryCompleteWaveIfClear();
        }

        private void HandleEnemyReachedEnd(EnemyBase enemy)
        {
            enemy.OnEnemyDied -= HandleEnemyDied;
            enemy.OnEnemyReachedEnd -= HandleEnemyReachedEnd;
            activeEnemyCount = Mathf.Max(0, activeEnemyCount - 1);
            NotifyEnemyRemovedFromWave();

            TryCompleteWaveIfClear();
        }

        private void ResetWaveSpawnTracking(WaveData wave)
        {
            _waveSpawnPlanned = 0;
            if (wave.enemies != null)
            {
                foreach (var s in wave.enemies)
                    if (s != null) _waveSpawnPlanned += Mathf.Max(0, s.count);
            }
            _waveSpawnRemoved = 0;
            OnWaveClearProgress?.Invoke(0f);
        }

        private void NotifyEnemyRemovedFromWave()
        {
            if (_waveSpawnPlanned <= 0) return;
            _waveSpawnRemoved++;
            float t = Mathf.Clamp01((float)_waveSpawnRemoved / _waveSpawnPlanned);
            OnWaveClearProgress?.Invoke(t);
        }

        private void TryCompleteWaveIfClear()
        {
            if (!isSpawning && activeEnemyCount <= 0)
                CompleteWave();
        }

        private void CompleteWave()
        {
            var wave = waves[currentWaveIndex];
            GameManager.Instance?.EarnGold(wave.goldBonus);
            OnWaveCompleted?.Invoke(wave.waveNumber);

            currentWaveIndex++;

            if (currentWaveIndex >= waves.Length)
            {
                allWavesCompleted = true;
                OnAllWavesCompleted?.Invoke();
                GameManager.Instance?.TriggerVictory();
                return;
            }

            if (timeBetweenWaves <= 0f)
                OnNextWaveUnlocked?.Invoke();
            else
                StartCoroutine(UnlockNextWaveAfterCooldown());
        }

        private IEnumerator UnlockNextWaveAfterCooldown()
        {
            yield return new WaitForSeconds(timeBetweenWaves);
            OnNextWaveUnlocked?.Invoke();
        }
    }
}
