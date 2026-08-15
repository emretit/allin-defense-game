using UnityEngine;
using DefenderChronicles.Towers;

namespace DefenderChronicles.Enemies
{
    public class SiegeEnemy : EnemyBase
    {
        [SerializeField] private float siegeRange = 2.0f;
        [SerializeField] private int towerDamage = 12;
        [SerializeField] private float attackInterval = 2f;
        [SerializeField] private LayerMask towerLayer = ~0;

        private float attackTimer;

        protected override void Update()
        {
            base.Update();
            if (Data == null || IsDead) return;

            attackTimer -= Time.deltaTime;
            if (attackTimer > 0f)
                return;

            var tower = FindTowerTarget();
            if (tower == null)
                return;

            tower.TakeDamage(towerDamage);
            attackTimer = attackInterval;
        }

        private TowerBase FindTowerTarget()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, siegeRange, towerLayer);
            TowerBase best = null;
            float bestDistance = float.MaxValue;

            foreach (var hit in hits)
            {
                var tower = hit.GetComponent<TowerBase>();
                if (tower == null)
                    continue;

                float distance = Vector3.Distance(transform.position, tower.transform.position);
                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                best = tower;
            }

            return best;
        }
    }
}
