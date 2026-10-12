namespace AllinDefense.Units
{
    public class RangedUnit : FriendlyUnitBase
    {
        protected override void Awake()
        {
            maxHealth = 70;
            damage = 8;
            moveSpeed = 2.9f;
            attackRange = 3.8f;
            attackSpeed = 1.4f;
            base.Awake();
        }
    }
}
