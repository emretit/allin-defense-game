namespace AllinDefense.Presentation
{
    public static class PresentationActorAnimationContract
    {
        public const string MovingParameter = "Moving";

        public const string IdleState = "Idle";
        public const string MoveState = "Move";
        public const string AttackState = "Attack";
        public const string HitState = "Hit";
        public const string DefeatState = "Defeat";
        public const string AbilityState = "Ability";
        public const string VictoryState = "Victory";
        public const string PhaseState = "Phase";
        public const string BossAttackState = "BossAttack";

        public static readonly string[] ClipNames =
        {
            IdleState,
            MoveState,
            AttackState,
            HitState,
            DefeatState,
            AbilityState,
            VictoryState,
            PhaseState,
            BossAttackState
        };

        public static readonly string[] TriggerParameters =
        {
            AttackState,
            HitState,
            DefeatState,
            AbilityState,
            VictoryState,
            PhaseState,
            BossAttackState
        };
    }
}
