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

        public DefenderAudioController Audio => audioController;
        public DefenderVfxPool Vfx => vfxPool;

        public void Initialize(GameSessionController owner, DefenderPresentationCatalog value)
        {
            session = owner;
            catalog = value;
            audioController = gameObject.AddComponent<DefenderAudioController>();
            audioController.Initialize(catalog);
            vfxPool = gameObject.AddComponent<DefenderVfxPool>();
            vfxPool.Initialize(catalog);
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
            audioController.Play(signal.cue);
            if (signal.cue is not (PresentationCue.UiClick or PresentationCue.UiError or PresentationCue.WaveStarted))
                vfxPool.Play(signal);

            StylizedActorView view = signal.source != null
                ? signal.source.GetComponent<StylizedActorView>()
                : null;
            if (signal.cue is PresentationCue.EnemyHit) view?.Hit();
            if (signal.cue is PresentationCue.HeroAttacked or PresentationCue.OutpostAttacked or PresentationCue.BossAttack)
                view?.Attack();
            if (signal.cue == PresentationCue.OutpostDisrupted) view?.Disabled();

            if (signal.cue is PresentationCue.EnemyDefeated or PresentationCue.UnitDefeated)
            {
                GameObject prefab = catalog.ActorPrefab(signal.actor);
                StylizedActorView.PlayDefeatEcho(prefab, catalog, signal.position, catalog.ActorScale(signal.actor));
            }
        }

        private void OnPhaseChanged(GamePhase phase)
        {
            audioController.SetPaused(phase == GamePhase.Paused);
            if (phase == GamePhase.Paused) return;
            MusicState state = phase switch
            {
                GamePhase.Preparing or GamePhase.Intermission => MusicState.Preparation,
                GamePhase.Victory => MusicState.Victory,
                GamePhase.Defeat => MusicState.Defeat,
                GamePhase.Wave when session.CurrentWaveIndex >= session.WaveCount - 1 => MusicState.Boss,
                GamePhase.Wave when session.CurrentWaveIndex >= 4 => MusicState.Intense,
                _ => MusicState.Combat
            };
            audioController.SetMusic(state);
        }
    }
}
