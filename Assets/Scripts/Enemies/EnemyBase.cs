using System;
using System.Collections;
using UnityEngine;
using AllinDefense.Combat;
using AllinDefense.Core;
using AllinDefense.Data;

namespace AllinDefense.Enemies
{
    public class EnemyBase : MonoBehaviour
    {
        [Header("Stats")]
        [SerializeField] protected EnemyData enemyData;

        [Header("Components")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private GameObject healthBarPrefab;

        public event Action<EnemyBase> OnEnemyDied;
        public event Action<EnemyBase> OnEnemyReachedEnd;

        protected int currentHealth;
        protected int currentWaypointIndex = 0;
        private bool isDead = false;
        private bool reachedEnd = false;

        private Transform healthBarTransform;
        private UnityEngine.UI.Slider healthSlider;

        private float _speedMultiplier = 1f;

        public int CurrentHealth => currentHealth;
        public float HealthPercent => enemyData != null && enemyData.maxHealth > 0 ? (float)currentHealth / enemyData.maxHealth : 0f;
        public bool IsDead => isDead;
        public EnemyData Data => enemyData;
        public int CurrentWaypointIndex => currentWaypointIndex;
        public float RouteProgress => PathManager.Instance != null
            ? PathManager.Instance.GetRouteProgress(transform.position, currentWaypointIndex)
            : transform.position.x;

        protected virtual void Awake()
        {
            if (enemyData != null)
                enemyData = GameDataClone.Enemy(enemyData);
        }

        protected virtual void Start()
        {
            if (enemyData == null)
            {
                enabled = false;
                Debug.LogWarning($"{nameof(EnemyBase)} disabled on {name}: missing enemy data.");
                return;
            }

            currentHealth = enemyData.maxHealth;
            SpawnHealthBar();
        }

        protected virtual void Update()
        {
            if (enemyData == null) return;
            if (isDead || reachedEnd) return;
            MoveAlongPath();
        }

        protected virtual void MoveAlongPath()
        {
            var pathManager = PathManager.Instance;
            if (pathManager == null) return;

            if (currentWaypointIndex >= pathManager.GetWaypointCount())
            {
                HandleReachedEnd();
                return;
            }

            Vector3 target = pathManager.GetWaypointAt(currentWaypointIndex);
            Vector3 direction = (target - transform.position).normalized;
            transform.position += direction * (enemyData.moveSpeed * _speedMultiplier) * Time.deltaTime;

            // Flip sprite based on movement direction
            if (spriteRenderer != null && direction.x != 0)
                spriteRenderer.flipX = direction.x < 0;

            if (Vector3.Distance(transform.position, target) < 0.1f)
                currentWaypointIndex++;
        }

        protected virtual void HandleReachedEnd()
        {
            if (reachedEnd) return;
            reachedEnd = true;
            GameManager.Instance?.LoseLife(enemyData.damage);
            OnEnemyReachedEnd?.Invoke(this);
            Destroy(gameObject);
        }

        public virtual void TakeDamage(int damage)
        {
            if (isDead) return;
            var result = DamageResolver.Resolve(new DamageRequest(damage));
            currentHealth -= result.FinalDamage;
            currentHealth = Mathf.Clamp(currentHealth, 0, enemyData.maxHealth);
            UpdateHealthBar();

            StartCoroutine(FlashRed());

            if (currentHealth <= 0)
                Die();
        }

        public virtual void Heal(int amount)
        {
            if (isDead || amount <= 0) return;
            currentHealth = Mathf.Clamp(currentHealth + amount, 0, enemyData.maxHealth);
            UpdateHealthBar();
        }

        protected virtual void Die()
        {
            if (isDead) return;
            isDead = true;
            GameManager.Instance?.EarnGold(enemyData.goldReward);
            AllinDefense.Utils.AudioManager.Instance?.PlayEnemyDie();
            OnEnemyDied?.Invoke(this);

            // Death animation could go here
            StartCoroutine(DeathSequence());
        }

        private IEnumerator DeathSequence()
        {
            if (spriteRenderer != null)
            {
                float elapsed = 0f;
                Color startColor = spriteRenderer.color;
                while (elapsed < 0.3f)
                {
                    elapsed += Time.deltaTime;
                    spriteRenderer.color = Color.Lerp(startColor, Color.clear, elapsed / 0.3f);
                    yield return null;
                }
            }
            Destroy(gameObject);
        }

        private IEnumerator FlashRed()
        {
            if (spriteRenderer == null) yield break;
            Color original = spriteRenderer.color;
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = original;
        }

        private void SpawnHealthBar()
        {
            if (healthBarPrefab == null) return;
            var hb = Instantiate(healthBarPrefab, transform);
            hb.transform.localPosition = new Vector3(0, 0.7f, 0);
            healthSlider = hb.GetComponentInChildren<UnityEngine.UI.Slider>();
            healthBarTransform = hb.transform;
        }

        private void UpdateHealthBar()
        {
            if (healthSlider != null)
                healthSlider.value = HealthPercent;
        }

        public virtual void ApplySlow(float slowFactor, float duration)
        {
            _ = slowFactor;
            _ = duration;
            _speedMultiplier = 1f;
        }
    }
}
