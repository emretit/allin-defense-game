namespace DefenderChronicles.Units
{
    public class MeleeUnit : FriendlyUnitBase
    {
        protected override void Awake()
        {
            maxHealth = 120;
            damage = 10;
            moveSpeed = 2.7f;
            attackRange = 0.75f;
            attackSpeed = 1.1f;
            base.Awake();
        }
    }
}
