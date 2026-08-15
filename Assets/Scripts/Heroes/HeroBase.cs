using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using DefenderChronicles.Data;
using DefenderChronicles.Utils;
using DefenderChronicles.Enemies;
using DefenderChronicles.Grid;

namespace DefenderChronicles.Heroes
{
    // Heroes are special units (Defender Chronicles 2 style)
    // They gain XP, level up, and have unique special abilities
    public class HeroBase : MonoBehaviour
    {
        [Header("Hero Data")]
        [SerializeField] protected HeroData heroData;

        [Header("Events")]
        public UnityEvent<int> OnLevelUp;
        public UnityEvent OnSpecialAbilityUsed;

        [Header("Components")]
        [SerializeField] protected SpriteRenderer spriteRenderer;
        [SerializeField] protected float moveSpeed = 3f;

        protected int currentHealth;
        protected int currentExperience;
        protected float attackCooldown;
        protected float specialAbilityCooldown = 0f;
        protected bool isDead = false;
        private Vector3 commandPoint;

        [Header("Special Ability")]
        [SerializeField] protected float specialAbilityMaxCooldown = 15f;
        [SerializeField] protected LayerMask enemyLayer;

        public HeroData Data => heroData;
        public int CurrentHealth => currentHealth;

        private void Awake()
        {
            if (OnLevelUp == null) OnLevelUp = new UnityEvent<int>();
            if (OnSpecialAbilityUsed == null) OnSpecialAbilityUsed = new UnityEvent();
        }

        public void ApplyRuntimeHeroData(HeroData data)
        {
            if (data != null)
                heroData = GameDataClone.Hero(data);
        }

        public void ConfigureCombatLayers(LayerMask enemies) => enemyLayer = enemies;
        public float HealthPercent => heroData != null && heroData.maxHealth > 0 ? (float)currentHealth / heroData.maxHealth : 0f;
        public int Level => heroData != null ? heroData.level : 0;
        public int Experience => currentExperience;
        public float SpecialAbilityProgress =>
            specialAbilityMaxCooldown > 0.001f
                ? 1f - Mathf.Clamp01(specialAbilityCooldown / specialAbilityMaxCooldown)
                : 1f;

        public float SpecialCooldownRemaining => Mathf.Max(0f, specialAbilityCooldown);
        public bool SpecialAbilityReady => specialAbilityCooldown <= 0f;
        public bool IsAlive => !isDead;
        public Vector3 CurrentCommandPointPosition => commandPoint;

        protected virtual void Start()
        {
            if (heroData == null)
            {
                enabled = false;
                Debug.LogWarning($"{nameof(HeroBase)} disabled on {name}: missing hero data.");
                return;
            }

            currentHealth = heroData.maxHealth;
            currentExperience = 0;
            commandPoint = transform.position;
        }

        protected virtual void Update()
        {
            if (heroData == null) return;
            if (isDead) return;

            if (attackCooldown > 0) attackCooldown -= Time.deltaTime;
            if (specialAbilityCooldown > 0) specialAbilityCooldown -= Time.deltaTime;

            MoveTowardCommandPoint();
            AttackNearbyEnemies();
        }

        public virtual void CommandTo(CommandPoint point)
        {
            if (point == null)
                return;

            commandPoint = point.Position;
        }

        public virtual void CommandTo(Vector3 point)
        {
            commandPoint = point;
        }

        protected virtual void AttackNearbyEnemies()
        {
            if (heroData == null) return;
            if (attackCooldown > 0) return;

            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, heroData.attackRange, enemyLayer);
            EnemyBase closestEnemy = null;
            float closestDist = float.MaxValue;

            foreach (var hit in hits)
            {
                var enemy = hit.GetComponent<EnemyBase>();
                if (enemy == null || enemy.IsDead) continue;

                float dist = Vector3.Distance(transform.position, enemy.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestEnemy = enemy;
                }
            }

            if (closestEnemy != null)
            {
                PerformAttack(closestEnemy);
                attackCooldown = 1f / heroData.attackSpeed;
            }
        }

        protected virtual void PerformAttack(EnemyBase target)
        {
            if (target == null || heroData == null) return;
            bool wasAlive = target != null && !target.IsDead;
            target.TakeDamage(heroData.damage);
            if (wasAlive && target.IsDead)
                GainExperience(Mathf.Max(1, target.Data.goldReward));
        }

        public virtual void UseSpecialAbility()
        {
            if (heroData == null) return;
            if (specialAbilityCooldown > 0) return;
            specialAbilityCooldown = specialAbilityMaxCooldown;
            OnSpecialAbilityUsed?.Invoke();
            ExecuteSpecialAbility();
        }

        protected virtual void ExecuteSpecialAbility()
        {
            // Override in subclasses
        }

        public void GainExperience(int amount)
        {
            currentExperience += amount;
            CheckLevelUp();
        }

        private void CheckLevelUp()
        {
            if (heroData == null) return;
            if (heroData.level >= 10) return;
            if (currentExperience >= heroData.experienceToNextLevel)
            {
                currentExperience -= heroData.experienceToNextLevel;
                heroData.level++;
                heroData.experienceToNextLevel = Mathf.RoundToInt(heroData.experienceToNextLevel * 1.5f);
                ApplyLevelUpBonus();
                OnLevelUp?.Invoke(heroData.level);
                AudioManager.Instance?.PlayLevelUp();
                Debug.Log($"{heroData.heroName} leveled up to {heroData.level}!");
            }
        }

        protected virtual void ApplyLevelUpBonus()
        {
            heroData.damage = Mathf.RoundToInt(heroData.damage * 1.2f);
            heroData.maxHealth = Mathf.RoundToInt(heroData.maxHealth * 1.15f);
            currentHealth = heroData.maxHealth;
        }

        public virtual void TakeDamage(int damage)
        {
            if (heroData == null) return;
            if (isDead) return;
            currentHealth -= damage;

            if (currentHealth <= 0)
            {
                isDead = true;
                StartCoroutine(RespawnSequence());
            }
        }

        private IEnumerator RespawnSequence()
        {
            if (spriteRenderer != null)
                spriteRenderer.color = new Color(1, 1, 1, 0.3f);

            yield return new WaitForSeconds(10f);

            isDead = false;
            currentHealth = heroData.maxHealth;

            if (spriteRenderer != null)
                spriteRenderer.color = Color.white;

            Debug.Log($"{heroData.heroName} has respawned!");
        }

        protected virtual void MoveTowardCommandPoint()
        {
            if (Vector3.Distance(transform.position, commandPoint) <= 0.05f)
                return;

            transform.position = Vector3.MoveTowards(transform.position, commandPoint, moveSpeed * Time.deltaTime);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, heroData?.attackRange ?? 3f);
        }
    }
}
