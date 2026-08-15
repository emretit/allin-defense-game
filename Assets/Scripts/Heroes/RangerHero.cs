using UnityEngine;
using DefenderChronicles.Enemies;
using DefenderChronicles.Heroes;

namespace DefenderChronicles.Heroes
{
    // Ranger Hero: Long range sniper, special = Explosive Shot
    public class RangerHero : HeroBase
    {
        [Header("Ranger Special - Explosive Shot")]
        [SerializeField] private float explosionRadius = 2.5f;
        [SerializeField] private int explosionDamageMultiplier = 5;

        protected override void Start()
        {
            base.Start();
            if (heroData == null) return;
            heroData.attackRange *= 1.5f; // Extra range
        }

        protected override void ExecuteSpecialAbility()
        {
            var hits = Physics2D.OverlapCircleAll(transform.position, heroData.attackRange, enemyLayer);

            // Find the strongest enemy in range
            EnemyBase bestTarget = null;
            int maxHp = 0;
            foreach (var hit in hits)
            {
                var enemy = hit.GetComponent<EnemyBase>();
                if (enemy == null) continue;
                if (enemy.CurrentHealth > maxHp)
                {
                    maxHp = enemy.CurrentHealth;
                    bestTarget = enemy;
                }
            }

            if (bestTarget == null) return;

            Debug.Log("Ranger Hero: EXPLOSIVE SHOT!");
            int damage = heroData.damage * explosionDamageMultiplier;
            bestTarget.TakeDamage(damage);

            // Explosion
            var splashHits = Physics2D.OverlapCircleAll(bestTarget.transform.position, explosionRadius, enemyLayer);
            foreach (var hit in splashHits)
            {
                var enemy = hit.GetComponent<EnemyBase>();
                if (enemy != bestTarget)
                    enemy?.TakeDamage(Mathf.RoundToInt(damage * 0.5f));
            }
        }
    }
}
