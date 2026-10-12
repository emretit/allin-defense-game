using System;
using System.Collections;
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
        private UiStatPill profileStars;
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

        private void BuildMainMenuScreen()
        {
            menuKit ??= new UiKit(theme.designSystem);
            Transform root = screens[AppScreen.MainMenu].transform;
            var stack = MenuStack(root, "Main Menu Layout", padding: Edge);
            UiKit.Stretch(stack);
            stack.GetComponent<VerticalLayoutGroup>().spacing = UiTokens.Space3;
            var top = MenuStack(stack, "Profile Row", false);
            var profile = menuKit.Surface(top, "Profile Crest", UiTokens.RadiusPill, false);
            UiKit.Size(profile, flex: 1);
            menuKit.Icon(profile, "Profile Brand Mark", theme.brandMark, 76);
            var identity = MenuStack(profile, "Profile Identity");
            UiKit.Size(identity, flex: 1);
            profileLabel = MenuText(identity, "Profile", "", 32, true);
            profileLabel.enableAutoSizing = true; profileLabel.fontSizeMin = 26; profileLabel.fontSizeMax = 32;
            profileStars = menuKit.StatPill(identity, "Profile Stars", menuKit.Assets.iconStarFilled, 0);
            profileStars.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleLeft;
            var profileButton = profile.gameObject.AddComponent<Button>();
            profileButton.targetGraphic = profile.GetComponent<UiVisualState>().Surface;
            profileButton.onClick.AddListener(() => MenuClick(() => ShowScreen(AppScreen.Login)));
            profile.gameObject.AddComponent<UiPointSize>().Configure(UiMetrics.MinTouchPt);
            saveErrorIcon = menuKit.Icon(profile, "Save Error", menuKit.Assets.iconClose, 32);
            saveErrorIcon.color = UiTokens.Danger;
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
            chapterWaves = menuKit.Chip(facts, "Chapter Waves", "", 28).GetComponentInChildren<TMP_Text>();
            chapterTime = menuKit.Chip(facts, "Chapter Time", "", 28).GetComponentInChildren<TMP_Text>();
            chapterGold = menuKit.Chip(facts, "Chapter Gold", "", 28).GetComponentInChildren<TMP_Text>();
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
                regionTabs.Add(MenuButton(tabs, $"Region Tab {i + 1}", (i + 1).ToString(), null, () => FocusRegion(region)));
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
            selectedLevelDetails = (TextMeshProUGUI)menuKit.Chip(facts, "Selected Details Chip", "", 28).GetComponentInChildren<TMP_Text>();
            UiId.Assign(selectedLevelDetails.transform, "Selected Details");
            briefingTime = menuKit.Chip(facts, "Briefing Time", "", 28).GetComponentInChildren<TMP_Text>();
            var preview = CreatePanel("Tactical Preview", body, UiTokens.TacticalSurface);
            UiKit.Size((RectTransform)preview.transform, height: 150);
            preview.GetComponent<Image>().raycastTarget = false;
            tacticalMapPreview = preview.AddComponent<FrontendTacticalMapPreview>();
            enemyChips = MenuStack(body, "Enemy Intel", false);
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
            SetAnchors((RectTransform)card.transform, Vector2.zero, new Vector2(1, 0), new Vector2(Edge, 36), new Vector2(-Edge, 746));
            var eyebrow = CreateText("Eyebrow", card.transform, FrontendCopy.WelcomeEyebrow, 30, catalog.titleFont,
                FontStyles.Bold, TextAlignmentOptions.Center, theme.commandGold);
            SetAnchors(eyebrow.rectTransform, new Vector2(0, 1), Vector2.one, new Vector2(24, -88), new Vector2(-24, -26));
            var subtitle = CreateText("Subtitle", card.transform,
                FrontendCopy.WelcomeDescription, 32,
                catalog.bodyFont, FontStyles.Normal, TextAlignmentOptions.Center, theme.textPrimary);
            SetAnchors(subtitle.rectTransform, new Vector2(0, 1), Vector2.one, new Vector2(28, -238), new Vector2(-28, -98));
            var start = CreateButton("Start", card.transform, FrontendCopy.PlayNow.Value, new Vector2(700, 140),
                theme.commandGold, () => LaunchCampaignLevel(1));
            SetAnchors((RectTransform)start.transform, Vector2.zero, new Vector2(1, 0), new Vector2(28, 286), new Vector2(-28, 426));
            var account = CreateButton("Link Account", card.transform, FrontendCopy.LinkAccount, new Vector2(700, 140), theme.defenderBlue,
                () => ShowScreen(AppScreen.Login));
            account.gameObject.SetActive(false);
            SetAnchors((RectTransform)account.transform, Vector2.zero, new Vector2(1, 0), new Vector2(28, 128), new Vector2(-28, 268));
            var note = CreateText("Guest Note", card.transform,
                FrontendCopy.GuestNote, 27, catalog.bodyFont,
                FontStyles.Normal, TextAlignmentOptions.Center, theme.textMuted);
            SetAnchors(note.rectTransform, Vector2.zero, new Vector2(1, 0), new Vector2(28, 18), new Vector2(-28, 110));
        }

        private void BuildLoginScreen()
        {
            Transform root = screens[AppScreen.Login].transform;
            var scrim = CreateImage("Account Scrim", root, UiTokens.AccountScrim);
            Stretch((RectTransform)scrim.transform);
            var card = CreatePanel("Account Chronicle", root, theme.panel);
            AddCardAccent(card.transform, theme.defenderBlue);
            root.gameObject.AddComponent<FrontendResponsiveLayout>().Configure(size =>
                SetRect((RectTransform)card.transform, Vector2.zero, new Vector2(Mathf.Min(size.x - 88, 1100), Mathf.Min(size.y - 60, 1320)), Center(), Center()));
            var close = CreateButton("Back", card.transform, "×", new Vector2(140, 140), theme.night, ReturnFromAccount);
            SetRect((RectTransform)close.transform, new Vector2(-20, -20), new Vector2(140, 140), TopRight(), TopRight());
            var title = CreateText("Heading", card.transform, FrontendCopy.ProtectProgress, 38, catalog.titleFont,
                FontStyles.Bold, TextAlignmentOptions.Left, theme.textPrimary);
            SetAnchors(title.rectTransform, new Vector2(0, 1), Vector2.one, new Vector2(32, -154), new Vector2(-184, -28));
            var explanation = CreateText("Account Explanation", card.transform,
                FrontendCopy.AccountDescription,
                30, catalog.bodyFont, FontStyles.Normal, TextAlignmentOptions.Left, theme.textMuted);
            SetAnchors(explanation.rectTransform, new Vector2(0, 1), Vector2.one, new Vector2(32, -338), new Vector2(-32, -184));
            var guest = CreateButton("Guest Sign In", card.transform, FrontendCopy.StartAsGuest, new Vector2(700, 140), theme.commandGold, SignInAsGuest);
            SetAnchors((RectTransform)guest.transform, new Vector2(0, 1), Vector2.one, new Vector2(32, -518), new Vector2(-32, -378));
            var link = CreateButton("Player Account Sign In", card.transform, FrontendCopy.LinkAccount, new Vector2(700, 140), theme.defenderBlue,
                () => RequestExternalSignIn(PlayerAuthProvider.PlayerAccount));
            SetAnchors((RectTransform)link.transform, new Vector2(0, 1), Vector2.one, new Vector2(32, -684), new Vector2(-32, -544));
            loginStatus = CreateText("Login Status", card.transform, "", 34, catalog.bodyFont, FontStyles.Normal,
                TextAlignmentOptions.Center, theme.commandGold);
            SetAnchors(loginStatus.rectTransform, new Vector2(0, 1), Vector2.one, new Vector2(32, -834), new Vector2(-32, -708));
            var privacy = CreateButton("Privacy Policy", card.transform, FrontendCopy.PrivacyPolicy, new Vector2(700, 140), theme.night,
                () => OpenLegalUrl(theme.privacyPolicyUrl, "Gizlilik Politikası"));
            SetAnchors((RectTransform)privacy.transform, Vector2.zero, new Vector2(1, 0), new Vector2(32, 188), new Vector2(-32, 328));
            var terms = CreateButton("Terms Of Service", card.transform, FrontendCopy.TermsOfService, new Vector2(700, 140), theme.night,
                () => OpenLegalUrl(theme.termsOfServiceUrl, "Kullanım Koşulları"));
            SetAnchors((RectTransform)terms.transform, Vector2.zero, new Vector2(1, 0), new Vector2(32, 28), new Vector2(-32, 168));
        }

        private void BuildSettingsModal()
        {
            settingsModal = new GameObject("Settings Modal", typeof(RectTransform));
            settingsModal.transform.SetParent(screens[AppScreen.MainMenu].transform.parent, false);
            Stretch((RectTransform)settingsModal.transform);
            var scrim = CreateImage("Settings Scrim", settingsModal.transform, UiTokens.SettingsScrim);
            Stretch((RectTransform)scrim.transform);
            scrim.GetComponent<Image>().raycastTarget = true;
            scrim.AddComponent<Button>().onClick.AddListener(CloseSettings);
            var card = CreatePanel("Settings Chronicle", settingsModal.transform, theme.panel);
            card.GetComponent<Image>().raycastTarget = true;
            Stretch((RectTransform)card.transform, new Vector2(Edge, 30), new Vector2(-Edge, -30));
            var title = CreateText("Settings Title", card.transform, FrontendCopy.SettingsTitle, 44, catalog.titleFont, FontStyles.Bold,
                TextAlignmentOptions.Left, theme.textPrimary);
            SetAnchors(title.rectTransform, new Vector2(0, 1), Vector2.one, new Vector2(32, -110), new Vector2(-32, -22));
            var scrollRoot = new GameObject("Settings Scroll", typeof(RectTransform), typeof(ScrollRect));
            scrollRoot.transform.SetParent(card.transform, false);
            SetAnchors((RectTransform)scrollRoot.transform, Vector2.zero, Vector2.one, new Vector2(24, 188), new Vector2(-24, -130));
            var viewport = CreateImage("Viewport", scrollRoot.transform, Color.clear);
            viewport.GetComponent<Image>().raycastTarget = true;
            Stretch((RectTransform)viewport.transform); viewport.AddComponent<RectMask2D>();
            var body = new GameObject("Settings Content", typeof(RectTransform));
            body.transform.SetParent(viewport.transform, false);
            var rect = (RectTransform)body.transform;
            rect.anchorMin = new Vector2(0, 1); rect.anchorMax = Vector2.one; rect.pivot = new Vector2(.5f, 1);
            rect.sizeDelta = new Vector2(0, 1366);
            var scroll = scrollRoot.GetComponent<ScrollRect>();
            scroll.horizontal = false; scroll.vertical = true; scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.viewport = (RectTransform)viewport.transform; scroll.content = rect;
            turkishLanguageButton = CreateButton("Turkish Language", body.transform, "TÜRKÇE", new Vector2(350, 140), theme.defenderBlue,
                () => FrontendPreferences.Language = GameLanguage.Turkish);
            englishLanguageButton = CreateButton("English Language", body.transform, "ENGLISH", new Vector2(350, 140), theme.defenderBlue,
                () => FrontendPreferences.Language = GameLanguage.English);
            SetAnchors((RectTransform)turkishLanguageButton.transform, new Vector2(0, 1), new Vector2(.5f, 1), new Vector2(4, -146), new Vector2(-10, -6));
            SetAnchors((RectTransform)englishLanguageButton.transform, new Vector2(.5f, 1), Vector2.one, new Vector2(10, -146), new Vector2(-4, -6));
            CreateSliderRow(body.transform, "MÜZİK Row", FrontendCopy.Music, new Vector2(0, -180), FrontendPreferences.MusicVolume, value =>
            {
                FrontendPreferences.MusicVolume = value;
            });
            CreateSliderRow(body.transform, "EFEKTLER Row", FrontendCopy.Effects, new Vector2(0, -346), FrontendPreferences.SfxVolume, value =>
            {
                FrontendPreferences.SfxVolume = value;
                ApplyStoredAudioPreferences();
            });
            CreateToggleRow(body.transform, "TİTREŞİM Row", FrontendCopy.Haptics, new Vector2(0, -512), FrontendPreferences.Haptics, value => FrontendPreferences.Haptics = value);
            CreateToggleRow(body.transform, "KAMERA SARSINTISI Row", FrontendCopy.CameraShake, new Vector2(0, -678), FrontendPreferences.CameraShake, value => FrontendPreferences.CameraShake = value);
            CreateToggleRow(body.transform, "AZALTILMIŞ HAREKET Row", FrontendCopy.ReducedMotion, new Vector2(0, -844), FrontendPreferences.ReducedMotion, value => FrontendPreferences.ReducedMotion = value);
            var heroName = CreateButton("Settings Hero", body.transform, FrontendCopy.MyHero, new Vector2(700, 140), theme.defenderBlue, OpenHeroName);
            SetAnchors((RectTransform)heroName.transform, new Vector2(0, 1), Vector2.one, new Vector2(4, -1174), new Vector2(-4, -1034));
            var account = CreateButton("Settings Account", body.transform, FrontendCopy.AccountManagement, new Vector2(700, 140), theme.defenderBlue,
                () => { CloseSettings(); ShowScreen(AppScreen.Login); });
            SetAnchors((RectTransform)account.transform, new Vector2(0, 1), Vector2.one, new Vector2(4, -1340), new Vector2(-4, -1200));
            var done = CreateButton("Close Settings", card.transform, FrontendCopy.Done, new Vector2(700, 140), theme.commandGold, CloseSettings);
            SetAnchors((RectTransform)done.transform, Vector2.zero, new Vector2(1, 0), new Vector2(28, 26), new Vector2(-28, 166));
            UiId.AssignHierarchy(settingsModal.transform);
            settingsModal.SetActive(false);
        }

        private void CreateSliderRow(Transform parent, string rowName, LocalizedCopy label, Vector2 position, float value, Action<float> changed)
        {
            var row = CreatePanel(rowName, parent, theme.panelRaised);
            SetAnchors((RectTransform)row.transform, new Vector2(0, 1), Vector2.one, new Vector2(4, position.y - 140), new Vector2(-4, position.y));
            var text = CreateText("Label", row.transform, label, 30, catalog.bodyFont, FontStyles.Bold, TextAlignmentOptions.Left, theme.textPrimary);
            SetAnchors(text.rectTransform, Vector2.zero, new Vector2(.42f, 1), new Vector2(20, 8), new Vector2(-8, -8));
            var sliderObject = CreateImage("Slider", row.transform, Color.clear);
            SetAnchors((RectTransform)sliderObject.transform, new Vector2(.43f, 0), Vector2.one, new Vector2(4, 0), new Vector2(-20, 0));
            sliderObject.GetComponent<Image>().raycastTarget = true;
            var slider = sliderObject.AddComponent<Slider>();
            var track = CreateImage("Track", sliderObject.transform, theme.nightDeep);
            SetAnchors((RectTransform)track.transform, new Vector2(0, .42f), new Vector2(1, .58f), Vector2.zero, Vector2.zero);
            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderObject.transform, false);
            SetAnchors((RectTransform)fillArea.transform, new Vector2(0, .42f), new Vector2(1, .58f), new Vector2(30, 0), new Vector2(-30, 0));
            var fill = CreateImage("Fill", fillArea.transform, theme.commandGold);
            Stretch((RectTransform)fill.transform);
            slider.fillRect = (RectTransform)fill.transform;
            var handleArea = new GameObject("Handle Area", typeof(RectTransform));
            handleArea.transform.SetParent(sliderObject.transform, false);
            SetAnchors((RectTransform)handleArea.transform, new Vector2(0, .22f), new Vector2(1, .78f), new Vector2(30, 0), new Vector2(-30, 0));
            var handle = CreateImage("Handle", handleArea.transform, theme.commandGold);
            handle.GetComponent<Image>().sprite = theme.buttonSprite;
            handle.GetComponent<Image>().type = Image.Type.Sliced;
            SetRect((RectTransform)handle.transform, Vector2.zero, new Vector2(60, 0), Center(), Center());
            slider.handleRect = (RectTransform)handle.transform;
            slider.targetGraphic = handle.GetComponent<Image>(); slider.minValue = 0; slider.maxValue = 1;
            slider.SetValueWithoutNotify(value); slider.onValueChanged.AddListener(v => changed(v));
        }

        private void CreateToggleRow(Transform parent, string rowName, LocalizedCopy label, Vector2 position, bool value, Action<bool> changed)
        {
            var row = CreatePanel(rowName, parent, theme.panelRaised);
            SetAnchors((RectTransform)row.transform, new Vector2(0, 1), Vector2.one, new Vector2(4, position.y - 140), new Vector2(-4, position.y));
            row.GetComponent<Image>().raycastTarget = true;
            var text = CreateText("Label", row.transform, label, 30, catalog.bodyFont, FontStyles.Bold, TextAlignmentOptions.Left, theme.textPrimary);
            Stretch(text.rectTransform, new Vector2(20, 12), new Vector2(-150, -12));
            var box = CreateImage("Toggle Outline", row.transform, theme.textMuted);
            box.GetComponent<Image>().sprite = theme.buttonSprite;
            box.GetComponent<Image>().type = Image.Type.Sliced;
            SetRect((RectTransform)box.transform, new Vector2(-28, -28), new Vector2(84, 84), TopRight(), TopRight());
            var inset = CreateImage("Toggle Background", box.transform, theme.nightDeep);
            Stretch((RectTransform)inset.transform, new Vector2(8, 8), new Vector2(-8, -8));
            var check = CreateImage("Checkmark", inset.transform, theme.commandGold);
            Stretch((RectTransform)check.transform, new Vector2(10, 10), new Vector2(-10, -10));
            var toggle = row.AddComponent<Toggle>(); toggle.targetGraphic = row.GetComponent<Image>(); toggle.graphic = check.GetComponent<Image>();
            toggle.SetIsOnWithoutNotify(value); toggle.onValueChanged.AddListener(v => changed(v));
        }

    }
}
