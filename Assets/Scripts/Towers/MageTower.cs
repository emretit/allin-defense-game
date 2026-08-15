using System.Collections;
using UnityEngine;
using DefenderChronicles.Enemies;
using DefenderChronicles.Utils;

namespace DefenderChronicles.Towers
{
    // AoE splash damage, slow effect on hit
    public class MageTower : TowerBase
    {
        [Header("Mage Specific")]
        [SerializeField] private float splashRadius = 1.5f;
        [SerializeField] private float slowFactor = 0.5f;
        [SerializeField] private float slowDuration = 2f;

        protected override void Attack()
        {
            if (currentTarget == null) return;

            // AoE damage at target location
            Vector3 targetPos = currentTarget.transform.position;
            Collider2D[] hits = Physics2D.OverlapCircleAll(targetPos, splashRadius, enemyLayer);

            foreach (var hit in hits)
            {
                var enemy = hit.GetComponent<EnemyBase>();
                if (enemy == null) continue;

                enemy.TakeDamage(towerData.damage);
                enemy.ApplySlow(slowFactor, slowDuration);
            }

            SpawnSplashEffect(targetPos);
        }

        private void SpawnSplashEffect(Vector3 position)
        {
            SimpleVfx.Burst(position, new Color(0.6f, 0.2f, 1f, 1f), 0.45f);
            AudioManager.Instance?.PlayFireball();
            Debug.Log($"Mage tower AOE at {position}, hits within radius {splashRadius}");
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.5f, 0f, 1f, 0.3f);
            if (currentTarget != null)
                Gizmos.DrawWireSphere(currentTarget.transform.position, splashRadius);
        }
    }
}
