using System.Collections;
using UnityEngine;
using DefenderChronicles.Enemies;

namespace DefenderChronicles.Enemies
{
    // Very slow, very tanky, regenerates health
    public class TrollEnemy : EnemyBase
    {
        [Header("Troll Specific")]
        [SerializeField] private int regenAmountPerSecond = 5;
        [SerializeField] private float regenInterval = 1f;

        protected override void Start()
        {
            base.Start();
            StartCoroutine(RegenerateHealth());
        }

        private IEnumerator RegenerateHealth()
        {
            while (!IsDead)
            {
                yield return new WaitForSeconds(regenInterval);
                if (!IsDead && CurrentHealth < Data.maxHealth)
                {
                    int healAmount = Mathf.Min(regenAmountPerSecond, Data.maxHealth - CurrentHealth);
                    Heal(healAmount);
                }
            }
        }

        public override void TakeDamage(int damage)
        {
            // Trolls take less damage from physical attacks, more from fire
            base.TakeDamage(damage);
        }
    }
}
