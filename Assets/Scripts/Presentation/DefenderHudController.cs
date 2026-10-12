using System;
using System.Collections;
using AllinDefense.Core;
using AllinDefense.Data;
using AllinDefense.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AllinDefense.Presentation
{
    public sealed partial class DefenderHudController : MonoBehaviour
    {
        public enum BuildButtonAvailabilityState
        {
            NoSlot,
            Affordable,
            Unaffordable
        }

        public enum SelectedPanelState
        {
            EmptySlot,
            UpgradeAvailable,
            MaxLevel,
            Disrupted
        }

        public enum TopBarSignalState
        {
            Stable,
            Warning,
            Boss,
            Terminal
        }

        public readonly struct BuildButtonPresentationSpec
        {
            public readonly BuildButtonAvailabilityState state;
            public readonly bool interactable;
            public readonly string title;
            public readonly string detail;
            public readonly Color stripeColor;
            public readonly bool warningStripe;

            public BuildButtonPresentationSpec(BuildButtonAvailabilityState state, bool interactable,
                string title, string detail, Color stripeColor, bool warningStripe)
            {
                this.state = state;
                this.interactable = interactable;
                this.title = title;
                this.detail = detail;
                this.stripeColor = stripeColor;
                this.warningStripe = warningStripe;
            }
        }

        public readonly struct PhaseBadgePresentationSpec
        {
            public readonly string hint;
            public readonly string label;
            public readonly Color accent;
            public readonly float stripeHeight;
            public readonly float pulseAlpha;

            public PhaseBadgePresentationSpec(string hint, string label, Color accent, float stripeHeight,
                float pulseAlpha)
            {
                this.hint = hint;
                this.label = label;
                this.accent = accent;
                this.stripeHeight = stripeHeight;
                this.pulseAlpha = pulseAlpha;
            }
        }

        public readonly struct SelectedPanelPresentationSpec
        {
            public readonly SelectedPanelState state;
            public readonly string title;
            public readonly string badge;
            public readonly string role;
            public readonly string hint;
            public readonly string stats;
            public readonly string upgradeLabel;
            public readonly string sellLabel;
            public readonly bool upgradeInteractable;
            public readonly bool sellInteractable;
            public readonly bool warningStripe;
            public readonly Color accent;
            public readonly Color badgeColor;

            public SelectedPanelPresentationSpec(SelectedPanelState state, string title, string badge, string role,
                string hint, string stats, string upgradeLabel, string sellLabel, bool upgradeInteractable,
                bool sellInteractable, bool warningStripe, Color accent, Color badgeColor)
            {
                this.state = state;
                this.title = title;
                this.badge = badge;
                this.role = role;
                this.hint = hint;
                this.stats = stats;
                this.upgradeLabel = upgradeLabel;
                this.sellLabel = sellLabel;
                this.upgradeInteractable = upgradeInteractable;
                this.sellInteractable = sellInteractable;
                this.warningStripe = warningStripe;
                this.accent = accent;
                this.badgeColor = badgeColor;
            }
        }

        public readonly struct TopBarPresentationSpec
        {
            public readonly TopBarSignalState state;
            public readonly string goldText;
            public readonly string livesText;
            public readonly string waveText;
            public readonly Color goldColor;
            public readonly Color livesColor;
            public readonly Color waveColor;
            public readonly bool livesWarning;
            public readonly bool bossSignal;

            public TopBarPresentationSpec(TopBarSignalState state, string goldText, string livesText,
                string waveText, Color goldColor, Color livesColor, Color waveColor, bool livesWarning,
                bool bossSignal)
            {
                this.state = state;
                this.goldText = goldText;
                this.livesText = livesText;
                this.waveText = waveText;
                this.goldColor = goldColor;
                this.livesColor = livesColor;
                this.waveColor = waveColor;
                this.livesWarning = livesWarning;
                this.bossSignal = bossSignal;
            }
        }

        private static int ArcherCost => TowerProgression.Base(OutpostKind.Archer).cost;
        private static int BarracksCost => TowerProgression.Base(OutpostKind.Barracks).cost;
        private static int MageCost => TowerProgression.Base(OutpostKind.Mage).cost;

        private static readonly Color RailTint = Color.clear;
        private static readonly Color PanelTint = UiTokens.HudPanel;
        private static readonly Color ButtonReadyTint = UiTokens.HudButtonReady;
        private static readonly Color ButtonPressedTint = UiTokens.HudButtonPressed;
        private static readonly Color ButtonDisabledTint = UiTokens.HudButtonDisabled;
        private static readonly Color TextPrimary = UiTokens.TextPrimary;
        private static readonly Color TextMuted = UiTokens.TextSecondary;
        private static readonly Color GoldAccent = UiTokens.Gold;
        private static readonly Color KeepAccent = UiTokens.Danger;
        private static readonly Color WaveAccent = UiTokens.Moon;
        private static readonly Color ReadyAccent = UiTokens.Success;
        private static readonly Color DisabledAccent = UiTokens.Danger;
        private static readonly Color ArcherAccent = UiTokens.Gold;
        private static readonly Color BarracksAccent = UiTokens.TextSecondary;
        private static readonly Color MageAccent = UiTokens.Moon;

        private UiCopyCatalog copy;
        private TMP_Text goldText;
        private TMP_Text livesText;
        private TMP_Text waveText;
        private TMP_Text phaseText;
        private TMP_Text phaseHintText;
        private TMP_Text previewText;
        private TMP_Text previewTitleText;
        private TMP_Text messageText;
        private TMP_Text heroText;
        private TMP_Text selectedText;
        private TMP_Text selectedTitleText;
        private TMP_Text selectedBadgeText;
        private TMP_Text selectedRoleText;
        private TMP_Text selectedHintText;
        private TMP_Text resultTitle;
        private TMP_Text resultBadgeText;
        private TMP_Text resultHintText;
        private TMP_Text nextWaveLabel;
        private TMP_Text pauseLabel;
        private TMP_Text speedLabel;
        private TMP_Text pauseOverlayTitleText;
        private TMP_Text pauseOverlayHintText;
        private TMP_Text pauseOverlayBadgeText;
        private Image selectedIconImage;
        private Image selectedHeaderImage;
        private Image resultCardImage;
        private Button archerButton;
        private Button barracksButton;
        private Button mageButton;
        private Button abilityButton;
        private Button cancelButton;
        private Button nextWaveButton;
        private Button pauseButton;
        private Button speedButton;
        private Button upgradeButton;
        private Button sellButton;
        private Button restartButton;
        private UnityEngine.UI.Button pauseResumeButton;
        private UnityEngine.UI.Button pauseMainMenuButton;
        private GameObject selectedPanel;
        private RectTransform contextualBuildChoices;
        private GameObject resultPanel;
        private GameObject pauseOverlay;
        private Image abilityCooldownFillImage;
        private Image abilityReadyGlowImage;
        private Image abilityBlockedVeilImage;
        private Image phaseChromeImage;
        private Image phaseStripeImage;
        private Image previewChromeImage;
        private Image messageChromeImage;
        private CanvasGroup pauseOverlayGroup;
        private RectTransform pauseCardRect;
        private Sprite panelSprite;
        private Sprite buttonSprite;
        private Sprite frameSprite;
        private Sprite archerIconSprite;
        private Sprite barracksIconSprite;
        private Sprite mageIconSprite;
        private GamePhase lastPhase = (GamePhase)(-1);
        private float phasePulseUntil;
        private bool pauseOverlayVisible;
        private DefenderHudResponsiveLayout responsiveLayout;

        public void Initialize(DefenderPresentationCatalog catalog, Action<OutpostKind> build, Action ability, Action cancel,
            Action nextWave, Action pause, Action mainMenu, Action speed, Action upgrade, Action sell, Action restart,
            Action uiClick)
        {
            copy = catalog.uiCopy;
            FrontendPresentationTheme frontendTheme = catalog.frontendTheme;
            panelSprite = frontendTheme != null && frontendTheme.panelSprite != null
                ? frontendTheme.panelSprite
                : catalog.panelSprite;
            buttonSprite = frontendTheme != null && frontendTheme.buttonSprite != null
                ? frontendTheme.buttonSprite
                : panelSprite;
            // The legacy decorative border was authored as a small tiled texture and visibly
            // smeared when stretched across modern HUD controls. The shared medieval 9-slice
            // family now owns both panel shape and state feedback.
            frameSprite = null;
            archerIconSprite = catalog.archerIcon;
            barracksIconSprite = catalog.barracksIcon;
            mageIconSprite = catalog.mageIcon;
            CacheReferences();
            ApplyThemeFonts(catalog);
            Bind(archerButton, () => build(OutpostKind.Archer), uiClick);
            Bind(barracksButton, () => build(OutpostKind.Barracks), uiClick);
            Bind(mageButton, () => build(OutpostKind.Mage), uiClick);
            Bind(abilityButton, ability, uiClick);
            Bind(cancelButton, cancel, uiClick);
            Bind(nextWaveButton, nextWave, uiClick);
            Bind(pauseButton, pause, uiClick);
            Bind(pauseResumeButton, pause, uiClick);
            Bind(pauseMainMenuButton, mainMenu, uiClick);
            Bind(speedButton, speed, uiClick);
            Bind(upgradeButton, upgrade, uiClick);
            sellAction = sell;
            Bind(sellButton, SellFromUi, uiClick);
            Bind(restartButton, restart, uiClick);
            AddTextIcon(goldText, catalog.goldIcon);
            AddTextIcon(livesText, catalog.keepIcon);
            AddTextIcon(waveText, catalog.waveIcon);
            AddButtonIcon(archerButton, catalog.archerIcon);
            AddButtonIcon(barracksButton, catalog.barracksIcon);
            AddButtonIcon(mageButton, catalog.mageIcon);
            AddButtonIcon(abilityButton, catalog.abilityIcon);
            ApplyStaticCopy();
            ApplyReadabilityStyle();
            responsiveLayout = GetComponent<DefenderHudResponsiveLayout>();
            if (responsiveLayout == null)
                responsiveLayout = gameObject.AddComponent<DefenderHudResponsiveLayout>();
            responsiveLayout.Initialize();
            abilityButton.gameObject.AddComponent<AbilityRangePreview>();
            ConfigureContextualBuildChoices();
            foreach (var button in GetComponentsInChildren<Button>(true)) MobileTouchTarget.Attach(button);
            UiId.AssignHierarchy(transform);
            UiId.Assign(transform.parent, transform.parent.name);
            selectedPanel.SetActive(false);
            resultPanel.SetActive(false);
        }

        public void Refresh(GameSessionController session)
        {
            if (session == null || goldText == null) return;
            if (session.Phase != saleConfirmationPhase) CancelSaleConfirmation();
            SetBuildAvailability(session.CanBuildOnSelection, session.Gold);
            TopBarPresentationSpec topBar = TopBarSpec(copy.Get(UiTextKey.Gold), copy.Get(UiTextKey.Keep),
                copy.Get(UiTextKey.Wave), session.Gold, session.Lives, session.CurrentWaveIndex,
                session.WaveCount, session.UnpausedPhase, session.NextWaveHasBoss);
            goldText.text = topBar.goldText;
            goldText.color = topBar.goldColor;
            livesText.text = topBar.livesText;
            livesText.color = topBar.livesColor;
            waveText.text = topBar.waveText;
            waveText.color = topBar.waveColor;
            ApplyPhaseBadge(session);
            pauseLabel.text = session.Phase == GamePhase.Paused ? copy.Get(UiTextKey.Continue) : copy.Get(UiTextKey.Pause);
            if (speedLabel != null)
                speedLabel.text = GameSessionController.SpeedLabelForScale(session.CurrentGameSpeed);
            bool canCallWave = session.Phase is GamePhase.Preparing or GamePhase.Intermission;
            nextWaveButton.gameObject.SetActive(canCallWave);
            nextWaveButton.interactable = canCallWave;
            SetButtonStripe(nextWaveButton, canCallWave ? GoldAccent : DisabledAccent, !canCallWave);
            SetButtonStripe(pauseButton, session.Phase == GamePhase.Paused ? GoldAccent : WaveAccent);
            ApplyPauseOverlay(session.Phase);

            GreyboxHero hero = session.Hero;
            if (hero != null)
            {
                heroText.text = hero.IsAlive
                    ? $"{session.HeroDisplayName}  {Mathf.CeilToInt(hero.HealthFraction * 100f)}%  ·  " +
                      (hero.AbilityRemaining <= 0f ? copy.Get(UiTextKey.AbilityReady) : $"{Mathf.CeilToInt(hero.AbilityRemaining)} s")
                    : FrontendCopy.Returns.Format(session.HeroDisplayName) +
                      $"  {Mathf.CeilToInt(hero.RespawnRemaining)} s";
                bool abilityReady = hero.IsAlive && hero.AbilityRemaining <= 0f && session.Phase != GamePhase.Paused;
                abilityButton.interactable = abilityReady;
                SetAbilityLabel(abilityButton, hero, session.HeroDisplayName, abilityReady, session.Phase == GamePhase.Paused);
                ApplyAbilityVisualState(hero, session.Phase, abilityReady);
                SetButtonStripe(abilityButton, abilityReady ? ReadyAccent : DisabledAccent, !abilityReady);
            }
        }

        public void SetPreview(string value)
        {
            string raw = value ?? string.Empty;
            string body = raw.Trim();
            bool upcoming = body.StartsWith("Sıradaki:", StringComparison.OrdinalIgnoreCase) || body.StartsWith("Next:", StringComparison.OrdinalIgnoreCase);
            if (upcoming)
                body = body[(body.IndexOf(':')+1)..].Trim();

            bool boss = GameSessionController.Instance != null && GameSessionController.Instance.NextWaveHasBoss;

            if (previewTitleText != null)
            {
                previewTitleText.text = string.IsNullOrWhiteSpace(body)
                    ? string.Empty
                    : boss
                        ? (upcoming ? FrontendCopy.BossApproaching.Value : FrontendCopy.BossPressure.Value)
                        : (upcoming ? FrontendCopy.NextWave.Value : FrontendCopy.CurrentWave.Value);
            }

            if (previewText != null)
            {
                previewText.text = string.IsNullOrWhiteSpace(body)
                    ? string.Empty
                    : $"<b>{body}</b>";
                previewText.color = boss ? KeepAccent : upcoming ? GoldAccent : TextPrimary;
            }

            if (previewChromeImage != null)
            {
                Color accent = boss ? KeepAccent : upcoming ? GoldAccent : WaveAccent;
                previewChromeImage.color = UiTokens.PreviewSurface(accent);
                previewChromeImage.gameObject.SetActive(!string.IsNullOrWhiteSpace(body));
            }
        }
        public void SetMessage(string value)
        {
            string body = value ?? string.Empty;
            messageText.text = body;
            bool visible = !string.IsNullOrWhiteSpace(body);
            messageText.gameObject.SetActive(visible);
            if (messageChromeImage != null) messageChromeImage.gameObject.SetActive(visible);
        }
        public void SetNextWaveLabel(string value) => nextWaveLabel.text = value ?? string.Empty;

        public void SetSelected(GreyboxOutpost outpost, GreyboxOutpostSlot slot, bool visible)
        {
            if (!visible || displayedOutpost != outpost) CancelSaleConfirmation();
            displayedOutpost = visible ? outpost : null;
            cancelButton.interactable = visible;
            bool emptySlot = slot != null && slot.Occupant == null;
            if (contextualBuildChoices != null) contextualBuildChoices.gameObject.SetActive(visible && emptySlot);
            if (upgradeButton != null) upgradeButton.gameObject.SetActive(visible && !emptySlot);
            if (sellButton != null) sellButton.gameObject.SetActive(visible && !emptySlot);
            if (selectedText != null) selectedText.gameObject.SetActive(visible && !emptySlot);
            if (selectedHintText != null) selectedHintText.gameObject.SetActive(visible && !emptySlot);

            if (outpost != null)
            {
                ApplySelectedOutpostView(outpost);
                ApplySpecializationSelection(outpost);
            }
            else if (slot != null)
            {
                ApplySelectedSlotView(slot);
            }
            else
            {
                ApplySelectedEmptyView();
            }
            ApplySaleConfirmationLabel();
            selectedPanel.SetActive(visible);
            if (!visible) responsiveLayout?.ClearContextualSelection();
        }

        public void SetSelectionAnchor(Camera camera, Vector3 worldPosition, bool emptySlot)
        {
            if (camera == null || responsiveLayout == null) return;
            Vector3 screenPoint = camera.WorldToScreenPoint(worldPosition + Vector3.up * 0.7f);
            if (screenPoint.z <= 0f) return;
            responsiveLayout.SetContextualSelectionFromScreenPoint(screenPoint, emptySlot);
        }

        public void ShowResult(bool victory)
        {
            resultPanel.SetActive(true);
            resultTitle.text = copy.Get(victory ? UiTextKey.Victory : UiTextKey.Defeat);
            resultTitle.color = victory ? UiTokens.Gold : UiTokens.Danger;
            if (resultBadgeText != null)
            {
                resultBadgeText.text = victory ? FrontendCopy.Result.Value : FrontendCopy.Critical.Value;
                resultBadgeText.color = victory ? GoldAccent : KeepAccent;
            }

            if (resultHintText != null)
            {
                resultHintText.text = victory
                    ? FrontendCopy.HudVictoryHint.Value
                    : FrontendCopy.HudDefeatHint.Value;
                resultHintText.color = victory ? TextMuted : UiTokens.DefeatText;
            }

            if (resultCardImage != null)
            {
                Color accent = victory ? GoldAccent : KeepAccent;
                resultCardImage.color = UiTokens.TintSurface(accent, 0.10f, 0.96f);
            }

            CanvasGroup group = resultPanel.GetComponent<CanvasGroup>();
            if (group == null) group = resultPanel.AddComponent<CanvasGroup>();
            if (isActiveAndEnabled) StartCoroutine(AnimateOverlay(group, resultCardImage != null ? resultCardImage.rectTransform : null));
        }

        public void SetBuildInteractable(bool archer, bool barracks, bool mage)
        {
            archerButton.interactable = archer;
            barracksButton.interactable = barracks;
            mageButton.interactable = mage;
            SetBuildButtonState(archerButton, BuildButtonSpec(copy.Get(UiTextKey.Archer), ArcherCost, archer, GoldAccent));
            SetBuildButtonState(barracksButton, BuildButtonSpec(copy.Get(UiTextKey.Barracks), BarracksCost, barracks, WaveAccent));
            SetBuildButtonState(mageButton, BuildButtonSpec(copy.Get(UiTextKey.Mage), MageCost, mage, UiTokens.Violet));
        }

        public void SetBuildAvailability(bool slotSelected, int gold)
        {
            ApplyBuildButtonState(archerButton, copy.Get(UiTextKey.Archer), ArcherCost, slotSelected, gold, GoldAccent);
            ApplyBuildButtonState(barracksButton, copy.Get(UiTextKey.Barracks), BarracksCost, slotSelected, gold, WaveAccent);
            ApplyBuildButtonState(mageButton, copy.Get(UiTextKey.Mage), MageCost, slotSelected, gold, UiTokens.Violet);
            var session = GameSessionController.Instance;
            if (session != null)
                foreach (var pair in new[] { (barracksButton, OutpostKind.Barracks), (mageButton, OutpostKind.Mage) })
                    if (!session.CanBuild(pair.Item2))
                    {
                        pair.Item1.interactable = false;
                        SetButtonLabel(pair.Item1, GameSessionController.OutpostDisplayName(pair.Item2) + "\n" +
                            FrontendCopy.Level.Format((pair.Item2 == OutpostKind.Barracks ? 2 : 3)));
                    }
        }

        public void SetBuildPreview(OutpostKind? kind)
        {
            if (kind == null) return;
            Button selected = kind switch
            {
                OutpostKind.Archer => archerButton,
                OutpostKind.Barracks => barracksButton,
                _ => mageButton
            };
            int cost = kind switch
            {
                OutpostKind.Archer => ArcherCost,
                OutpostKind.Barracks => BarracksCost,
                _ => MageCost
            };
            Color accent = OutpostAccent(kind.Value);
            SetButtonLabel(selected, FrontendCopy.Build.Format(cost));
            SetButtonStripe(selected, accent);
            if (selected.targetGraphic != null) selected.targetGraphic.color = ButtonPressedTint;
        }

        private void ApplyStaticCopy()
        {
            SetBuildButtonState(archerButton, BuildButtonSpec(copy.Get(UiTextKey.Archer), ArcherCost, true, GoldAccent));
            SetBuildButtonState(barracksButton, BuildButtonSpec(copy.Get(UiTextKey.Barracks), BarracksCost, true, WaveAccent));
            SetBuildButtonState(mageButton, BuildButtonSpec(copy.Get(UiTextKey.Mage), MageCost, true, UiTokens.Violet));
            SetButtonLabel(abilityButton, $"{HeroIdentity.DefaultName}\n{copy.Get(UiTextKey.AbilityReady)}");
            SetButtonLabel(cancelButton, copy.Get(UiTextKey.Cancel));
            SetButtonLabel(upgradeButton, copy.Get(UiTextKey.Upgrade));
            SetButtonLabel(sellButton, copy.Get(UiTextKey.Sell));
            SetButtonLabel(restartButton, copy.Get(UiTextKey.Restart));
            nextWaveLabel.text = copy.Get(UiTextKey.FirstWave);
            pauseLabel.text = copy.Get(UiTextKey.Pause);
        }

        private void CacheReferences()
        {
            goldText = Find<TMP_Text>("TopBar/Gold");
            livesText = Find<TMP_Text>("TopBar/Lives");
            waveText = Find<TMP_Text>("TopBar/Wave");
            EnsureTopStatusChrome();
            phaseText = Find<TMP_Text>("TopBar/Phase");
            EnsurePhaseChrome();
            pauseButton = Find<Button>("TopBar/Pause");
            pauseLabel = Find<TMP_Text>("TopBar/Pause/Label");
            EnsureSpeedButton();
            speedButton = Find<Button>("TopBar/Speed");
            speedLabel = Find<TMP_Text>("TopBar/Speed/Label");
            previewText = Find<TMP_Text>("Preview");
            EnsurePreviewChrome();
            messageText = Find<TMP_Text>("Message");
            EnsureMessageChrome();
            archerButton = Find<Button>("BottomBar/Archer");
            barracksButton = Find<Button>("BottomBar/Barracks");
            mageButton = Find<Button>("BottomBar/Mage");
            abilityButton = Find<Button>("BottomBar/Ability");
            EnsureAbilityButtonChrome();
            cancelButton = Find<Button>("BottomBar/Cancel");
            heroText = Find<TMP_Text>("BottomBar/Hero");
            nextWaveButton = Find<Button>("BottomBar/NextWave");
            nextWaveLabel = Find<TMP_Text>("BottomBar/NextWave/Label");
            EnsureCommandDeckChrome();
            selectedPanel = transform.Find("SelectedPanel").gameObject;
            selectedText = Find<TMP_Text>("SelectedPanel/Description");
            upgradeButton = Find<Button>("SelectedPanel/Upgrade");
            sellButton = Find<Button>("SelectedPanel/Sell");
            EnsureSelectedPanelChrome();
            resultPanel = transform.Find("ResultPanel").gameObject;
            resultTitle = Find<TMP_Text>("ResultPanel/Title");
            restartButton = Find<Button>("ResultPanel/Restart");
            EnsureResultPanelChrome();
            EnsurePauseOverlayChrome();
        }

        private void ApplyReadabilityStyle()
        {
            StylePanel("TopBar", RailTint, true);
            StylePanel("BottomBar", RailTint, true);
            StylePanel("SelectedPanel", PanelTint, false);
            StylePanel("ResultPanel", UiTokens.HudResultSurface, false);
            StyleImage(selectedHeaderImage, UiTokens.HudHeader, frame: false);

            StyleText(goldText, 36f, GoldAccent, FontStyles.Bold);
            StyleText(livesText, 36f, TextPrimary, FontStyles.Bold);
            StyleText(waveText, 36f, WaveAccent, FontStyles.Bold);
            StyleText(phaseText, 30f, TextPrimary, FontStyles.Bold);
            phaseText.alignment = TextAlignmentOptions.MidlineLeft;
            phaseText.enableWordWrapping = false;
            StyleText(phaseHintText, 20f, TextMuted, FontStyles.Bold);
            phaseHintText.gameObject.SetActive(true);
            StyleText(previewText, 26f, TextPrimary, FontStyles.Bold);
            previewText.enableWordWrapping = true;
            previewText.alignment = TextAlignmentOptions.TopLeft;
            StyleText(previewTitleText, 20f, TextMuted, FontStyles.Bold);
            previewTitleText.alignment = TextAlignmentOptions.TopLeft;
            StyleText(messageText, 28f, TextPrimary, FontStyles.Bold);
            messageText.enableWordWrapping = true;
            messageText.enableAutoSizing = true;
            messageText.fontSizeMin = 24f;
            messageText.fontSizeMax = 28f;
            StyleText(heroText, 24f, TextMuted, FontStyles.Bold);
            StyleText(selectedText, 30f, TextPrimary);
            selectedText.enableWordWrapping = true;
            selectedText.overflowMode = TextOverflowModes.Overflow;
            StyleText(selectedTitleText, 32f, TextPrimary, FontStyles.Bold);
            selectedTitleText.enableAutoSizing = true;
            selectedTitleText.fontSizeMin = 24f;
            selectedTitleText.fontSizeMax = 32f;
            StyleText(selectedBadgeText, 24f, GoldAccent, FontStyles.Bold);
            StyleText(selectedRoleText, 24f, TextMuted);
            StyleText(selectedHintText, 26f, TextMuted);
            StyleText(resultTitle, 68f, GoldAccent, FontStyles.Bold);
            StyleText(resultBadgeText, 17f, GoldAccent, FontStyles.Bold);
            StyleText(resultHintText, 19f, TextMuted);
            StyleText(pauseOverlayTitleText, 42f, TextPrimary, FontStyles.Bold);
            StyleText(pauseOverlayBadgeText, 17f, GoldAccent, FontStyles.Bold);
            StyleText(pauseOverlayHintText, 19f, TextMuted);
            selectedHintText.enableWordWrapping = true;
            selectedHintText.overflowMode = TextOverflowModes.Ellipsis;
            resultHintText.enableWordWrapping = true;
            resultHintText.overflowMode = TextOverflowModes.Overflow;
            pauseOverlayHintText.enableWordWrapping = true;
            pauseOverlayHintText.overflowMode = TextOverflowModes.Overflow;
            StyleButton(archerButton, GoldAccent);
            StyleButton(barracksButton, WaveAccent);
            StyleButton(mageButton, UiTokens.Violet);
            StyleButton(abilityButton, ReadyAccent);
            TMP_Text heroButtonLabel = abilityButton.GetComponentInChildren<TMP_Text>(true);
            heroButtonLabel.fontSizeMin = 16f;
            heroButtonLabel.enableWordWrapping = false;
            heroButtonLabel.richText = false;
            heroText.richText = false;
            StyleButton(cancelButton, KeepAccent);
            StyleButton(nextWaveButton, GoldAccent);
            StylePrimaryButton(nextWaveButton);
            StyleButton(pauseButton, WaveAccent);
            StyleButton(speedButton, GoldAccent);
            StyleButton(upgradeButton, GoldAccent);
            StyleButton(sellButton, KeepAccent);
            StyleButton(restartButton, GoldAccent);
            StylePrimaryButton(restartButton);
            StyleButton(pauseResumeButton, GoldAccent);
            StyleButton(pauseMainMenuButton, KeepAccent);
        }

        private T Find<T>(string path) where T : Component
        {
            Transform item = transform.Find(path);
            if (item == null || !item.TryGetComponent(out T component))
                throw new InvalidOperationException($"HUD prefab is missing {typeof(T).Name} at {path}.");
            return component;
        }

        private static void Bind(Button button, Action action, Action click)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                click?.Invoke();
                action?.Invoke();
            });
        }

        public bool GetButtonInteractable(string path)
        {
            Transform item = FindControl(path);
            if (item == null) return false;
            if (!item.TryGetComponent(out Button button)) button = item.GetComponentInChildren<Button>(true);
            return button != null && button.interactable;
        }

        private Transform FindControl(string path)
        {
            Transform item = transform.Find(path);
            if (item != null) return item;

            if (path.StartsWith("TopBar/", StringComparison.Ordinal))
                return transform.Find(path.Replace("TopBar/", "TopBar/SafeContent/"));
            if (path.StartsWith("BottomBar/", StringComparison.Ordinal))
            {
                if (path == "BottomBar/Archer") return archerButton != null ? archerButton.transform : null;
                if (path == "BottomBar/Barracks") return barracksButton != null ? barracksButton.transform : null;
                if (path == "BottomBar/Mage") return mageButton != null ? mageButton.transform : null;
                return transform.Find(path.Replace("BottomBar/", "BottomBar/SafeContent/"));
            }
            return null;
        }

        private void ConfigureContextualBuildChoices()
        {
            RectTransform panelRect = selectedPanel != null ? selectedPanel.transform as RectTransform : null;
            if (panelRect == null) return;

            Transform existing = panelRect.Find("BuildChoices");
            contextualBuildChoices = existing as RectTransform;
            if (contextualBuildChoices == null)
            {
                contextualBuildChoices = new GameObject("BuildChoices", typeof(RectTransform)).GetComponent<RectTransform>();
                contextualBuildChoices.SetParent(panelRect, false);
            }
            contextualBuildChoices.anchorMin = new Vector2(0f, 0f);
            contextualBuildChoices.anchorMax = new Vector2(1f, 0f);
            contextualBuildChoices.pivot = new Vector2(0.5f, 0f);
            contextualBuildChoices.anchoredPosition = new Vector2(0f, 14f);
            contextualBuildChoices.sizeDelta = new Vector2(-20f, 86f);

            LayoutContextualBuildButton(archerButton, 0f, 1f / 3f);
            LayoutContextualBuildButton(barracksButton, 1f / 3f, 2f / 3f);
            LayoutContextualBuildButton(mageButton, 2f / 3f, 1f);

            Transform buildDeck = transform.Find("BottomBar/SafeContent/BuildDeck");
            if (buildDeck != null) buildDeck.gameObject.SetActive(false);
            contextualBuildChoices.gameObject.SetActive(false);
            responsiveLayout?.RefreshLayout();
        }

        private void LayoutContextualBuildButton(Button button, float minX, float maxX)
        {
            if (button == null || contextualBuildChoices == null) return;
            RectTransform rect = button.transform as RectTransform;
            rect.SetParent(contextualBuildChoices, false);
            rect.anchorMin = new Vector2(minX, 0f);
            rect.anchorMax = new Vector2(maxX, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(5f, 0f);
            rect.offsetMax = new Vector2(-5f, 0f);
        }

        private static void SetButtonLabel(Button button, string value)
        {
            if (button == null) return;
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null) label.text = value;
        }

        private static string StatLabel(string title, string value) =>
            $"<size=70%><color=#A6B4B8>{title}</color></size>  <b>{value}</b>";

        private static string PhaseLabel(string label)
        {
            return label;
        }

        public static TopBarPresentationSpec TopBarSpec(string goldLabel, string keepLabel, string waveLabel,
            int gold, int lives, int currentWaveIndex, int waveCount, GamePhase phase, bool waveHasBoss = false)
        {
            int safeWaveCount = Mathf.Max(0, waveCount);
            int displayedWave = Mathf.Min(currentWaveIndex + (phase == GamePhase.Wave ? 1 : 0), safeWaveCount);
            bool livesWarning = lives <= 5;
            bool bossSignal = waveHasBoss &&
                              phase is GamePhase.Preparing or GamePhase.Intermission or GamePhase.Wave;
            TopBarSignalState state = phase is GamePhase.Victory or GamePhase.Defeat
                ? TopBarSignalState.Terminal
                : bossSignal ? TopBarSignalState.Boss : livesWarning ? TopBarSignalState.Warning : TopBarSignalState.Stable;
            string keepTitle = livesWarning ? $"{keepLabel}!" : keepLabel;
            string waveTitle = bossSignal ? "BOSS" : waveLabel;
            Color livesColor = phase == GamePhase.Defeat || livesWarning ? KeepAccent : TextPrimary;
            Color waveColor = bossSignal ? KeepAccent : WaveAccent;

            return new TopBarPresentationSpec(state, StatLabel(goldLabel, gold.ToString()),
                StatLabel(keepTitle, Mathf.Max(0, lives).ToString()),
                StatLabel(waveTitle, $"{displayedWave}/{safeWaveCount}"), GoldAccent, livesColor, waveColor,
                livesWarning, bossSignal);
        }

        private void ApplyPhaseBadge(GameSessionController session)
        {
            if (session.Phase != lastPhase)
            {
                lastPhase = session.Phase;
                phasePulseUntil = Time.unscaledTime + (FrontendPreferences.ReducedMotion ? 0f : 0.42f);
            }

            float pulse = Mathf.Clamp01((phasePulseUntil - Time.unscaledTime) / 0.42f);
            bool bossUpcoming = session.NextWaveHasBoss &&
                                session.Phase is GamePhase.Preparing or GamePhase.Intermission;
            bool bossActive = session.NextWaveHasBoss && session.Phase == GamePhase.Wave;
            PhaseBadgePresentationSpec spec = PhaseBadgeSpec(session.Phase, bossUpcoming, bossActive, pulse);

            phaseText.text = $"<b>{spec.label}</b>";
            phaseText.color = spec.accent;
            if (phaseHintText != null)
            {
                phaseHintText.text = spec.hint;
                phaseHintText.color = Color.Lerp(TextMuted, spec.accent, 0.55f + spec.pulseAlpha * 0.18f);
            }

            if (phaseChromeImage != null)
                phaseChromeImage.color = UiTokens.TintSurface(spec.accent, .15f + spec.pulseAlpha * .05f, .86f);
            if (phaseStripeImage != null)
            {
                phaseStripeImage.color = spec.accent;
                phaseStripeImage.rectTransform.sizeDelta = new Vector2(0f, spec.stripeHeight);
            }
        }

        public static PhaseBadgePresentationSpec PhaseBadgeSpec(GamePhase phase, bool bossUpcoming,
            bool bossActive, float transitionPulse)
        {
            string hint = FrontendCopy.Status.Value;
            string label = phase switch
            {
                GamePhase.Preparing => FrontendCopy.Preparing.Value,
                GamePhase.Wave => FrontendCopy.Defending.Value,
                GamePhase.Intermission => FrontendCopy.Intermission.Value,
                GamePhase.Paused => FrontendCopy.Paused.Value,
                GamePhase.Victory => FrontendCopy.Victory.Value,
                GamePhase.Defeat => FrontendCopy.Defeat.Value,
                _ => FrontendCopy.Loading.Value
            };

            Color accent = PhaseColor(phase);
            float clampedPulse = Mathf.Clamp01(transitionPulse);
            float stripeHeight = Mathf.Lerp(5f, 8f, clampedPulse);
            float pulseAlpha = clampedPulse * 0.45f;

            if (bossUpcoming)
            {
                hint = FrontendCopy.Critical.Value;
                label = FrontendCopy.PhaseBossApproaching.Value;
                accent = KeepAccent;
                stripeHeight = Mathf.Lerp(7f, 11f, clampedPulse);
                pulseAlpha = Mathf.Max(pulseAlpha, 0.22f);
            }
            else if (bossActive)
            {
                hint = FrontendCopy.Critical.Value;
                label = FrontendCopy.BossPressure.Value;
                accent = KeepAccent;
                stripeHeight = Mathf.Lerp(8f, 12f, clampedPulse);
                pulseAlpha = Mathf.Max(pulseAlpha, 0.30f);
            }

            return new PhaseBadgePresentationSpec(hint, label, accent, stripeHeight, pulseAlpha);
        }

        private static Color PhaseColor(GamePhase phase) => phase switch
        {
            GamePhase.Preparing => GoldAccent,
            GamePhase.Wave => KeepAccent,
            GamePhase.Intermission => WaveAccent,
            GamePhase.Paused => TextMuted,
            GamePhase.Victory => ReadyAccent,
            GamePhase.Defeat => KeepAccent,
            _ => TextPrimary
        };

        public static BuildButtonPresentationSpec BuildButtonSpec(string label, int cost, bool selectedSlot,
            int gold, Color accent)
        {
            if (!selectedSlot)
                return new BuildButtonPresentationSpec(BuildButtonAvailabilityState.NoSlot, false,
                    label, FrontendCopy.SelectSite.Format(cost), TextMuted, false);

            if (gold >= cost)
                return new BuildButtonPresentationSpec(BuildButtonAvailabilityState.Affordable, true,
                    label, cost.ToString(), accent, false);

            return new BuildButtonPresentationSpec(BuildButtonAvailabilityState.Unaffordable, false,
                label, FrontendCopy.GoldNeed.Format(cost, cost-gold), DisabledAccent, true);
        }

        private static BuildButtonPresentationSpec BuildButtonSpec(string label, int cost, bool available, Color accent)
        {
            return available
                ? BuildButtonSpec(label, cost, true, cost, accent)
                : new BuildButtonPresentationSpec(BuildButtonAvailabilityState.Unaffordable, false,
                    FrontendCopy.NotEnough.Value, $"{label} {cost}", DisabledAccent, true);
        }

        private static void ApplyBuildButtonState(Button button, string label, int cost, bool selectedSlot,
            int gold, Color accent)
        {
            SetBuildButtonState(button, BuildButtonSpec(label, cost, selectedSlot, gold, accent));
        }

        private static void SetBuildButtonState(Button button, BuildButtonPresentationSpec spec)
        {
            if (button == null) return;
            button.interactable = spec.interactable;
            SetButtonLabel(button, $"{spec.title}\n{spec.detail}");
            SetButtonStripe(button, spec.stripeColor, spec.warningStripe);
            if (button.targetGraphic != null)
            {
                button.targetGraphic.color = spec.state switch
                {
                    BuildButtonAvailabilityState.Affordable => ButtonReadyTint,
                    BuildButtonAvailabilityState.NoSlot => UiTokens.BuildNoSlot,
                    _ => ButtonDisabledTint
                };
            }
        }

        private void SetAbilityLabel(Button button, GreyboxHero hero, string heroName, bool ready, bool paused)
        {
            if (hero == null) return;
            string state = ready ? FrontendCopy.Ready.Value : !hero.IsAlive ? FrontendCopy.Returning.Value : paused ? FrontendCopy.AbilityPaused.Value : $"{Mathf.CeilToInt(hero.AbilityRemaining)} s";
            SetButtonLabel(button, $"{heroName}\n{state}");
        }

        public static SelectedPanelPresentationSpec SelectedOutpostPanelSpec(OutpostKind kind, int level,
            string roleLabel, int damage, float range, float attackRate, bool canUpgrade, int upgradeCost,
            int sellValue, bool isDisrupted, float disabledRemaining)
        {
            Color accent = OutpostAccent(kind);
            SelectedPanelState state = isDisrupted
                ? SelectedPanelState.Disrupted
                : canUpgrade ? SelectedPanelState.UpgradeAvailable : SelectedPanelState.MaxLevel;
            string rate = FrontendCopy.AttackRate.Format(attackRate);
            string upgrade = canUpgrade
                ? FrontendCopy.GoldCost.Format(upgradeCost)
                : FrontendCopy.MaxLevelLabel.Value;
            string disruptedSuffix = isDisrupted
                ? $"  •  BOZULDU {Mathf.CeilToInt(disabledRemaining)} s"
                : string.Empty;
            string hint = isDisrupted
                ? FrontendCopy.TemporarilyDisruptedSellForGold.Format(sellValue)
                : canUpgrade
                    ? FrontendCopy.UpgradeForGoldSellForGold.Format(upgradeCost, sellValue)
                    : FrontendCopy.MaxLevelSellForGold.Format(sellValue);
            string stats =
                $"{StatLabel(FrontendCopy.Damage.Value, damage.ToString())}\n" +
                $"{StatLabel(FrontendCopy.Range.Value, $"{range:0.0}")}\n" +
                $"{StatLabel(FrontendCopy.Rate.Value, rate)}\n\n" +
                $"{StatLabel(FrontendCopy.UpgradeStat.Value, upgrade)}\n" +
                $"{StatLabel(FrontendCopy.SellStat.Value, sellValue.ToString())}";

            return new SelectedPanelPresentationSpec(state, GameSessionController.OutpostDisplayName(kind),
                FrontendCopy.TowerLevel.Format(level), roleLabel + disruptedSuffix, hint, stats,
                canUpgrade ? FrontendCopy.Upgrade.Format(upgradeCost)
                    : FrontendCopy.MaxLv.Value,
                FrontendCopy.Sell.Format(sellValue), canUpgrade, true,
                isDisrupted || !canUpgrade, accent,
                canUpgrade ? GoldAccent : ReadyAccent);
        }

        public static SelectedPanelPresentationSpec SelectedSlotPanelSpec(int slotIndex)
        {
            string stats = string.Empty;

            return new SelectedPanelPresentationSpec(SelectedPanelState.EmptySlot,
                FrontendCopy.OutpostSlot.Value, $"#{slotIndex + 1}",
                FrontendCopy.ChooseADefenceType.Value,
                FrontendCopy.BuildArcherBarracksOrMage.Value, stats,
                FrontendCopy.UpgradeLocked.Value,
                FrontendCopy.SellLocked.Value, false, false, true, WaveAccent, WaveAccent);
        }

        private void ApplyAbilityVisualState(GreyboxHero hero, GamePhase phase, bool ready)
        {
            if (hero == null || abilityButton == null) return;

            bool paused = phase == GamePhase.Paused;
            bool blocked = !hero.IsAlive || paused;
            float duration = Mathf.Max(0.01f, hero.AbilityCooldownDuration);
            float cooldownProgress = hero.IsAlive ? 1f - Mathf.Clamp01(hero.AbilityRemaining / duration) : 0f;

            if (abilityCooldownFillImage != null)
            {
                abilityCooldownFillImage.gameObject.SetActive(hero.IsAlive && !ready);
                abilityCooldownFillImage.color = Color.Lerp(UiTokens.CooldownStart,
                    UiTokens.CooldownEnd, cooldownProgress);
                float fillHeight = Mathf.Lerp(0f, 56f, cooldownProgress);
                abilityCooldownFillImage.rectTransform.offsetMin = new Vector2(8f, 8f);
                abilityCooldownFillImage.rectTransform.offsetMax = new Vector2(-8f, 8f + fillHeight);
            }

            if (abilityReadyGlowImage != null)
            {
                abilityReadyGlowImage.gameObject.SetActive(ready);
                float pulse = FrontendPreferences.ReducedMotion
                    ? 0.28f
                    : 0.20f + 0.16f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 4.6f));
                abilityReadyGlowImage.color = UiTokens.WithAlpha(ReadyAccent, ready ? pulse : 0f);
            }

            if (abilityBlockedVeilImage != null)
            {
                abilityBlockedVeilImage.gameObject.SetActive(blocked);
                abilityBlockedVeilImage.color = !hero.IsAlive
                    ? UiTokens.TintSurface(KeepAccent, .26f, .36f)
                    : UiTokens.WithAlpha(Color.black, .26f);
            }

            if (abilityButton.targetGraphic != null)
            {
                abilityButton.targetGraphic.color = blocked
                    ? UiTokens.AbilityBlocked
                    : ready
                        ? UiTokens.AbilityReady
                        : UiTokens.AbilityIdle;
            }
        }

        private void ApplyPauseOverlay(GamePhase phase)
        {
            if (pauseOverlay == null) return;
            var session = GameSessionController.Instance;
            bool visible = phase == GamePhase.Paused && (session == null || !session.SpecializationModalOpen);
            if (!visible)
            {
                if (pauseOverlayVisible) pauseOverlay.SetActive(false);
                pauseOverlayVisible = false;
                return;
            }

            if (!pauseOverlayVisible)
            {
                pauseOverlay.SetActive(true);
                StartCoroutine(AnimateOverlay(pauseOverlayGroup, pauseCardRect));
            }
            pauseOverlayVisible = true;

            if (pauseOverlayBadgeText != null)
            {
                pauseOverlayBadgeText.text = FrontendCopy.Status.Value;
                pauseOverlayBadgeText.color = GoldAccent;
            }

            if (pauseOverlayTitleText != null)
                pauseOverlayTitleText.text = GameLocalization.Uppercase(copy.Get(UiTextKey.Paused));

            if (pauseOverlayHintText != null)
                pauseOverlayHintText.text = FrontendCopy.PauseHint.Value;
        }

        private void StylePanel(string path, Color color, bool rail)
        {
            Transform item = transform.Find(path);
            if (item != null && item.TryGetComponent(out Image image))
            {
                if (rail)
                {
                    image.enabled = false;
                    image.raycastTarget = false;
                    image.color = color;
                    image.sprite = null;
                    image.type = Image.Type.Simple;
                    return;
                }
                StyleImage(image, color, true);
            }
        }

        private void StyleImage(Image image, Color color, bool frame)
        {
            if (image == null) return;
            image.color = color;
            if (panelSprite != null)
            {
                image.sprite = panelSprite;
                image.type = Image.Type.Sliced;
            }

            if (!frame) return;
            EnsureFrameOverlay(image.transform as RectTransform, "FrameOverlay", UiTokens.HudFrame);
        }

        private void EnsureFrameOverlay(RectTransform parent, string name, Color color)
        {
            if (parent == null || frameSprite == null) return;
            Transform existing = parent.Find(name);
            Image image;
            RectTransform rect;
            if (existing != null && existing.TryGetComponent(out image))
            {
                rect = image.rectTransform;
            }
            else
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(parent, false);
                go.transform.SetAsLastSibling();
                image = go.GetComponent<Image>();
                rect = image.rectTransform;
            }

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = railInset(parent.name) * Vector2.one;
            rect.offsetMax = railInset(parent.name) * -Vector2.one;
            image.sprite = frameSprite;
            image.type = Image.Type.Sliced;
            image.color = color;
            image.raycastTarget = false;
        }

        private static float railInset(string parentName) =>
            parentName.Contains("TopBar", StringComparison.OrdinalIgnoreCase) ||
            parentName.Contains("BottomBar", StringComparison.OrdinalIgnoreCase) ? 6f : 8f;

        private static void StyleText(TMP_Text text, float size, Color color, FontStyles style = FontStyles.Normal)
        {
            if (text == null) return;
            text.fontSize = size;
            text.color = color;
            text.fontStyle = style;
            text.richText = true;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Ellipsis;
            if (text.GetComponent<Shadow>() == null)
            {
                var shadow = text.gameObject.AddComponent<Shadow>();
                shadow.effectColor = UiTokens.HudTextShadow;
                shadow.effectDistance = new Vector2(1.4f, -1.4f);
                shadow.useGraphicAlpha = true;
            }
        }

        private void StyleButton(Button button, Color accent)
        {
            if (button == null) return;
            if (button.targetGraphic is Image targetImage)
            {
                targetImage.sprite = buttonSprite;
                targetImage.type = buttonSprite != null ? Image.Type.Sliced : Image.Type.Simple;
                targetImage.color = ButtonReadyTint;
            }
            else if (button.targetGraphic != null)
                button.targetGraphic.color = ButtonReadyTint;

            Transform legacyFrame = button.transform.Find("LicensedFrame");
            if (legacyFrame != null) legacyFrame.gameObject.SetActive(false);

            ColorBlock colors = button.colors;
            colors.normalColor = ButtonReadyTint;
            colors.highlightedColor = Color.Lerp(ButtonReadyTint, accent, 0.34f);
            colors.pressedColor = ButtonPressedTint;
            colors.selectedColor = Color.Lerp(ButtonReadyTint, accent, 0.24f);
            colors.disabledColor = ButtonDisabledTint;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.fontSize = 30f;
                label.enableAutoSizing = true;
                label.fontSizeMin = 26f;
                label.fontSizeMax = 32f;
                label.alignment = TextAlignmentOptions.Center;
                label.fontStyle = FontStyles.Bold;
                label.color = TextPrimary;
                label.enableWordWrapping = true;
                label.overflowMode = TextOverflowModes.Ellipsis;
                if (label.GetComponent<Shadow>() == null)
                {
                    var shadow = label.gameObject.AddComponent<Shadow>();
                    shadow.effectColor = UiTokens.HudButtonShadow;
                    shadow.effectDistance = new Vector2(1.1f, -1.1f);
                    shadow.useGraphicAlpha = true;
                }
            }

            SetButtonStripe(button, accent);
        }

        private static void StylePrimaryButton(Button button)
        {
            if (button == null) return;
            Color normal = UiTokens.HudGoldButton;
            ColorBlock colors = button.colors;
            colors.normalColor = normal;
            colors.highlightedColor = UiTokens.HudGoldHighlight;
            colors.pressedColor = UiTokens.HudGoldPressed;
            colors.selectedColor = UiTokens.HudGoldSelected;
            colors.disabledColor = ButtonDisabledTint;
            button.colors = colors;
            if (button.targetGraphic != null) button.targetGraphic.color = normal;
        }

        private static void SetButtonStripe(Button button, Color accent, bool warning = false)
        {
            if (button == null) return;
            Image stripe = EnsureButtonStripe(button);
            if (stripe == null) return;
            stripe.color = warning ? DisabledAccent : accent;
            stripe.rectTransform.sizeDelta = new Vector2(0f, warning ? 8f : 5f);
        }

        private static Image EnsureButtonStripe(Button button)
        {
            Transform existing = button.transform.Find("StateStripe");
            if (existing != null && existing.TryGetComponent(out Image existingImage))
                return existingImage;

            var stripeObject = new GameObject("StateStripe", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            stripeObject.transform.SetParent(button.transform, false);
            stripeObject.transform.SetAsFirstSibling();
            var rect = stripeObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = new Vector2(8f, 5f);
            rect.offsetMax = new Vector2(-8f, 10f);
            var image = stripeObject.GetComponent<Image>();
            image.raycastTarget = false;
            return image;
        }

        private static void AddTextIcon(TMP_Text text, Sprite sprite)
        {
            if (text == null || sprite == null || text.transform.Find("Icon") != null) return;
            var iconObject = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObject.transform.SetParent(text.transform, false);
            var rect = iconObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(3f, 0f);
            rect.sizeDelta = new Vector2(30f, 30f);
            var image = iconObject.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            text.margin = new Vector4(text.margin.x + 38f, text.margin.y, text.margin.z, text.margin.w);
        }

        private static void AddButtonIcon(Button button, Sprite sprite)
        {
            if (button == null || sprite == null || button.transform.Find("Icon") != null) return;
            var iconObject = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObject.transform.SetParent(button.transform, false);
            var rect = iconObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(7f, 0f);
            rect.sizeDelta = new Vector2(38f, 38f);
            var image = iconObject.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                RectTransform labelRect = label.rectTransform;
                labelRect.offsetMin = new Vector2(Mathf.Max(labelRect.offsetMin.x, 44f), labelRect.offsetMin.y);
            }
        }

        private void ApplyThemeFonts(DefenderPresentationCatalog catalog)
        {
            if (catalog == null) return;
            if (catalog.titleFont != null)
            {
                waveText.font = catalog.titleFont;
                phaseText.font = catalog.titleFont;
                selectedTitleText.font = catalog.titleFont;
                resultTitle.font = catalog.titleFont;
                pauseOverlayTitleText.font = catalog.titleFont;
                nextWaveLabel.font = catalog.titleFont;
            }

            if (catalog.bodyFont == null) return;
            foreach (TMP_Text text in GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.font == null) text.font = catalog.bodyFont;
            }
        }

        private void EnsureTopStatusChrome()
        {
            EnsureStatusCard(goldText, "GoldChrome", GoldAccent);
            EnsureStatusCard(livesText, "LivesChrome", KeepAccent);
            EnsureStatusCard(waveText, "WaveChrome", WaveAccent);
        }

        private void EnsureStatusCard(TMP_Text label, string name, Color accent)
        {
            if (label == null || label.transform.parent == null) return;
            RectTransform labelRect = label.rectTransform;
            Image card = EnsureImage(label.transform.parent, name,
                UiTokens.HudLabelBacking, labelRect.anchorMin, labelRect.anchorMax,
                labelRect.anchoredPosition, labelRect.sizeDelta);
            card.sprite = panelSprite;
            card.type = panelSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            card.transform.SetSiblingIndex(Mathf.Max(0, label.transform.GetSiblingIndex()));
            Image stripe = EnsureImage(card.rectTransform, "Accent", accent,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(10f, 6f), new Vector2(-10f, 11f));
            stripe.sprite = null;
        }

        private void EnsureCommandDeckChrome()
        {
            RectTransform parent = archerButton?.transform.parent as RectTransform;
            if (parent == null) return;
            EnsureDeckPanel(parent, "CommandDeck", UiTokens.CommandDeck);
            EnsureDeckPanel(parent, "HeroDeck", UiTokens.HeroDeck);
            EnsureDeckPanel(parent, "BuildDeck", UiTokens.BuildDeck);
        }

        private void EnsureDeckPanel(RectTransform parent, string name, Color color)
        {
            Image image = EnsureImage(parent, name, color, Vector2.zero, Vector2.one,
                new Vector2(4f, 4f), new Vector2(-4f, -4f));
            image.sprite = panelSprite;
            image.type = panelSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.enabled = false;
            image.raycastTarget = false;
            image.transform.SetAsFirstSibling();
        }

        private void EnsureMessageChrome()
        {
            RectTransform rootRect = transform as RectTransform;
            if (rootRect == null) return;
            messageChromeImage = EnsureImage(rootRect, "MessageChrome",
                UiTokens.HudHintSurface, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -190f), new Vector2(760f, 54f));
            messageChromeImage.sprite = buttonSprite;
            messageChromeImage.type = buttonSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            messageChromeImage.transform.SetSiblingIndex(Mathf.Max(0, messageText.transform.GetSiblingIndex()));
            messageChromeImage.gameObject.SetActive(false);
            messageText.gameObject.SetActive(false);
        }

        private void EnsureSelectedPanelChrome()
        {
            RectTransform panelRect = selectedPanel.GetComponent<RectTransform>();
            selectedHeaderImage = EnsureImage(panelRect, "Header", UiTokens.SelectedHeader,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(10f, -124f), new Vector2(-10f, -14f));
            selectedIconImage = EnsureImage(selectedHeaderImage.rectTransform, "Icon", Color.white,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(42f, 42f));
            selectedIconImage.preserveAspect = true;
            selectedIconImage.raycastTarget = false;

            selectedTitleText = EnsureLabel(selectedHeaderImage.rectTransform, "Title",
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(62f, -76f), new Vector2(-12f, -2f),
                TextAlignmentOptions.Left);
            selectedBadgeText = EnsureLabel(selectedHeaderImage.rectTransform, "Badge",
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-12f, 4f), new Vector2(76f, 42f),
                TextAlignmentOptions.Center);
            selectedRoleText = EnsureLabel(selectedHeaderImage.rectTransform, "Role",
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(12f, 2f), new Vector2(-90f, 44f),
                TextAlignmentOptions.Left);
            selectedHintText = EnsureLabel(panelRect, "Hint",
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(14f, 150f), new Vector2(-14f, 222f),
                TextAlignmentOptions.Left);

            RectTransform descriptionRect = selectedText.rectTransform;
            descriptionRect.anchorMin = new Vector2(0f, 0f);
            descriptionRect.anchorMax = new Vector2(1f, 1f);
            descriptionRect.offsetMin = new Vector2(16f, 230f);
            descriptionRect.offsetMax = new Vector2(-16f, -134f);
            foreach (var button in new[] { upgradeButton, sellButton })
            {
                var rect = (RectTransform)button.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(button == upgradeButton ? .25f : .75f, 0);
                rect.pivot = new Vector2(.5f, 0);
                rect.anchoredPosition = new Vector2(0, 12);
                rect.sizeDelta = new Vector2(196, 130);
            }
        }

        private void EnsurePreviewChrome()
        {
            RectTransform previewRect = previewText.rectTransform;
            RectTransform rootRect = previewRect.parent as RectTransform;
            if (rootRect == null) return;

            previewChromeImage = EnsureImage(rootRect, "PreviewChrome", UiTokens.PreviewChrome,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -96f), new Vector2(760f, 86f));
            previewChromeImage.sprite = panelSprite;
            previewChromeImage.type = panelSprite != null ? Image.Type.Sliced : Image.Type.Simple;

            previewTitleText = EnsureLabel(rootRect, "PreviewTitle",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -104f), new Vector2(660f, 22f),
                TextAlignmentOptions.TopLeft);

            previewRect.anchorMin = previewRect.anchorMax = new Vector2(0.5f, 1f);
            previewRect.pivot = new Vector2(0.5f, 1f);
            previewRect.anchoredPosition = new Vector2(0f, -126f);
            previewRect.sizeDelta = new Vector2(660f, 38f);
            previewText.margin = new Vector4(4f, 0f, 0f, 0f);
            previewText.transform.SetAsLastSibling();
            previewTitleText.transform.SetAsLastSibling();
            EnsureFrameOverlay(previewChromeImage.rectTransform, "FrameOverlay", UiTokens.WithAlpha(UiTokens.HudFrame, 0.28f));
            SetPreview(previewText.text);
        }

        private void EnsurePhaseChrome()
        {
            RectTransform phaseRect = phaseText.rectTransform;
            RectTransform parentRect = phaseRect.parent as RectTransform;
            if (parentRect == null) return;

            phaseChromeImage = EnsureImage(parentRect, "PhaseChrome", UiTokens.PhaseChrome,
                phaseRect.anchorMin, phaseRect.anchorMax, phaseRect.offsetMin, phaseRect.offsetMax);
            phaseChromeImage.sprite = panelSprite;
            phaseChromeImage.type = panelSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            phaseChromeImage.transform.SetSiblingIndex(phaseText.transform.GetSiblingIndex());

            phaseHintText = EnsureLabel(phaseChromeImage.rectTransform, "PhaseHint",
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(14f, 38f), new Vector2(-12f, -5f),
                TextAlignmentOptions.MidlineLeft);

            phaseText.alignment = TextAlignmentOptions.MidlineLeft;
            phaseText.margin = new Vector4(14f, 19f, 12f, 3f);

            phaseStripeImage = EnsureImage(phaseChromeImage.rectTransform, "Stripe", WaveAccent,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(10f, 6f), new Vector2(-10f, 11f));
            phaseStripeImage.sprite = null;

            EnsureFrameOverlay(phaseChromeImage.rectTransform, "FrameOverlay", UiTokens.WithAlpha(UiTokens.HudFrame, 0.24f));
        }

        private void EnsureAbilityButtonChrome()
        {
            if (abilityButton == null) return;
            RectTransform buttonRect = abilityButton.transform as RectTransform;
            if (buttonRect == null) return;

            abilityCooldownFillImage = EnsureImage(buttonRect, "CooldownFill", UiTokens.CooldownFill,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(8f, 8f), new Vector2(-8f, 8f));
            abilityCooldownFillImage.transform.SetAsFirstSibling();

            abilityReadyGlowImage = EnsureImage(buttonRect, "ReadyGlow", UiTokens.WithAlpha(ReadyAccent, 0f),
                Vector2.zero, Vector2.one, new Vector2(6f, 6f), new Vector2(-6f, -6f));
            abilityReadyGlowImage.sprite = panelSprite;
            abilityReadyGlowImage.type = panelSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            abilityReadyGlowImage.transform.SetAsFirstSibling();

            abilityBlockedVeilImage = EnsureImage(buttonRect, "BlockedVeil", Color.clear,
                Vector2.zero, Vector2.one, new Vector2(6f, 6f), new Vector2(-6f, -6f));
            abilityBlockedVeilImage.sprite = panelSprite;
            abilityBlockedVeilImage.type = panelSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            abilityBlockedVeilImage.transform.SetAsLastSibling();
        }

        private void EnsureResultPanelChrome()
        {
            RectTransform panelRect = resultPanel.GetComponent<RectTransform>();
            resultCardImage = EnsureImage(panelRect, "Card", UiTokens.ResultCard,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -8f), new Vector2(860f, 412f));
            resultCardImage.sprite = panelSprite;
            resultCardImage.type = panelSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            EnsureFrameOverlay(resultCardImage.rectTransform, "FrameOverlay", UiTokens.HudFrame);

            resultBadgeText = EnsureLabel(resultCardImage.rectTransform, "Badge",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -34f), new Vector2(240f, 26f),
                TextAlignmentOptions.Center);
            resultHintText = EnsureLabel(resultCardImage.rectTransform, "Hint",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 130f), new Vector2(740f, 56f),
                TextAlignmentOptions.Center);

            RectTransform titleRect = resultTitle.rectTransform;
            titleRect.anchorMin = titleRect.anchorMax = new Vector2(0.5f, 0.5f);
            titleRect.pivot = new Vector2(0.5f, 0.5f);
            titleRect.anchoredPosition = new Vector2(0f, 28f);
            titleRect.sizeDelta = new Vector2(720f, 150f);

            RectTransform restartRect = restartButton.GetComponent<RectTransform>();
            restartRect.anchorMin = restartRect.anchorMax = new Vector2(0.5f, 0.5f);
            restartRect.pivot = new Vector2(0.5f, 0.5f);
            restartRect.anchoredPosition = new Vector2(0f, -126f);
            restartRect.sizeDelta = new Vector2(340f, 84f);
        }

        private void EnsurePauseOverlayChrome()
        {
            RectTransform rootRect = transform as RectTransform;
            if (rootRect == null) return;

            Transform existing = transform.Find("PauseOverlay");
            if (existing != null)
            {
                pauseOverlay = existing.gameObject;
            }
            else
            {
                pauseOverlay = new GameObject("PauseOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                pauseOverlay.transform.SetParent(transform, false);
                var overlayRect = pauseOverlay.GetComponent<RectTransform>();
                overlayRect.anchorMin = Vector2.zero;
                overlayRect.anchorMax = Vector2.one;
                overlayRect.offsetMin = Vector2.zero;
                overlayRect.offsetMax = Vector2.zero;
                var overlayImage = pauseOverlay.GetComponent<Image>();
                overlayImage.color = UiTokens.PauseVeil;
                overlayImage.raycastTarget = true;
            }

            UnityEngine.UI.Image blocker = pauseOverlay.GetComponent<UnityEngine.UI.Image>();
            if (blocker != null) blocker.raycastTarget = true;

            RectTransform overlayRoot = pauseOverlay.GetComponent<RectTransform>();
            pauseOverlayGroup = pauseOverlay.GetComponent<CanvasGroup>();
            if (pauseOverlayGroup == null) pauseOverlayGroup = pauseOverlay.AddComponent<CanvasGroup>();
            Image pauseCard = EnsureImage(overlayRoot, "Card", UiTokens.PauseCard,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -8f), new Vector2(920f, 440f));
            pauseCardRect = pauseCard.rectTransform;
            pauseCard.sprite = panelSprite;
            pauseCard.type = panelSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            EnsureFrameOverlay(pauseCard.rectTransform, "FrameOverlay", UiTokens.WithAlpha(UiTokens.HudFrame, 0.26f));

            pauseOverlayBadgeText = EnsureLabel(pauseCard.rectTransform, "Badge",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(220f, 24f),
                TextAlignmentOptions.Center);
            pauseOverlayTitleText = EnsureLabel(pauseCard.rectTransform, "Title",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 86f), new Vector2(620f, 72f),
                TextAlignmentOptions.Center);
            pauseOverlayHintText = EnsureLabel(pauseCard.rectTransform, "Hint",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 18f), new Vector2(680f, 64f),
                TextAlignmentOptions.Center);

            pauseResumeButton = EnsureActionButton(pauseCard.rectTransform, "Resume", FrontendCopy.Resume.Value,
                new Vector2(-184f, -122f), new Vector2(328f, 80f));
            pauseMainMenuButton = EnsureActionButton(pauseCard.rectTransform, "MainMenu", FrontendCopy.MainMenu.Value,
                new Vector2(184f, -122f), new Vector2(328f, 80f));

            pauseOverlay.transform.SetAsLastSibling();
            pauseOverlay.SetActive(false);
        }

        private IEnumerator AnimateOverlay(CanvasGroup group, RectTransform card)
        {
            if (group == null) yield break;
            if (FrontendPreferences.ReducedMotion)
            {
                group.alpha = 1f;
                if (card != null) card.localScale = Vector3.one;
                yield break;
            }

            group.alpha = 0f;
            if (card != null) card.localScale = Vector3.one * 0.96f;
            const float duration = UiTokens.FadeDuration;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                t = UiTokens.EvaluateEaseOut(t);
                group.alpha = t;
                if (card != null) card.localScale = Vector3.one * Mathf.Lerp(0.96f, 1f, t);
                yield return null;
            }
            group.alpha = 1f;
            if (card != null) card.localScale = Vector3.one;
        }

        private UnityEngine.UI.Button EnsureActionButton(Transform parent, string name, string label,
            Vector2 anchoredPosition, Vector2 size)
        {
            Transform existing = parent.Find(name);
            UnityEngine.UI.Button button;
            RectTransform rect;
            UnityEngine.UI.Image image;
            if (existing != null && existing.TryGetComponent(out button))
            {
                rect = existing as RectTransform;
                image = existing.GetComponent<UnityEngine.UI.Image>();
            }
            else
            {
                var root = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer),
                    typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
                root.transform.SetParent(parent, false);
                rect = root.GetComponent<RectTransform>();
                image = root.GetComponent<UnityEngine.UI.Image>();
                button = root.GetComponent<UnityEngine.UI.Button>();
            }

            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            image.sprite = buttonSprite;
            image.type = buttonSprite != null ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
            image.raycastTarget = true;
            button.targetGraphic = image;

            TMP_Text buttonLabel = EnsureLabel(rect, "Label", Vector2.zero, Vector2.one,
                new Vector2(12f, 10f), new Vector2(-12f, -10f), TextAlignmentOptions.Center);
            buttonLabel.text = label;
            return button;
        }

        private void EnsureSpeedButton()
        {
            if (FindControl("TopBar/Speed") != null)
                return;

            if (pauseButton == null)
                pauseButton = Find<Button>("TopBar/Pause");
            if (pauseButton == null)
                return;

            RectTransform pauseRect = pauseButton.transform as RectTransform;
            RectTransform parentRect = pauseRect?.parent as RectTransform;
            if (pauseRect == null || parentRect == null)
                return;

            GameObject speedObject = Instantiate(pauseButton.gameObject, parentRect);
            speedObject.name = "Speed";
            RectTransform speedRect = speedObject.transform as RectTransform;
            if (speedRect != null)
            {
                speedRect.anchorMin = pauseRect.anchorMin;
                speedRect.anchorMax = pauseRect.anchorMax;
                speedRect.pivot = pauseRect.pivot;
                speedRect.anchoredPosition = pauseRect.anchoredPosition + new Vector2(-108f, 0f);
                speedRect.sizeDelta = new Vector2(92f, pauseRect.sizeDelta.y);
            }

            TMP_Text label = speedObject.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                label.text = "1x";
        }

        private void ApplySelectedOutpostView(GreyboxOutpost outpost)
        {
            SelectedPanelPresentationSpec spec = SelectedOutpostPanelSpec(outpost.Kind, outpost.Level,
                outpost.RoleLabel, outpost.DamageValue, outpost.RangeValue, outpost.AttackRateValue,
                outpost.CanUpgrade, outpost.UpgradeCost, outpost.SellValue, outpost.IsDisrupted,
                outpost.DisabledRemaining);
            ApplySelectedActionState(spec);
            Color accent = spec.accent;
            selectedHeaderImage.color = UiTokens.TintSurface(accent, 0.18f, 0.98f);
            selectedIconImage.sprite = OutpostSprite(outpost.Kind);
            selectedIconImage.color = accent;
            selectedTitleText.text = spec.title;
            selectedBadgeText.text = spec.badge;
            selectedBadgeText.color = spec.badgeColor;
            selectedRoleText.text = spec.role;
            selectedRoleText.color = spec.state == SelectedPanelState.Disrupted ? KeepAccent : TextMuted;
            selectedHintText.text = spec.hint;
            selectedText.text = spec.stats;
        }

        private void ApplySelectedSlotView(GreyboxOutpostSlot slot)
        {
            SelectedPanelPresentationSpec spec = SelectedSlotPanelSpec(slot.Index);
            ApplySelectedActionState(spec);
            selectedHeaderImage.color = UiTokens.SelectedEmptyHeader;
            selectedIconImage.sprite = null;
            selectedIconImage.color = UiTokens.SelectedEmptyIcon;
            selectedTitleText.text = spec.title;
            selectedBadgeText.text = spec.badge;
            selectedBadgeText.color = spec.badgeColor;
            selectedRoleText.text = spec.role;
            selectedRoleText.color = TextMuted;
            selectedHintText.text = spec.hint;
            selectedText.text = spec.stats;
        }

        private void ApplySelectedEmptyView()
        {
            ApplySelectedActionState(SelectedSlotPanelSpec(-1));
            selectedHeaderImage.color = UiTokens.SelectedIdleHeader;
            selectedIconImage.sprite = null;
            selectedTitleText.text = FrontendCopy.NoSelection.Value;
            selectedBadgeText.text = string.Empty;
            selectedRoleText.text = FrontendCopy.SelectionHint.Value;
            selectedRoleText.color = TextMuted;
            selectedHintText.text = string.Empty;
            selectedText.text = string.Empty;
        }

        private void ApplySelectedActionState(SelectedPanelPresentationSpec spec)
        {
            upgradeButton.interactable = spec.upgradeInteractable;
            sellButton.interactable = spec.sellInteractable;
            SetButtonLabel(upgradeButton, spec.upgradeLabel);
            SetButtonLabel(sellButton, spec.sellLabel);
            SetButtonStripe(upgradeButton, spec.upgradeInteractable ? GoldAccent : DisabledAccent,
                !spec.upgradeInteractable || spec.warningStripe);
            SetButtonStripe(sellButton, spec.sellInteractable ? KeepAccent : DisabledAccent,
                !spec.sellInteractable);
        }

        private static Image EnsureImage(Transform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 anchoredPosition, Vector2 size)
        {
            Transform existing = parent.Find(name);
            Image image;
            RectTransform rect;
            if (existing != null && existing.TryGetComponent(out image))
            {
                rect = image.rectTransform;
            }
            else
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(parent, false);
                image = go.GetComponent<Image>();
                rect = image.rectTransform;
            }

            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2((anchorMin.x + anchorMax.x) * 0.5f, (anchorMin.y + anchorMax.y) * 0.5f);
            if (anchorMin == anchorMax)
            {
                rect.anchoredPosition = anchoredPosition;
                rect.sizeDelta = size;
            }
            else
            {
                rect.offsetMin = anchoredPosition;
                rect.offsetMax = size;
            }
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static TMP_Text EnsureLabel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 anchoredPosition, Vector2 size, TextAlignmentOptions alignment)
        {
            Transform existing = parent.Find(name);
            TMP_Text text;
            RectTransform rect;
            if (existing != null && existing.TryGetComponent(out text))
            {
                rect = text.rectTransform;
            }
            else
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                go.transform.SetParent(parent, false);
                text = go.GetComponent<TextMeshProUGUI>();
                rect = text.rectTransform;
            }

            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2((anchorMin.x + anchorMax.x) * 0.5f, (anchorMin.y + anchorMax.y) * 0.5f);
            if (anchorMin == anchorMax)
            {
                rect.anchoredPosition = anchoredPosition;
                rect.sizeDelta = size;
            }
            else
            {
                rect.offsetMin = anchoredPosition;
                rect.offsetMax = size;
            }
            text.alignment = alignment;
            text.raycastTarget = false;
            text.enableWordWrapping = true;
            text.overflowMode = TextOverflowModes.Ellipsis;
            return text;
        }

        private Sprite OutpostSprite(OutpostKind kind) => kind switch
        {
            OutpostKind.Archer => archerIconSprite,
            OutpostKind.Barracks => barracksIconSprite,
            OutpostKind.Mage => mageIconSprite,
            _ => null
        };

        private static Color OutpostAccent(OutpostKind kind) => kind switch
        {
            OutpostKind.Archer => ArcherAccent,
            OutpostKind.Barracks => BarracksAccent,
            OutpostKind.Mage => MageAccent,
            _ => TextPrimary
        };
    }
}
