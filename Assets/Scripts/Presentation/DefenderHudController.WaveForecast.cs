using System;
using AllinDefense.Core;
using AllinDefense.UI;
using AllinDefense.UI.Kit;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AllinDefense.Presentation
{
    public sealed partial class DefenderHudController
    {
        private RectTransform forecastStrip;
        private TMP_Text forecastLabel;
        private Image forecastIcon, waveCountdownRing;
        private UiOverlay restartConfirmation;
        private Button returnToGame;
        private string forecastValue = string.Empty;
        private float forecastAvailableWidth;
        public bool RestartConfirmationOpen => restartConfirmation != null && restartConfirmation.IsOpen;

        private void InitializeWaveForecast(Action restart, Action click)
        {
            previewText.transform.parent.gameObject.SetActive(false);
            forecastStrip = UiKit.Node(transform.Find("TopBar"), "WaveForecast");
            UiKit.Size(forecastStrip).ignoreLayout = true;
            forecastStrip.anchorMin = forecastStrip.anchorMax = new Vector2(.5f, 0);
            forecastStrip.pivot = new Vector2(.5f, 1);
            forecastStrip.anchoredPosition = new Vector2(0, -UiTokens.Space1);
            forecastStrip.sizeDelta = new Vector2(0, UiTokens.Space4);
            frameKit.Decoration(forecastStrip, "WaveForecast.Surface", frameKit.Assets.roundedSmall, UiTokens.HudPanel);
            var layout = UiKit.Layout(forecastStrip, false, UiTokens.Space1);
            layout.padding.top = layout.padding.bottom = 0;
            layout.childForceExpandWidth = false;
            layout.childAlignment = TextAnchor.MiddleLeft;
            forecastIcon = frameKit.Icon(forecastStrip, "WaveForecast.Icon", frameKit.Assets.iconEnemy, UiTokens.Space2);
            forecastLabel = frameKit.Text(forecastStrip, "WaveForecast.Label", "", UiTokens.TypeCompact);
            UiKit.Size(forecastLabel.rectTransform, flex: 1);
            waveCountdownRing = frameKit.Decoration(nextWaveButton.transform, "WaveCountdownRing", frameKit.Assets.cooldownRing, UiTokens.Gold);
            waveCountdownRing.type = Image.Type.Filled;
            waveCountdownRing.fillMethod = Image.FillMethod.Radial360;
            waveCountdownRing.fillOrigin = (int)Image.Origin360.Top;
            waveCountdownRing.fillClockwise = true;
            waveCountdownRing.rectTransform.anchorMin = waveCountdownRing.rectTransform.anchorMax = new Vector2(1, .5f);
            waveCountdownRing.rectTransform.sizeDelta = Vector2.one * UiTokens.Space3;
            waveCountdownRing.rectTransform.anchoredPosition = new Vector2(-UiTokens.Space2, 0);
            // Reserve the right edge for the clock without shrinking the touch target.
            nextWaveLabel.rectTransform.offsetMax += new Vector2(-UiTokens.Space3, 0);

            restartConfirmation = frameKit.Modal(transform, "RestartConfirmation", FrontendCopy.PauseRestart);
            restartConfirmation.DismissOnScrim = false;
            var restartCard = restartConfirmation.transform.Find("RestartConfirmation.Card");
            restartCard.Find("RestartConfirmation.Close").gameObject.SetActive(false);
            var title = restartCard.Find("RestartConfirmation.Title").GetComponent<TMP_Text>();
            title.fontSize = title.fontSizeMin = title.fontSizeMax = 20;
            title.enableAutoSizing = false;
            UiKit.Size(title.rectTransform, height: 30);
            var warning = frameKit.Text(restartConfirmation.Body, "RestartWarning", FrontendCopy.WaveRestartWarning, 14);
            warning.textWrappingMode = TextWrappingModes.Normal;
            UiKit.Size(warning.rectTransform, height: UiTokens.Space8 * 2);
            returnToGame = frameKit.Button(restartConfirmation.Body, "ReturnToGame", FrontendCopy.WaveReturnToGame, UiButtonVariant.Gold);
            restartConfirmation.SetFirstFocus(returnToGame.GetComponent<UiAccessibleButton>());
            var confirm = frameKit.Button(restartConfirmation.Body, "ConfirmRestart", FrontendCopy.PauseRestart, UiButtonVariant.Glass);
            foreach (var button in new[] { returnToGame, confirm })
            {
                var label = button.GetComponentInChildren<TMP_Text>();
                label.enableAutoSizing = false;
                label.fontSize = label.fontSizeMin = label.fontSizeMax = 14;
            }
            Bind(returnToGame, () => restartConfirmation.Hide(() => pauseResumeButton.onClick.Invoke()), click);
            Bind(confirm, () => { restartConfirmation.Hide(); restart?.Invoke(); }, click);
            Bind(pauseModal.Body.Find("PauseRestart").GetComponent<Button>(), () =>
            {
                restartConfirmation.transform.SetAsLastSibling();
                restartConfirmation.Show();
                if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(returnToGame.gameObject);
            }, click);
            restartConfirmation.gameObject.SetActive(false);
        }

        private void RefreshWaveForecast(GameSessionController session)
        {
            previewText.transform.parent.gameObject.SetActive(false);
            bool waiting = session.UnpausedPhase is GamePhase.Preparing or GamePhase.Intermission;
            forecastStrip.gameObject.SetActive(waiting && session.UpcomingEnemyCount > 0);
            float availableWidth = ((RectTransform)forecastStrip.parent).rect.width;
            if (forecastLabel.text != forecastValue || !Mathf.Approximately(forecastAvailableWidth, availableWidth))
            {
                forecastLabel.text = forecastValue;
                forecastAvailableWidth = availableWidth;
                float contentWidth = forecastLabel.GetPreferredValues(forecastValue).x + UiTokens.Space2 + 3 * UiTokens.Space1;
                forecastStrip.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Min(contentWidth, availableWidth * .4f));
            }
            forecastLabel.color = forecastIcon.color = session.NextWaveHasBoss ? UiTokens.Danger : UiTokens.TextSecondary;
            bool countdown = session.UnpausedPhase == GamePhase.Intermission;
            waveCountdownRing.gameObject.SetActive(countdown);
            waveCountdownRing.fillAmount = countdown ? 1 - Mathf.Clamp01(session.IntermissionRemaining / session.IntermissionDuration) : 0;
            bool pulse = countdown && session.Phase != GamePhase.Paused && session.IntermissionRemaining <= 3 && !FrontendPreferences.ReducedMotion;
            waveCountdownRing.transform.localScale = Vector3.one * (pulse ? 1 + .04f * Mathf.Sin(Time.unscaledTime * Mathf.PI * 2) : 1);
        }
    }
}
