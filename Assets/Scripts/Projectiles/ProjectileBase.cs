using UnityEngine;
using DefenderChronicles.Enemies;

namespace DefenderChronicles.Projectiles
{
    public class ProjectileBase : MonoBehaviour
    {
        [Header("Projectile Settings")]
        [SerializeField] protected float speed = 8f;
        [SerializeField] protected float lifeTime = 5f;

        protected EnemyBase target;
        protected int damage;
        private bool hasHit = false;

        public void Initialize(EnemyBase targetEnemy, int dmg)
        {
            target = targetEnemy;
            damage = dmg;
            Destroy(gameObject, lifeTime);
        }

        protected virtual void Update()
        {
            if (hasHit || target == null || target.IsDead)
            {
                Destroy(gameObject);
                return;
            }

            MoveTowardTarget();
            CheckHit();
        }

        protected virtual void MoveTowardTarget()
        {
            Vector3 direction = (target.transform.position - transform.position).normalized;
            transform.position += direction * speed * Time.deltaTime;

            // Rotate to face movement direction
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        protected virtual void CheckHit()
        {
            if (Vector3.Distance(transform.position, target.transform.position) < 0.2f)
                OnHit();
        }

        protected virtual void OnHit()
        {
            if (hasHit) return;
            hasHit = true;
            target.TakeDamage(damage);
            OnHitEffect();
            Destroy(gameObject);
        }

        protected virtual void OnHitEffect()
        {
            // Override in subclasses for hit effects (particles, sounds)
        }
    }
}
