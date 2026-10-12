using System;
using System.Collections;
using DefenderChronicles.Core;
using DefenderChronicles.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DefenderChronicles.UI
{
    public sealed partial class AppFlowController
    {
        private const float Edge = 44f;
        private const float TouchHeight = 140f;

        private void BuildMainMenuScreen()
        {
            Transform root = screens[AppScreen.MainMenu].transform;
            var brand = CreateBrandImage("Brand Logo", root, theme.brandLogo);
            SetAnchors((RectTransform)brand.transform, new Vector2(0, 1), Vector2.one,
                new Vector2(Edge, -260), new Vector2(-Edge, -16));
            var profile = CreatePanel("Profile Crest", root, UiTokens.ProfileSurface);
            SetAnchors((RectTransform)profile.transform, new Vector2(0, 1), Vector2.one,
                new Vector2(Edge, -400), new Vector2(-Edge - 156, -264));
            var mark = CreateBrandImage("Profile Brand Mark", profile.transform, theme.brandMark);
            SetRect((RectTransform)mark.transform, new Vector2(20, -28), new Vector2(76, 76), TopLeft(), TopLeft());
            profileLabel = CreateText("Profile", profile.transform, "", 34, catalog.bodyFont, FontStyles.Normal,
                TextAlignmentOptions.Left, theme.textPrimary);
            Stretch(profileLabel.rectTransform, new Vector2(114, 16), new Vector2(-18, -16));
            var profileButton = profile.AddComponent<Button>();
            profile.GetComponent<Image>().raycastTarget = true;
            profileButton.targetGraphic = profile.GetComponent<Image>();
            profileButton.onClick.AddListener(() => ShowScreen(AppScreen.Login));
            var settings = CreateButton("Settings", root, FrontendCopy.SettingsButton, new Vector2(140, 136), theme.night, OpenSettings);
            SetRect((RectTransform)settings.transform, new Vector2(-Edge, -264), new Vector2(140, 136), TopRight(), TopRight());

            var card = CreatePanel("Continue Chronicle", root, UiTokens.WithAlpha(theme.panel, .9f));
            AddCardAccent(card.transform, theme.commandGold);
            SetAnchors((RectTransform)card.transform, Vector2.zero, new Vector2(1, 0),
                new Vector2(Edge, 212), new Vector2(-Edge, 700));
            var title = CreateText("Callout", card.transform, FrontendCopy.MainMenuCallout, 48,
                catalog.titleFont, FontStyles.Bold, TextAlignmentOptions.Left, theme.textPrimary);
            SetAnchors(title.rectTransform, new Vector2(0, 1), Vector2.one, new Vector2(32, -152), new Vector2(-32, -20));
            continueDetails = CreateText("Continue Details", card.transform, "", 34, catalog.bodyFont,
                FontStyles.Normal, TextAlignmentOptions.Left, theme.textMuted);
            SetAnchors(continueDetails.rectTransform, new Vector2(0, 1), Vector2.one, new Vector2(32, -282), new Vector2(-32, -164));
            continueButton = CreateButton("Continue", card.transform, GameLocalization.Text("DEVAM ET  ›", "CONTINUE  ›"),
                new Vector2(800, TouchHeight), theme.commandGold, ContinueCampaign);
            SetAnchors((RectTransform)continueButton.transform, Vector2.zero, new Vector2(1, 0), new Vector2(28, 26), new Vector2(-28, 166));
            var map = CreateButton("Campaign", root, FrontendCopy.RealmMap, new Vector2(800, TouchHeight), theme.defenderBlue, OpenCampaign);
            SetAnchors((RectTransform)map.transform, Vector2.zero, new Vector2(1, 0), new Vector2(Edge, 40), new Vector2(-Edge, 180));
            BuildGuestImportAction();
            RefreshProfile();
        }

        private void BuildCampaignScreen()
        {
            Transform root = screens[AppScreen.Campaign].transform;
            CreateHeader(root, FrontendCopy.RealmMap, "", () => ShowScreen(AppScreen.MainMenu));
            var tabs = new GameObject("Region Tabs", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            tabs.transform.SetParent(root, false);
            SetAnchors((RectTransform)tabs.transform, new Vector2(0, 1), Vector2.one,
                new Vector2(Edge, -300), new Vector2(-Edge, -166));
            var layout = tabs.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 14; layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = true;
            for (int i = 0; i < VisibleRegionCount; i++)
            {
                int region = i;
                regionTabs.Add(CreateButton($"Region Tab {i + 1}", tabs.transform, (i + 1).ToString(),
                    new Vector2(160, 134), theme.defenderBlue, () => FocusRegion(region)));
            }
            var scroll = new GameObject("Campaign Scroll", typeof(RectTransform), typeof(FrontendCampaignPager));
            scroll.transform.SetParent(root, false);
            campaignScroll = scroll.GetComponent<ScrollRect>();
            campaignScroll.horizontal = true; campaignScroll.vertical = false;
            campaignScroll.movementType = ScrollRect.MovementType.Clamped;
            campaignScroll.decelerationRate = .06f;
            var viewport = CreateImage("Viewport", scroll.transform, UiTokens.ViewportScrim);
            viewport.GetComponent<Image>().raycastTarget = true;
            Stretch((RectTransform)viewport.transform);
            viewport.AddComponent<RectMask2D>();
            var content = new GameObject("Content", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            var rect = (RectTransform)content.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, .5f); rect.sizeDelta = Vector2.zero;
            var row = content.GetComponent<HorizontalLayoutGroup>();
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = false; row.childForceExpandHeight = true;
            content.GetComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            campaignScroll.viewport = (RectTransform)viewport.transform; campaignScroll.content = rect;
            for (int i = 0; i < VisibleRegionCount; i++) BuildRegion(content.transform, CampaignCatalog.Regions[i]);
            campaignScroll.onValueChanged.AddListener(value =>
            {
                int current = Mathf.Clamp(Mathf.RoundToInt(value.x * (VisibleRegionCount - 1)), 0, VisibleRegionCount - 1);
                EnsureRegion(current);
                if (navigationState.Region != current) { navigationState.Region = current; UpdateRegionTabs(); }
            });

            var panel = CreatePanel("Selection Bar", root, UiTokens.WithAlpha(theme.panel, .96f));
            AddCardAccent(panel.transform, theme.commandGold);
            selectedLevelTitle = CreateText("Selected Level", panel.transform, "", 38, catalog.titleFont,
                FontStyles.Bold, TextAlignmentOptions.Left, theme.textPrimary);
            selectedLevelDetails = CreateText("Selected Details", panel.transform, "", 32, catalog.bodyFont,
                FontStyles.Normal, TextAlignmentOptions.Left, theme.textMuted);
            briefingEnemies = CreateText("Enemy Intel", panel.transform, "", 32, catalog.bodyFont,
                FontStyles.Normal, TextAlignmentOptions.Left, theme.textPrimary);
            briefingReward = CreateText("Briefing Reward", panel.transform, "", 32, catalog.bodyFont,
                FontStyles.Normal, TextAlignmentOptions.Left, theme.commandGold);
            var preview = CreatePanel("Tactical Preview", panel.transform, UiTokens.TacticalSurface);
            preview.GetComponent<Image>().raycastTarget = false;
            tacticalMapPreview = preview.AddComponent<FrontendTacticalMapPreview>();
            playSelectedButton = CreateButton("Play Selected", panel.transform, FrontendCopy.StartDefence,
                new Vector2(700, TouchHeight), theme.commandGold, StartSelectedLevel);
            root.gameObject.AddComponent<FrontendResponsiveLayout>().Configure(size => LayoutCampaign(size, (RectTransform)scroll.transform, (RectTransform)panel.transform));
            selectedLevelTitle.enableAutoSizing = true;
            selectedLevelTitle.fontSizeMin = 28; selectedLevelTitle.fontSizeMax = 38;
            briefingEnemies.enableAutoSizing = true;
            briefingEnemies.fontSizeMin = 26; briefingEnemies.fontSizeMax = 32;
            RefreshLevelButtons();
            UpdateSelectedDetails();
            UpdateRegionTabs();
        }

        private void LayoutCampaign(Vector2 size, RectTransform map, RectTransform panel)
        {
            bool columns = FrontendResponsiveLayout.UseColumns(size);
            SetAnchors(map, Vector2.zero, new Vector2(columns ? .62f : 1, 1),
                new Vector2(Edge, columns ? 30 : 654), new Vector2(columns ? -16 : -Edge, -324));
            if (columns)
                SetAnchors(panel, new Vector2(.62f, 1), Vector2.one, new Vector2(8, -324 - Mathf.Min(900, size.y - 354)), new Vector2(-Edge, -324));
            else
                SetAnchors(panel, Vector2.zero, new Vector2(1, 0), new Vector2(Edge, 26), new Vector2(-Edge, 630));
            SetAnchors(selectedLevelTitle.rectTransform, new Vector2(0, 1), Vector2.one, new Vector2(28, -110), new Vector2(-28, -22));
            SetAnchors(selectedLevelDetails.rectTransform, new Vector2(0, 1), Vector2.one,
                new Vector2(28, columns ? -238 : -178), new Vector2(-28, -114));
            RectTransform preview = tacticalMapPreview.transform as RectTransform;
            if (columns)
                SetAnchors(preview, new Vector2(0, 1), Vector2.one, new Vector2(24, -510), new Vector2(-24, -252));
            else
                SetAnchors(preview, new Vector2(0, 0), new Vector2(.55f, 1), new Vector2(28, 178), new Vector2(-14, -190));
            SetAnchors(briefingEnemies.rectTransform, new Vector2(columns ? 0 : .57f, 1), Vector2.one,
                new Vector2(28, columns ? -620 : -330), new Vector2(-28, columns ? -526 : -198));
            SetAnchors(briefingReward.rectTransform, new Vector2(columns ? 0 : .57f, 1), Vector2.one,
                new Vector2(28, columns ? -734 : -454), new Vector2(-28, columns ? -634 : -342));
            SetAnchors((RectTransform)playSelectedButton.transform, Vector2.zero, new Vector2(1, 0),
                new Vector2(24, 24), new Vector2(-24, 164));
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
            var route = new GameObject("Route Content", typeof(RectTransform));
            route.transform.SetParent(viewport.transform, false);
            var routeRect = (RectTransform)route.transform;
            routeRect.anchorMin = new Vector2(0, 1); routeRect.anchorMax = Vector2.one;
            routeRect.pivot = new Vector2(.5f, 1); routeRect.sizeDelta = new Vector2(0, 1720);
            vertical.viewport = (RectTransform)viewport.transform; vertical.content = routeRect;
            regionScrolls.Add(vertical);
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
            var route = routeRect.gameObject;
            var heading = CreateText("Region Name", route.transform,
                $"{region.number} · {region.localizedName.ToUpperInvariant()}", 32, catalog.titleFont,
                FontStyles.Bold, TextAlignmentOptions.Left, theme.commandGold);
            SetAnchors(heading.rectTransform, new Vector2(0, 1), Vector2.one, new Vector2(24, -98), new Vector2(-24, -16));
            var buttons = new RectTransform[region.levels.Count];
            var labels = new RectTransform[region.levels.Count];
            var lines = new RectTransform[region.levels.Count - 1];
            for (int i = 0; i < lines.Length; i++)
            {
                var line = CreateImage("Route", route.transform, UiTokens.RouteLine);
                lines[i] = (RectTransform)line.transform;
            }
            for (int i = 0; i < region.levels.Count; i++)
            {
                var level = region.levels[i];
                var button = CreateButton($"Level {level.number}", route.transform, level.number.ToString(),
                    new Vector2(144, 144), region.accentColor, () => SelectLevel(level.number));
                button.interactable = level.number <= progress.Data.unlockedLevel;
                buttons[i] = (RectTransform)button.transform;
                CreateLevelSelectionRing(button.transform); levelButtons.Add(button); levelNumbers.Add(level.number);
                var stars = CreateText("Progress", route.transform, "", 25, catalog.bodyFont, FontStyles.Bold,
                    TextAlignmentOptions.Center, theme.textPrimary);
                labels[i] = stars.rectTransform; levelStars.Add(stars);
            }
            page.AddComponent<FrontendResponsiveLayout>().Configure(size =>
            {
                float height = Mathf.Max(1720, size.y);
                routeRect.sizeDelta = new Vector2(0, height);
                for (int i = 0; i < buttons.Length; i++)
                {
                    float x = size.x * (i % 2 == 0 ? .29f : .71f);
                    float y = -190 - i * ((height - 350) / 9f);
                    SetRect(buttons[i], new Vector2(x, y), new Vector2(144, 144), TopLeft(), TopLeft());
                    buttons[i].pivot = new Vector2(.5f, .5f);
                    SetRect(labels[i], new Vector2(x, y - 78), new Vector2(210, 44), TopLeft(), TopLeft());
                    labels[i].pivot = new Vector2(.5f, 1);
                    if (i > 0)
                    {
                        Vector2 from = buttons[i - 1].anchoredPosition;
                        Vector2 delta = buttons[i].anchoredPosition - from;
                        SetRect(lines[i - 1], from, new Vector2(delta.magnitude, 7), TopLeft(), TopLeft());
                        lines[i - 1].pivot = new Vector2(0, .5f);
                        lines[i - 1].localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
                    }
                }
            });
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
            var start = CreateButton("Start", card.transform, GameLocalization.Text("OYUNA BAŞLA  ›", "PLAY NOW  ›"), new Vector2(700, 140),
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
            var handleArea = new GameObject("Handle Area", typeof(RectTransform));
            handleArea.transform.SetParent(sliderObject.transform, false);
            SetAnchors((RectTransform)handleArea.transform, new Vector2(0, .22f), new Vector2(1, .78f), Vector2.zero, Vector2.zero);
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
