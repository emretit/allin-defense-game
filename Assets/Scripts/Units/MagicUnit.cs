using UnityEngine;
using DefenderChronicles.Enemies;

namespace DefenderChronicles.Units
{
    public class MagicUnit : FriendlyUnitBase
    {
        [SerializeField] private float splashRadius = 1.2f;
        [SerializeField] private float slowFactor = 0.65f;
        [SerializeField] private float slowDuration = 1.5f;

        protected override void Awake()
        {
            maxHealth = 60;
            damage = 6;
            moveSpeed = 2.6f;
            attackRange = 3.2f;
            attackSpeed = 0.9f;
            base.Awake();
        }

        protected override void TryAttack(EnemyBase target)
        {
            if (target == null)
                return;

            if (attackCooldown > 0f)
                return;

            Collider2D[] hits = Physics2D.OverlapCircleAll(target.transform.position, splashRadius, enemyLayer);
            foreach (var hit in hits)
            {
                var enemy = hit.GetComponent<EnemyBase>();
                if (enemy == null || enemy.IsDead)
                    continue;

                enemy.TakeDamage(damage);
                enemy.ApplySlow(slowFactor, slowDuration);
            }

            attackCooldown = 1f / Mathf.Max(0.1f, attackSpeed);
        }
    }
}
