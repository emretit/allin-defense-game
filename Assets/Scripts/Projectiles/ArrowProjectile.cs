using UnityEngine;
using DefenderChronicles.Projectiles;

namespace DefenderChronicles.Projectiles
{
    public class ArrowProjectile : ProjectileBase
    {
        [Header("Arrow Specific")]
        [SerializeField] private TrailRenderer trail;

        private void Awake()
        {
            speed = 12f;
        }

        protected override void OnHitEffect()
        {
            if (trail != null) trail.transform.parent = null;
        }
    }
}
