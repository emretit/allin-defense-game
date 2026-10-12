using UnityEngine;
using AllinDefense.Enemies;
using AllinDefense.Utils;

namespace AllinDefense.Towers
{
    // Long range, very high AoE damage, slow fire rate
    public class CatapultTower : TowerBase
    {
        [Header("Catapult Specific")]
        [SerializeField] private float explosionRadius = 2f;
        [SerializeField] private float stunDuration = 1f;

        protected override void Attack()
        {
            if (currentTarget == null) return;

            // Damage only from delayed AoE (avoids double-hit: projectile + explosion on same target).
            Vector3 targetPos = currentTarget.transform.position;
            StartCoroutine(DelayedExplosion(targetPos));
        }

        private System.Collections.IEnumerator DelayedExplosion(Vector3 pos)
        {
            // Wait for projectile travel time
            yield return new WaitForSeconds(0.5f);
            Explode(pos);
        }

        private void Explode(Vector3 position)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(position, explosionRadius, enemyLayer);
            foreach (var hit in hits)
            {
                var enemy = hit.GetComponent<EnemyBase>();
                if (enemy == null) continue;

                enemy.TakeDamage(towerData.damage);
                enemy.ApplySlow(0f, stunDuration); // 0 speed = stun
            }

            SimpleVfx.Burst(position, new Color(1f, 0.45f, 0.1f, 1f), 0.55f);
            AudioManager.Instance?.PlayExplosion();
            Debug.Log($"Catapult EXPLOSION at {position}, {hits.Length} enemies hit!");
        }

        protected override void OnUpgraded()
        {
            base.OnUpgraded();
            explosionRadius *= 1.15f;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
            if (currentTarget != null)
                Gizmos.DrawWireSphere(currentTarget.transform.position, explosionRadius);
        }
    }
}
