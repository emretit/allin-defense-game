using System;
using System.Collections;
using System.Linq;
using AllinDefense.Core;
using AllinDefense.Data;
using AllinDefense.UI.Kit;
using AllinDefense.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AllinDefense.UI
{
    public sealed partial class AppFlowController
    {
        private const float Edge = 44f;
        private const float TouchHeight = 140f;

        private UiKit menuKit;
        private TMP_Text chapterEyebrow, chapterTitle, chapterWaves, chapterTime, chapterGold, chapterProgress;
        private TMP_Text briefingTime;
        private TMP_Text profileStars;
        private UiStarRow chapterStars;
        private UiProgressSegments regionProgress;
        private Image saveErrorIcon;
        private RectTransform enemyChips;
        private UiOverlay briefingSheet;
        private readonly System.Collections.Generic.List<Image> levelLocks = new();

        private RectTransform MenuStack(Transform parent, string id, bool vertical = true, float padding = 0)
        {
            var rect = UiKit.Node(parent, id);
            var layout = UiKit.Layout(rect, vertical, padding);
            layout.spacing = UiTokens.Space2;
            return rect;
        }

        private TextMeshProUGUI MenuText(Transform parent, string id, LocalizedCopy copy, float size = 32, bool bold = false)
        {
            return (TextMeshProUGUI)menuKit.Text(parent, id, copy, size, bold);
        }

        private void MenuClick(Action action)
        {
            if (navigation.IsTransitioning || (heroNameMotion != null && !heroNameMotion.IsComplete && !heroNameMotion.IsCancelled) || (settingsFade != null && !settingsFade.IsComplete && !settingsFade.IsCancelled)) return;
            PlayFrontendCue(null, PresentationCue.UiClick);
            action?.Invoke();
        }

        private Button MenuButton(Transform parent, string id, LocalizedCopy copy, Sprite icon, Action action, bool gold = false)
        {
            var button = menuKit.Button(parent, id, copy, gold ? UiButtonVariant.Gold : UiButtonVariant.Glass, icon, clicked: () => MenuClick(action));
            button.GetComponentInChildren<TMP_Text>(true).fontSize = 34;
            var glyph = button.GetComponent<UiVisualState>().Icon;
            if (glyph != null) glyph.GetComponent<UiProportionalIcon>().Configure(button.GetComponent<UiPointSize>());
            UiKit.Size((RectTransform)button.transform, height: TouchHeight, flex: 1);
            return button;
        }

        private TMP_Text FactChip(Transform parent, string id, Sprite icon)
        {
            var chip = menuKit.Surface(parent, id, UiTokens.RadiusPill, false);
            chip.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleLeft;
            chip.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(16, 16, 8, 8);
            if (icon != null) menuKit.Icon(chip, id + " Icon", icon, 36);
            else
            {
                var clock = menuKit.Icon(chip, id + " Clock", menuKit.Assets.cooldownRing, 36);
                var hand = menuKit.Decoration(clock.transform, id + " Clock Hand", menuKit.Assets.roundedSmall, UiTokens.Moon);
                hand.rectTransform.anchorMin = new Vector2(.45f, .5f); hand.rectTransform.anchorMax = new Vector2(.55f, .85f);
                var shortHand = menuKit.Decoration(clock.transform, id + " Clock Short Hand", menuKit.Assets.roundedSmall, UiTokens.Moon);
                shortHand.rectTransform.anchorMin = new Vector2(.5f, .45f); shortHand.rectTransform.anchorMax = new Vector2(.75f, .55f);
            }
            return MenuText(chip, id + " Label", "", 28);
        }

        private void BuildMainMenuScreen()
        {
            menuKit ??= new UiKit(theme.designSystem);
            Transform root = screens[AppScreen.MainMenu].transform;
            var stack = MenuStack(root, "Main Menu Layout", padding: Edge);
            UiKit.Stretch(stack);
            stack.GetComponent<VerticalLayoutGroup>().spacing = UiTokens.Space3;
            var top = MenuStack(stack, "Profile Row", false);
            var profile = menuKit.Surface(top, "Profile Crest", UiTokens.RadiusPill, false);
            profile.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            menuKit.Icon(profile, "Profile Brand Mark", menuKit.Assets.iconShield, 64);
            var identity = MenuStack(profile, "Profile Identity");

            profileLabel = MenuText(identity, "Profile", "", 32, true);
            profileLabel.enableAutoSizing = true; profileLabel.fontSizeMin = 26; profileLabel.fontSizeMax = 32;
            var profileScore = MenuStack(identity, "Profile Stars", false);
            profileScore.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleLeft;
            menuKit.Icon(profileScore, "Profile Star Icon", menuKit.Assets.iconStarFilled, 32);
            profileStars = MenuText(profileScore, "Profile Star Count", FrontendCopy.StarCount.Format(0, PlayableLevelCount * 3), 26);
            var profileButton = profile.gameObject.AddComponent<Button>();
            profileButton.targetGraphic = profile.GetComponent<UiVisualState>().Surface;
            profileButton.onClick.AddListener(() => MenuClick(() => ShowScreen(AppScreen.Login)));
            profile.gameObject.AddComponent<UiPointSize>().Configure(UiMetrics.MinTouchPt);
            saveErrorIcon = menuKit.Icon(profile, "Save Error", menuKit.Assets.iconClose, 32);
            saveErrorIcon.color = UiTokens.Moon;
            var profileSpace = UiKit.Node(top, "Profile Space"); UiKit.Size(profileSpace, flex: 1);
            var settings = menuKit.IconButton(top, "Settings", menuKit.Assets.iconSettings, () => MenuClick(OpenSettings));
            UiKit.Size((RectTransform)settings.transform, 140, 140);
            var brand = CreateBrandImage("Brand Logo", stack, theme.brandLogo);
            UiKit.Size((RectTransform)brand.transform, height: 360);
            var space = UiKit.Node(stack, "Menu Artwork Space");
            UiKit.Size(space).flexibleHeight = 1;
            var card = menuKit.Card(stack, "Continue Chronicle");
            card.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(32, 32, 28, 28);
            var chapterHeader = MenuStack(card, "Chapter Header", false);
            chapterEyebrow = MenuText(chapterHeader, "Chapter Region", "", 25, true);
            UiKit.Size(chapterEyebrow.rectTransform, flex: 1);
            chapterEyebrow.color = UiTokens.Gold;
            chapterStars = menuKit.StarRow(chapterHeader, "Chapter Stars", 0);
            chapterTitle = MenuText(card, "Callout", "", 48, true);
            chapterTitle.font = catalog.titleFont;
            continueDetails = MenuText(card, "Continue Details", FrontendCopy.ChapterDescription, 30);
            continueDetails.textWrappingMode = TextWrappingModes.Normal;
            UiKit.Size(continueDetails.rectTransform, height: 82);
            var facts = MenuStack(card, "Chapter Facts", false);
            facts.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleLeft;
            chapterWaves = FactChip(facts, "Chapter Waves", catalog.waveIcon);
            chapterTime = FactChip(facts, "Chapter Time", null);
            chapterGold = FactChip(facts, "Chapter Gold", catalog.goldIcon);
            regionProgress = menuKit.ProgressSegments(card, "Region Progress", 10);
            chapterProgress = MenuText(card, "Region Progress Label", "", 26);
            chapterProgress.color = UiTokens.TextSecondary;
            continueButton = MenuButton(stack, "Continue", FrontendCopy.MenuStart, menuKit.Assets.iconPlay, ContinueCampaign, true);
            var actions = MenuStack(stack, "Menu Actions", false);
            MenuButton(actions, "Campaign", FrontendCopy.MenuMap, menuKit.Assets.iconMap, OpenCampaign);
            MenuButton(actions, "Menu Hero", FrontendCopy.MenuHero, menuKit.Assets.iconShield, OpenHeroName);
            BuildGuestImportAction(stack);
            RefreshProfile();
        }

        private void BuildCampaignScreen()
        {
            menuKit ??= new UiKit(theme.designSystem);
            Transform root = screens[AppScreen.Campaign].transform;
            var stack = MenuStack(root, "Campaign Layout", padding: Edge);
            UiKit.Stretch(stack);
            var header = MenuStack(stack, "Map Header", false);
            var back = menuKit.IconButton(header, "Back", menuKit.Assets.iconBack, () => MenuClick(() => ShowScreen(AppScreen.MainMenu)));
            UiKit.Size((RectTransform)back.transform, 140, 140);
            UiKit.Size(MenuText(header, "Map Title", FrontendCopy.RealmMap, 40, true).rectTransform, flex: 1);
            var tabs = MenuStack(stack, "Region Tabs", false);
            for (int i = 0; i < VisibleRegionCount; i++)
            {
                int region = i;
                var tab = MenuButton(tabs, $"Region Tab {i + 1}", (i + 1).ToString(), null, () => FocusRegion(region));
                regionTabs.Add(tab);
                var locked = menuKit.Icon(tab.transform, $"Region Tab Lock {i + 1}", menuKit.Assets.iconLock, 32);
                regionTabLocks.Add(locked);
            }
            tabs.gameObject.SetActive(VisibleRegionCount > 1);
            var scroll = UiKit.Node(stack, "Campaign Scroll");
            UiKit.Size(scroll, height: 320).flexibleHeight = 1;
            campaignScroll = scroll.gameObject.AddComponent<FrontendCampaignPager>();
            ((FrontendCampaignPager)campaignScroll).PageCount = VisibleRegionCount;
            campaignScroll.horizontal = VisibleRegionCount > 1; campaignScroll.vertical = false;
            campaignScroll.movementType = ScrollRect.MovementType.Clamped;
            campaignScroll.decelerationRate = .06f;
            var viewport = CreateImage("Viewport", scroll, UiTokens.ViewportScrim);
            viewport.GetComponent<Image>().raycastTarget = true;
            Stretch((RectTransform)viewport.transform);
            viewport.AddComponent<RectMask2D>();
            var content = MenuStack(viewport.transform, "Content", false);
            content.anchorMin = Vector2.zero; content.anchorMax = new Vector2(0, 1);
            content.pivot = new Vector2(0, .5f); content.sizeDelta = Vector2.zero;
            var row = content.GetComponent<HorizontalLayoutGroup>();
            row.spacing = 0; row.childForceExpandHeight = true;
            content.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            campaignScroll.viewport = (RectTransform)viewport.transform; campaignScroll.content = content;
            for (int i = 0; i < VisibleRegionCount; i++) BuildRegion(content, CampaignCatalog.Regions[i]);
            campaignScroll.onValueChanged.AddListener(value =>
            {
                int current = Mathf.Clamp(Mathf.RoundToInt(value.x * (VisibleRegionCount - 1)), 0, VisibleRegionCount - 1);
                EnsureRegion(current);
                if (navigationState.Region != current) { navigationState.Region = current; UpdateRegionTabs(); }
            });

            briefingSheet = menuKit.BottomSheet(stack, "Selection Bar", "");
            briefingSheet.DismissOnScrim = false;
            briefingSheet.GetComponent<Image>().color = UiTokens.Clear;
            briefingSheet.GetComponent<Image>().raycastTarget = false;
            var sheetCard = briefingSheet.Body.parent;
            sheetCard.Find("Selection Bar.Title").gameObject.SetActive(false);
            sheetCard.Find("Selection Bar.Close").gameObject.SetActive(false);
            var body = briefingSheet.Body;
            selectedLevelTitle = MenuText(body, "Selected Level", "", 38, true);
            selectedLevelTitle.font = catalog.titleFont;
            var facts = MenuStack(body, "Briefing Facts", false);
            facts.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleLeft;
            selectedLevelDetails = (TextMeshProUGUI)FactChip(facts, "Selected Details Chip", catalog.waveIcon);
            UiId.Assign(selectedLevelDetails.transform, "Selected Details");
            briefingTime = FactChip(facts, "Briefing Time", null);
            var preview = CreatePanel("Tactical Preview", body, UiTokens.TacticalSurface);
            UiKit.Size((RectTransform)preview.transform, height: 150);
            preview.GetComponent<Image>().raycastTarget = false;
            tacticalMapPreview = preview.AddComponent<FrontendTacticalMapPreview>();
            var intelScroll = UiKit.Node(body, "Enemy Intel Scroll");
            UiKit.Size(intelScroll, height: 140);
            var intelViewport = UiKit.Node(intelScroll, "Enemy Intel Viewport"); UiKit.Stretch(intelViewport);
            intelViewport.gameObject.AddComponent<Image>().color = UiTokens.Clear;
            intelViewport.gameObject.AddComponent<RectMask2D>();
            enemyChips = MenuStack(intelViewport, "Enemy Intel", false);
            enemyChips.anchorMin = Vector2.zero; enemyChips.anchorMax = new Vector2(0, 1); enemyChips.pivot = new Vector2(0, .5f);
            enemyChips.sizeDelta = Vector2.zero;
            enemyChips.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            var intel = intelScroll.gameObject.AddComponent<ScrollRect>();
            intel.viewport = intelViewport; intel.content = enemyChips; intel.vertical = false;
            intel.movementType = ScrollRect.MovementType.Clamped;
            briefingReward = MenuText(body, "Briefing Reward", "", 28);
            briefingReward.color = UiTokens.Gold;
            playSelectedButton = MenuButton(body, "Play Selected", FrontendCopy.MenuStart, menuKit.Assets.iconPlay, StartSelectedLevel, true);
            RefreshLevelButtons();
            UpdateSelectedDetails();
            UpdateRegionTabs();
        }

        private void ShowBriefingSheet()
        {
            if (briefingSheet == null || !briefingSheet.gameObject.activeInHierarchy) return;
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)briefingSheet.transform);
            briefingSheet.Show();
        }

        private void BuildRegion(Transform parent, CampaignRegionDefinition region)
        {
            var page = new GameObject($"Region {region.number}", typeof(RectTransform), typeof(LayoutElement));
            page.transform.SetParent(parent, false);
            page.AddComponent<FrontendCampaignPageFitter>().Configure(campaignScroll.viewport, 1);
            var vertical = page.AddComponent<FrontendRegionScrollRect>();
            vertical.Pages = campaignScroll;
            vertical.horizontal = false; vertical.vertical = true;
            vertical.movementType = ScrollRect.MovementType.Clamped;
            var viewport = CreateImage("Route Viewport", page.transform, UiTokens.RouteViewport);
            viewport.GetComponent<Image>().raycastTarget = true;
            Stretch((RectTransform)viewport.transform); viewport.AddComponent<RectMask2D>();
            var route = new GameObject("Route Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            route.transform.SetParent(viewport.transform, false);
            var routeRect = (RectTransform)route.transform;
            routeRect.anchorMin = new Vector2(0, 1); routeRect.anchorMax = Vector2.one;
            routeRect.pivot = new Vector2(.5f, 1); routeRect.sizeDelta = Vector2.zero;
            var routeLayout = route.GetComponent<VerticalLayoutGroup>();
            routeLayout.padding = new RectOffset(24, 24, 16, 48);
            routeLayout.spacing = 24;
            routeLayout.childControlWidth = routeLayout.childControlHeight = true;
            routeLayout.childForceExpandWidth = true; routeLayout.childForceExpandHeight = false;
            route.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            vertical.viewport = (RectTransform)viewport.transform; vertical.content = routeRect;
            regionScrolls.Add(vertical);
            UiId.AssignHierarchy(page.transform);
            regionBuilders.Add(() => BuildRegionContents(page, routeRect, region));
        }

        private void EnsureRegion(int index)
        {
            if (index < 0 || index >= regionBuilders.Count || !builtRegions.Add(index)) return;
            regionBuilders[index]();
            RefreshLevelButtons();
        }

        private void QueueRegionBuild(int index)
        {
            index = Mathf.Clamp(index, 0, VisibleRegionCount - 1);
            if (builtRegions.Contains(index)) return;
            if (campaignRegionBuild != null) StopCoroutine(campaignRegionBuild);
            campaignRegionBuild = StartCoroutine(BuildRegionAfterFirstFrame(index));
        }

        private IEnumerator BuildRegionAfterFirstFrame(int index)
        {
            // Let the map shell and transition paint before creating route labels and buttons.
            yield return null;
            EnsureRegion(index);
            campaignRegionBuild = null;
        }

        private void BuildRegionContents(GameObject page, RectTransform routeRect, CampaignRegionDefinition region)
        {
            var heading = MenuText(routeRect, "Region Name",
                $"{region.number} · {GameLocalization.Uppercase(region.localizedName)}", 32, true);
            heading.color = UiTokens.Gold;
            var lockedCard = menuKit.Card(routeRect, $"Region Locked {region.number}");
            menuKit.Icon(lockedCard, "Region Lock Icon", menuKit.Assets.iconLock, 64);
            var condition = MenuText(lockedCard, "Region Lock Condition", "", 32, true);
            condition.textWrappingMode = TextWrappingModes.Normal;
            condition.enableAutoSizing = true; condition.fontSizeMin = 24; condition.fontSizeMax = 32;
            UiKit.Size(condition.rectTransform, height: 92);
            regionLockCards[region.number] = lockedCard;
            regionLockConditions[region.number] = condition;
            var nodes = new RectTransform[region.levels.Count];
            var lines = new RectTransform[region.levels.Count - 1];
            for (int i = 0; i < lines.Length; i++)
            {
                var line = CreateImage("Route", routeRect, UiTokens.HairlineStrong);
                lines[i] = (RectTransform)line.transform;
                UiKit.Size(lines[i]).ignoreLayout = true;
                lines[i].pivot = new Vector2(0, .5f);
                lines[i].SetAsFirstSibling();
            }
            for (int i = 0; i < region.levels.Count; i++)
            {
                var level = region.levels[i];
                var row = MenuStack(routeRect, $"Route Row {level.number}", false);
                row.GetComponent<HorizontalLayoutGroup>().childAlignment = i % 2 == 0 ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight;
                row.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(72, 72, 0, 0);
                var medal = MenuStack(row, $"Medallion {level.number}");
                medal.GetComponent<VerticalLayoutGroup>().childForceExpandWidth = false;
                var button = menuKit.Button(medal, $"Level {level.number}", level.number.ToString());
                UiKit.Size((RectTransform)button.transform, 144, 144);
                var state = button.GetComponent<UiVisualState>();
                state.Surface.sprite = menuKit.Assets.filledCircle;
                state.Surface.type = Image.Type.Simple; state.SetCircular(true);
                button.GetComponentInChildren<TMP_Text>(true).fontSize = 40;
                menuKit.Decoration(button.transform, "Medallion Border", menuKit.Assets.cooldownRing, UiTokens.HairlineStrong);
                menuKit.Decoration(button.transform, "Selection Ring", menuKit.Assets.cooldownRing, UiTokens.Gold);
                nodes[i] = (RectTransform)button.transform;
                var locked = menuKit.Decoration(button.transform, "Lock", menuKit.Assets.iconLock, UiTokens.TextSecondary);
                locked.rectTransform.anchorMin = Vector2.one * .3f; locked.rectTransform.anchorMax = Vector2.one * .7f;
                locked.preserveAspect = true;
                levelLocks.Add(locked);
                button.onClick.AddListener(() => MenuClick(() => { SelectLevel(level.number); ShowBriefingSheet(); }));
                levelButtons.Add(button); levelNumbers.Add(level.number);
                levelStars.Add(menuKit.StarRow(medal, $"Level Stars {level.number}", 0));
            }
            page.GetComponent<FrontendCampaignPageFitter>().ConfigureRoute(nodes, lines);
            LayoutRebuilder.ForceRebuildLayoutImmediate(routeRect);
        }
        private void BuildWelcomeScreen()
        {
            Transform root = screens[AppScreen.Welcome].transform;
            var brand = CreateBrandImage("Welcome Brand Logo", root, theme.brandLogo);
            SetAnchors((RectTransform)brand.transform, new Vector2(.1f, .65f), new Vector2(.9f, .97f), Vector2.zero, Vector2.zero);
            var card = CreatePanel("Welcome Chronicle", root, UiTokens.WithAlpha(theme.panel, .9f));
            AddCardAccent(card.transform, theme.commandGold);
            SetAnchors((RectTransform)card.transform, Vector2.zero, new Vector2(1, 0), new Vector2(Edge, 36), new Vector2(-Edge, 1090));
            var eyebrow = CreateText("Eyebrow", card.transform, FrontendCopy.WelcomeEyebrow, 30, catalog.titleFont,
                FontStyles.Bold, TextAlignmentOptions.Center, theme.commandGold);
            SetAnchors(eyebrow.rectTransform, new Vector2(0, 1), Vector2.one, new Vector2(24, -88), new Vector2(-24, -26));
            var subtitle = CreateText("Subtitle", card.transform,
                FrontendCopy.WelcomeDescription, 32,
                catalog.bodyFont, FontStyles.Normal, TextAlignmentOptions.Center, theme.textPrimary);
            SetAnchors(subtitle.rectTransform, new Vector2(0, 1), Vector2.one, new Vector2(28, -238), new Vector2(-28, -98));
            var apple = ExternalSignInButton(card.transform, "Welcome Apple Sign In", PlayerAuthProvider.Apple, FrontendCopy.ContinueWithApple);
            apple.gameObject.SetActive(AppleSignInAvailable());
            SetAnchors((RectTransform)apple.transform, Vector2.zero, new Vector2(1, 0), new Vector2(28, 650), new Vector2(-28, 790));
            var google = ExternalSignInButton(card.transform, "Welcome Google Sign In", PlayerAuthProvider.Google, FrontendCopy.ContinueWithGoogle);
            SetAnchors((RectTransform)google.transform, Vector2.zero, new Vector2(1, 0), new Vector2(28, 492), new Vector2(-28, 632));
            var start = CreateButton("Start", card.transform, FrontendCopy.StartAsGuest, new Vector2(700, 140),
                theme.commandGold, () => LaunchCampaignLevel(1));
            SetAnchors((RectTransform)start.transform, Vector2.zero, new Vector2(1, 0), new Vector2(28, 334), new Vector2(-28, 474));
            var account = CreateButton("Link Account", card.transform, FrontendCopy.LinkAccount, new Vector2(700, 140), theme.defenderBlue,
                () => ShowScreen(AppScreen.Login));
            account.gameObject.SetActive(false);
            SetAnchors((RectTransform)account.transform, Vector2.zero, new Vector2(1, 0), new Vector2(28, 128), new Vector2(-28, 268));
            deletionWelcomeStatus = CreateText("Account Deleted Status", card.transform, "", 28, catalog.bodyFont,
                FontStyles.Normal, TextAlignmentOptions.Center, theme.textPrimary);
            SetAnchors(deletionWelcomeStatus.rectTransform, Vector2.zero, new Vector2(1, 0), new Vector2(28, 160), new Vector2(-28, 260));
            welcomeLoginStatus = CreateText("Welcome Login Status", card.transform, "", 28, catalog.bodyFont,
                FontStyles.Normal, TextAlignmentOptions.Center, theme.textPrimary);
            SetAnchors(welcomeLoginStatus.rectTransform, Vector2.zero, new Vector2(1, 0), new Vector2(28, 160), new Vector2(-28, 300));
            var note = CreateText("Guest Note", card.transform,
                FrontendCopy.GuestNote, 27, catalog.bodyFont,
                FontStyles.Normal, TextAlignmentOptions.Center, theme.textMuted);
            SetAnchors(note.rectTransform, Vector2.zero, new Vector2(1, 0), new Vector2(28, 18), new Vector2(-28, 110));
        }

        private TMP_Text welcomeLoginStatus;
        private Button accountAppleSignIn, accountGoogleSignIn;
        private TMP_Text accountIdentity;
        private Button guestSignIn;
        private Button settingsAccount;
        private UiSegmentedControl languageSegments;
        private CanvasGroup settingsGroup;
        private UiMotion.Handle settingsFade;
        private bool settingsOpen;

        private void BuildLoginScreen()
        {
            menuKit ??= new UiKit(theme.designSystem);
            Transform root = screens[AppScreen.Login].transform;
            var scrim = CreateImage("Account Scrim", root, UiTokens.AccountScrim);
            Stretch((RectTransform)scrim.transform);
            var card = menuKit.Card(root, "Account Chronicle");
            card.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            root.gameObject.AddComponent<FrontendResponsiveLayout>().Configure(size =>
                SetRect(card, Vector2.zero, new Vector2(Mathf.Min(size.x - 88, 1000), card.sizeDelta.y), Center(), Center()));
            var header = MenuStack(card, "Account Header", false);
            var title = MenuText(header, "Heading", FrontendCopy.ProtectProgress, 40, true);
            UiKit.Size(title.rectTransform, flex: 1);
            title.textWrappingMode = TextWrappingModes.Normal;
            title.enableAutoSizing = true; title.fontSizeMin = 28; title.fontSizeMax = 40;
            var close = menuKit.IconButton(header, "Back", menuKit.Assets.iconClose, ReturnFromAccount);
            UiKit.Size((RectTransform)close.transform, 140, 140);
            accountIdentity = MenuText(card, "Account Identity", "", 30);
            var explanation = MenuText(card, "Account Explanation", FrontendCopy.AccountDescription, 30);
            explanation.textWrappingMode = TextWrappingModes.Normal;
            UiKit.Size(explanation.rectTransform, height: 132);
            accountAppleSignIn = ExternalSignInButton(card, "Account Apple Sign In", PlayerAuthProvider.Apple, FrontendCopy.LinkWithApple);
            accountAppleSignIn.gameObject.SetActive(AppleSignInAvailable());
            accountGoogleSignIn = ExternalSignInButton(card, "Account Google Sign In", PlayerAuthProvider.Google, FrontendCopy.LinkWithGoogle);
            MenuButton(card, "Player Account Sign In", FrontendCopy.LinkAccount, menuKit.Assets.iconShield,
                () => RequestExternalSignIn(PlayerAuthProvider.PlayerAccount), true);
            guestSignIn = MenuButton(card, "Guest Sign In", FrontendCopy.StartAsGuest, null, SignInAsGuest);
            loginStatus = MenuText(card, "Login Status", "", 28);
            loginStatus.textWrappingMode = TextWrappingModes.Normal;
            UiKit.Size(loginStatus.rectTransform, height: 100);
            var links = MenuStack(card, "Legal Links", false);
            LegalLink(links, "Privacy Policy", FrontendCopy.PrivacyPolicy, theme.privacyPolicyUrl);
            LegalLink(links, "Terms Of Service", FrontendCopy.TermsOfService, theme.termsOfServiceUrl);
            AccountDeleteButton(card, "Delete Account", FrontendCopy.DeleteAccount, ConfirmAccountDeletion);
        }

        private Button ExternalSignInButton(Transform parent, string id, PlayerAuthProvider provider, LocalizedCopy copy)
        {
            bool apple = provider == PlayerAuthProvider.Apple;
            var rect = UiKit.Node(parent, id);
            var surface = rect.gameObject.AddComponent<Image>();
            menuKit ??= new UiKit(theme.designSystem);
            surface.sprite = menuKit.Assets.roundedMedium;
            surface.type = Image.Type.Sliced;
            surface.color = apple ? Color.black : UiTokens.White;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = surface;
            var colors = button.colors;
            colors.highlightedColor = UiTokens.White;
            colors.selectedColor = UiTokens.White;
            colors.pressedColor = UiTokens.PressedTint;
            button.colors = colors;
            rect.gameObject.AddComponent<UiPointSize>().Configure(UiMetrics.MinTouchPt);
            UiKit.Size(rect, height: TouchHeight, flex: 1);
            var label = CreateText(id + " Label", rect, copy, 34, catalog.bodyFont,
                FontStyles.Normal, TextAlignmentOptions.Center, apple ? UiTokens.White : UiTokens.Night);
            Stretch(label.rectTransform, new Vector2(100, 12), new Vector2(-70, -12));
            label.enableAutoSizing = true; label.fontSizeMin = 28; label.fontSizeMax = 34;
            var logoRoot = UiKit.Node(rect, id + " Logo");
            var logo = logoRoot.gameObject.AddComponent<Image>();
            logo.sprite = apple ? theme.appleSignInLogo : theme.googleSignInLogo;
            logo.color = UiTokens.White; logo.preserveAspect = true; logo.raycastTarget = false;
            SetRect(logoRoot, new Vector2(56, 0), new Vector2(54, 54), new Vector2(0, .5f), new Vector2(.5f, .5f));
            rect.gameObject.AddComponent<UiAccessibleButton>().Configure(copy);
            button.onClick.AddListener(() => MenuClick(() => RequestExternalSignIn(provider)));
            MobileTouchTarget.Attach(button);
            return button;
        }

        private static void RefreshExternalSignInLabel(Button button, LocalizedCopy copy)
        {
            var label = button.GetComponentInChildren<TMP_Text>();
            label.GetComponent<LocalizedTmpText>().Configure(label, copy);
            button.GetComponent<UiAccessibleButton>().Configure(copy);
        }

        private void LegalLink(Transform parent, string id, LocalizedCopy copy, string url)
        {
            PlainTextButton(parent, id, copy, () => OpenLegalUrl(url, copy.Value));
        }

        private Button PlainTextButton(Transform parent, string id, LocalizedCopy copy, Action action)
        {
            var rect = UiKit.Node(parent, id);
            var image = rect.gameObject.AddComponent<Image>(); image.color = UiTokens.Clear;
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            rect.gameObject.AddComponent<UiPointSize>().Configure(UiMetrics.MinTouchPt);
            UiKit.Size(rect, height: 140, flex: 1);
            var label = MenuText(rect, id + " Label", copy, 28);
            UiKit.Stretch(label.rectTransform); label.alignment = TextAlignmentOptions.Center;
            button.onClick.AddListener(() => MenuClick(action));
            return button;
        }

        private void RefreshAccount()
        {
            if (accountIdentity == null) return;
            accountIdentity.text = progress.Data.authProvider is PlayerAuthProvider.Guest or PlayerAuthProvider.None
                ? FrontendCopy.GuestDeviceProgress.Value : progress.Data.displayName;
            bool guest = progress.Data.authProvider is PlayerAuthProvider.Guest or PlayerAuthProvider.None;
            RefreshExternalSignInLabel(accountAppleSignIn, guest ? FrontendCopy.LinkWithApple : FrontendCopy.ContinueWithApple);
            RefreshExternalSignInLabel(accountGoogleSignIn, guest ? FrontendCopy.LinkWithGoogle : FrontendCopy.ContinueWithGoogle);
            guestSignIn.gameObject.SetActive(navigationState.AccountReturn == AppScreen.Welcome && progress.Data.playerId == "local-guest");
        }

        private void BuildSettingsModal()
        {
            menuKit ??= new UiKit(theme.designSystem);
            settingsModal = UiKit.Node(canvas.transform, "Settings Modal").gameObject;
            UiKit.Stretch((RectTransform)settingsModal.transform);
            settingsGroup = settingsModal.AddComponent<CanvasGroup>();
            var scrim = CreateImage("Settings Scrim", settingsModal.transform, UiTokens.SettingsScrim);
            Stretch((RectTransform)scrim.transform);
            scrim.GetComponent<Image>().raycastTarget = true;
            scrim.AddComponent<Button>().onClick.AddListener(CloseSettings);
            var card = menuKit.Card(settingsModal.transform, "Settings Chronicle");
            settingsModal.AddComponent<FrontendResponsiveLayout>().Configure(size =>
                SetRect(card, Vector2.zero, new Vector2(size.x - Edge * 2, Mathf.Min(size.y - 60, 1550)), Center(), Center()));
            var header = MenuStack(card, "Settings Header", false);
            var title = MenuText(header, "Settings Title", FrontendCopy.SettingsTitle, 44, true);
            UiKit.Size(title.rectTransform, flex: 1);
            var close = menuKit.IconButton(header, "Close Settings", menuKit.Assets.iconClose, CloseSettings);
            UiKit.Size((RectTransform)close.transform, 140, 140);
            var scrollRoot = UiKit.Node(card, "Settings Scroll");
            UiKit.Size(scrollRoot).flexibleHeight = 1;
            var scroll = scrollRoot.gameObject.AddComponent<ScrollRect>();
            var viewport = UiKit.Node(scrollRoot, "Viewport"); UiKit.Stretch(viewport);
            viewport.gameObject.AddComponent<Image>().color = UiTokens.Clear;
            viewport.gameObject.AddComponent<RectMask2D>();
            var body = MenuStack(viewport, "Settings Content");
            body.anchorMin = new Vector2(0, 1); body.anchorMax = Vector2.one; body.pivot = new Vector2(.5f, 1);
            body.sizeDelta = Vector2.zero;
            body.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.viewport = viewport; scroll.content = body;
            var audio = menuKit.Card(body, "Audio Settings");
            MenuText(audio, "Audio Heading", FrontendCopy.AudioGroup, 28, true);
            SettingsSlider(audio, "MÜZİK Row", FrontendCopy.Music, AudioIcon(true), FrontendPreferences.MusicVolume, v => FrontendPreferences.MusicVolume = v);
            SettingsSlider(audio, "EFEKTLER Row", FrontendCopy.Effects, AudioIcon(false), FrontendPreferences.SfxVolume,
                v => { FrontendPreferences.SfxVolume = v; ApplyStoredAudioPreferences(); });
            var gameplay = menuKit.Card(body, "Gameplay Settings");
            MenuText(gameplay, "Gameplay Heading", FrontendCopy.GameplayGroup, 28, true);
            SettingsSwitch(gameplay, "AZALTILMIŞ HAREKET Row", FrontendCopy.ReducedMotion, FrontendPreferences.ReducedMotion, v => FrontendPreferences.ReducedMotion = v);
            SettingsSwitch(gameplay, "KAMERA SARSINTISI Row", FrontendCopy.CameraShake, FrontendPreferences.CameraShake, v => FrontendPreferences.CameraShake = v);
            SettingsSwitch(gameplay, "TİTREŞİM Row", FrontendCopy.Haptics, FrontendPreferences.Haptics, v => FrontendPreferences.Haptics = v);
            MenuButton(gameplay, "Replay Guide", FrontendCopy.ReplayGuide, menuKit.Assets.iconRestart, () =>
            {
                FrontendPreferences.ResetLearningGuide();
                (replayGuideStatus.GetComponent<UiKitText>() ?? replayGuideStatus.gameObject.AddComponent<UiKitText>()).Configure(replayGuideStatus, FrontendCopy.ReplayGuideReady);
            });
            replayGuideStatus = MenuText(gameplay, "Replay Guide Status", "", 26);
            replayGuideStatus.textWrappingMode = TextWrappingModes.Normal;
            UiKit.Size(replayGuideStatus.rectTransform, height: 100);
            var language = menuKit.Card(body, "Language Settings");
            MenuText(language, "Language Heading", FrontendCopy.LanguageGroup, 28, true);
            languageSegments = menuKit.SegmentedControl(language, "Language Segments", new LocalizedCopy[] { "TR", "EN" },
                FrontendPreferences.Language == GameLanguage.Turkish ? 0 : 1);
            var buttons = languageSegments.GetComponentsInChildren<Button>();
            turkishLanguageButton = buttons[0]; englishLanguageButton = buttons[1];
            UiId.Assign(turkishLanguageButton.transform, "Turkish Language");
            UiId.Assign(englishLanguageButton.transform, "English Language");
            foreach (var button in buttons)
            {
                button.GetComponentInChildren<TMP_Text>().fontSize = UiTokens.TypeLabel;
                button.GetComponent<UiPointSize>().Configure(UiMetrics.MinTouchPt);
                UiKit.Size((RectTransform)button.transform, height: 140, flex: 1);
            }
            languageSegments.Changed += index => FrontendPreferences.Language = index == 0 ? GameLanguage.Turkish : GameLanguage.English;
            settingsAccount = MenuButton(body, "Settings Account", FrontendCopy.GuestLinkAccount, menuKit.Assets.iconShield,
                () => { CloseSettings(); ShowScreen(AppScreen.Login); });
            UiId.AssignHierarchy(settingsModal.transform);
            settingsModal.SetActive(false);
        }

        private void SettingsSlider(Transform parent, string id, LocalizedCopy copy, Sprite icon, float value, Action<float> changed)
        {
            var row = MenuStack(parent, id, false);
            var label = MenuText(row, "Label", copy, 30, true);
            UiKit.Size(label.rectTransform, width: 230);
            var slider = menuKit.Slider(row, "Slider", icon, value, changed);
            UiKit.Size((RectTransform)slider.transform, height: 140, flex: 1);
            slider.handleRect.sizeDelta = new Vector2(54, 54);
            slider.GetComponent<HorizontalLayoutGroup>().padding.right = 40;
            slider.GetComponent<UiVisualState>().Icon.GetComponent<UiProportionalIcon>().Configure(slider.GetComponent<UiPointSize>());
        }

        private static Sprite musicIcon, effectsIcon;

        private static Sprite AudioIcon(bool music)
        {
            if (music && musicIcon != null) return musicIcon;
            if (!music && effectsIcon != null) return effectsIcon;
            var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            var pixels = new Color[64 * 64];
            for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
            {
                bool ink;
                if (music)
                    ink = Vector2.Distance(new Vector2(x, y), new Vector2(18, 17)) < 8 ||
                        Vector2.Distance(new Vector2(x, y), new Vector2(42, 24)) < 8 ||
                        x >= 21 && x <= 26 && y >= 17 && y <= 51 ||
                        x >= 45 && x <= 50 && y >= 24 && y <= 58 ||
                        x >= 21 && x <= 50 && Mathf.Abs(y - (44 + x * .28f)) < 4;
                else
                {
                    float radius = Vector2.Distance(new Vector2(x, y), new Vector2(31, 32));
                    ink = x >= 10 && x <= 23 && y >= 24 && y <= 40 ||
                        x >= 23 && x <= 34 && Mathf.Abs(y - 32) <= x - 10 ||
                        x >= 38 && Mathf.Abs(y - 32) <= x - 31 &&
                        (Mathf.Abs(radius - 18) < 2 || Mathf.Abs(radius - 27) < 2);
                }
                pixels[y * 64 + x] = ink ? Color.white : Color.clear;
            }
            texture.SetPixels(pixels); texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0, 0, 64, 64), Vector2.one * .5f, 64);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            if (music) musicIcon = sprite; else effectsIcon = sprite;
            return sprite;
        }

        private void SettingsSwitch(Transform parent, string id, LocalizedCopy copy, bool value, Action<bool> changed)
        {
            var row = MenuStack(parent, id, false);
            var label = MenuText(row, "Label", copy, 30, true);
            UiKit.Size(label.rectTransform, flex: 1);
            var control = menuKit.Switch(row, "Switch", value, changed);
            UiKit.Size((RectTransform)control.transform, 300, 140);
            control.GetComponentInChildren<TMP_Text>().fontSize = 28;
        }
        private TMP_Text replayGuideStatus;
        private UiOverlay newEnemyModal;
        private bool enemyFromBriefing;
        private EnemyArchetype introducedEnemy;
        private float enemyReturnSpeed;
        private bool enemyOwnsPause;

        private LocalizedCopy EnemyNameCopy(EnemyArchetype enemy) => enemy switch
        {
            EnemyArchetype.Vanguard => FrontendCopy.EnemyVanguardName,
            EnemyArchetype.Support => FrontendCopy.EnemySupportName,
            EnemyArchetype.Siege => FrontendCopy.EnemySiegeName,
            EnemyArchetype.Regenerator => FrontendCopy.EnemyRegeneratorName,
            EnemyArchetype.Boss => FrontendCopy.BossName,
            _ => FrontendCopy.EnemyRaiderName
        };

        private Sprite EnemyIcon(EnemyArchetype enemy) => enemy switch
        {
            EnemyArchetype.Vanguard => menuKit.Assets.iconShield,
            EnemyArchetype.Support => menuKit.Assets.iconUpgrade,
            EnemyArchetype.Siege => menuKit.Assets.iconBlast,
            EnemyArchetype.Regenerator => menuKit.Assets.iconRapidFire,
            EnemyArchetype.Boss => menuKit.Assets.iconAssaultGuard,
            _ => menuKit.Assets.iconEnemy
        };

        private LocalizedCopy EnemyTrait(EnemyArchetype enemy) => enemy switch
        {
            EnemyArchetype.Vanguard => FrontendCopy.VanguardTrait,
            EnemyArchetype.Support => FrontendCopy.SupportTrait,
            EnemyArchetype.Siege => FrontendCopy.SiegeTrait,
            EnemyArchetype.Regenerator => FrontendCopy.RegeneratorTrait,
            EnemyArchetype.Boss => FrontendCopy.BossTrait,
            _ => FrontendCopy.RaiderTrait
        };

        private bool ShowNewEnemy(WaveDefinition wave)
        {
            if (CurrentScreen != AppScreen.Battle) return false;
            var enemies = wave.groups.Select(group => group.enemy).Distinct();
            foreach (var enemy in enemies)
            {
                if (FrontendPreferences.HasSeenEnemy(enemy)) continue;
                menuKit ??= new UiKit(theme.designSystem);
                if (newEnemyModal != null) return true;
                introducedEnemy = enemy;
                enemyReturnSpeed = Time.timeScale;
                enemyOwnsPause = session.Phase != GamePhase.Paused;
                if (enemyOwnsPause) session.TogglePause();
                enemyFromBriefing = false;
                BuildEnemyCard(enemy);
                return true;
            }
            return false;
        }

        private void ShowBriefingEnemy(EnemyArchetype enemy)
        {
            if (newEnemyModal != null || CurrentScreen != AppScreen.Campaign) return;
            enemyFromBriefing = true;
            enemyOwnsPause = false;
            introducedEnemy = enemy;
            BuildEnemyCard(enemy);
        }

        private void BuildEnemyCard(EnemyArchetype enemy)
        {
            newEnemyModal = menuKit.Modal(canvas.transform, "New Enemy", enemyFromBriefing ? FrontendCopy.EnemyInfoHeading : FrontendCopy.NewEnemyHeading);
            newEnemyModal.DismissOnScrim = false;
            var card = (RectTransform)newEnemyModal.Body.parent;
            UiKit.Size(card, 1000);
            var close = card.Find("New Enemy.Close"); close.gameObject.SetActive(false);
            menuKit.Icon(newEnemyModal.Body, "New Enemy Icon", EnemyIcon(enemy), 96);
            MenuText(newEnemyModal.Body, "New Enemy Name", EnemyNameCopy(enemy), 42, true);
            MenuText(newEnemyModal.Body, "New Enemy Trait", EnemyTrait(enemy), 28);
            var tactic = MenuText(newEnemyModal.Body, "New Enemy Tactic", DefenceAdvice(enemy), 28);
            tactic.textWrappingMode = TextWrappingModes.Normal;
            UiKit.Size(tactic.rectTransform, height: 120);
            MenuButton(newEnemyModal.Body, "Acknowledge Enemy", FrontendCopy.Understood, null, AcknowledgeEnemy, true);
            newEnemyModal.Show();
        }

        private void AcknowledgeEnemy()
        {
            if (newEnemyModal == null) return;
            if (!enemyFromBriefing) FrontendPreferences.MarkEnemySeen(introducedEnemy);
            var modal = newEnemyModal; newEnemyModal = null;
            modal.Hide(() => Destroy(modal.gameObject));
            if (enemyFromBriefing) { enemyFromBriefing = false; return; }
            if (enemyOwnsPause && session.Phase == GamePhase.Paused)
            {
                session.TogglePause();
                Time.timeScale = enemyReturnSpeed;
            }
            enemyOwnsPause = false;
            session.StartNextWave();
        }

    }
}
