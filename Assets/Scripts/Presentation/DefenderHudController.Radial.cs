using System;
using System.Collections.Generic;
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
        private DefenderPresentationCatalog radialCatalog;
        private Button radialAction;
        private TMP_Text radialActionLabel;
        private UiVisualState radialActionState;
        private readonly Dictionary<string, GameObject> radialLocks = new();
        public Button RadialAction => radialAction;
        private UiRadialMenu battleRadial;
        private UiRadialLayout battleRadialLayout;
        private readonly Dictionary<string, Button> radialButtons = new();
        private UiRadialOption[] radialOptions;
        private GreyboxOutpostSlot radialSlot;
        private GreyboxOutpost radialTower;
        private int radialLevel;
        private OutpostSpecialization radialBranch;
        private string previewOption;
        private string lockedPreviewOption;
        private readonly RectTransform[] cardStatTiles = new RectTransform[3];
        private readonly TMP_Text[] cardStatLabels = new TMP_Text[3];
        private readonly TMP_Text[] cardStatValues = new TMP_Text[3];
        private TMP_Text cardDetail;
        private Action cancelRadialSelection;
        private CombatRangeVisual radialRange;
        private bool closingRadial;
        private readonly List<Rect> radialObstacles = new(16);
        private readonly List<Rect> radialFrameRects = new(16);
        private readonly List<Renderer> radialRenderers = new(16);
        private readonly List<Renderer> radialSlotRenderers = new(16);
        private readonly Vector3[] radialCorners = new Vector3[4];
        private readonly Dictionary<string, TMP_Text> radialLabels = new();
        private readonly Dictionary<string, GameObject> radialMarks = new();
        private Button[] radialTutorialControls;
        private Vector2 placedCenter;
        private Rect placedSafeArea;
        private Vector2 placedSize;
        private bool radialPositionDirty = true;
        // Preview changes preserve both the ring shape and the action position.
        private bool radialRingDirty = true;
        // Compact card reserves two role/detail lines, three stat tiles and one hint line.
        private const float SellCaptionWidthPt = 68f;
        private const float ReservedInformationCardHeight = DefenderHudResponsiveLayout.InformationCardMaxHeight;
        private const float RadialButtonPt = 60f;
        private const float RadialActionWidthPt = 164f;
        private const float RadialSelectedScale = 1.08f;
        private const float RadialActionSearchStepPt = 8f;
        private bool radialActionPositioned;
        private float selectionCardHeight;
        private const float RadialFollowThresholdPt = 6f;
        public UiRadialMenu BattleRadial => battleRadial;
        public bool HasRadialRangePreview => battleRadial != null && battleRadial.IsOpen && previewOption != null && previewOption != "Sell";
        public event Action<string> RadialRejected;

        private void InitializeBattleRadial(Action cancel, Action uiClick)
        {
            cancelRadialSelection = cancel;
            foreach (var old in new[] { archerButton, barracksButton, mageButton, upgradeButton, sellButton })
            { old.gameObject.SetActive(false); old.transform.SetParent(null); Destroy(old.gameObject); }
            battleRadial = frameKit.RadialMenu(transform, "BattleRadial", new[] { new UiRadialOption("Archer", archerIconSprite, ArcherCost) });
            battleRadial.GetComponent<Image>().raycastTarget = false;
            battleRadialLayout = battleRadial.GetComponent<UiRadialLayout>();
            battleRadialLayout.MinimumDiameter = RadialButtonPt;
            var first = battleRadial.GetComponentInChildren<Button>();
            first.name = "Archer"; UiId.Assign(first.transform, "Archer");
            radialButtons.Add("Archer", first);
            StyleRadialButton(first);
            archerButton = first;
            barracksButton = MakeRadialButton("Barracks", barracksIconSprite, BarracksCost);
            mageButton = MakeRadialButton("Mage", mageIconSprite, MageCost);
            upgradeButton = MakeRadialButton("Upgrade", radialCatalog.radialUpgradeIcon, 0);
            sellButton = MakeRadialButton("Sell", radialCatalog.radialSellIcon, 0);
            foreach (OutpostSpecialization branch in Enum.GetValues(typeof(OutpostSpecialization)))
                if (branch != OutpostSpecialization.None)
                    MakeRadialButton(branch.ToString(), BranchIcon(branch), 0);
            radialAction = frameKit.Button(battleRadial.transform, "RadialAction", FrontendCopy.RadialBuild, UiButtonVariant.Gold, radialCatalog.goldIcon);
            radialAction.GetComponent<UiPointSize>().IgnoreOwnScale = true;
            radialAction.GetComponent<UiPointSize>().Configure(UiMetrics.PrimaryCtaPt);
            radialAction.GetComponent<LayoutElement>().ignoreLayout = true;
            radialActionLabel = radialAction.GetComponentInChildren<TMP_Text>(true);
            radialActionLabel.fontSize = 14;
            radialActionLabel.enableAutoSizing = false;
            radialActionLabel.textWrappingMode = TextWrappingModes.NoWrap;
            radialActionLabel.overflowMode = TextOverflowModes.Overflow;
            radialActionState = radialAction.GetComponent<UiVisualState>();
            radialActionState.PaintedIcon = true;
            radialAction.onClick.AddListener(ConfirmRadialAction);
            radialAction.gameObject.AddComponent<UiRadialAction>().Configure(() => RejectRadial(previewOption));
            radialAction.gameObject.AddComponent<UiAccessibleButton>();
            radialAction.gameObject.SetActive(false);
            battleRadial.CloseOnConfirm = false;
            battleRadial.Previewed += PreviewRadial;
            battleRadial.Confirmed += ConfirmRadial;
            battleRadial.Rejected += option => RejectRadial(option.Id);
            battleRadial.Closed += DismissRadial;
            foreach (var button in radialButtons.Values)
            {
                button.interactable = false;
                button.onClick.AddListener(() => uiClick?.Invoke());
            }
            battleRadial.gameObject.SetActive(false);
            LayoutInformationCard();
            InitializeTutorial();
            radialTutorialControls = new[] { tutorialHelp, tutorialSkip };
            foreach (var pair in radialButtons)
            {
                radialLabels.Add(pair.Key, pair.Value.GetComponentsInChildren<TMP_Text>(true)[0]);
                radialMarks.Add(pair.Key, pair.Value.transform.Find("ConfirmationMark").gameObject);
            }
        }

        private Sprite BranchIcon(OutpostSpecialization branch) => branch switch
        {
            OutpostSpecialization.RapidFire => radialCatalog.radialRapidFireIcon,
            OutpostSpecialization.Sharpshooter => radialCatalog.radialSharpshooterIcon,
            OutpostSpecialization.ShieldGuard => radialCatalog.radialShieldGuardIcon,
            OutpostSpecialization.AssaultGuard => radialCatalog.radialAssaultGuardIcon,
            OutpostSpecialization.Blast => radialCatalog.radialBlastIcon,
            _ => radialCatalog.radialFrostIcon
        };

        private Button MakeRadialButton(string id, Sprite icon, int price)
        {
            var button = frameKit.Button(battleRadial.transform, id, "", UiButtonVariant.Glass, icon, price, verticalContent: true);
            button.GetComponent<UiPointSize>().Configure(id == "Sell" ? SellCaptionWidthPt : RadialButtonPt);
            button.GetComponent<UiVisualState>().Icon.GetComponent<UiProportionalIcon>().Configure(button.GetComponent<UiPointSize>());
            StyleRadialButton(button);
            radialButtons.Add(id, button);
            return button;
        }

        private void StyleRadialButton(Button button)
        {
            button.GetComponent<UiPointSize>().IgnoreOwnScale = true;
            foreach (var group in button.GetComponentsInChildren<LayoutGroup>(true)) group.enabled = false;
            var content = button.transform.Find(button.name + ".Content") as RectTransform;
            if (content == null) content = button.transform.Find("BattleRadial.Archer.Content") as RectTransform;
            UiKit.Stretch(content);
            var state = button.GetComponent<UiVisualState>();
            var icon = state.Icon.rectTransform;
            icon.anchorMin = icon.anchorMax = icon.pivot = new Vector2(.5f, .5f);
            icon.anchoredPosition = new Vector2(0, 3); icon.sizeDelta = Vector2.one * 48;
            var proportional = icon.GetComponent<UiProportionalIcon>();
            if (proportional != null) proportional.enabled = false;
            state.PaintedIcon = true;
            state.SelectedScale = RadialSelectedScale;
            state.GoldBorder = true;
            var mask = UiKit.Node(content, "IconMask");
            mask.anchorMin = mask.anchorMax = mask.pivot = new Vector2(.5f, .5f);
            mask.anchoredPosition = new Vector2(0, 3); mask.sizeDelta = Vector2.one * 48;
            var maskImage = mask.gameObject.AddComponent<Image>(); maskImage.sprite = frameKit.Assets.filledCircle; maskImage.raycastTarget = false;
            mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            icon.SetParent(mask, false); UiKit.Stretch(icon);
            var chip = (RectTransform)state.Price.transform.parent;
            chip.anchorMin = chip.anchorMax = chip.pivot = new Vector2(.5f, 0);
            chip.anchoredPosition = new Vector2(0, -1); chip.sizeDelta = new Vector2(52, 18);
            var coin = frameKit.Decoration(chip, "PriceCoin", radialCatalog.goldIcon, UiTokens.White);
            coin.rectTransform.anchorMin = coin.rectTransform.anchorMax = coin.rectTransform.pivot = new Vector2(0, .5f);
            coin.rectTransform.anchoredPosition = new Vector2(3, 0); coin.rectTransform.sizeDelta = Vector2.one * 14;
            UiKit.Stretch(state.Price.rectTransform); state.Price.rectTransform.offsetMin = new Vector2(16, 0); state.Price.alignment = TextAlignmentOptions.Center;
            chip.SetAsLastSibling();
            var label = button.GetComponentInChildren<TMP_Text>(true);
            label.gameObject.SetActive(true);
            var rect = label.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, 0);
            rect.anchoredPosition = new Vector2(0, -20); rect.sizeDelta = new Vector2(72, 20);
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 13;
            label.enableAutoSizing = false;
            label.overflowMode = TextOverflowModes.Overflow;
            state.Price.fontSize = 13;
            state.Price.enableAutoSizing = false;
            state.Price.overflowMode = TextOverflowModes.Overflow;
            var mark = frameKit.Decoration(button.transform, "ConfirmationMark", frameKit.Assets.cooldownRing, UiTokens.GoldHigh);
            UiKit.Stretch(mark.rectTransform);
            mark.rectTransform.offsetMin = Vector2.one * -3; mark.rectTransform.offsetMax = Vector2.one * 3;
            mark.transform.SetAsFirstSibling();
            var lockBadge = frameKit.Decoration(button.transform, "LockBadge", frameKit.Assets.iconLock, UiTokens.GoldHigh);
            lockBadge.rectTransform.anchorMin = lockBadge.rectTransform.anchorMax = lockBadge.rectTransform.pivot = Vector2.one;
            lockBadge.rectTransform.anchoredPosition = new Vector2(-1, -1); lockBadge.rectTransform.sizeDelta = Vector2.one * 22;
            radialLocks.Add(button.name, lockBadge.gameObject);
            button.gameObject.AddComponent<UiAccessibleButton>();
            button.gameObject.AddComponent<UiRadialAction>().Configure(() => RejectRadial(button.name));
            mark.gameObject.SetActive(false);
            state.Surface.sprite = frameKit.Assets.filledCircle;
            state.Surface.type = Image.Type.Simple;
        }

        private void LayoutInformationCard()
        {
            var layout = selectedPanel.GetComponent<VerticalLayoutGroup>();
            if (layout != null) layout.enabled = false;
            selectedHeaderImage.gameObject.SetActive(false);
            foreach (var text in new[] { selectedTitleText, selectedBadgeText, selectedRoleText }) text.transform.SetParent(selectedPanel.transform, false);
            PlaceCardText(selectedTitleText, 8, 0, 200, 22, 16);
            PlaceCardText(selectedBadgeText, 212, -4, 40, 18, 11);
            PlaceCardText(selectedRoleText, 8, -22, 244, 32, 13);
            PlaceCardText(selectedHintText, 8, -112, 244, 28, 11);
            selectedTitleText.color = UiTokens.TextPrimary;
            selectedText.richText = selectedHintText.richText = false;
            for (int i = 0; i < cardStatTiles.Length; i++)
            {
                var tile = frameKit.Decoration(selectedPanel.transform, "CardStat" + i, frameKit.Assets.pill, UiTokens.GlassStrong).rectTransform;
                tile.anchorMin = tile.anchorMax = tile.pivot = new Vector2(0, 1);
                tile.sizeDelta = new Vector2(76, 40);
                cardStatTiles[i] = tile;
                cardStatLabels[i] = frameKit.Text(tile, "CardStatLabel" + i, "", 11);
                cardStatValues[i] = i == 0 ? selectedText : frameKit.Text(tile, "CardStatValue" + i, "", 16);
                cardStatValues[i].transform.SetParent(tile, false);
                PlaceCardText(cardStatLabels[i], 4, -1, 68, 16, 11);
                PlaceCardText(cardStatValues[i], 4, -17, 68, 23, 16);
                cardStatValues[i].color = UiTokens.TextPrimary;
            }
            cardDetail = frameKit.Text(selectedPanel.transform, "CardDetail", "", 13);
            PlaceCardText(cardDetail, 8, -100, 244, 32, 13);
        }

        private void LayoutCardContent(bool statsVisible)
        {
            float y = 22;
            float roleHeight = Mathf.Ceil(selectedRoleText.GetPreferredValues(selectedRoleText.text, 244, 0).y);
            selectedRoleText.rectTransform.sizeDelta = new Vector2(244, roleHeight);
            y += roleHeight + 1;
            for (int i = 0; i < cardStatTiles.Length; i++)
            {
                cardStatTiles[i].gameObject.SetActive(statsVisible);
                cardStatTiles[i].anchoredPosition = new Vector2(8 + i * 84, -y);
            }
            if (statsVisible) y += 40;
            bool detailVisible = statsVisible && !string.IsNullOrEmpty(cardDetail.text);
            cardDetail.gameObject.SetActive(detailVisible);
            if (detailVisible)
            {
                float height = Mathf.Ceil(cardDetail.GetPreferredValues(cardDetail.text, 244, 0).y);
                cardDetail.rectTransform.anchoredPosition = new Vector2(8, -y);
                cardDetail.rectTransform.sizeDelta = new Vector2(244, height);
                y += height;
            }
            float hintHeight = Mathf.Ceil(selectedHintText.GetPreferredValues(selectedHintText.text, 244, 0).y);
            selectedHintText.rectTransform.anchoredPosition = new Vector2(8, -y);
            selectedHintText.rectTransform.sizeDelta = new Vector2(244, hintHeight);
            selectionCardHeight = Mathf.Max(selectionCardHeight, 120, y + hintHeight + 4);
            responsiveLayout.SetInformationCardHeight(selectionCardHeight);
        }

        private static void PlaceCardText(TMP_Text text, float x, float y, float width, float height, float size)
        {
            var rect = text.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, y); rect.sizeDelta = new Vector2(width, height);
            text.fontSize = size; text.enableAutoSizing = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.color = UiTokens.TextSecondary;
        }

        private void UpdateRadialSelection(GreyboxOutpost tower, GreyboxOutpostSlot slot, bool visible)
        {
            if (battleRadial == null) return;
            slot = tower != null ? tower.Slot : slot;
            if (!visible || slot == null)
            {
                if (battleRadial.IsOpen) { closingRadial = true; battleRadial.Close(); closingRadial = false; }
                CloseSpecializationChoices();
                selectionCardHeight = 0; radialActionPositioned = false;
                radialSlot = null; radialTower = null; previewOption = lockedPreviewOption = null;
                radialAction.gameObject.SetActive(false);
                radialRange?.Show(Vector3.zero, 1, false);
                SetInformationCardVisible(false);
                return;
            }
            bool changed = radialSlot != slot || radialTower != tower || radialLevel != (tower != null ? tower.Level : 0) ||
                radialBranch != (tower != null ? tower.Specialization : OutpostSpecialization.None);
            if (changed)
            {
                selectionCardHeight = 0; radialActionPositioned = false;
                CloseSpecializationChoices();
                radialSlot = slot; radialTower = tower; radialLevel = tower != null ? tower.Level : 0;
                responsiveLayout.ClearContextualSelection();
                if (Camera.main != null)
                    responsiveLayout.SetContextualSelectionFromScreenPoint(Camera.main.WorldToScreenPoint(slot.transform.position), tower == null);
                radialBranch = tower != null ? tower.Specialization : OutpostSpecialization.None;
                previewOption = lockedPreviewOption = null; radialAction.gameObject.SetActive(false);
                radialRange?.Show(Vector3.zero, 1, false);
                ConfigureRadialOptions();
                radialPositionDirty = radialRingDirty = true;
                battleRadial.gameObject.SetActive(true);
            }
            RefreshRadialAvailability();
            RefreshRadialCard();
        }

        private void ConfigureRadialOptions()
        {
            var choices = new List<UiRadialOption>();
            if (radialTower == null)
                foreach (OutpostKind kind in new[] { OutpostKind.Archer, OutpostKind.Barracks, OutpostKind.Mage })
                    choices.Add(new UiRadialOption(kind.ToString(), OutpostSprite(kind), TowerProgression.Base(kind).cost));
            else
            {
                if (radialTower.CanSpecialize)
                    for (int i = 0; i < 2; i++)
                    {
                        var branch = TowerProgression.Branch(radialTower.Kind, i);
                        choices.Add(new UiRadialOption(branch.ToString(), BranchIcon(branch), TowerProgression.Cost(radialTower.Kind)));
                    }
                else choices.Add(new UiRadialOption("Upgrade", radialCatalog.radialUpgradeIcon, radialTower.UpgradeCost));
                choices.Add(new UiRadialOption("Sell", radialCatalog.radialSellIcon, -radialTower.SellValue));
            }
            foreach (var button in radialButtons.Values) { button.gameObject.SetActive(false); button.interactable = false; }
            var buttons = new Button[choices.Count];
            for (int i = 0; i < choices.Count; i++)
            {
                buttons[i] = radialButtons[choices[i].Id];
                buttons[i].gameObject.SetActive(true); buttons[i].transform.SetAsLastSibling();
                buttons[i].GetComponent<UiPointSize>().Configure(choices[i].Id == "Sell" ? SellCaptionWidthPt : RadialButtonPt);
                buttons[i].GetComponent<UiVisualState>().Price.text = choices[i].Price < 0 ? $"+{-choices[i].Price}" : choices[i].Price.ToString();
            }
            radialOptions = choices.ToArray();
            battleRadial.Configure(battleRadialLayout, buttons, radialOptions);
        }

        private void RefreshRadialAvailability()
        {
            var session = GameSessionController.Instance;
            if (session == null || radialOptions == null || !battleRadial.IsOpen) return;
            for (int i = 0; i < radialOptions.Length; i++)
            {
                var option = radialOptions[i];
                if (radialTower == null && !Enum.TryParse<OutpostKind>(option.Id, out _)) return;
                bool enabled = radialTower == null ? session.CanBuild(Enum.Parse<OutpostKind>(option.Id)) :
                    option.Id == "Sell" || (option.Id == "Upgrade" ? radialTower.CanUpgrade : session.UnlockedCampaignLevel >= TowerProgression.UnlockLevel(radialTower.Kind));
                battleRadial.SetAvailable(i, enabled);
                battleRadial.SetAffordable(i, session.Gold >= option.Price);
                var state = radialButtons[option.Id].GetComponent<UiVisualState>();
                state.SetPreviewed(previewOption == option.Id && option.Id != "Sell");
                var label = radialLabels[option.Id];
                label.text = option.Id == "Sell" ? (saleConfirmationOutpost != null ? FrontendCopy.RadialConfirm.Value : FrontendCopy.RadialSell.Value) :
                    "";
                radialMarks[option.Id].SetActive(previewOption == option.Id && option.Id != "Sell");
                label.fontSize = 13;
                if (option.Id == "Sell")
                {
                    label.gameObject.SetActive(false);
                    state.Price.text = saleConfirmationOutpost != null ? FrontendCopy.RadialConfirm.Value : FrontendCopy.SweepRadialSell.Format(radialTower.SellValue);
                    var chip = (RectTransform)state.Price.transform.parent;
                    chip.sizeDelta = new Vector2(SellCaptionWidthPt, 18);
                    chip.anchoredPosition = Vector2.zero;
                    state.Price.rectTransform.offsetMin = Vector2.zero;
                    chip.Find("PriceCoin").gameObject.SetActive(false);
                }
                else state.Price.transform.parent.gameObject.SetActive(previewOption != option.Id);
                radialLocks[option.Id].SetActive(!enabled);
                state.Price.color = !enabled ? UiTokens.TextSecondary : session.Gold < option.Price ? UiTokens.Danger : UiTokens.Gold;
                state.Price.fontSize = 13;
                var name = option.Id == "Upgrade" ? FrontendCopy.RadialUpgrade.Value : option.Id == "Sell" ? FrontendCopy.RadialSell.Value :
                    radialTower == null ? GameSessionController.OutpostDisplayName(Enum.Parse<OutpostKind>(option.Id)) : TowerProgression.Name(Enum.Parse<OutpostSpecialization>(option.Id));
                radialButtons[option.Id].GetComponent<UiAccessibleButton>().Configure(new LocalizedCopy(
                    name + (!enabled ? " · " + FrontendCopy.Locked.Value : "") + " · " + state.Price.text,
                    name + (!enabled ? " · " + FrontendCopy.Locked.Value : "") + " · " + state.Price.text));
            }
            RefreshRadialAction();
        }

        private void RefreshRadialAction()
        {
            bool visible = previewOption != null && previewOption != "Sell" && radialSlot != null;
            if (radialAction.gameObject.activeSelf != visible) { radialAction.gameObject.SetActive(visible); radialPositionDirty = true; }
            if (!visible) return;
            var option = radialOptions[battleRadial.PreviewIndex];
            bool affordable = GameSessionController.Instance.Gold >= option.Price;
            string verb = radialTower == null ? FrontendCopy.RadialBuild.Value : previewOption == "Upgrade" ? FrontendCopy.RadialUpgrade.Value : FrontendCopy.RadialSelect.Value;
            if (radialAction.interactable != affordable) radialActionState.SetState(affordable ? UiState.Normal : UiState.Disabled);
            radialActionLabel.text = affordable ? $"{verb} · {option.Price}" : FrontendCopy.SweepRadialMissingAction.Format(verb, option.Price - GameSessionController.Instance.Gold);
            radialAction.GetComponent<UiAccessibleButton>().Configure(new LocalizedCopy(radialActionLabel.text, radialActionLabel.text));
        }

        private void ConfirmRadialAction()
        {
            if (previewOption == null || battleRadial.PreviewIndex < 0) return;
            ConfirmRadial(radialOptions[battleRadial.PreviewIndex]);
        }

        private void PreviewRadial(UiRadialOption option)
        {
            if (radialSlot == null) return;
            if (option.Id != "Sell") CancelSaleConfirmation();
            lockedPreviewOption = null;
            previewOption = option.Id; radialPositionDirty = true;
            if (option.Id == "Sell") { radialRange?.Show(Vector3.zero, 1, false); CloseSpecializationChoices(); SellFromUi(); }
            else if (radialTower != null && Enum.TryParse(option.Id, out OutpostSpecialization branch))
                BeginRadialSpecialization(GameSessionController.Instance);
            RefreshRadialAvailability(); RefreshRadialCard();
            GameSessionController.Instance.RefreshRadialSelectionPreview();
        }

        private UiMotion.Handle rejectionMotion;
        private Vector2 rejectionRest;
        private RectTransform rejectionTarget;
        private void RejectRadial(string id)
        {
            rejectionMotion?.Cancel();
            if (rejectionTarget != null) rejectionTarget.anchoredPosition = rejectionRest;
            if (id == null || !radialButtons.ContainsKey(id)) return;
            if (!IsRadialOptionUnlocked(id))
            {
                CloseSpecializationChoices();
                previewOption = null;
                lockedPreviewOption = id;
                battleRadial.ResetPreview();
                radialAction.gameObject.SetActive(false);
                radialRange?.Show(Vector3.zero, 1, false);
                radialPositionDirty = true;
            }
            rejectionTarget = (RectTransform)radialButtons[id].GetComponent<UiVisualState>().Price.transform.parent;
            if (!rejectionTarget.gameObject.activeInHierarchy)
                rejectionTarget = radialAction.gameObject.activeInHierarchy ? (RectTransform)radialAction.transform :
                    (RectTransform)radialButtons[id].transform;
            rejectionRest = rejectionTarget.anchoredPosition;
            rejectionMotion = (GetComponent<UiMotion>() ?? gameObject.AddComponent<UiMotion>()).Shake(rejectionTarget);
            UiSounds.Play(PresentationCue.UiError);
            UiHaptics.Error();
            RefreshRadialCard();
            RadialRejected?.Invoke(id);
        }

        private void ConfirmRadial(UiRadialOption option)
        {
            var session = GameSessionController.Instance;
            if (session == null || radialSlot == null) return;
            if (!IsRadialOptionUnlocked(option.Id)) { RejectRadial(option.Id); return; }
            if (option.Id == "Sell") { SellFromUi(); RefreshRadialAvailability(); return; }
            if (session.Gold < option.Price) { RejectRadial(option.Id); return; }
            var slot = radialSlot;
            if (radialTower == null && slot.Occupant == null)
                session.TryBuildOutpost(slot.Index, Enum.Parse<OutpostKind>(option.Id));
            else if (radialTower != null && slot.Occupant == radialTower)
            {
                if (option.Id == "Upgrade") session.TryUpgradeOutpost(slot.Index);
                else
                {
                    CloseSpecializationChoices();
                    session.TrySpecializeOutpost(slot.Index, Enum.Parse<OutpostSpecialization>(option.Id));
                }
            }
            previewOption = lockedPreviewOption = null; radialAction.gameObject.SetActive(false);
            battleRadial.ResetPreview();
            radialRange?.Show(Vector3.zero, 1, false);
            UpdateRadialSelection(slot.Occupant, slot, true);
            session.RefreshRadialSelectionPreview();
        }

        private void DismissRadial()
        {
            if (closingRadial) return;
            CloseSpecializationChoices(); CancelSaleConfirmation();
            cancelRadialSelection?.Invoke();
        }

        private bool IsRadialOptionUnlocked(string id)
        {
            var session = GameSessionController.Instance;
            if (session == null) return false;
            if (radialTower == null) return Enum.TryParse(id, out OutpostKind kind) && session.CanBuild(kind);
            return id == "Sell" || (id == "Upgrade" ? radialTower.CanUpgrade :
                session.UnlockedCampaignLevel >= TowerProgression.UnlockLevel(radialTower.Kind));
        }

        private void RefreshRadialCard()
        {
            if (radialSlot == null) return;
            var session = GameSessionController.Instance;
            string option = lockedPreviewOption ?? previewOption;
            if (radialTower == null && option == null)
            {
                SetInformationCardVisible(false);
                return;
            }
            SetInformationCardVisible(true);
            OutpostKind kind = radialTower != null ? radialTower.Kind : Enum.Parse<OutpostKind>(option);
            var before = TowerProgression.Stats(kind, radialTower != null ? radialTower.Level : 1,
                radialTower != null ? radialTower.Specialization : OutpostSpecialization.None);
            var after = before;
            int price = radialTower == null ? TowerProgression.Base(kind).cost : 0;
            selectedTitleText.text = GameSessionController.OutpostDisplayName(kind);
            selectedRoleText.text = kind switch { OutpostKind.Archer => FrontendCopy.ArcherRole.Value, OutpostKind.Barracks => FrontendCopy.BarracksRole.Value, _ => FrontendCopy.MageRole.Value };
            selectedBadgeText.text = radialTower != null ? FrontendCopy.TowerLevel.Format(radialTower.Level) : "";
            selectedRoleText.gameObject.SetActive(true);
            selectedRoleText.color = UiTokens.TextSecondary;
            bool upgradePreview = option == "Upgrade" && radialTower.CanUpgrade;
            bool comparing = upgradePreview || radialTower != null && Enum.TryParse(option, out OutpostSpecialization _);
            cardDetail.text = "";
            if (upgradePreview)
            {
                after = TowerProgression.Stats(kind, 2);
                price = radialTower.UpgradeCost;
            }
            else if (radialTower != null && Enum.TryParse(option, out OutpostSpecialization branch) && branch != OutpostSpecialization.None)
            {
                after = TowerProgression.Stats(kind, 2, branch);
                price = TowerProgression.Cost(kind);
                selectedTitleText.text = TowerProgression.Name(branch);
                selectedRoleText.text = TowerProgression.Description(branch);
                cardDetail.text = branch switch
                {
                    OutpostSpecialization.ShieldGuard => FrontendCopy.BattleCardGuardHealth.Format(before.guardHealth, after.guardHealth,
                        before.guardHealth * before.guardCount, after.guardHealth * after.guardCount),
                    OutpostSpecialization.Blast => FrontendCopy.BattleCardSplash.Format(before.splashRadius, after.splashRadius),
                    _ => ""
                };
            }
            else if (radialTower != null && radialTower.Specialization != OutpostSpecialization.None)
            {
                selectedTitleText.text = TowerProgression.Name(radialTower.Specialization);
                selectedRoleText.text = TowerProgression.Description(radialTower.Specialization);
            }
            cardStatLabels[0].text = FrontendCopy.Damage.Value;
            cardStatLabels[1].text = FrontendCopy.Range.Value;
            cardStatLabels[2].text = FrontendCopy.BattleCardRate.Value;
            cardStatValues[0].text = comparing ? FrontendCopy.BattleCardDelta.Format(before.damage, after.damage) : before.damage.ToString();
            cardStatValues[1].text = comparing ? FrontendCopy.BattleCardDecimalDelta.Format(before.range, after.range) : FrontendCopy.BattleCardDecimal.Format(before.range);
            cardStatValues[2].text = comparing ? FrontendCopy.BattleCardDecimalDelta.Format(before.attacksPerSecond, after.attacksPerSecond) : FrontendCopy.BattleCardDecimal.Format(before.attacksPerSecond);
            bool missingGold = price > 0 && session != null && session.Gold < price;
            selectedHintText.text = option == "Sell" ? FrontendCopy.SellingReturnsGold.Format(radialTower.SellValue) :
                missingGold ? FrontendCopy.RadialMissing.Format(price - session.Gold) :
                specializationSession != null ? FrontendCopy.BattlePaused.Value : "";
            if (lockedPreviewOption != null)
            {
                int chapter = radialTower != null ? TowerProgression.UnlockLevel(kind) : TowerProgression.BuildUnlockLevel(kind);
                selectedHintText.text = lockedPreviewOption == "Upgrade" ? FrontendCopy.MaxLevelSellForGold.Format(radialTower.SellValue) :
                    FrontendCopy.BattleCardUnlockChapter.Format(chapter);
            }
            selectedHintText.color = lockedPreviewOption != null ? UiTokens.Gold :
                price > 0 && session != null && session.Gold < price ? UiTokens.Danger : UiTokens.TextSecondary;
            LayoutCardContent(true);
            if (previewOption != null && previewOption != "Sell")
            {
                if (radialRange == null) radialRange = CombatRangeVisual.Create(transform, "Radial preview range", UiTokens.Gold);
                radialRange.Show(radialSlot.transform.position, after.range, true);
            }
            if (Camera.main != null) PositionBattleRadial(Camera.main.WorldToScreenPoint(radialSlot.transform.position));
        }

        private void SetInformationCardVisible(bool visible)
        {
            if (selectedPanel.activeSelf == visible) return;
            selectedPanel.SetActive(visible);
            responsiveLayout.RefreshLayout();
            radialPositionDirty = true;
        }

        private Rect SelectionScreenFootprint(Vector2 screenPoint)
        {
            var camera = Camera.main;
            var target = radialTower != null ? radialTower.transform : radialSlot != null ? radialSlot.transform : null;
            Vector2 origin = camera != null && target != null ? (Vector2)camera.WorldToScreenPoint(radialSlot.transform.position) : screenPoint;
            Vector2 min = origin - Vector2.one * 12, max = origin + Vector2.one * 12;
            if (camera != null && target != null)
            {
                radialRenderers.Clear();
                target.GetComponentsInChildren(radialRenderers);
                if (radialTower != null && radialSlot != null)
                {
                    radialSlotRenderers.Clear();
                    radialSlot.GetComponentsInChildren(radialSlotRenderers);
                    radialRenderers.AddRange(radialSlotRenderers);
                }
                foreach (var renderer in radialRenderers)
                {
                    if (!renderer.enabled || renderer is LineRenderer || !renderer.gameObject.activeInHierarchy) continue;
                    var bounds = renderer.bounds;
                    for (int i = 0; i < 8; i++)
                    {
                        var point = bounds.center + Vector3.Scale(bounds.extents,
                            new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                        Vector2 screen = camera.WorldToScreenPoint(point);
                        min = Vector2.Min(min, screen); max = Vector2.Max(max, screen);
                    }
                }
            }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private bool RadialActionFits(Vector2 point, Rect safe, Vector2 size)
        {
            var candidate = new Rect(point - size / 2, size);
            if (candidate.xMin < safe.xMin || candidate.xMax > safe.xMax || candidate.yMin < safe.yMin || candidate.yMax > safe.yMax) return false;
            foreach (var obstacle in radialObstacles) if (candidate.Overlaps(obstacle)) return false;
            foreach (var button in radialButtons.Values)
            {
                if (!button.gameObject.activeSelf) continue;
                var target = (RectTransform)button.transform;
                Vector2 c = ((RectTransform)battleRadial.transform).InverseTransformPoint(target.TransformPoint(target.rect.center));
                float diameter = Mathf.Max(target.rect.width, target.rect.height) + UiTokens.Space1 * 2;
                if (candidate.Overlaps(new Rect(c - Vector2.one * diameter / 2, Vector2.one * diameter))) return false;
            }
            return true;
        }

        private void PositionRadialAction(Rect safe, Vector2 center)
        {
            if (!radialAction.gameObject.activeSelf || previewOption == null || radialActionPositioned) return;
            var actionRect = (RectTransform)radialAction.transform;
            actionRect.anchorMin = actionRect.anchorMax = actionRect.pivot = new Vector2(.5f, .5f);
            Vector2 size = new Vector2(RadialActionWidthPt, UiMetrics.PrimaryCtaPt);
            actionRect.sizeDelta = size;
            float bottom = center.y, top = center.y;
            foreach (var button in radialButtons.Values)
            {
                if (!button.gameObject.activeSelf) continue;
                var target = (RectTransform)button.transform;
                Vector2 c = ((RectTransform)battleRadial.transform).InverseTransformPoint(target.TransformPoint(target.rect.center));
                bottom = Mathf.Min(bottom, c.y - target.rect.height / 2);
                top = Mathf.Max(top, c.y + target.rect.height / 2);
            }
            Vector2 position = new Vector2(center.x, bottom - UiTokens.Space2 - size.y / 2);
            if (!RadialActionFits(position, safe, size))
            {
                position = new Vector2(center.x, top + UiTokens.Space2 + size.y / 2);
                if (!RadialActionFits(position, safe, size))
                {
                    float best = float.MaxValue;
                    Vector2 desired = new Vector2(center.x, bottom - UiTokens.Space2 - size.y / 2);
                    for (float y = safe.yMin + size.y / 2; y <= safe.yMax - size.y / 2; y += RadialActionSearchStepPt)
                        for (float x = safe.xMin + size.x / 2; x <= safe.xMax - size.x / 2; x += RadialActionSearchStepPt)
                        {
                            var point = new Vector2(x, y);
                            float score = (point - desired).sqrMagnitude;
                            if (score >= best || !RadialActionFits(point, safe, size)) continue;
                            position = point; best = score;
                        }
                }
            }
            actionRect.anchoredPosition = position;
            radialActionPositioned = true;
            radialAction.transform.SetAsLastSibling();
        }

        private void PositionBattleRadial(Vector2 screenPoint)
        {
            if (battleRadial == null || !battleRadial.IsOpen) return;
            var root = (RectTransform)transform;
            var rect = (RectTransform)battleRadial.transform;
            var points = UiMetrics.PointsPerCanvasUnit(GetComponentInParent<Canvas>());
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.Scale(root.rect.size, points);
            rect.localScale = new Vector3(1 / points.x, 1 / points.y, 1);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screenPoint, null, out var center);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, Screen.safeArea.min, null, out var min);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, Screen.safeArea.max, null, out var max);
            var safe = Rect.MinMaxRect(min.x + 6, min.y + 6, max.x - 6, max.y - 6);
            // Ignore small anchor shifts from camera hit shake.
            bool moved = (placedCenter - center).sqrMagnitude > RadialFollowThresholdPt * RadialFollowThresholdPt ||
                placedSafeArea != safe || placedSize != rect.sizeDelta;
            if (!radialPositionDirty && !radialRingDirty && !moved) return;
            radialPositionDirty = false;
            if (!radialRingDirty && !moved)
            {
                PositionRadialAction(safe, placedCenter);
                return;
            }
            radialActionPositioned = false;
            var obstacles = radialObstacles;
            obstacles.Clear();
            Rect footprint = SelectionScreenFootprint(screenPoint);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, footprint.min, null, out var footprintMin);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, footprint.max, null, out var footprintMax);
            obstacles.Add(Rect.MinMaxRect(footprintMin.x - 8, footprintMin.y - 8, footprintMax.x + 8, footprintMax.y + 8));
            radialFrameRects.Clear();
            responsiveLayout.AppendFrameScreenRects(radialFrameRects, includeInformationCard: false);
            radialFrameRects.Add(responsiveLayout.ToastScreenRect());
            radialFrameRects.Add(responsiveLayout.InformationCardScreenRect(ReservedInformationCardHeight));
            foreach (var screenRect in radialFrameRects)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screenRect.min, null, out var a);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screenRect.max, null, out var b);
                float frameGap = UiTokens.Space1 + RadialButtonPt * (RadialSelectedScale - 1) / 2;
                obstacles.Add(Rect.MinMaxRect(a.x - frameGap, a.y - frameGap,
                    b.x + frameGap, b.y + frameGap));
            }
            foreach (var target in radialTutorialControls)
            {
                if (target == null || !target.gameObject.activeInHierarchy) continue;
                var corners = radialCorners; ((RectTransform)target.transform).GetWorldCorners(corners);
                Vector2 a = rect.InverseTransformPoint(corners[0]), b = rect.InverseTransformPoint(corners[2]);
                obstacles.Add(Rect.MinMaxRect(a.x - 4, a.y - 4, b.x + 4, b.y + 4));
            }
            if (tutorialBubble != null && tutorialBubble.gameObject.activeInHierarchy)
            {
                tutorialBubble.GetWorldCorners(radialCorners);
                Vector2 a = rect.InverseTransformPoint(radialCorners[0]), b = rect.InverseTransformPoint(radialCorners[2]);
                obstacles.Add(Rect.MinMaxRect(a.x - 4, a.y - 4, b.x + 4, b.y + 4));
            }
            if (radialRingDirty || moved)
            {
                // Obstacles reach the layout group only here, so later layout rebuilds reproduce the same ring.
                radialRingDirty = false; placedCenter = center; placedSafeArea = safe; placedSize = rect.sizeDelta;
                battleRadialLayout.SetObstacles(obstacles);
                battleRadialLayout.Place(center, safe);
                foreach (var button in radialButtons.Values)
                    if (button.gameObject.activeSelf) button.GetComponent<UiPointSize>().Refresh();
                LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
            }
            PositionRadialAction(safe, center);
        }
    }
}
