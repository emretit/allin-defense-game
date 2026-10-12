using System;
using AllinDefense.Core;
using AllinDefense.UI;
using UnityEngine;

namespace AllinDefense.Presentation
{
    public sealed partial class DefenderHudController
    {
        private const float SaleConfirmationSeconds = 3f;
        private GreyboxOutpost displayedOutpost;
        private GreyboxOutpost saleConfirmationOutpost;
        private float saleConfirmationUntil;
        private GamePhase saleConfirmationPhase;
        private Action sellAction;
        private GameSessionController saleSession;

        private void OnEnable()
        {
            saleSession = GameSessionController.Instance;
            if (saleSession != null) saleSession.PhaseChanged += CancelSaleOnPhaseChange;
        }

        private void CancelSaleOnPhaseChange(GamePhase phase) => CancelSaleConfirmation();

        private void SellFromUi()
        {
            var session = GameSessionController.Instance;
            if (displayedOutpost == null || session == null ||
                session.Phase is GamePhase.Paused or GamePhase.Victory or GamePhase.Defeat) return;
            if (saleConfirmationOutpost == displayedOutpost && Time.unscaledTime < saleConfirmationUntil &&
                saleConfirmationPhase == session.Phase)
            {
                CancelSaleConfirmation();
                sellAction?.Invoke();
                return;
            }
            saleConfirmationOutpost = displayedOutpost;
            saleConfirmationUntil = Time.unscaledTime + SaleConfirmationSeconds;
            saleConfirmationPhase = session.Phase;
            ApplySaleConfirmationLabel();
        }

        private void CancelSaleConfirmation()
        {
            saleConfirmationOutpost = null;
            if (sellButton != null && displayedOutpost != null)
                SetButtonLabel(sellButton, FrontendCopy.RadialSell.Value);
        }

        private void ApplySaleConfirmationLabel()
        {
            if (saleConfirmationOutpost != null && sellButton != null)
                SetButtonLabel(sellButton, FrontendCopy.RadialConfirm.Value);
        }

        private void LateUpdate()
        {
            UpdateTutorialPresentation();
            DismissPausedRadialOnBackground();
            var session = GameSessionController.Instance;
            if (saleConfirmationOutpost != null && (Time.unscaledTime >= saleConfirmationUntil ||
                selectedPanel == null || !selectedPanel.activeInHierarchy || session == null ||
                session.Phase != saleConfirmationPhase)) CancelSaleConfirmation();
        }

        private void OnDisable()
        {
            if (saleSession != null) saleSession.PhaseChanged -= CancelSaleOnPhaseChange;
            saleSession = null;
            CloseSpecializationChoices();
            CancelSaleConfirmation();
        }
    }
}
