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
        private UiRadialMenu battleRadial;
        private UiRadialLayout battleRadialLayout;
        private readonly Dictionary<string, Button> radialButtons = new();
        private UiRadialOption[] radialOptions;
        private GreyboxOutpostSlot radialSlot;
        private GreyboxOutpost radialTower;
        private int radialLevel;
        private OutpostSpecialization radialBranch;
        private string previewOption;
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
            var first = battleRadial.GetComponentInChildren<Button>();
            first.name = "Archer"; UiId.Assign(first.transform, "Archer");
            radialButtons.Add("Archer", first);
            StyleRadialButton(first);
            archerButton = first;
            barracksButton = MakeRadialButton("Barracks", barracksIconSprite, BarracksCost);
            mageButton = MakeRadialButton("Mage", mageIconSprite, MageCost);
            upgradeButton = MakeRadialButton("Upgrade", frameKit.Assets.iconUpgrade, 0);
            sellButton = MakeRadialButton("Sell", frameKit.Assets.iconSell, 0);
            foreach (OutpostSpecialization branch in Enum.GetValues(typeof(OutpostSpecialization)))
                if (branch != OutpostSpecialization.None)
                    MakeRadialButton(branch.ToString(), BranchIcon(branch), 0);
            battleRadial.CloseOnConfirm = false;
            battleRadial.Previewed += PreviewRadial;
            battleRadial.Confirmed += ConfirmRadial;
            battleRadial.Rejected += option => { RefreshRadialCard(); RadialRejected?.Invoke(option.Id); };
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
            OutpostSpecialization.RapidFire => frameKit.Assets.iconRapidFire,
            OutpostSpecialization.Sharpshooter => frameKit.Assets.iconSharpshooter,
            OutpostSpecialization.ShieldGuard => frameKit.Assets.iconShield,
            OutpostSpecialization.AssaultGuard => frameKit.Assets.iconAssaultGuard,
            OutpostSpecialization.Blast => frameKit.Assets.iconBlast,
            _ => frameKit.Assets.iconFrost
        };

        private Button MakeRadialButton(string id, Sprite icon, int price)
        {
            var button = frameKit.Button(battleRadial.transform, id, "", UiButtonVariant.Glass, icon, price, verticalContent: true);
            button.GetComponent<UiPointSize>().Configure(60);
            button.GetComponent<UiVisualState>().Icon.GetComponent<UiProportionalIcon>().Configure(button.GetComponent<UiPointSize>());
            StyleRadialButton(button);
            radialButtons.Add(id, button);
            return button;
        }

        private void StyleRadialButton(Button button)
        {
            foreach (var group in button.GetComponentsInChildren<LayoutGroup>(true)) group.enabled = false;
            var content = button.transform.Find(button.name + ".Content") as RectTransform;
            if (content == null) content = button.transform.Find("BattleRadial.Archer.Content") as RectTransform;
            UiKit.Stretch(content);
            var state = button.GetComponent<UiVisualState>();
            var icon = state.Icon.rectTransform;
            icon.anchorMin = icon.anchorMax = icon.pivot = new Vector2(.5f, .5f);
            icon.anchoredPosition = new Vector2(0, 7); icon.sizeDelta = Vector2.one * 27;
            var proportional = icon.GetComponent<UiProportionalIcon>();
            if (proportional != null) proportional.enabled = false;
            var chip = (RectTransform)state.Price.transform.parent;
            chip.anchorMin = chip.anchorMax = chip.pivot = new Vector2(.5f, 0);
            chip.anchoredPosition = new Vector2(0, 3); chip.sizeDelta = new Vector2(48, 18);
            UiKit.Stretch(state.Price.rectTransform); state.Price.alignment = TextAlignmentOptions.Center;
            var label = button.GetComponentInChildren<TMP_Text>(true);
            label.gameObject.SetActive(true);
            var rect = label.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, 1);
            rect.anchoredPosition = Vector2.zero; rect.sizeDelta = new Vector2(62, 18);
            label.alignment = TextAlignmentOptions.Center;
            var mark = frameKit.Decoration(button.transform, "ConfirmationMark", frameKit.Assets.iconCheck, UiTokens.Gold);
            mark.rectTransform.anchorMin = mark.rectTransform.anchorMax = mark.rectTransform.pivot = Vector2.one;
            mark.rectTransform.anchoredPosition = new Vector2(-2, -2); mark.rectTransform.sizeDelta = Vector2.one * 18;
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
            PlaceCardText(selectedTitleText, 8, -3, 158, 18, 12);
            PlaceCardText(selectedBadgeText, 168, -3, 28, 18, 10);
            PlaceCardText(selectedRoleText, 8, -22, 182, 14, 9);
            PlaceCardText(selectedText, 8, -38, 182, 16, 10);
            PlaceCardText(selectedHintText, 8, -57, 182, 22, 9);
            selectedText.richText = false;
            selectedHintText.richText = false;
        }

        private static void PlaceCardText(TMP_Text text, float x, float y, float width, float height, float size)
        {
            var rect = text.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, y); rect.sizeDelta = new Vector2(width, height);
            text.fontSize = size; text.enableAutoSizing = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Ellipsis;
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
                radialSlot = null; radialTower = null; previewOption = null;
                radialRange?.Show(Vector3.zero, 1, false);
                return;
            }
            bool changed = radialSlot != slot || radialTower != tower || radialLevel != (tower != null ? tower.Level : 0) ||
                radialBranch != (tower != null ? tower.Specialization : OutpostSpecialization.None);
            if (changed)
            {
                CloseSpecializationChoices();
                radialSlot = slot; radialTower = tower; radialLevel = tower != null ? tower.Level : 0;
                radialBranch = tower != null ? tower.Specialization : OutpostSpecialization.None;
                previewOption = null;
                radialRange?.Show(Vector3.zero, 1, false);
                ConfigureRadialOptions();
                radialPositionDirty = true;
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
                else choices.Add(new UiRadialOption("Upgrade", frameKit.Assets.iconUpgrade, radialTower.UpgradeCost));
                choices.Add(new UiRadialOption("Sell", frameKit.Assets.iconSell, -radialTower.SellValue));
            }
            foreach (var button in radialButtons.Values) { button.gameObject.SetActive(false); button.interactable = false; }
            var buttons = new Button[choices.Count];
            for (int i = 0; i < choices.Count; i++)
            {
                buttons[i] = radialButtons[choices[i].Id];
                buttons[i].gameObject.SetActive(true); buttons[i].transform.SetAsLastSibling();
                buttons[i].GetComponent<UiPointSize>().Configure(60);
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
                var label = radialLabels[option.Id];
                label.text = option.Id == "Sell" ? (saleConfirmationOutpost != null ? FrontendCopy.RadialConfirm.Value : FrontendCopy.RadialSell.Value) :
                    !enabled ? FrontendCopy.Locked.Value : "";
                radialMarks[option.Id].SetActive(previewOption == option.Id && session.Gold >= option.Price && option.Id != "Sell");
                label.fontSize = 10;
                if (option.Id == "Sell") { label.text = saleConfirmationOutpost != null ? FrontendCopy.RadialConfirm.Value : FrontendCopy.RadialSell.Value; }
                state.Price.fontSize = 12;
            }
        }

        private void PreviewRadial(UiRadialOption option)
        {
            if (radialSlot == null) return;
            if (option.Id != "Sell") CancelSaleConfirmation();
            previewOption = option.Id;
            if (option.Id == "Sell") { radialRange?.Show(Vector3.zero, 1, false); CloseSpecializationChoices(); SellFromUi(); }
            else if (radialTower != null && Enum.TryParse(option.Id, out OutpostSpecialization branch))
                BeginRadialSpecialization(GameSessionController.Instance);
            RefreshRadialAvailability(); RefreshRadialCard();
        }

        private void ConfirmRadial(UiRadialOption option)
        {
            var session = GameSessionController.Instance;
            if (session == null || radialSlot == null) return;
            if (option.Id == "Sell") { SellFromUi(); RefreshRadialAvailability(); return; }
            if (session.Gold < option.Price) { RadialRejected?.Invoke(option.Id); RefreshRadialCard(); return; }
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
            previewOption = null;
            battleRadial.ResetPreview();
            radialRange?.Show(Vector3.zero, 1, false);
            UpdateRadialSelection(slot.Occupant, slot, true);
        }

        private void DismissRadial()
        {
            if (closingRadial) return;
            CloseSpecializationChoices(); CancelSaleConfirmation();
            cancelRadialSelection?.Invoke();
        }

        private void RefreshRadialCard()
        {
            if (radialSlot == null) return;
            var session = GameSessionController.Instance;
            if (radialTower == null && previewOption == null) return;
            OutpostKind kind = radialTower != null ? radialTower.Kind : Enum.Parse<OutpostKind>(previewOption);
            var before = TowerProgression.Stats(kind, radialTower != null ? radialTower.Level : 1,
                radialTower != null ? radialTower.Specialization : OutpostSpecialization.None);
            var after = before;
            int price = radialTower == null ? TowerProgression.Base(kind).cost : 0;
            selectedTitleText.text = GameSessionController.OutpostDisplayName(kind);
            selectedRoleText.text = kind switch { OutpostKind.Archer => FrontendCopy.ArcherRole.Value, OutpostKind.Barracks => FrontendCopy.BarracksRole.Value, _ => FrontendCopy.MageRole.Value };
            selectedBadgeText.text = radialTower != null ? FrontendCopy.TowerLevel.Format(radialTower.Level) : "";
            bool comparing = previewOption == "Upgrade" || radialTower != null && Enum.TryParse(previewOption, out OutpostSpecialization _);
            selectedRoleText.gameObject.SetActive(!comparing);
            selectedText.rectTransform.anchoredPosition = new Vector2(8, comparing ? -24 : -38);
            selectedText.rectTransform.sizeDelta = new Vector2(182, comparing ? 29 : 16);
            selectedText.text = FrontendCopy.RadialStats.Format(before.damage, before.range, before.attacksPerSecond);
            if (previewOption == "Upgrade")
            {
                after = TowerProgression.Stats(kind, 2);
                price = radialTower.UpgradeCost;
                selectedText.text = FrontendCopy.RadialUpgradeStats.Format(before.damage, after.damage, before.range, after.range, before.attacksPerSecond, after.attacksPerSecond);
            }
            else if (radialTower != null && Enum.TryParse(previewOption, out OutpostSpecialization branch) && branch != OutpostSpecialization.None)
            {
                after = TowerProgression.Stats(kind, 2, branch);
                price = TowerProgression.Cost(kind);
                selectedTitleText.text = TowerProgression.Name(branch);
                selectedRoleText.text = TowerProgression.Description(branch);
                selectedText.text = SpecializationDifferences(before, after, branch);
            }
            else if (radialTower != null && radialTower.Specialization != OutpostSpecialization.None)
            {
                selectedTitleText.text = TowerProgression.Name(radialTower.Specialization);
                selectedRoleText.text = TowerProgression.Description(radialTower.Specialization);
            }
            string cost = price > 0 ? FrontendCopy.Gold.Format(price) + " · " + (session != null && session.Gold < price
                ? FrontendCopy.RadialMissing.Format(price - session.Gold) : FrontendCopy.RadialTapAgain.Value) : previewOption == "Sell" ? FrontendCopy.SellingReturnsGold.Format(radialTower.SellValue) : "";
            selectedHintText.text = specializationSession != null && comparing ? FrontendCopy.BattlePaused.Value + " · " +
                (session.Gold < price ? FrontendCopy.RadialMissing.Format(price - session.Gold) : FrontendCopy.Gold.Format(price)) +
                "\n" + FrontendCopy.ChangeBranchHint.Value : specializationSession != null ?
                FrontendCopy.BattlePaused.Value + "\n" + FrontendCopy.ChangeBranchHint.Value : cost;
            selectedHintText.color = price > 0 && session != null && session.Gold < price ? UiTokens.Danger : UiTokens.TextSecondary;
            if (previewOption != null && previewOption != "Sell")
            {
                if (radialRange == null) radialRange = CombatRangeVisual.Create(transform, "Radial preview range", UiTokens.Gold);
                radialRange.Show(radialSlot.transform.position, after.range, true);
            }
        }

        private static string SpecializationDifferences(TowerCombatStats before, TowerCombatStats after, OutpostSpecialization branch) => branch switch
        {
            OutpostSpecialization.ShieldGuard => FrontendCopy.GuardDifferences.Format(before.guardHealth, after.guardHealth, before.guardHealth * before.guardCount, after.guardHealth * after.guardCount),
            OutpostSpecialization.Blast => FrontendCopy.BlastDifferences.Format(before.damage, after.damage, before.splashRadius, after.splashRadius),
            OutpostSpecialization.Frost => FrontendCopy.FrostDifferences.Format(before.damage, after.damage, Mathf.RoundToInt((1 - after.slowFactor) * 100), after.slowSeconds),
            _ => FrontendCopy.AttackDifferences.Format(before.damage, after.damage, before.attacksPerSecond, after.attacksPerSecond)
        };

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
            var obstacles = radialObstacles;
            obstacles.Clear();
            Rect footprint = SelectionScreenFootprint(screenPoint);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, footprint.min, null, out var footprintMin);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, footprint.max, null, out var footprintMax);
            obstacles.Add(Rect.MinMaxRect(footprintMin.x - 8, footprintMin.y - 8, footprintMax.x + 8, footprintMax.y + 8));
            radialFrameRects.Clear();
            responsiveLayout.AppendFrameScreenRects(radialFrameRects);
            foreach (var screenRect in radialFrameRects)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screenRect.min, null, out var a);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screenRect.max, null, out var b);
                obstacles.Add(Rect.MinMaxRect(a.x - 4, a.y - 4, b.x + 4, b.y + 4));
            }
            foreach (var target in radialTutorialControls)
            {
                if (target == null || !target.gameObject.activeInHierarchy) continue;
                var corners = radialCorners; ((RectTransform)target.transform).GetWorldCorners(corners);
                Vector2 a = rect.InverseTransformPoint(corners[0]), b = rect.InverseTransformPoint(corners[2]);
                obstacles.Add(Rect.MinMaxRect(a.x - 4, a.y - 4, b.x + 4, b.y + 4));
            }
            bool changed = battleRadialLayout.SetObstacles(obstacles);
            var safe = Rect.MinMaxRect(min.x + 6, min.y + 6, max.x - 6, max.y - 6);
            if (!radialPositionDirty && !changed && placedCenter == center && placedSafeArea == safe && placedSize == rect.sizeDelta) return;
            radialPositionDirty = false; placedCenter = center; placedSafeArea = safe; placedSize = rect.sizeDelta;
            battleRadialLayout.Place(center, safe);
            foreach (var button in radialButtons.Values)
                if (button.gameObject.activeSelf) button.GetComponent<UiPointSize>().Refresh();
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        }
    }
}
