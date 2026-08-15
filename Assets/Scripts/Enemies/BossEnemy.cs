using UnityEngine;
using DefenderChronicles.Towers;

namespace DefenderChronicles.Enemies
{
    public class BossEnemy : EnemyBase
    {
        [SerializeField] private float disruptionRange = 2.6f;
        [SerializeField] private int towerDamage = 20;
        [SerializeField] private float disruptionInterval = 5f;
        [SerializeField] private LayerMask towerLayer = ~0;

        private int phase = 1;
        private float disruptionTimer;

        protected override void Update()
        {
            base.Update();
            if (Data == null || IsDead) return;
            UpdatePhase();
            TryDisruptOutpost();
        }

        private void UpdatePhase()
        {
            int nextPhase = HealthPercent <= 0.33f ? 3 : HealthPercent <= 0.66f ? 2 : 1;
            if (nextPhase == phase)
                return;

            phase = nextPhase;
            Data.moveSpeed *= phase == 2 ? 1.1f : 1.15f;
        }

        private void TryDisruptOutpost()
        {
            disruptionTimer -= Time.deltaTime;
            if (disruptionTimer > 0f)
                return;

            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, disruptionRange, towerLayer);
            foreach (var hit in hits)
            {
                var tower = hit.GetComponent<TowerBase>();
                if (tower == null)
                    continue;

                tower.TakeDamage(towerDamage);
                disruptionTimer = disruptionInterval;
                return;
            }
        }
    }
}
