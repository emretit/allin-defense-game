using System;
using System.Linq;
using AllinDefense.Core;
using AllinDefense.Data;
using AllinDefense.UI;
using AllinDefense.UI.Kit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AllinDefense.Presentation
{
    public sealed partial class DefenderHudController
    {
        private UiKit frameKit;
        private UiStatPill goldPill, livesPill;
        private UiRadialCooldown radialCooldown;
        private UiProgressSegments waveProgress;
        private UiOverlay pauseModal, mapConfirmation;
        private UiSwitch musicSwitch, effectsSwitch;
        private GameObject bossBar;
        private Image bossFill;
        private TMP_Text bossName, bossHealth;
        private bool criticalLives, frameValuesReady;
        private GreyboxEnemy shownBoss;
        private int segmentCount;
        private int countdownSecond = -1, returnSecond = -1;
        private GameLanguage countdownLanguage, returnLanguage, bossLanguage;
        private float musicBeforeMute = 1, effectsBeforeMute = 1;
        public string HeroDisplayLabel => heroText.text;
        public bool MapConfirmationOpen => mapConfirmation != null && mapConfirmation.gameObject.activeSelf;

        private void CacheFrameModal()
        {
            pauseModal = Find<UiOverlay>("PauseOverlay");
            mapConfirmation = Find<UiOverlay>("MapConfirmation");
            pauseOverlay = pauseModal.gameObject;
            pauseResumeButton = pauseModal.Body.Find("Resume").GetComponent<Button>();
            pauseMainMenuButton = pauseModal.Body.Find("MainMenu").GetComponent<Button>();
            musicSwitch = pauseModal.Body.Find("MusicRow/MusicSwitch").GetComponent<UiSwitch>();
            effectsSwitch = pauseModal.Body.Find("EffectsRow/EffectsSwitch").GetComponent<UiSwitch>();
            AccessiblePauseControl(pauseResumeButton, FrontendCopy.HudContinue);
            AccessiblePauseControl(pauseMainMenuButton, FrontendCopy.ReturnToMap);
            AccessiblePauseControl(pauseModal.Body.Find("PauseRestart"), FrontendCopy.PauseRestart);
            AccessiblePauseControl(musicSwitch, FrontendCopy.Music);
            AccessiblePauseControl(effectsSwitch, FrontendCopy.Effects);
            AccessiblePauseControl(mapConfirmation.Body.Find("ConfirmMap"), FrontendCopy.ReturnToMap);
            AccessiblePauseControl(mapConfirmation.Body.Find("KeepPlaying"), FrontendCopy.Cancel);
            foreach (var modal in new[] { pauseModal, mapConfirmation })
                (modal.GetComponent<UiAccessibleButton>() ?? modal.gameObject.AddComponent<UiAccessibleButton>()).ConfigureModalScope();
        }

        private static void AccessiblePauseControl(Component control, LocalizedCopy copy) =>
            (control.GetComponent<UiAccessibleButton>() ?? control.gameObject.AddComponent<UiAccessibleButton>()).Configure(copy);

        private void InitializeFrame(DefenderPresentationCatalog catalog, Action map, Action restart, Action click)
        {
            frameKit = new UiKit(catalog.frontendTheme.designSystem);
            radialCooldown.ActionRect.SetAsFirstSibling();
            radialCooldown.ActionRect.GetComponent<UiPointSize>().Configure(64);
            UiKit.Size(radialCooldown.ActionRect, 64, 64);
            radialCooldown.GetComponent<VerticalLayoutGroup>().spacing = 2;
            waveProgress = Find<UiProgressSegments>("TopBar/WavePanel/WaveProgress");
            bossBar = transform.Find("TopBar/WavePanel/BossBar").gameObject;
            bossFill = bossBar.transform.Find("BossTrack/BossFill").GetComponent<Image>();
            bossName = bossBar.transform.Find("BossName").GetComponent<TMP_Text>();
            bossHealth = bossBar.transform.Find("BossHealth").GetComponent<TMP_Text>();
            Bind(pauseMainMenuButton, () => { mapConfirmation.transform.SetAsLastSibling(); mapConfirmation.Show(); }, click);
            InitializeWaveForecast(restart, click);
            Bind(mapConfirmation.Body.Find("ConfirmMap").GetComponent<Button>(), () => { mapConfirmation.Hide(); map?.Invoke(); }, click);
            Bind(mapConfirmation.Body.Find("KeepPlaying").GetComponent<Button>(), mapConfirmation.Hide, click);
            musicSwitch.Changed += on =>
            {
                if (!on && FrontendPreferences.MusicVolume > 0) musicBeforeMute = FrontendPreferences.MusicVolume;
                FrontendPreferences.MusicVolume = on ? musicBeforeMute : 0;
            };
            effectsSwitch.Changed += on =>
            {
                if (!on && FrontendPreferences.SfxVolume > 0) effectsBeforeMute = FrontendPreferences.SfxVolume;
                FrontendPreferences.SfxVolume = on ? effectsBeforeMute : 0;
            };
            goldPill.SetValue(0, false);
        }

        public static string FramePhaseLabel(GamePhase phase, bool actualBossWave) =>
            actualBossWave ? FrontendCopy.BossPhase.Value : phase == GamePhase.Wave
                ? FrontendCopy.BattlePhase.Value : FrontendCopy.HudPreparing.Value;

        public static string WaveActionLabel(bool first, int reward) => first ? FrontendCopy.HudFirstWave.Value :
            reward > 0 ? FrontendCopy.EarlyWaveReward.Format(reward) : FrontendCopy.HudNextWave.Value;

        private void RefreshFrame(GameSessionController session)
        {
            goldPill.SetValue(session.Gold, frameValuesReady, frameValuesReady);
            frameValuesReady = true;
            livesPill.SetValue(session.Lives, false, false);
            bool critical = session.Lives <= 5;
            if (critical && !criticalLives)
                (livesPill.GetComponent<UiMotion>() ?? livesPill.gameObject.AddComponent<UiMotion>()).Punch(livesPill.transform);
            criticalLives = critical;
            livesText.color = critical ? UiTokens.Danger : UiTokens.TextPrimary;
            speedLabel.text = GameSessionController.SpeedLabelForScale(session.CurrentGameSpeed);
            phaseText.text = FramePhaseLabel(session.UnpausedPhase, session.NextWaveHasBoss);
            phaseText.color = session.NextWaveHasBoss ? UiTokens.Danger : UiTokens.Moon;
            int count = Mathf.Max(1, session.WaveCount);
            if (count != segmentCount)
            {
                waveProgress.gameObject.SetActive(false);
                Destroy(waveProgress.gameObject);
                waveProgress = frameKit.ProgressSegments(waveText.transform.parent, "WaveProgress", count);
                segmentCount = count;
                responsiveLayout?.RefreshLayout();
            }
            int displayed = session.CurrentWaveIndex + (session.UnpausedPhase == GamePhase.Wave ? 1 : 0);
            waveText.text = $"{FrontendCopy.HudWave.Value} {displayed}/{session.WaveCount}";
            waveProgress.SetProgress(session.CurrentWaveIndex + session.CurrentWaveCompletion);
            previewText.text = FrontendCopy.UpcomingCount.Format(session.UpcomingEnemyCount);
            RefreshWaveForecast(session);
            RefreshBoss(session);
            bool canCall = session.Phase is GamePhase.Preparing or GamePhase.Intermission;
            nextWaveButton.gameObject.SetActive(canCall);
            nextWaveButton.interactable = canCall;
            if (session.UnpausedPhase == GamePhase.Intermission)
            {
                int second = Mathf.CeilToInt(session.IntermissionRemaining);
                if (second != countdownSecond || countdownLanguage != GameLocalization.CurrentLanguage)
                {
                    nextWaveLabel.text = FrontendCopy.WaveCountdown.Format(second);
                    countdownSecond = second; countdownLanguage = GameLocalization.CurrentLanguage;
                }
            }
            else
            {
                countdownSecond = -1;
                nextWaveLabel.text = WaveActionLabel(session.CurrentWaveIndex == 0, session.EarlyWaveReward);
            }
            var hero = session.Hero;
            if (hero != null)
            {
                if (hero.IsAlive)
                {
                    returnSecond = -1;
                    heroText.text = session.HeroDisplayName;
                    radialCooldown.SetReturning(false);
                    radialCooldown.SetCooldown(hero.AbilityRemaining, hero.AbilityCooldownDuration);
                }
                else
                {
                    int second = Mathf.CeilToInt(hero.RespawnRemaining);
                    if (second != returnSecond || returnLanguage != GameLocalization.CurrentLanguage)
                    {
                        heroText.text = FrontendCopy.WaveHeroReturning.Format(second);
                        returnSecond = second; returnLanguage = GameLocalization.CurrentLanguage;
                    }
                    radialCooldown.SetReturning(true);
                }
                abilityButton.interactable = hero.IsAlive && hero.AbilityRemaining <= 0 && session.Phase != GamePhase.Paused;
            }
            bool paused = session.Phase == GamePhase.Paused && !session.SpecializationModalOpen;
            if (paused != pauseOverlayVisible)
            {
                pauseOverlayVisible = paused;
                if (paused)
                {
                    musicSwitch.SetValue(FrontendPreferences.MusicVolume > 0, false);
                    effectsSwitch.SetValue(FrontendPreferences.SfxVolume > 0, false);
                    pauseModal.transform.SetAsLastSibling(); pauseModal.Show();
                }
                else { pauseModal.Hide(); mapConfirmation.Hide(); restartConfirmation.Hide(); }
            }
        }

        private void RefreshBoss(GameSessionController session)
        {
            var boss = session.Enemies.FirstOrDefault(enemy => enemy != null && enemy.IsAlive && enemy.Archetype == EnemyArchetype.Boss);
            bool visible = boss != null;
            bossBar.SetActive(visible);
            waveText.gameObject.SetActive(!visible);
            phaseText.transform.parent.gameObject.SetActive(!visible);
            waveProgress.gameObject.SetActive(!visible);
            if (visible)
            {
                if (boss != shownBoss || bossLanguage != GameLocalization.CurrentLanguage)
                {
                    bossName.text = session.BossDisplayName;
                    bossLanguage = GameLocalization.CurrentLanguage;
                }
                bossHealth.text = $"{boss.Health}/{boss.MaxHealth}";
                UiKit.SetHorizontalFill(bossFill, (float)boss.Health / Mathf.Max(1, boss.MaxHealth));
                if (boss != shownBoss)
                {
                    var motion = bossBar.GetComponent<UiMotion>() ?? bossBar.AddComponent<UiMotion>();
                    motion.Animate(t => bossBar.transform.localScale = Vector3.one * Mathf.Lerp(.96f, 1, t), .6f);
                }
            }
            shownBoss = boss;
        }
    }
}
