namespace AllinDefense.Core
{
    public sealed partial class GameSessionController
    {
        public bool IntroModalOpen { get; private set; }
        private bool introOwnsPause;

        public bool BeginIntroModal()
        {
            if (!initialized || IntroModalOpen ||
                Phase is not (GamePhase.Preparing or GamePhase.Intermission or GamePhase.Wave or GamePhase.Paused)) return false;
            IntroModalOpen = true;
            introOwnsPause = Phase != GamePhase.Paused;
            if (introOwnsPause) OnPausePressed();
            return true;
        }

        public void EndIntroModal()
        {
            if (!IntroModalOpen) return;
            IntroModalOpen = false;
            bool resume = introOwnsPause && Phase == GamePhase.Paused;
            introOwnsPause = false;
            if (resume) OnPausePressed();
        }

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
