using DefenderChronicles.Core;
using UnityEngine;

namespace DefenderChronicles.Presentation
{
    public sealed class DefenderPresentationDirector : MonoBehaviour
    {
        private GameSessionController session;
        private DefenderPresentationCatalog catalog;
        private DefenderAudioController audioController;
        private DefenderVfxPool vfxPool;
        private DefenderPresentationFeedbackController feedbackController;

        public DefenderAudioController Audio => audioController;
        public DefenderVfxPool Vfx => vfxPool;
        public DefenderPresentationFeedbackController Feedback => feedbackController;

        public void Initialize(GameSessionController owner, DefenderPresentationCatalog value)
        {
            session = owner;
            catalog = value;
            audioController = gameObject.AddComponent<DefenderAudioController>();
            audioController.Initialize(catalog);
            vfxPool = gameObject.AddComponent<DefenderVfxPool>();
            vfxPool.Initialize(catalog);
            feedbackController = gameObject.AddComponent<DefenderPresentationFeedbackController>();
            feedbackController.Initialize(Camera.main);
            session.PresentationSignaled += OnPresentationSignal;
            session.PhaseChanged += OnPhaseChanged;
        }

        private void OnDestroy()
        {
            if (session == null) return;
            session.PresentationSignaled -= OnPresentationSignal;
            session.PhaseChanged -= OnPhaseChanged;
        }

        private void OnPresentationSignal(PresentationSignal signal)
        {
            audioController.Play(signal);
            feedbackController.Play(signal);
            if (signal.cue is PresentationCue.WaveStarted or PresentationCue.BossPhase or
                PresentationCue.Victory or PresentationCue.Defeat)
                DefenderSceneLook.Pulse(signal.cue);
            if (signal.cue is not (PresentationCue.UiClick or PresentationCue.UiError or PresentationCue.WaveStarted))
                vfxPool.Play(signal);

            StylizedActorView view = signal.source != null
                ? signal.source.GetComponent<StylizedActorView>()
                : null;
            switch (AnimationTriggerFor(signal))
            {
                case PresentationActorAnimationContract.AttackState:
                    view?.Attack();
                    break;
                case PresentationActorAnimationContract.HitState:
                    view?.Hit();
                    break;
                case PresentationActorAnimationContract.AbilityState:
                    view?.Ability();
                    break;
                case PresentationActorAnimationContract.PhaseState:
                    view?.Phase();
                    break;
                case PresentationActorAnimationContract.BossAttackState:
                    view?.BossAttack();
                    break;
                case PresentationActorAnimationContract.VictoryState:
                    session.Hero?.GetComponent<StylizedActorView>()?.Victory();
                    break;
            }

            if (signal.cue is PresentationCue.EnemyDefeated or PresentationCue.UnitDefeated)
            {
                GameObject prefab = catalog.ActorPrefab(signal.actor);
                StylizedActorView.PlayDefeatEcho(prefab, catalog, signal.position, catalog.ActorScale(signal.actor),
                    catalog.ActorAnimator(signal.actor));
            }
        }

        public static string AnimationTriggerFor(PresentationSignal signal) => signal.cue switch
        {
            PresentationCue.EnemyHit => PresentationActorAnimationContract.HitState,
            PresentationCue.HeroAttacked or PresentationCue.OutpostAttacked => PresentationActorAnimationContract.AttackState,
            PresentationCue.OutpostDisrupted => PresentationActorAnimationContract.HitState,
            PresentationCue.HeroAbility or PresentationCue.HealPulse => PresentationActorAnimationContract.AbilityState,
            PresentationCue.BossPhase => PresentationActorAnimationContract.PhaseState,
            PresentationCue.BossAttack => PresentationActorAnimationContract.BossAttackState,
            PresentationCue.Victory => PresentationActorAnimationContract.VictoryState,
            PresentationCue.EnemyDefeated or PresentationCue.UnitDefeated => PresentationActorAnimationContract.DefeatState,
            _ => null
        };

        private void OnPhaseChanged(GamePhase phase)
        {
            audioController.SetPaused(phase == GamePhase.Paused);
            if (phase == GamePhase.Paused) return;
            MusicState state = phase switch
            {
                GamePhase.Preparing or GamePhase.Intermission => MusicState.Preparation,
                GamePhase.Victory => MusicState.Victory,
                GamePhase.Defeat => MusicState.Defeat,
                GamePhase.Wave when session.NextWaveHasBoss => MusicState.Boss,
                GamePhase.Wave when session.CurrentWaveIndex >= 4 => MusicState.Intense,
                _ => MusicState.Combat
            };
            audioController.SetMusic(state);
        }
    }
}
