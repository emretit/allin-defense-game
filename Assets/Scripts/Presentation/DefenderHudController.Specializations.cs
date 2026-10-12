using AllinDefense.Core;
using AllinDefense.Data;
using System;
using AllinDefense.UI;

namespace AllinDefense.Presentation
{
    public sealed partial class DefenderHudController
    {
        private GameSessionController specializationSession;

        public void ShowSpecializationChoices(GreyboxOutpost outpost, GameSessionController session)
        {
            if (outpost == null || session == null || !outpost.CanSpecialize) return;
            SetSelected(outpost, outpost.Slot, true);
            BeginRadialSpecialization(session);
            RefreshRadialCard();
        }

        private void BeginRadialSpecialization(GameSessionController session)
        {
            if (session == null || specializationSession != null) return;
            if (!session.BeginSpecializationModal()) return;
            CancelSaleConfirmation();
            specializationSession = session;
            session.PhaseChanged += HandleSpecializationPhaseChanged;
        }

        private void HandleSpecializationPhaseChanged(GamePhase phase)
        {
            if (phase != GamePhase.Paused) CloseSpecializationChoices();
        }

        public void CloseSpecializationChoices()
        {
            var session = specializationSession;
            specializationSession = null;
            if (session != null)
            {
                if (Enum.TryParse(previewOption, out OutpostSpecialization branch) && branch != OutpostSpecialization.None)
                {
                    previewOption = null;
                    battleRadial.ResetPreview();
                    radialRange?.Show(UnityEngine.Vector3.zero, 1, false);
                }
                session.PhaseChanged -= HandleSpecializationPhaseChanged;
                session.EndSpecializationModal();
            }
        }
    }
}
