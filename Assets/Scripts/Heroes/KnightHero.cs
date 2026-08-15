using UnityEngine;
using DefenderChronicles.Enemies;
using DefenderChronicles.Heroes;
using DefenderChronicles.Utils;

namespace DefenderChronicles.Heroes
{
    // Knight Hero: Tanky, special = Shield Bash (stun + damage)
    public class KnightHero : HeroBase
    {
        [Header("Knight Special - Shield Bash")]
        [SerializeField] private float bashRadius = 2f;
        [SerializeField] private float stunDuration = 2f;
        [SerializeField] private int bashDamageMultiplier = 3;

        protected override void ExecuteSpecialAbility()
        {
            Debug.Log("Knight Hero: SHIELD BASH!");
            SimpleVfx.Burst(transform.position, new Color(0.4f, 0.7f, 1f, 1f), 0.5f);
            AudioManager.Instance?.PlayExplosion();
            var hits = Physics2D.OverlapCircleAll(transform.position, bashRadius, enemyLayer);
            foreach (var hit in hits)
            {
                var enemy = hit.GetComponent<EnemyBase>();
                if (enemy == null) continue;
                enemy.TakeDamage(heroData.damage * bashDamageMultiplier);
                enemy.ApplySlow(0f, stunDuration);
            }
        }

        protected override void ApplyLevelUpBonus()
        {
            base.ApplyLevelUpBonus();
            heroData.maxHealth = Mathf.RoundToInt(heroData.maxHealth * 1.1f);
        }
    }
}
