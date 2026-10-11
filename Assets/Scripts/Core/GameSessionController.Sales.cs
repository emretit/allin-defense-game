using DefenderChronicles.UI;
using UnityEngine;
using UnityEngine.UI;

namespace DefenderChronicles.Core
{
    public sealed partial class GameSessionController
    {
        private GreyboxOutpost legacySaleOutpost;
        private float legacySaleUntil;

        // The primitive editor HUD uses the same two-tap policy as the production HUD.
        private void SellFromLegacyUi()
        {
            if (!CanChangeDefence || selectedOutpost == null) return;
            if (legacySaleOutpost == selectedOutpost && Time.unscaledTime < legacySaleUntil)
            {
                CancelLegacySaleConfirmation();
                SellSelected();
                return;
            }
            legacySaleOutpost = selectedOutpost;
            legacySaleUntil = Time.unscaledTime + 3f;
            RefreshLegacySaleLabel();
        }

        private void CancelLegacySaleConfirmation()
        {
            legacySaleOutpost = null;
            RefreshLegacySaleLabel();
        }

        private void RefreshLegacySaleLabel()
        {
            if (sellButton == null) return;
            if (legacySaleOutpost != selectedOutpost || Time.unscaledTime >= legacySaleUntil ||
                selectedPanel == null || !selectedPanel.activeInHierarchy) legacySaleOutpost = null;
            var label = sellButton.GetComponentInChildren<Text>();
            if (label == null) return;
            label.text = legacySaleOutpost != null
                ? GameLocalization.Text($"ONAYLA +{legacySaleOutpost.SellValue}", $"CONFIRM +{legacySaleOutpost.SellValue}")
                : selectedOutpost != null
                    ? GameLocalization.Text($"SAT +{selectedOutpost.SellValue}", $"SELL +{selectedOutpost.SellValue}")
                    : GameLocalization.Text("SAT", "SELL");
        }
    }
}
