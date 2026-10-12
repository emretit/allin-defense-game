using UnityEngine;

namespace AllinDefense.Enemies
{
    public class SupportEnemy : EnemyBase
    {
        [SerializeField] private float supportRadius = 2.2f;
        [SerializeField] private int healAmount = 8;
        [SerializeField] private float pulseInterval = 2.5f;
        [SerializeField] private LayerMask enemyLayer = ~0;

        private float pulseTimer;

        protected override void Update()
        {
            base.Update();
            if (Data == null || IsDead) return;

            pulseTimer -= Time.deltaTime;
            if (pulseTimer > 0f)
                return;

            pulseTimer = pulseInterval;
            PulseHeal();
        }

        private void PulseHeal()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, supportRadius, enemyLayer);
            foreach (var hit in hits)
            {
                var enemy = hit.GetComponent<EnemyBase>();
                if (enemy == null || enemy == this || enemy.IsDead)
                    continue;

                enemy.Heal(healAmount);
            }
        }
    }
}
