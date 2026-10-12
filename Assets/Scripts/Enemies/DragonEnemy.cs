using System.Collections;
using UnityEngine;
using AllinDefense.Enemies;
using AllinDefense.Utils;

namespace AllinDefense.Enemies
{
    // Boss enemy - flies, breathing fire in area
    public class DragonEnemy : EnemyBase
    {
        [Header("Dragon Specific")]
        [SerializeField] private float fireBreathRadius = 2f;
        [SerializeField] private int fireBreathDamage = 10;
        [SerializeField] private float fireBreathCooldown = 5f;
        [SerializeField] private LayerMask towerLayer;

        protected override void Start()
        {
            base.Start();
            StartCoroutine(FireBreathRoutine());
        }

        private IEnumerator FireBreathRoutine()
        {
            while (!IsDead)
            {
                yield return new WaitForSeconds(fireBreathCooldown);
                FireBreath();
            }
        }

        private void FireBreath()
        {
            // Deal damage to nearby towers
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, fireBreathRadius, towerLayer);
            foreach (var hit in hits)
            {
                var tower = hit.GetComponent<Towers.TowerBase>();
                tower?.TakeDamage(fireBreathDamage);
            }
            SimpleVfx.Burst(transform.position, new Color(1f, 0.35f, 0f, 1f), 0.5f);
            AudioManager.Instance?.PlayFireball();
            Debug.Log($"Dragon breathes fire! Hit {hits.Length} towers.");
        }

        protected override void MoveAlongPath()
        {
            // Dragons fly slightly above the path - hover effect
            base.MoveAlongPath();
            transform.position = new Vector3(
                transform.position.x,
                transform.position.y + Mathf.Sin(Time.time * 2f) * 0.05f,
                transform.position.z);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, fireBreathRadius);
        }
    }
}
