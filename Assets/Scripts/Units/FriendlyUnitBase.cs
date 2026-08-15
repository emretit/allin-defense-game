using UnityEngine;
using DefenderChronicles.Enemies;

namespace DefenderChronicles.Units
{
    public class FriendlyUnitBase : MonoBehaviour
    {
        [Header("Unit Stats")]
        [SerializeField] protected int maxHealth = 80;
        [SerializeField] protected int damage = 8;
        [SerializeField] protected float moveSpeed = 3f;
        [SerializeField] protected float attackRange = 0.8f;
        [SerializeField] protected float attackSpeed = 1f;
        [SerializeField] protected LayerMask enemyLayer = ~0;

        private Vector3 rallyPoint;
        private int currentHealth;
        protected float attackCooldown;
        private bool isDefeated;

        public bool IsDefeated => isDefeated;
        public float HealthFraction => maxHealth > 0 ? Mathf.Clamp01((float)currentHealth / maxHealth) : 0f;

        protected virtual void Awake()
        {
            currentHealth = maxHealth;
            rallyPoint = transform.position;
        }

        protected virtual void Update()
        {
            if (isDefeated)
                return;

            if (attackCooldown > 0f)
                attackCooldown -= Time.deltaTime;

            var target = FindTarget();
            if (target != null && IsInAttackRange(target))
            {
                TryAttack(target);
                return;
            }

            MoveTowardRallyPoint();
        }

        public void AssignRallyPoint(Vector3 point)
        {
            rallyPoint = point;
        }

        public virtual void TakeDamage(int amount)
        {
            if (isDefeated)
                return;

            currentHealth = Mathf.Clamp(currentHealth - Mathf.Max(0, amount), 0, maxHealth);
            if (currentHealth <= 0)
                Defeat();
        }

        public virtual void ResetUnit(Vector3 spawnPosition, Vector3 point)
        {
            transform.position = spawnPosition;
            rallyPoint = point;
            currentHealth = maxHealth;
            attackCooldown = 0f;
            isDefeated = false;
            gameObject.SetActive(true);
        }

        protected virtual EnemyBase FindTarget()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, attackRange, enemyLayer);
            EnemyBase best = null;
            float bestProgress = float.MinValue;

            foreach (var hit in hits)
            {
                var enemy = hit.GetComponent<EnemyBase>();
                if (enemy == null || enemy.IsDead)
                    continue;

                if (enemy.RouteProgress <= bestProgress)
                    continue;

                bestProgress = enemy.RouteProgress;
                best = enemy;
            }

            return best;
        }

        protected virtual bool IsInAttackRange(EnemyBase target)
        {
            return target != null && Vector3.Distance(transform.position, target.transform.position) <= attackRange;
        }

        protected virtual void TryAttack(EnemyBase target)
        {
            if (attackCooldown > 0f)
                return;

            target.TakeDamage(damage);
            attackCooldown = 1f / Mathf.Max(0.1f, attackSpeed);
        }

        protected virtual void MoveTowardRallyPoint()
        {
            if (Vector3.Distance(transform.position, rallyPoint) <= 0.05f)
                return;

            transform.position = Vector3.MoveTowards(transform.position, rallyPoint, moveSpeed * Time.deltaTime);
        }

        protected virtual void Defeat()
        {
            isDefeated = true;
            gameObject.SetActive(false);
        }
    }
}
