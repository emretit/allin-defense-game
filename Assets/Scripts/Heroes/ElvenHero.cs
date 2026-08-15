using System.Collections;
using UnityEngine;
using DefenderChronicles.Enemies;
using DefenderChronicles.Heroes;

namespace DefenderChronicles.Heroes
{
    // Elven Hero: Fast attacker, special = Rain of Arrows (AoE)
    public class ElvenHero : HeroBase
    {
        [Header("Elven Special - Rain of Arrows")]
        [SerializeField] private int arrowCount = 8;
        [SerializeField] private float arrowDamageMultiplier = 0.7f;

        protected override void ExecuteSpecialAbility()
        {
            StartCoroutine(RainOfArrows());
        }

        private IEnumerator RainOfArrows()
        {
            Debug.Log("Elven Hero: RAIN OF ARROWS!");
            var hits = Physics2D.OverlapCircleAll(transform.position, heroData.attackRange * 1.5f, enemyLayer);

            for (int i = 0; i < arrowCount; i++)
            {
                if (hits.Length > 0)
                {
                    var randomEnemy = hits[Random.Range(0, hits.Length)].GetComponent<EnemyBase>();
                    randomEnemy?.TakeDamage(Mathf.RoundToInt(heroData.damage * arrowDamageMultiplier));
                }
                yield return new WaitForSeconds(0.1f);
            }
        }

        protected override void ApplyLevelUpBonus()
        {
            base.ApplyLevelUpBonus();
            heroData.attackSpeed *= 1.1f;
        }
    }
}
