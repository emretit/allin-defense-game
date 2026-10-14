using System.Collections;
using System.Collections.Generic;
using AllinDefense.Data;
using AllinDefense.UI.Kit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AllinDefense.UI
{
    public sealed partial class AppFlowController
    {
        private readonly List<Image> regionTabLocks = new();
        private readonly Dictionary<int, RectTransform> regionLockCards = new();
        private readonly Dictionary<int, TMP_Text> regionLockConditions = new();
        private RectTransform campaignCompletionCard;
        private TMP_Text completionHeading, completionNext;
        private readonly List<TMP_Text> completionRegions = new();
        private Button completionAction;
        private bool resultWasVictory;
        private int resultEarnedStars;
        private int? campaignDestinationLevel;

        private bool IsRegionUnlocked(int region) => region == 1 ||
            progress.ProgressFor((region - 1) * CampaignCatalog.LevelsPerRegion)?.stars > 0;

        private void RefreshRegionLocks()
        {
            foreach (var pair in regionLockCards)
            {
                bool unlocked = IsRegionUnlocked(pair.Key);
                pair.Value.gameObject.SetActive(!unlocked);
                regionLockConditions[pair.Key].text = FrontendCopy.RegionLocked.Format(
                    (pair.Key - 1) * CampaignCatalog.LevelsPerRegion);
                foreach (Transform child in pair.Value.parent)
                    if (child != pair.Value && child.name != "Region Name") child.gameObject.SetActive(unlocked);
            }
            if (briefingSheet != null)
                briefingSheet.gameObject.SetActive(IsRegionUnlocked(navigationState.Region + 1));
        }

        private TMP_Text CompletionText(Transform parent, string id, float size, bool bold = false)
        {
            var text = MenuText(parent, id, "", size, bold);
            text.textWrappingMode = TextWrappingModes.Normal;
            text.enableAutoSizing = true; text.fontSizeMin = size - 6; text.fontSizeMax = size;
            UiKit.Size(text.rectTransform, height: size * 1.6f);
            return text;
        }

        private void BuildCampaignCompletionCard(Transform parent)
        {
            campaignCompletionCard = menuKit.Card(parent, "Campaign Completion");
            campaignCompletionCard.GetComponent<VerticalLayoutGroup>().spacing = UiTokens.Space1;
            completionHeading = CompletionText(campaignCompletionCard, "Completion Heading", 34, true);
            completionHeading.color = UiTokens.Gold;
            UiKit.Size(completionHeading.rectTransform, height: 88);
            completionNext = CompletionText(campaignCompletionCard, "Completion Next", 28);
            for (int region = 1; region <= CampaignCatalog.RegionCount; region++)
                completionRegions.Add(CompletionText(campaignCompletionCard, $"Completion Region {region}", 26));
            var actions = MenuStack(campaignCompletionCard, "Completion Actions", false);
            completionAction = MenuButton(actions, "Completion Continue", FrontendCopy.GoToNextRegion,
                menuKit.Assets.iconMap, () =>
                {
                    int level = session.CampaignLevelNumber == CampaignCatalog.LevelCount
                        ? progress.LowestStarLevel(PlayableLevelCount) : session.CampaignLevelNumber + 1;
                    OpenCampaignAtLevel(level);
                }, true);
            var map = MenuButton(actions, "Completion Map", FrontendCopy.ReturnToMap, menuKit.Assets.iconMap, OpenCampaign);
            foreach (var button in new[] { completionAction, map })
            {
                var label = button.GetComponentInChildren<TMP_Text>();
                label.textWrappingMode = TextWrappingModes.Normal;
                label.enableAutoSizing = true; label.fontSizeMin = 22; label.fontSizeMax = 34;
                UiKit.Size(label.rectTransform, height: 88, flex: 1);
            }
            campaignCompletionCard.gameObject.SetActive(false);
        }

        private void RefreshCampaignCompletionCard(bool victory, int stars)
        {
            resultWasVictory = victory;
            resultEarnedStars = stars;
            int level = session.CampaignLevelNumber;
            bool milestone = victory && stars > 0 && level % CampaignCatalog.LevelsPerRegion == 0;
            campaignCompletionCard.gameObject.SetActive(milestone);
            if (!milestone) return;
            bool final = level == CampaignCatalog.LevelCount;
            int region = level / CampaignCatalog.LevelsPerRegion;
            resultUnlock.gameObject.SetActive(false);
            resultStarRow.gameObject.SetActive(!final);
            resultChapter.gameObject.SetActive(!final);
            resultTitle.text = final ? FrontendCopy.KingdomSafe.Value : FrontendCopy.VictoryHeading.Value;
            resultReward.text = final ? FrontendCopy.ProfileStars.Format(progress.VisibleStars(PlayableLevelCount), PlayableLevelCount * 3)
                : FrontendCopy.ResultStars.Format(stars);
            completionHeading.gameObject.SetActive(!final);
            completionHeading.text = FrontendCopy.RegionCompleted.Format(CampaignCatalog.Regions[region - 1].localizedName,
                progress.RegionStars(region));
            completionNext.gameObject.SetActive(!final);
            completionNext.text = region < VisibleRegionCount
                ? FrontendCopy.NextRegion.Format(CampaignCatalog.Regions[region].localizedName)
                : FrontendCopy.RegionsComingSoon.Value;
            for (int i = 0; i < completionRegions.Count; i++)
            {
                completionRegions[i].gameObject.SetActive(final);
                completionRegions[i].text = FrontendCopy.RegionStars.Format(CampaignCatalog.Regions[i].localizedName,
                    progress.RegionStars(i + 1));
            }
            completionAction.gameObject.SetActive(final || region < VisibleRegionCount);
            var actionText = completionAction.GetComponentInChildren<TMP_Text>(true);
            actionText.GetComponent<UiKitText>().Configure(actionText,
                final ? FrontendCopy.ReturnToMissingStars : FrontendCopy.GoToNextRegion);
        }

        private void OpenCampaignAtLevel(int level)
        {
            level = Mathf.Clamp(level, 1, MaxPlayableLevel);
            navigationState.HasMapPosition = true;
            navigationState.SelectedLevel = level;
            navigationState.Region = (level - 1) / CampaignCatalog.LevelsPerRegion;
            navigationState.Horizontal = VisibleRegionCount <= 1 ? 0 : navigationState.Region / (VisibleRegionCount - 1f);
            selectedLevel = level;
            campaignDestinationLevel = level;
            OpenCampaign();
        }

        private IEnumerator FocusCampaignDestination(int level)
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            SelectLevel(level);
            FocusRegion((level - 1) / CampaignCatalog.LevelsPerRegion);
            navigationState.Horizontal = campaignScroll.horizontalNormalizedPosition;
        }
    }
}
