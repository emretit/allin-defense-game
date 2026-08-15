using System.Collections;
using UnityEngine;
using DefenderChronicles.Enemies;
using DefenderChronicles.Heroes;

namespace DefenderChronicles.Heroes
{
    // Wizard Hero: AoE mage, special = Blizzard (freeze all in range)
    public class WizardHero : HeroBase
    {
        [Header("Wizard Special - Blizzard")]
        [SerializeField] private float blizzardRadius = 4f;
        [SerializeField] private float freezeDuration = 3f;
        [SerializeField] private int blizzardTickDamage = 15;
        [SerializeField] private int blizzardTicks = 5;

        protected override void ExecuteSpecialAbility()
        {
            StartCoroutine(Blizzard());
        }

        private IEnumerator Blizzard()
        {
            Debug.Log("Wizard Hero: BLIZZARD!");
            for (int tick = 0; tick < blizzardTicks; tick++)
            {
                var hits = Physics2D.OverlapCircleAll(transform.position, blizzardRadius, enemyLayer);
                foreach (var hit in hits)
                {
                    var enemy = hit.GetComponent<EnemyBase>();
                    if (enemy == null) continue;
                    enemy.TakeDamage(blizzardTickDamage);
                    enemy.ApplySlow(0.1f, freezeDuration);
                }
                yield return new WaitForSeconds(0.5f);
            }
        }

        protected override void PerformAttack(EnemyBase target)
        {
            // Wizard hits nearby enemies with chain lightning
            var nearby = Physics2D.OverlapCircleAll(target.transform.position, 1.5f, enemyLayer);
            foreach (var hit in nearby)
            {
                var enemy = hit.GetComponent<EnemyBase>();
                enemy?.TakeDamage(Mathf.RoundToInt(heroData.damage * 0.6f));
            }
            target.TakeDamage(heroData.damage);
            GainExperience(target.Data.goldReward / 2);
        }
    }
}
