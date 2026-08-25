using System;
using UnityEngine;

namespace DefenderChronicles.Presentation
{
    public readonly struct PresentationAnimationFeedback
    {
        public readonly PresentationCue cue;
        public readonly Vector3 position;
        public readonly GameObject source;
        public readonly PresentationActorKind actor;

        public PresentationAnimationFeedback(PresentationCue cue, Vector3 position, GameObject source,
            PresentationActorKind actor)
        {
            this.cue = cue;
            this.position = position;
            this.source = source;
            this.actor = actor;
        }
    }

    public sealed class PresentationAnimationEventRelay : MonoBehaviour
    {
        private GameObject source;
        private PresentationActorKind actor;

        public event Action<PresentationAnimationFeedback> FeedbackSignaled;

        public void Configure(GameObject value, PresentationActorKind kind)
        {
            source = value;
            actor = kind;
        }

        public void Impact(string cueName)
        {
            if (!Enum.TryParse(cueName, out PresentationCue cue) || !IsPresentationOnlyCue(cue)) return;
            FeedbackSignaled?.Invoke(new PresentationAnimationFeedback(cue,
                source != null ? source.transform.position : transform.position, source, actor));
        }

        public static bool IsPresentationOnlyCue(PresentationCue cue) => cue is
            PresentationCue.EnemyHit or
            PresentationCue.EnemyDefeated or
            PresentationCue.UnitSpawned or
            PresentationCue.UnitDefeated or
            PresentationCue.HeroAttacked or
            PresentationCue.HeroAbility or
            PresentationCue.OutpostAttacked or
            PresentationCue.MageImpact or
            PresentationCue.HealPulse or
            PresentationCue.OutpostDisrupted or
            PresentationCue.SlowApplied or
            PresentationCue.BossPhase or
            PresentationCue.BossAttack or
            PresentationCue.Victory or
            PresentationCue.Defeat;
    }
}
