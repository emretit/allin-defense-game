using UnityEngine;
using AllinDefense.Enemies;
using AllinDefense.Utils;

namespace AllinDefense.Enemies
{
    // Fast but fragile - rushes through the path quickly
    public class GoblinEnemy : EnemyBase
    {
        [Header("Goblin Specific")]
        [SerializeField] private float dodgeChance = 0.15f;

        public override void TakeDamage(int damage)
        {
            // Chance to dodge attacks
            if (Random.value < dodgeChance)
            {
                ShowDodgeText();
                return;
            }
            base.TakeDamage(damage);
        }

        private void ShowDodgeText()
        {
            SimpleVfx.Burst(transform.position + Vector3.up * 0.35f, Color.cyan, 0.25f);
            Debug.Log("Goblin dodged!");
        }
    }
}
