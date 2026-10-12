using UnityEngine;
using AllinDefense.Enemies;
using AllinDefense.Utils;
using AllinDefense.Projectiles;

namespace AllinDefense.Projectiles
{
    public class FireballProjectile : ProjectileBase
    {
        [Header("Fireball Specific")]
        [SerializeField] private float splashRadius = 1.5f;
        [SerializeField] private LayerMask enemyLayer;
        [SerializeField] private GameObject explosionEffect;

        private void Awake()
        {
            speed = 7f;
        }

        protected override void OnHit()
        {
            // Override to do AoE instead of single target
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, splashRadius, enemyLayer);
            foreach (var hit in hits)
            {
                var enemy = hit.GetComponent<EnemyBase>();
                enemy?.TakeDamage(damage);
            }
            OnHitEffect();
            Destroy(gameObject);
        }

        protected override void OnHitEffect()
        {
            if (explosionEffect != null)
            {
                var effect = Instantiate(explosionEffect, transform.position, Quaternion.identity);
                Destroy(effect, 1f);
            }
            SimpleVfx.Burst(transform.position, new Color(1f, 0.4f, 0.05f, 1f), 0.4f);
            AudioManager.Instance?.PlayExplosion();
        }
    }
}
