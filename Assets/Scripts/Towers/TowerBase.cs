using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AllinDefense.Core;
using AllinDefense.Data;
using AllinDefense.Enemies;
using AllinDefense.Projectiles;
using AllinDefense.Utils;

namespace AllinDefense.Towers
{
    public class TowerBase : MonoBehaviour
    {
        [Header("Tower Data")]
        [SerializeField] protected TowerData towerData;

        [Header("Components")]
        [SerializeField] protected Transform firePoint;
        [SerializeField] protected SpriteRenderer towerRenderer;
        [SerializeField] protected GameObject projectilePrefab;

        [Header("Targeting")]
        [SerializeField] protected LayerMask enemyLayer;
        [SerializeField] private TargetingStrategy targetingStrategy = TargetingStrategy.First;

        protected EnemyBase currentTarget;
        protected float attackCooldown = 0f;
        private int currentHealth = 100;
        private int maxHealth = 100;

        public enum TargetingStrategy { First, Last, Strongest, Weakest, Closest }

        protected virtual void Awake()
        {
            if (towerData != null)
                towerData = GameDataClone.Tower(towerData);
        }

        public TowerData Data => towerData;
        public int Level => towerData != null ? towerData.level : 0;
        public float HealthFraction => maxHealth > 0 ? Mathf.Clamp01((float)currentHealth / maxHealth) : 0f;

        protected virtual void Update()
        {
            if (towerData == null) return;

            if (attackCooldown > 0)
                attackCooldown -= Time.deltaTime;

            FindTarget();

            if (currentTarget != null && attackCooldown <= 0)
            {
                Attack();
                attackCooldown = 1f / towerData.attackSpeed;
            }
        }

        protected virtual void FindTarget()
        {
            if (towerData == null) return;

            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, towerData.attackRange, enemyLayer);

            if (hits.Length == 0)
            {
                currentTarget = null;
                return;
            }

            EnemyBase best = null;
            float bestValue = float.MinValue;

            foreach (var hit in hits)
            {
                var enemy = hit.GetComponent<EnemyBase>();
                if (enemy == null || enemy.IsDead) continue;

                float value = targetingStrategy switch
                {
                    TargetingStrategy.First    => enemy.RouteProgress,
                    TargetingStrategy.Last     => -enemy.RouteProgress,
                    TargetingStrategy.Strongest => enemy.CurrentHealth,
                    TargetingStrategy.Weakest  => -enemy.CurrentHealth,
                    TargetingStrategy.Closest  => -Vector3.Distance(transform.position, enemy.transform.position),
                    _ => 0
                };

                if (value > bestValue)
                {
                    bestValue = value;
                    best = enemy;
                }
            }

            currentTarget = best;
        }

        protected virtual void Attack()
        {
            if (currentTarget == null || projectilePrefab == null) return;

            Vector3 origin = firePoint != null ? firePoint.position : transform.position;
            var proj = Instantiate(projectilePrefab, origin, Quaternion.identity);
            var projComp = proj.GetComponent<ProjectileBase>();
            if (projComp != null)
            {
                projComp.Initialize(currentTarget, towerData.damage);
            }

            switch (towerData.projectileType)
            {
                case ProjectileType.Arrow:
                    AudioManager.Instance?.PlayArrowShoot();
                    break;
                case ProjectileType.Fireball:
                    AudioManager.Instance?.PlayFireball();
                    break;
                default:
                    AudioManager.Instance?.PlayArrowShoot();
                    break;
            }

            OnAttack();
        }

        protected virtual void OnAttack()
        {
            // Override in subclasses for custom attack effects
        }

        public virtual bool CanUpgrade()
        {
            return towerData != null && towerData.level < 3 && towerData.upgradeCost > 0;
        }

        public virtual bool Upgrade()
        {
            if (!CanUpgrade()) return false;
            if (!GameManager.Instance.SpendGold(towerData.upgradeCost)) return false;

            towerData.level++;
            towerData.damage = Mathf.RoundToInt(towerData.damage * 1.4f);
            towerData.attackSpeed *= 1.2f;
            towerData.attackRange *= 1.1f;
            towerData.upgradeCost = Mathf.RoundToInt(towerData.upgradeCost * 1.5f);

            OnUpgraded();
            return true;
        }

        protected virtual void OnUpgraded()
        {
            Debug.Log($"{towerData.towerName} upgraded to level {towerData.level}!");
        }

        public virtual void TakeDamage(int damage)
        {
            currentHealth = Mathf.Clamp(currentHealth - damage, 0, maxHealth);
            if (currentHealth <= 0)
                Destroy(gameObject);
        }

        public virtual int GetSellValue()
        {
            if (towerData == null) return 0;
            return Mathf.RoundToInt(towerData.cost * 0.6f);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, towerData?.attackRange ?? 3f);
        }
    }
}
