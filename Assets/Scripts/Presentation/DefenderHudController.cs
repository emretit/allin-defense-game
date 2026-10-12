using System;
using System.Collections;
using AllinDefense.Core;
using AllinDefense.Data;
using AllinDefense.UI;
using AllinDefense.UI.Kit;
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
        private TMP_Text previewText;
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
        private TMP_Text speedLabel;
        private Image selectedIconImage;
        private Image selectedHeaderImage;
        private Image resultCardImage;
        private Button archerButton;
        private Button barracksButton;
        private Button mageButton;
        private Button abilityButton;
        private Button nextWaveButton;
        private Button pauseButton;
        private Button speedButton;
        private Button upgradeButton;
        private Button sellButton;
        private Button restartButton;
        private UnityEngine.UI.Button pauseResumeButton;
        private UnityEngine.UI.Button pauseMainMenuButton;
        private GameObject selectedPanel;

        private GameObject resultPanel;
        private GameObject pauseOverlay;
        private Image messageChromeImage;
        private Sprite panelSprite;
        private Sprite buttonSprite;
        private Sprite frameSprite;
        private Sprite archerIconSprite;
        private Sprite barracksIconSprite;
        private Sprite mageIconSprite;
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
            ApplyContextAndResultFonts(catalog);
            InitializeFrame(catalog, mainMenu, restart, uiClick);
            Bind(archerButton, () => build(OutpostKind.Archer), uiClick);
            Bind(barracksButton, () => build(OutpostKind.Barracks), uiClick);
            Bind(mageButton, () => build(OutpostKind.Mage), uiClick);
            Bind(abilityButton, ability, uiClick);

            Bind(nextWaveButton, nextWave, uiClick);
            Bind(pauseButton, pause, uiClick);
            Bind(pauseResumeButton, pause, uiClick);

            Bind(speedButton, speed, uiClick);
            Bind(upgradeButton, upgrade, uiClick);
            sellAction = sell;
            Bind(sellButton, SellFromUi, uiClick);
            Bind(restartButton, restart, uiClick);
            AddButtonIcon(archerButton, catalog.archerIcon);
            AddButtonIcon(barracksButton, catalog.barracksIcon);
            AddButtonIcon(mageButton, catalog.mageIcon);
            ApplyStaticCopy();
            ApplyReadabilityStyle();
            responsiveLayout = GetComponent<DefenderHudResponsiveLayout>();
            if (responsiveLayout == null)
                responsiveLayout = gameObject.AddComponent<DefenderHudResponsiveLayout>();
            responsiveLayout.Initialize();
            abilityButton.gameObject.AddComponent<AbilityRangePreview>();
            InitializeBattleRadial(cancel);
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
            RefreshFrame(session);
        }

        public void SetPreview(string value) { }

        public void SetMessage(string value)
        {
            string body = value ?? string.Empty;
            messageText.text = body;
            bool visible = !string.IsNullOrWhiteSpace(body);
            messageText.gameObject.SetActive(visible);
            if (messageChromeImage != null) messageChromeImage.gameObject.SetActive(visible);
        }
        public void SetNextWaveLabel(string value) { }

        public void SetSelected(GreyboxOutpost outpost, GreyboxOutpostSlot slot, bool visible)
        {
            if (!visible || displayedOutpost != outpost) CancelSaleConfirmation();
            displayedOutpost = visible ? outpost : null;
            bool emptySlot = slot != null && slot.Occupant == null;



            if (selectedText != null) selectedText.gameObject.SetActive(visible);
            if (selectedHintText != null) selectedHintText.gameObject.SetActive(visible);

            if (outpost != null)
            {
                ApplySelectedOutpostView(outpost);

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
            bool selectionChanged = selectedPanel.activeSelf != visible;
            selectedPanel.SetActive(visible);
            if (visible)
            {
                messageText.gameObject.SetActive(false);
                messageChromeImage.gameObject.SetActive(false);
                previewText.transform.parent.gameObject.SetActive(false);
            }
            if (selectionChanged) responsiveLayout?.RefreshLayout();
            UpdateRadialSelection(outpost, slot, visible);
            if (!visible) responsiveLayout?.ClearContextualSelection();
        }

        public void SetSelectionAnchor(Camera camera, Vector3 worldPosition, bool emptySlot)
        {
            if (camera == null || responsiveLayout == null) return;
            Vector3 screenPoint = camera.WorldToScreenPoint(worldPosition + Vector3.up * 0.7f);
            if (screenPoint.z <= 0f) return;
            responsiveLayout.SetContextualSelectionFromScreenPoint(screenPoint, emptySlot);
            PositionBattleRadial(camera.WorldToScreenPoint(worldPosition));
        }

        public void ShowResult(bool victory)
        {
            resultPanel.transform.SetAsLastSibling();
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
            if (battleRadial != null) RefreshRadialAvailability();
        }

        public void SetBuildPreview(OutpostKind? kind) { }

        private void ApplyStaticCopy()
        {
            SetBuildButtonState(archerButton, BuildButtonSpec(copy.Get(UiTextKey.Archer), ArcherCost, true, GoldAccent));
            SetBuildButtonState(barracksButton, BuildButtonSpec(copy.Get(UiTextKey.Barracks), BarracksCost, true, WaveAccent));
            SetBuildButtonState(mageButton, BuildButtonSpec(copy.Get(UiTextKey.Mage), MageCost, true, UiTokens.Violet));
            SetButtonLabel(upgradeButton, copy.Get(UiTextKey.Upgrade));
            SetButtonLabel(sellButton, copy.Get(UiTextKey.Sell));
            SetButtonLabel(restartButton, copy.Get(UiTextKey.Restart));
            nextWaveLabel.text = copy.Get(UiTextKey.FirstWave);
        }

        private void CacheReferences()
        {
            goldPill = Find<UiStatPill>("TopBar/Gold");
            livesPill = Find<UiStatPill>("TopBar/Lives");
            goldText = goldPill.GetComponentInChildren<TMP_Text>();
            livesText = livesPill.GetComponentInChildren<TMP_Text>();
            waveText = Find<TMP_Text>("TopBar/WavePanel/Wave");
            phaseText = Find<TMP_Text>("TopBar/WavePanel/PhaseChip/PhaseChip.Label");
            pauseButton = Find<Button>("TopBar/Pause");
            speedButton = Find<Button>("TopBar/Speed");
            speedLabel = speedButton.GetComponentInChildren<TMP_Text>(true);
            previewText = Find<TMP_Text>("Preview/Preview.Label");
            messageText = Find<TMP_Text>("Message");
            messageChromeImage = Find<Image>("MessageChrome");
            archerButton = Find<Button>("BottomBar/Archer");
            barracksButton = Find<Button>("BottomBar/Barracks");
            mageButton = Find<Button>("BottomBar/Mage");
            radialCooldown = Find<UiRadialCooldown>("BottomBar/AbilityCooldown");
            abilityButton = radialCooldown.GetComponentInChildren<Button>(true);
            heroText = Find<TMP_Text>("BottomBar/Hero");
            nextWaveButton = Find<Button>("BottomBar/NextWave");
            nextWaveLabel = nextWaveButton.GetComponentInChildren<TMP_Text>(true);
            selectedPanel = transform.Find("SelectedPanel").gameObject;
            selectedText = Find<TMP_Text>("SelectedPanel/Description");
            upgradeButton = Find<Button>("SelectedPanel/Upgrade");
            sellButton = Find<Button>("SelectedPanel/Sell");
            EnsureSelectedPanelChrome();
            resultPanel = transform.Find("ResultPanel").gameObject;
            resultTitle = Find<TMP_Text>("ResultPanel/Title");
            restartButton = Find<Button>("ResultPanel/Restart");
            EnsureResultPanelChrome();
            CacheFrameModal();
        }

        private void ApplyContextAndResultFonts(DefenderPresentationCatalog catalog)
        {
            if (catalog.titleFont != null)
            {
                selectedTitleText.font = catalog.titleFont;
                resultTitle.font = catalog.titleFont;
            }
            if (catalog.bodyFont == null) return;
            foreach (var panel in new[] { selectedPanel, resultPanel })
                foreach (var text in panel.GetComponentsInChildren<TMP_Text>(true))
                    if (text.font == null) text.font = catalog.bodyFont;
        }

        private void ApplyReadabilityStyle()
        {
            StylePanel("SelectedPanel", PanelTint, false);
            StylePanel("ResultPanel", UiTokens.HudResultSurface, false);
            StyleImage(selectedHeaderImage, UiTokens.HudHeader, frame: false);
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
            selectedHintText.enableWordWrapping = true;
            selectedHintText.overflowMode = TextOverflowModes.Ellipsis;
            resultHintText.enableWordWrapping = true;
            resultHintText.overflowMode = TextOverflowModes.Overflow;
            StyleButton(archerButton, GoldAccent);
            StyleButton(barracksButton, WaveAccent);
            StyleButton(mageButton, UiTokens.Violet);
            StyleButton(upgradeButton, GoldAccent);
            StyleButton(sellButton, KeepAccent);
            StyleButton(restartButton, GoldAccent);
            StylePrimaryButton(restartButton);
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
            if (path == "BottomBar/Ability") return abilityButton != null ? abilityButton.transform : null;
            if (path.StartsWith("BottomBar/", StringComparison.Ordinal))
            {
                if (path == "BottomBar/Archer") return archerButton != null ? archerButton.transform : null;
                if (path == "BottomBar/Barracks") return barracksButton != null ? barracksButton.transform : null;
                if (path == "BottomBar/Mage") return mageButton != null ? mageButton.transform : null;
                return transform.Find(path.Replace("BottomBar/", "BottomBar/SafeContent/"));
            }
            return null;
        }

        private static void SetButtonLabel(Button button, string value)
        {
            if (button == null) return;
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null) label.text = value;
        }

        private static string StatLabel(string title, string value) =>
            $"{title}  {value}";

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

        private void ApplySelectedOutpostView(GreyboxOutpost outpost)
        {
            Color accent = OutpostAccent(outpost.Kind);
            selectedHeaderImage.color = UiTokens.TintSurface(accent, 0.18f, 0.98f);
            selectedIconImage.sprite = OutpostSprite(outpost.Kind);
            selectedIconImage.color = accent;
            selectedTitleText.color = UiTokens.TextPrimary;
            selectedBadgeText.color = UiTokens.Gold;
        }

        private void ApplySelectedSlotView(GreyboxOutpostSlot slot)
        {
            selectedRoleText.gameObject.SetActive(true);
            SelectedPanelPresentationSpec spec = SelectedSlotPanelSpec(slot.Index);
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
            selectedHeaderImage.color = UiTokens.SelectedIdleHeader;
            selectedIconImage.sprite = null;
            selectedTitleText.text = FrontendCopy.NoSelection.Value;
            selectedBadgeText.text = string.Empty;
            selectedRoleText.text = FrontendCopy.SelectionHint.Value;
            selectedRoleText.color = TextMuted;
            selectedHintText.text = string.Empty;
            selectedText.text = string.Empty;
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
