using UnityEngine;
using AllinDefense.Enemies;
using AllinDefense.Projectiles;
using AllinDefense.Utils;

namespace AllinDefense.Towers
{
    // Fast attack speed, single target, medium damage
    public class ArcherTower : TowerBase
    {
        [Header("Archer Specific")]
        [SerializeField] private float critChance = 0.1f;
        [SerializeField] private float critMultiplier = 2f;

        protected override void Attack()
        {
            if (currentTarget == null || projectilePrefab == null) return;

            int damage = towerData.damage;
            bool isCrit = Random.value < critChance;
            Vector3 origin = firePoint != null ? firePoint.position : transform.position;
            if (isCrit)
            {
                damage = Mathf.RoundToInt(damage * critMultiplier);
                SimpleVfx.Burst(origin, Color.yellow, 0.2f);
                Debug.Log("CRITICAL HIT!");
            }

            var proj = Instantiate(projectilePrefab, origin, Quaternion.identity);
            var projComp = proj.GetComponent<ProjectileBase>();
            if (projComp != null)
                projComp.Initialize(currentTarget, damage);

            AudioManager.Instance?.PlayArrowShoot();
            OnAttack();
        }

        protected override void OnAttack()
        {
            // Rotate tower toward target
            if (currentTarget != null && firePoint != null)
            {
                Vector3 dir = currentTarget.transform.position - firePoint.position;
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                firePoint.rotation = Quaternion.Euler(0, 0, angle);
            }
        }
    }
}
