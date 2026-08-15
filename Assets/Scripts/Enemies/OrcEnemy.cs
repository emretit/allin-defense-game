using UnityEngine;
using DefenderChronicles.Enemies;
using DefenderChronicles.Utils;

namespace DefenderChronicles.Enemies
{
    // Tanky, medium speed, berserks at low health
    public class OrcEnemy : EnemyBase
    {
        [Header("Orc Specific")]
        [SerializeField] private float berserkHealthThreshold = 0.3f;
        [SerializeField] private float berserkSpeedMultiplier = 2f;
        private bool isBerserking = false;

        protected override void Update()
        {
            base.Update();
            CheckBerserk();
        }

        private void CheckBerserk()
        {
            if (!isBerserking && HealthPercent <= berserkHealthThreshold)
            {
                isBerserking = true;
                Data.moveSpeed *= berserkSpeedMultiplier;
                Debug.Log("Orc is BERSERKING!");
                SimpleVfx.Burst(transform.position, new Color(1f, 0.2f, 0.2f, 1f), 0.35f);
                var sr = GetComponent<SpriteRenderer>();
                if (sr != null) sr.color = new Color(1f, 0.45f, 0.45f);
            }
        }
    }
}
