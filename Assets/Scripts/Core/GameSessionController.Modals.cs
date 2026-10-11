namespace DefenderChronicles.Core
{
    public sealed partial class GameSessionController
    {
        public bool SpecializationModalOpen { get; private set; }
        private bool specializationOwnsPause;

        public bool BeginSpecializationModal()
        {
            if (!initialized || SpecializationModalOpen ||
                Phase is not (GamePhase.Preparing or GamePhase.Intermission or GamePhase.Wave or GamePhase.Paused)) return false;
            specializationOwnsPause = Phase != GamePhase.Paused;
            if (specializationOwnsPause) OnPausePressed();
            SpecializationModalOpen = true;
            return true;
        }

        public void EndSpecializationModal()
        {
            if (!SpecializationModalOpen) return;
            SpecializationModalOpen = false;
            bool resume = specializationOwnsPause && Phase == GamePhase.Paused;
            specializationOwnsPause = false;
            if (resume) OnPausePressed();
        }
    }
}
