using UnityEngine;
using DefenderChronicles.Enemies;

namespace DefenderChronicles.Towers
{
    // Melee range, very high damage, slows nearby enemies
    public class WarriorTower : TowerBase
    {
        [Header("Warrior Specific")]
        [SerializeField] private float blockChance = 0.2f;
        [SerializeField] private float auraSlowRadius = 2f;
        [SerializeField] private float auraSlow = 0.7f;

        private void Start()
        {
            // Warrior has melee range by default (close range)
            towerData.attackRange = 1.5f;
        }

        protected override void Attack()
        {
            if (currentTarget == null) return;
            currentTarget.TakeDamage(towerData.damage);
            ApplyAuraSlow();
        }

        private void ApplyAuraSlow()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, auraSlowRadius, enemyLayer);
            foreach (var hit in hits)
            {
                var enemy = hit.GetComponent<EnemyBase>();
                enemy?.ApplySlow(auraSlow, 0.5f);
            }
        }

        public bool TryBlock()
        {
            return Random.value < blockChance;
        }
    }
}
