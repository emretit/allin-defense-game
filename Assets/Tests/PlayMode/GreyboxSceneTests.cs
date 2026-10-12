using System.Collections;
using System.IO;
using System.Linq;
using DefenderChronicles.Core;
using DefenderChronicles.Presentation;
using DefenderChronicles.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Data = DefenderChronicles.Data;

namespace DefenderChronicles.Tests
{
    public sealed class GreyboxSceneTests
    {
        private static IEnumerator WaitForBattle(AppFlowController flow)
        {
            double deadline = Time.realtimeSinceStartupAsDouble + 15;
            while (flow.IsLaunchingBattle && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
            Assert.That(flow.IsLaunchingBattle, Is.False, "Battle setup must finish");
        }

        [UnityTest]
        public IEnumerator SelectableRaycastIgnoresCloserActorCollider()
        {
            int selectableLayer = LayerMask.NameToLayer("Selectable");
            GameObject actor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            GameObject target = GameObject.CreatePrimitive(PrimitiveType.Cube);
            actor.name = "Actor Raycast Blocker";
            target.name = "Selectable Raycast Target";
            actor.transform.position = new Vector3(0f, 0f, 2f);
            target.transform.position = new Vector3(0f, 0f, 4f);
            target.layer = selectableLayer;
            yield return new WaitForFixedUpdate();

            bool hitSomething = Physics.Raycast(Vector3.zero, Vector3.forward, out RaycastHit hit, 10f,
                LayerMask.GetMask("Selectable"), QueryTriggerInteraction.Ignore);

            Assert.That(hitSomething, Is.True);
            Assert.That(hit.collider.gameObject, Is.SameAs(target));
            Object.Destroy(actor);
            Object.Destroy(target);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDownSession()
        {
            Time.timeScale = 1f;
            foreach (var session in Object.FindObjectsByType<GameSessionController>(FindObjectsSortMode.None))
                if (session != null) Object.Destroy(session.gameObject);
            foreach (var hud in GameObject.FindGameObjectsWithTag("Untagged"))
                if (hud != null && hud.name == "Game HUD") Object.Destroy(hud);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SessionOpensWithCompleteGreyboxContent()
        {
            var gameObject = new GameObject("Test Game Session");
            var session = gameObject.AddComponent<GameSessionController>();
            session.Initialize();
            yield return null;

            Assert.That(session.Phase, Is.EqualTo(GamePhase.Preparing));
            Assert.That(session.Gold, Is.EqualTo(Data.CampaignCatalog.Level(10).startingGold));
            Assert.That(session.Lives, Is.EqualTo(20));
            Assert.That(session.CurrentGameSpeed, Is.EqualTo(1f));
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(session.WaveCount, Is.EqualTo(8));
            Assert.That(session.SlotCount, Is.EqualTo(8));
            Assert.That(session.Hero, Is.Not.Null);
            for (int i = 1; i <= 8; i++)
            {
                GameObject slot = GameObject.Find($"Outpost Slot {i}");
                Assert.That(slot, Is.Not.Null, $"Fixed outpost slot {i} must be visible.");
                Assert.That(slot.GetComponent<Collider>(), Is.Not.Null, $"Fixed outpost slot {i} must be selectable.");
            }

            Assert.That(session.TryBuildOutpost(0, Data.OutpostKind.Archer), Is.True);
            Assert.That(session.Gold, Is.EqualTo(Data.CampaignCatalog.Level(10).startingGold - 75));
            Assert.That(session.TryBuildOutpost(0, Data.OutpostKind.Mage), Is.False,
                "An occupied fixed slot must reject another outpost.");
            Assert.That(session.TryBuildOutpost(-1, Data.OutpostKind.Mage), Is.False,
                "A tap outside the fixed slot range must not build.");
            Assert.That(session.TryBuildOutpost(8, Data.OutpostKind.Mage), Is.False,
                "A tap outside the fixed slot range must not build.");
            Assert.That(session.Gold, Is.EqualTo(Data.CampaignCatalog.Level(10).startingGold - 75));
            Assert.That(session.TryUpgradeOutpost(0), Is.True);
            Assert.That(session.Gold, Is.EqualTo(Data.CampaignCatalog.Level(10).startingGold - 130));
            Assert.That(session.TrySellOutpost(0), Is.True);
            Assert.That(session.Gold, Is.EqualTo(Data.CampaignCatalog.Level(10).startingGold - 52));
            Assert.That(session.TryBuildOutpost(0, Data.OutpostKind.Mage), Is.True,
                "Selling must make the same fixed slot reusable.");
            Assert.That(session.Gold, Is.EqualTo(Data.CampaignCatalog.Level(10).startingGold - 177));
            Assert.That(session.SlotCount, Is.EqualTo(8));

            Vector3 guardianPosition = session.Hero.Position;
            yield return null;
            Assert.That(session.Hero.Position, Is.EqualTo(guardianPosition),
                "The keep guardian must remain stationary.");
            Assert.That(session.Hero.TryUseShieldBash(), Is.False);
            Assert.That(session.Hero.AbilityRemaining, Is.Zero);
            session.Hero.TakeDamage(10000);
            Assert.That(session.Hero.IsAlive, Is.False);
            Time.timeScale = 20f;
            yield return new WaitForSeconds(10.1f);
            Time.timeScale = 1f;
            Assert.That(session.Hero.IsAlive, Is.True);
            Assert.That(session.Hero.Position, Is.EqualTo(guardianPosition),
                "The keep guardian must respawn at the fixed guard position.");

            session.TogglePause();
            Assert.That(session.Phase, Is.EqualTo(GamePhase.Paused));
            session.TogglePause();
            Assert.That(session.Phase, Is.EqualTo(GamePhase.Preparing));
            LogAssert.NoUnexpectedReceived();
            Object.Destroy(gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator MultiEntranceBattleShowsIndicatorsUntilFirstWaveStarts()
        {
            var gameObject = new GameObject("Entrance Indicator Session");
            var session = gameObject.AddComponent<GameSessionController>();
            session.SetCampaignLevel(14);
            session.Initialize();
            yield return null;

            Transform indicators = session.transform.Find("Greybox World/Active Entrance Indicators");
            Assert.That(indicators, Is.Not.Null);
            Assert.That(indicators.gameObject.activeSelf, Is.True);
            Assert.That(indicators.Find("Entrance Arrow 1"), Is.Not.Null);
            Assert.That(indicators.Find("Entrance Arrow 2"), Is.Not.Null);

            session.StartNextWave();
            Assert.That(indicators.gameObject.activeSelf, Is.False);

            Object.Destroy(gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator AppFlowShowsTenLevelsWhileStorageKeepsFifty()
        {
            string savePath = Path.Combine(Path.GetTempPath(), $"dc-ui-{System.Guid.NewGuid():N}.json");
            var catalog = Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog");
            Assert.That(catalog, Is.Not.Null);
            var sessionObject = new GameObject("App Flow Session");
            var session = sessionObject.AddComponent<GameSessionController>();
            var flowObject = new GameObject("App Flow Test");
            var flow = flowObject.AddComponent<AppFlowController>();

            flow.Initialize(catalog, session, false, savePath);
            Assert.That(MobileOrientationController.CurrentMode,
                Is.EqualTo(MobileOrientationMode.PortraitFrontend));
            UnityEngine.UI.Image bootLogo = flow.GetComponentsInChildren<UnityEngine.UI.Image>(true)
                .Single(image => image.name == "Boot Brand Logo");
            Assert.That(bootLogo.sprite, Is.SameAs(catalog.frontendTheme.brandLogo));
            Assert.That(bootLogo.preserveAspect, Is.True);
            Assert.That(bootLogo.raycastTarget, Is.False);
            yield return new WaitForSecondsRealtime(0.90f);
            Assert.That(flow.CurrentScreen, Is.EqualTo(AppScreen.Welcome));
            Assert.That(flow.GetComponentInChildren<CanvasScaler>(true).referenceResolution,
                Is.EqualTo(new Vector2(1080f, 1920f)));
            Assert.That(GameObject.Find("Start"), Is.Not.Null);
            UnityEngine.UI.Image welcomeLogo = flow.GetComponentsInChildren<UnityEngine.UI.Image>(true)
                .Single(image => image.name == "Welcome Brand Logo");
            Assert.That(welcomeLogo.sprite, Is.SameAs(catalog.frontendTheme.brandLogo));
            Assert.That(welcomeLogo.preserveAspect, Is.True);
            Assert.That(flow.GetComponentsInChildren<Transform>(true)
                .Any(child => child.name == "Welcome Royal Crest" || child.name == "Boot Title"), Is.False);
            Assert.That(GameObject.Find("Subtitle").GetComponent<TMPro.TextMeshProUGUI>().text,
                Does.Contain("Şafağa kadar dayan"));

            InvokePrivate(flow, "ShowScreen", AppScreen.Login);
            Assert.That(flow.CurrentScreen, Is.EqualTo(AppScreen.Login));
            Assert.That(GameObject.Find("Guest Sign In"), Is.Not.Null);
            Assert.That(GameObject.Find("Player Account Sign In"), Is.Not.Null);
            Assert.That(GameObject.Find("Apple Sign In"), Is.Null);
            Assert.That(GameObject.Find("Google Sign In"), Is.Null);

            InvokePrivate(flow, "SignInAsGuest");
            Assert.That(flow.CurrentScreen, Is.EqualTo(AppScreen.MainMenu));
            UnityEngine.UI.Image mainMenuLogo = flow.GetComponentsInChildren<UnityEngine.UI.Image>(true)
                .Single(image => image.name == "Brand Logo");
            UnityEngine.UI.Image profileMark = flow.GetComponentsInChildren<UnityEngine.UI.Image>(true)
                .Single(image => image.name == "Profile Brand Mark");
            Assert.That(mainMenuLogo.sprite, Is.SameAs(catalog.frontendTheme.brandLogo));
            Assert.That(profileMark.sprite, Is.SameAs(catalog.frontendTheme.brandMark));
            Assert.That(GameObject.Find("Moonlit Sigil"), Is.Not.Null);
            Assert.That(GameObject.Find("Ember Sigil"), Is.Not.Null);
            Assert.That(GameObject.Find("Continue Chronicle").transform.Find("Accent Rail"), Is.Not.Null);
            Assert.That(flow.GetComponentsInChildren<Transform>(true)
                .Any(child => child.name == "Profile Sigil"), Is.False);
            InvokePrivate(flow, "OpenCampaign");
            yield return null;
            Assert.That(flow.CurrentScreen, Is.EqualTo(AppScreen.Campaign));
            FrontendTacticalMapPreview tacticalPreview = flow.GetComponentInChildren<FrontendTacticalMapPreview>(true);
            Assert.That(tacticalPreview, Is.Not.Null);
            Assert.That(tacticalPreview.EntranceMarkerCount, Is.EqualTo(1));
            Assert.That(tacticalPreview.SlotMarkerCount, Is.EqualTo(8));
            Assert.That(tacticalPreview.RouteSegmentCount, Is.GreaterThan(0));
            int levelButtonCount = flow.GetComponentsInChildren<Button>(true)
                .Count(button => button.name.StartsWith("Level "));
            Assert.That(levelButtonCount, Is.EqualTo(10));
            RectTransform firstLevel = flow.GetComponentsInChildren<Button>(true)
                .First(button => button.name == "Level 1").GetComponent<RectTransform>();
            RectTransform tenthLevel = flow.GetComponentsInChildren<Button>(true)
                .First(button => button.name == "Level 10").GetComponent<RectTransform>();
            Assert.That(firstLevel.anchoredPosition.y, Is.GreaterThan(tenthLevel.anchoredPosition.y),
                "The portrait kingdom path must progress from the top of the map to the bottom.");
            Assert.That(GameObject.Find("Level 1").GetComponent<Button>().interactable, Is.True);
            Assert.That(GameObject.Find("Level 2").GetComponent<Button>().interactable, Is.False);
            Transform selectionRing = GameObject.Find("Level 1").transform.Find("Selection Ring");
            Assert.That(selectionRing, Is.Not.Null);
            Assert.That(selectionRing.gameObject.activeSelf, Is.True);
            ScrollRect campaignScroll = flow.GetComponentsInChildren<ScrollRect>(true)
                .Single(item => item.name == "Campaign Scroll");
            LayoutElement firstRegionLayout = campaignScroll.content.Find("Region 1").GetComponent<LayoutElement>();
            Assert.That(firstRegionLayout.preferredWidth,
                Is.EqualTo(campaignScroll.viewport.rect.width).Within(0.5f));

            InvokePrivate(flow, "OpenBriefing");
            yield return null;
            Assert.That(flow.CurrentScreen, Is.EqualTo(AppScreen.Campaign));
            Assert.That(GameObject.Find("Selected Level"), Is.Not.Null);
            Assert.That(GameObject.Find("Enemy Intel"), Is.Not.Null);
            Assert.That(GameObject.Find("Play Selected"), Is.Not.Null);

            InvokePrivate(flow, "StartSelectedLevel");
            yield return WaitForBattle(flow);
            Assert.That(session.CampaignLevelNumber, Is.EqualTo(1));
            Assert.That(session.WaveCount, Is.EqualTo(3));
            Assert.That(session.GuidanceStep, Is.EqualTo(FirstBattleGuidanceStep.SelectSlot));
            Assert.That(session.SlotCount, Is.EqualTo(8));
            Assert.That(session.TryBuildOutpost(0, Data.OutpostKind.Archer), Is.True);
            Assert.That(session.GuidanceStep, Is.EqualTo(FirstBattleGuidanceStep.StartWave));
            Assert.That(MobileOrientationController.CurrentMode,
                Is.EqualTo(MobileOrientationMode.LandscapeBattle));
            Assert.That(flow.GetComponentInChildren<CanvasScaler>(true).referenceResolution,
                Is.EqualTo(new Vector2(1920f, 1080f)));
            for (int wave = 0; wave < session.WaveCount; wave++)
                InvokePrivate(session, "CompleteWave");
            yield return null;
            Assert.That(flow.CurrentScreen, Is.EqualTo(AppScreen.Result));
            Assert.That(MobileOrientationController.CurrentMode,
                Is.EqualTo(MobileOrientationMode.LandscapeBattle));
            Assert.That(flow.Progress.Data.unlockedLevel, Is.EqualTo(2));
            Assert.That(flow.Progress.ProgressFor(1).stars, Is.EqualTo(3));

            InvokePrivate(flow, "OpenCampaign");
            yield return null;
            Assert.That(flow.CurrentScreen, Is.EqualTo(AppScreen.Campaign));
            Assert.That(MobileOrientationController.CurrentMode,
                Is.EqualTo(MobileOrientationMode.PortraitFrontend));
            Assert.That(flow.GetComponentInChildren<CanvasScaler>(true).referenceResolution,
                Is.EqualTo(new Vector2(1080f, 1920f)));

            Object.Destroy(flowObject);
            Object.Destroy(sessionObject);
            if (File.Exists(savePath)) File.Delete(savePath);
            if (File.Exists(savePath + ".tmp")) File.Delete(savePath + ".tmp");
            yield return null;
        }

        [UnityTest]
        public IEnumerator BrandScreensCaptureAtAcceptanceRatios()
        {
            string outputRoot = System.Environment.GetEnvironmentVariable("DC_BRAND_CAPTURE_ROOT");
            if (string.IsNullOrWhiteSpace(outputRoot))
                yield break;

            Directory.CreateDirectory(outputRoot);
            string savePath = Path.Combine(Path.GetTempPath(), $"dc-brand-capture-{System.Guid.NewGuid():N}.json");
            var catalog = Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog");
            Assert.That(catalog, Is.Not.Null);
            var sessionObject = new GameObject("Brand Capture Session");
            var session = sessionObject.AddComponent<GameSessionController>();
            var flowObject = new GameObject("Brand Capture Flow");
            var flow = flowObject.AddComponent<AppFlowController>();
            flow.Initialize(catalog, session, false, savePath);

            (string name, int width, int height)[] ratios =
            {
                ("9x16", 1080, 1920),
                ("9x19_5", 1080, 2340),
                ("3x4", 1536, 2048)
            };
            (string name, AppScreen screen)[] screens =
            {
                ("boot", AppScreen.Boot),
                ("welcome", AppScreen.Welcome),
                ("main-menu", AppScreen.MainMenu),
                ("campaign", AppScreen.Campaign)
            };

            foreach ((string ratioName, int width, int height) in ratios)
            {
                Screen.SetResolution(width, height, FullScreenMode.Windowed);
                yield return new WaitForSecondsRealtime(0.25f);
                Assert.That(Screen.width, Is.EqualTo(width));
                Assert.That(Screen.height, Is.EqualTo(height));

                foreach ((string screenName, AppScreen screen) in screens)
                {
                    InvokePrivate(flow, "ShowScreen", screen);
                    yield return new WaitForEndOfFrame();
                    string outputPath = Path.Combine(outputRoot, $"{screenName}-{ratioName}.png");
                    if (File.Exists(outputPath)) File.Delete(outputPath);
                    ScreenCapture.CaptureScreenshot(outputPath, 1);

                    float deadline = Time.realtimeSinceStartup + 5f;
                    while (!File.Exists(outputPath) && Time.realtimeSinceStartup < deadline)
                        yield return null;
                    Assert.That(File.Exists(outputPath), Is.True, $"Missing brand capture: {outputPath}");
                }
            }

            Object.Destroy(flowObject);
            Object.Destroy(sessionObject);
            if (File.Exists(savePath)) File.Delete(savePath);
            if (File.Exists(savePath + ".tmp")) File.Delete(savePath + ".tmp");
            yield return null;
        }

        [UnityTest]
        public IEnumerator ReturningPlayerSkipsWelcomeAndContinueStartsClampedLevel()
        {
            string savePath = Path.Combine(Path.GetTempPath(), $"dc-returning-ui-{System.Guid.NewGuid():N}.json");
            var seededProgress = new CampaignProgressStore(savePath);
            seededProgress.SignIn(PlayerAuthProvider.Guest, "returning-guardian", "Dönen Muhafız");
            seededProgress.SelectLevel(99);

            var catalog = Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog");
            var sessionObject = new GameObject("Returning Player Session");
            var session = sessionObject.AddComponent<GameSessionController>();
            var flowObject = new GameObject("Returning Player Flow");
            var flow = flowObject.AddComponent<AppFlowController>();

            flow.Initialize(catalog, session, false, savePath);
            float frontendDeadline = Time.realtimeSinceStartup + 5f;
            while (flow.CurrentScreen == AppScreen.Boot && Time.realtimeSinceStartup < frontendDeadline)
                yield return null;
            Assert.That(flow.CurrentScreen, Is.EqualTo(AppScreen.MainMenu));

            InvokePrivate(flow, "ContinueCampaign");
            yield return null;
            Assert.That(flow.CurrentScreen, Is.EqualTo(AppScreen.Campaign),
                "A signed-in player with no completed defence should see the map before the first battle.");
            InvokePrivate(flow, "StartSelectedLevel");
            yield return WaitForBattle(flow);
            Assert.That(flow.CurrentScreen, Is.EqualTo(AppScreen.Battle));
            Assert.That(flow.Progress.Data.lastSelectedLevel, Is.EqualTo(1));
            Assert.That(Resources.Load<Texture2D>("Presentation/Frontend/GreenwatchWelcome"), Is.Not.Null);
            Assert.That(Resources.Load<Texture2D>("Presentation/Frontend/GreenwatchWarTable"), Is.Not.Null);
            Assert.That(flow.GetComponentsInChildren<Button>(true)
                .All(button => button.GetComponent<RectTransform>().rect.width >= 44f &&
                               button.GetComponent<RectTransform>().rect.height >= 44f), Is.True,
                "Every frontend button must meet the 44 point minimum touch target.");

            Object.Destroy(flowObject);
            Object.Destroy(sessionObject);
            if (File.Exists(savePath)) File.Delete(savePath);
            if (File.Exists(savePath + ".tmp")) File.Delete(savePath + ".tmp");
            yield return null;
        }

        [UnityTest]
        public IEnumerator PausedBattleCanResumeOrReturnToMainMenu()
        {
            string savePath = Path.Combine(Path.GetTempPath(), $"dc-pause-menu-{System.Guid.NewGuid():N}.json");
            var catalog = Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog");
            Assert.That(catalog, Is.Not.Null);
            var sessionObject = new GameObject("Pause Menu Session");
            var session = sessionObject.AddComponent<GameSessionController>();
            session.ConfigurePresentation(catalog);
            var flowObject = new GameObject("Pause Menu Flow");
            var flow = flowObject.AddComponent<AppFlowController>();

            flow.Initialize(catalog, session, false, savePath);
            session.ConfigureMainMenuAction(flow.ReturnToMainMenu);
            yield return new WaitForSecondsRealtime(0.90f);
            flow.CompleteGuestFallback();
            flow.LaunchCampaignLevel(1);
            yield return WaitForBattle(flow);
            Assert.That(flow.CurrentScreen, Is.EqualTo(AppScreen.Battle));
            Assert.That(MobileOrientationController.CurrentMode,
                Is.EqualTo(MobileOrientationMode.LandscapeBattle));

            session.TogglePause();
            yield return null;
            Assert.That(session.Phase, Is.EqualTo(GamePhase.Paused));
            var hud = GetPrivate<DefenderHudController>(session, "artHud");
            hud.Refresh(session);

            Transform pauseOverlay = hud.transform.Find("PauseOverlay");
            Assert.That(pauseOverlay, Is.Not.Null);
            Assert.That(pauseOverlay.gameObject.activeSelf, Is.True);
            Assert.That(pauseOverlay.GetComponent<Image>().raycastTarget, Is.True,
                "The pause overlay must block touches from reaching the battlefield.");

            Button resumeButton = pauseOverlay.Find("Card/Resume").GetComponent<Button>();
            Button mainMenuButton = pauseOverlay.Find("Card/MainMenu").GetComponent<Button>();
            Assert.That(resumeButton.GetComponent<RectTransform>().rect.width, Is.GreaterThanOrEqualTo(44f));
            Assert.That(resumeButton.GetComponent<RectTransform>().rect.height, Is.GreaterThanOrEqualTo(44f));
            Assert.That(mainMenuButton.GetComponent<RectTransform>().rect.width, Is.GreaterThanOrEqualTo(44f));
            Assert.That(mainMenuButton.GetComponent<RectTransform>().rect.height, Is.GreaterThanOrEqualTo(44f));

            resumeButton.onClick.Invoke();
            yield return null;
            Assert.That(session.Phase, Is.EqualTo(GamePhase.Preparing));
            Assert.That(Time.timeScale, Is.EqualTo(session.CurrentGameSpeed));

            session.TogglePause();
            hud.Refresh(session);
            mainMenuButton.onClick.Invoke();
            yield return null;
            Assert.That(flow.CurrentScreen, Is.EqualTo(AppScreen.MainMenu));
            Assert.That(MobileOrientationController.CurrentMode,
                Is.EqualTo(MobileOrientationMode.PortraitFrontend));
            Assert.That(Time.timeScale, Is.Zero);

            Object.Destroy(flowObject);
            Object.Destroy(sessionObject);
            if (File.Exists(savePath)) File.Delete(savePath);
            if (File.Exists(savePath + ".tmp")) File.Delete(savePath + ".tmp");
            yield return null;
        }

        [UnityTest]
        public IEnumerator ProductionPresentationPreservesEconomyAndEmitsTypedSignals()
        {
            var catalog = Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog");
            Assert.That(catalog, Is.Not.Null);
            var gameObject = new GameObject("Art Session Test");
            var session = gameObject.AddComponent<GameSessionController>();
            session.ConfigurePresentation(catalog);
            int buildSignals = 0;
            session.PresentationSignaled += signal =>
            {
                if (signal.cue == PresentationCue.OutpostBuilt) buildSignals++;
            };
            session.Initialize();
            yield return null;

            Assert.That(session.HasProductionPresentation, Is.True);
            Assert.That(session.TryBuildOutpost(0, Data.OutpostKind.Archer), Is.True);
            Assert.That(session.Gold, Is.EqualTo(Data.CampaignCatalog.Level(10).startingGold - 75));
            Assert.That(buildSignals, Is.EqualTo(1));
            Assert.That(session.TryBuildOutpost(0, Data.OutpostKind.Mage), Is.False);
            Assert.That(session.Gold, Is.EqualTo(Data.CampaignCatalog.Level(10).startingGold - 75));
            Assert.That(buildSignals, Is.EqualTo(1));
            GameObject greenwatchEnvironment = GameObject.Find("Greenwatch Environment");
            Assert.That(greenwatchEnvironment, Is.Not.Null);
            Renderer deepBackdrop = greenwatchEnvironment.GetComponentsInChildren<Renderer>(true)
                .FirstOrDefault(renderer => renderer.name.StartsWith("DeepBackdrop", System.StringComparison.OrdinalIgnoreCase));
            Assert.That(deepBackdrop, Is.Not.Null);
            Assert.That(deepBackdrop.enabled, Is.False,
                "Continuous terrain replaces the dark platform backdrop.");
            foreach (Renderer rigidBoundary in greenwatchEnvironment.GetComponentsInChildren<Renderer>(true)
                         .Where(renderer => renderer.name.StartsWith("NorthRidge", System.StringComparison.OrdinalIgnoreCase) ||
                                            renderer.name.StartsWith("SouthRidge", System.StringComparison.OrdinalIgnoreCase)))
                Assert.That(rigidBoundary.enabled, Is.False,
                    "The battlefield must have no raised platform border.");
            Assert.That(GameObject.Find("Licensed Nature 01 trees_A_medium"), Is.Not.Null);
            Assert.That(GameObject.Find("Scenic Battlefield Dressing"), Is.Not.Null);
            AssertVisualOnlyDressing("Road Organic Earth Surface");
            Assert.That(GameObject.Find("Road Hand-Laid Stone Borders"), Is.Not.Null);
            AssertVisualOnlyDressing("Ground Low-Contrast Valley Fields");
            AssertVisualOnlyDressing("Ground Moonlit Grass Breakup");
            AssertVisualOnlyDressing("Ground Warm Entry Scuffs");
            AssertVisualOnlyDressing("Enemy Gate Warm Read Pool");
            AssertVisualOnlyDressing("Defender Keep Cool Read Pool");
            AssertVisualOnlyDressing("Continuous Battlefield Ground");
            for (int i = 1; i <= 8; i++)
            {
                GameObject slot = GameObject.Find($"Outpost Slot {i}");
                Assert.That(slot, Is.Not.Null, $"Production fixed slot {i} must be visible.");
                Assert.That(slot.GetComponent<Collider>(), Is.Not.Null,
                    $"Production fixed slot {i} must be selectable.");
            }
            Assert.That(GameObject.Find("Free Placement 9"), Is.Null);
            Assert.That(GameObject.Find("Stationary Defender Anchor 1"), Is.Null);
            GameObject defenderHud = GameObject.Find("Defender HUD");
            Assert.That(defenderHud, Is.Not.Null);
            Assert.That(defenderHud.transform.parent.name, Is.EqualTo("Game HUD"),
                "Production chrome must fill the screen instead of shrinking to the safe area.");
            Assert.That(defenderHud.transform.parent.GetComponent<CanvasScaler>().referenceResolution,
                Is.EqualTo(new Vector2(1920f, 1080f)),
                "Battlefield HUD stays landscape while frontend screens use the portrait reference resolution.");
            Assert.That(defenderHud.GetComponent<DefenderHudResponsiveLayout>(), Is.Not.Null);
            var topRail = defenderHud.transform.Find("TopBar").GetComponent<UnityEngine.UI.Image>();
            var bottomRail = defenderHud.transform.Find("BottomBar").GetComponent<UnityEngine.UI.Image>();
            Assert.That(topRail.enabled, Is.False);
            Assert.That(bottomRail.enabled, Is.False);
            Assert.That(topRail.sprite, Is.Null, "The status controls must leave the terrain visible between cards.");
            Assert.That(bottomRail.sprite, Is.Null, "The command deck should float over the battlefield.");
            Assert.That(defenderHud.transform.Find("TopBar/SafeContent/GoldChrome"), Is.Not.Null);
            Assert.That(defenderHud.transform.Find("TopBar/SafeContent/LivesChrome"), Is.Not.Null);
            Assert.That(defenderHud.transform.Find("TopBar/SafeContent/WaveChrome"), Is.Not.Null);
            var phaseHint = defenderHud.transform.Find("TopBar/SafeContent/PhaseChrome/PhaseHint") as RectTransform;
            Assert.That(phaseHint, Is.Not.Null);
            Assert.That(phaseHint.offsetMin.y, Is.GreaterThanOrEqualTo(34f),
                "The phase hint must occupy its own upper row instead of overlapping the phase label.");
            Assert.That(defenderHud.transform.Find("BottomBar/SafeContent/BuildDeck").GetComponent<UnityEngine.UI.Image>().enabled, Is.False);
            Assert.That(defenderHud.transform.Find("BottomBar/SafeContent/HeroDeck").GetComponent<UnityEngine.UI.Image>().enabled, Is.False);
            Assert.That(defenderHud.transform.Find("BottomBar/SafeContent/CommandDeck").GetComponent<UnityEngine.UI.Image>().enabled, Is.False);
            Assert.That(defenderHud.transform.Find("MessageChrome"), Is.Not.Null);
            Assert.That(Camera.main.GetComponent<AdaptiveBattlefieldCamera>(), Is.Not.Null);
            Assert.That(session.Hero.GetComponent<StylizedActorView>(), Is.Not.Null);
            Assert.That(session.Hero.GetComponentInChildren<StylizedGroundingAura>(), Is.Not.Null);
            Assert.That(session.Hero.GetComponentInChildren<Animator>(), Is.Not.Null);
            int heroMaterials = session.Hero.GetComponentsInChildren<Renderer>()
                .SelectMany(renderer => renderer.sharedMaterials).Where(material => material != null).Distinct().Count();
            Assert.That(heroMaterials, Is.LessThanOrEqualTo(2));
            LogAssert.NoUnexpectedReceived();
            Object.Destroy(gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator InterruptedCameraShakeAlwaysReturnsToItsOriginalPosition()
        {
            var cameraObject = new GameObject("Camera Shake Regression");
            var camera = cameraObject.AddComponent<Camera>();
            Vector3 restPosition = new(4f, 8f, -12f);
            cameraObject.transform.localPosition = restPosition;
            var feedbackObject = new GameObject("Camera Feedback Regression");
            var feedback = feedbackObject.AddComponent<DefenderPresentationFeedbackController>();
            feedback.Initialize(camera);
            var signal = new PresentationSignal(PresentationCue.BossPhase, Vector3.zero);

            for (int index = 0; index < 40; index++)
            {
                feedback.Play(signal);
                yield return null;
            }
            yield return new WaitForSecondsRealtime(0.35f);

            Assert.That(cameraObject.transform.localPosition.x, Is.EqualTo(restPosition.x).Within(0.001f));
            Assert.That(cameraObject.transform.localPosition.y, Is.EqualTo(restPosition.y).Within(0.001f));
            Assert.That(cameraObject.transform.localPosition.z, Is.EqualTo(restPosition.z).Within(0.001f));
            Object.Destroy(feedbackObject);
            Object.Destroy(cameraObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CampaignSceneryKeepsSlotsClearAndBranchesMatchPrimaryRoad()
        {
            var catalog = Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog");
            Assert.That(catalog, Is.Not.Null);
            foreach (int levelNumber in new[] { 1, 10, 14, 24, 43 })
            {
                var root = new GameObject($"Map Visual Regression {levelNumber}");
                var session = root.AddComponent<GameSessionController>();
                session.ConfigurePresentation(catalog);
                session.SetCampaignLevel(levelNumber);
                session.Initialize();
                yield return null;
                Data.RuntimeCampaignLevel level = Data.CampaignLevelFactory.Create(levelNumber);
                Transform world = root.transform.Find("Greenwatch Valley");
                Assert.That(world, Is.Not.Null);
                Vector3[] slots = Enumerable.Range(1, 8).Select(index =>
                    GameObject.Find($"Outpost Slot {index}").transform.position).ToArray();
                foreach (Renderer renderer in world.GetComponentsInChildren<Renderer>(true))
                    if (renderer.enabled && renderer.gameObject.activeInHierarchy &&
                        DefenderBattlefieldDressing.IsDecoration(renderer.transform, world))
                        Assert.That(DefenderBattlefieldDressing.DecorationOverlapsGameplay(renderer.bounds, slots, level.routes),
                            Is.False, $"Level {levelNumber}: {renderer.name} obscures a slot or route.");

                Transform dressing = world.Find("Scenic Battlefield Dressing");
                Renderer primary = dressing.Find("Road Organic Earth Surface").GetComponent<Renderer>();
                MeshFilter[] branches = dressing.GetComponentsInChildren<MeshFilter>()
                    .Where(filter => filter.name == "Road Surface").ToArray();
                Assert.That(branches.Length, Is.EqualTo(level.routes.Length - 1));
                Renderer primaryEdge = dressing.Find("Road Hand-Laid Stone Borders").GetComponent<Renderer>();
                Assert.That(dressing.GetComponentsInChildren<Renderer>().Count(renderer => renderer.name == "Road Edge"),
                    Is.EqualTo(branches.Length));
                foreach (Renderer edge in dressing.GetComponentsInChildren<Renderer>().Where(renderer => renderer.name == "Road Edge"))
                    Assert.That(edge.sharedMaterial, Is.SameAs(primaryEdge.sharedMaterial));
                for (int index = 0; index < branches.Length; index++)
                {
                    MeshFilter branch = branches[index];
                    Assert.That(branch.GetComponent<Renderer>().sharedMaterial, Is.SameAs(primary.sharedMaterial));
                    Assert.That(branch.GetComponent<Collider>(), Is.Null);
                    Vector3[] route = level.routes[index + 1];
                    int owned = Data.CampaignLevelFactory.OwnedBranchSegments(route, level.path);
                    Vector3[] prefix = route.Take(owned + 1).ToArray();
                    Assert.That(branch.sharedMesh.normals.All(normal => Vector3.Dot(normal, Vector3.up) > 0.999f),
                        Is.True, "Branch pavement must receive the same lighting as the primary road.");
                    foreach (Vector3 vertex in branch.sharedMesh.vertices)
                    {
                        Assert.That(vertex.y, Is.InRange(0.081f, 0.085f), "Branch pavement must share primary surface height.");
                        Assert.That(Data.CampaignLevelFactory.DistanceToRoute(vertex, prefix), Is.LessThan(1.5f),
                            "Branch dressing must not redraw the shared primary tail.");
                    }
                }
                Object.Destroy(root);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator ProductionPresentationAllowsBuildUpgradeSellOnEveryFixedSlot()
        {
            var catalog = Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog");
            Assert.That(catalog, Is.Not.Null);
            var gameObject = new GameObject("Slot Regression Session");
            var session = gameObject.AddComponent<GameSessionController>();
            session.ConfigurePresentation(catalog);
            session.Initialize();
            GrantGold(session, 2200);
            yield return null;

            Data.OutpostKind[] kinds =
            {
                Data.OutpostKind.Archer,
                Data.OutpostKind.Barracks,
                Data.OutpostKind.Mage
            };

            for (int slotIndex = 0; slotIndex < 8; slotIndex++)
            {
                GameObject slot = GameObject.Find($"Outpost Slot {slotIndex + 1}");
                Assert.That(slot, Is.Not.Null, $"Production fixed slot {slotIndex + 1} must exist.");
                Assert.That(slot.GetComponent<Collider>(), Is.Not.Null,
                    $"Production fixed slot {slotIndex + 1} must keep a selectable collider.");

                Data.OutpostKind kind = kinds[slotIndex % kinds.Length];
                Assert.That(session.TryBuildOutpost(slotIndex, kind), Is.True,
                    $"Slot {slotIndex + 1} should accept a {kind} build.");
                Assert.That(session.TryUpgradeOutpost(slotIndex), Is.True,
                    $"Slot {slotIndex + 1} should allow its first upgrade.");
                Assert.That(session.TrySellOutpost(slotIndex), Is.True,
                    $"Slot {slotIndex + 1} should allow selling after upgrade.");
                Assert.That(session.TryBuildOutpost(slotIndex, Data.OutpostKind.Archer), Is.True,
                    $"Slot {slotIndex + 1} should be reusable after sell.");
                Assert.That(session.TrySellOutpost(slotIndex), Is.True,
                    $"Slot {slotIndex + 1} should sell cleanly after the reuse build.");

                Assert.That(session.SlotCount, Is.EqualTo(8));
                AssertVisualOnlyDressing("Road Organic Earth Surface");
                Assert.That(GameObject.Find("Road Hand-Laid Stone Borders"), Is.Not.Null);
                AssertVisualOnlyDressing("Ground Low-Contrast Valley Fields");
                AssertVisualOnlyDressing("Enemy Gate Warm Read Pool");
                AssertVisualOnlyDressing("Defender Keep Cool Read Pool");
                AssertVisualOnlyDressing("Continuous Battlefield Ground");
                yield return null;
            }

            LogAssert.NoUnexpectedReceived();
            Object.Destroy(gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ProductionPresentationShowsVictoryAndDefeatResults()
        {
            var catalog = Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog");
            Assert.That(catalog, Is.Not.Null);

            var victoryObject = new GameObject("Result Regression Victory Session");
            var victorySession = victoryObject.AddComponent<GameSessionController>();
            victorySession.ConfigurePresentation(catalog);
            int victorySignals = 0;
            victorySession.PresentationSignaled += signal =>
            {
                if (signal.cue == PresentationCue.Victory) victorySignals++;
            };
            victorySession.Initialize();
            yield return null;

            for (int i = 0; i < victorySession.WaveCount; i++)
                InvokePrivate(victorySession, "CompleteWave");
            yield return null;

            Assert.That(victorySession.Phase, Is.EqualTo(GamePhase.Victory));
            Assert.That(victorySignals, Is.EqualTo(1));
            AssertResultPanelExists();
            Time.timeScale = 1f;
            Object.Destroy(victoryObject);
            yield return null;

            var defeatObject = new GameObject("Result Regression Defeat Session");
            var defeatSession = defeatObject.AddComponent<GameSessionController>();
            defeatSession.ConfigurePresentation(catalog);
            int defeatSignals = 0;
            defeatSession.PresentationSignaled += signal =>
            {
                if (signal.cue == PresentationCue.Defeat) defeatSignals++;
            };
            defeatSession.Initialize();
            yield return null;

            InvokePrivate(defeatSession, "LoseLives", defeatSession.Lives);
            yield return null;

            Assert.That(defeatSession.Phase, Is.EqualTo(GamePhase.Defeat));
            Assert.That(defeatSignals, Is.EqualTo(1));
            AssertResultPanelExists();
            Time.timeScale = 1f;
            LogAssert.NoUnexpectedReceived();
            Object.Destroy(defeatObject);
            yield return null;
        }

        [Test]
        public void SceneLookRespectsQualityLightingProfile()
        {
            int originalQuality = QualitySettings.GetQualityLevel();
            int low = FindQualityIndexByName("low");
            int medium = FindQualityIndexByName("medium");
            int high = FindQualityIndexByName("high");

            if (low < 0 || medium < 0 || high < 0)
            {
                QualitySettings.SetQualityLevel(originalQuality, applyExpensiveChanges: true);
                Assert.Inconclusive(
                    $"Quality profile names do not include required tiers. Found: {string.Join(", ", QualitySettings.names ?? new string[0])}");
            }

            var createdObjects = new System.Collections.Generic.List<Object>();
            var root = new GameObject("Lighting Profile Test Scene");
            var camera = root.AddComponent<Camera>();
            createdObjects.Add(root);
            camera.transform.position = Vector3.up * 5f;
            camera.transform.rotation = Quaternion.Euler(45f, -45f, 0f);
            var cameraObject = camera.gameObject;
            cameraObject.tag = "MainCamera";

            try
            {
                QualitySettings.SetQualityLevel(low, applyExpensiveChanges: true);
                DefenderSceneLook.Apply(camera);
                Assert.That(GameObject.Find("Greenwatch Moon Key"), Is.Not.Null);
                Light keyLight = GameObject.Find("Greenwatch Moon Key").GetComponent<Light>();
                Assert.That(keyLight, Is.Not.Null);
                Assert.That(keyLight.type, Is.EqualTo(LightType.Directional));
                Assert.That(keyLight.shadows, Is.EqualTo(LightShadows.None));

                Light moonFill = GameObject.Find("Moon Fill").GetComponent<Light>();
                Light gateFill = GameObject.Find("Gate Ember Fill").GetComponent<Light>();
                Assert.That(moonFill, Is.Not.Null);
                Assert.That(gateFill, Is.Not.Null);
                Assert.That(moonFill.shadows, Is.EqualTo(LightShadows.None));
                Assert.That(gateFill.shadows, Is.EqualTo(LightShadows.None));
                Assert.That(moonFill.intensity, Is.EqualTo(0.96f * 0.65f).Within(0.0001f));
                Assert.That(moonFill.range, Is.EqualTo(24f * 0.6f).Within(0.0001f));

                QualitySettings.SetQualityLevel(medium, applyExpensiveChanges: true);
                DefenderSceneLook.Apply(camera);
                keyLight = GameObject.Find("Greenwatch Moon Key").GetComponent<Light>();
                Assert.That(keyLight.shadows, Is.EqualTo(LightShadows.None));
                moonFill = GameObject.Find("Moon Fill").GetComponent<Light>();
                Assert.That(moonFill, Is.Not.Null);
                Assert.That(moonFill.intensity, Is.EqualTo(0.96f).Within(0.0001f));
                Assert.That(moonFill.range, Is.EqualTo(24f).Within(0.0001f));

                QualitySettings.SetQualityLevel(high, applyExpensiveChanges: true);
                DefenderSceneLook.Apply(camera);
                keyLight = GameObject.Find("Greenwatch Moon Key").GetComponent<Light>();
                Assert.That(keyLight.shadows, Is.EqualTo(LightShadows.Soft));
                moonFill = GameObject.Find("Moon Fill").GetComponent<Light>();
                Assert.That(moonFill.intensity, Is.EqualTo(0.96f).Within(0.0001f));
                Assert.That(moonFill.range, Is.EqualTo(24f).Within(0.0001f));
            }
            finally
            {
                QualitySettings.SetQualityLevel(originalQuality, applyExpensiveChanges: true);
                foreach (var item in createdObjects)
                {
                    if (item != null) Object.DestroyImmediate(item);
                }

                // cleanup all lights generated by Apply and tests while leaving host scene untouched
                foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                {
                    if (light == null || light.name == "Greenwatch Moon Key" ||
                        light.name == "Moon Fill" || light.name == "Gate Ember Fill")
                    {
                        if (light != null && light.gameObject != null)
                            Object.DestroyImmediate(light.gameObject);
                    }
                }
            }
        }

        private static int FindQualityIndexByName(string qualityName)
        {
            string target = qualityName?.ToLowerInvariant() ?? string.Empty;
            string[] names = QualitySettings.names ?? System.Array.Empty<string>();
            for (int i = 0; i < names.Length; i++)
            {
                if ((names[i] ?? string.Empty).ToLowerInvariant() == target)
                    return i;
            }
            for (int i = 0; i < names.Length; i++)
            {
                if ((names[i] ?? string.Empty).ToLowerInvariant().Contains(target))
                    return i;
            }
            return -1;
        }

        private static void GrantGold(GameSessionController session, int amount)
        {
            var method = typeof(GameSessionController).GetMethod("EarnGold",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(session, new object[] { amount });
        }

        private static void InvokePrivate(object target, string methodName, params object[] args)
        {
            var method = target.GetType().GetMethod(methodName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"{methodName} must exist for the focused gameplay regression.");
            method.Invoke(target, args);
        }

        private static T GetPrivate<T>(object target, string fieldName) where T : class
        {
            var field = target.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"{fieldName} must exist for the focused gameplay regression.");
            var value = field.GetValue(target) as T;
            Assert.That(value, Is.Not.Null, $"{fieldName} must be initialized for the focused gameplay regression.");
            return value;
        }

        private static void AssertResultPanelExists()
        {
            GameObject defenderHud = GameObject.Find("Defender HUD");
            Assert.That(defenderHud, Is.Not.Null);
            Transform panel = defenderHud.transform.Find("ResultPanel");
            Assert.That(panel, Is.Not.Null);

            RectTransform hint = panel.Find("Card/Hint") as RectTransform;
            RectTransform restart = panel.Find("Restart") as RectTransform;
            Assert.That(hint, Is.Not.Null);
            Assert.That(restart, Is.Not.Null);

            Vector3[] hintCorners = new Vector3[4];
            Vector3[] restartCorners = new Vector3[4];
            hint.GetWorldCorners(hintCorners);
            restart.GetWorldCorners(restartCorners);
            Assert.That(hintCorners[0].y, Is.GreaterThanOrEqualTo(restartCorners[1].y - 0.01f),
                "Result description must remain above the retry action without overlapping it.");
        }

        private static void AssertVisualOnlyDressing(string objectName)
        {
            GameObject item = GameObject.Find(objectName);
            Assert.That(item, Is.Not.Null, $"{objectName} must be present in production dressing.");
            Assert.That(item.GetComponent<MeshRenderer>(), Is.Not.Null, $"{objectName} must render as scenic geometry.");
            Assert.That(item.GetComponent<Collider>(), Is.Null, $"{objectName} must not affect pathing or touch.");
        }

        [UnityTest]
        public IEnumerator AudioControllerCyclesReviewMusicAndCueContractsWithinVoiceBudget()
        {
            var catalog = Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog");
            Assert.That(catalog, Is.Not.Null);
            var audioObject = new GameObject("Audio Contract Test");
            var audio = audioObject.AddComponent<DefenderAudioController>();
            audio.Initialize(catalog);
            yield return null;
            Assert.That(audio.Ambient, Is.Not.Null);

            foreach (PresentationAudioReviewLevel level in
                     (PresentationAudioReviewLevel[])System.Enum.GetValues(typeof(PresentationAudioReviewLevel)))
            {
                audio.SetReviewLevel(level);
                Assert.That(audio.ReviewLevel, Is.EqualTo(level));
            }

            foreach (MusicState state in (MusicState[])System.Enum.GetValues(typeof(MusicState)))
            {
                audio.SetMusic(state);
                Assert.That(audio.CurrentState, Is.EqualTo(state));
                yield return null;
            }

            audio.SetReviewLevel(PresentationAudioReviewLevel.Normal);
            for (int i = 0; i < 24; i++)
                audio.Play(new PresentationSignal(PresentationCue.EnemyHit, Vector3.zero));
            audio.Play(new PresentationSignal(PresentationCue.HeroAbility, Vector3.zero,
                actor: PresentationActorKind.Knight));
            audio.Play(new PresentationSignal(PresentationCue.BossPhase, Vector3.zero,
                actor: PresentationActorKind.Boss));
            audio.Play(new PresentationSignal(PresentationCue.BossAttack, Vector3.zero,
                actor: PresentationActorKind.Boss));
            yield return new WaitForSecondsRealtime(0.35f);

            Assert.That(audio.ActiveVoiceCount, Is.LessThanOrEqualTo(10));
            audio.SetPaused(true);
            yield return null;
            audio.SetPaused(false);
            yield return null;

            Object.Destroy(audioObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ProductionHudButtonsAreInteractableAndClickable()
        {
            // VIS-613: HUD input regression
            var catalog = Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog");
            Assert.That(catalog, Is.Not.Null);
            var gameObject = new GameObject("HUD Input Regression Session");
            var session = gameObject.AddComponent<GameSessionController>();
            session.ConfigurePresentation(catalog);
            session.Initialize();
            yield return null;

            var hud = GetPrivate<DefenderHudController>(session, "artHud");
            Assert.That(hud, Is.Not.Null);

            hud.Refresh(session);
            yield return null;

            Assert.That(hud.GetButtonInteractable("BottomBar/Archer"), Is.False);
            Assert.That(hud.GetButtonInteractable("BottomBar/Barracks"), Is.False);
            Assert.That(hud.GetButtonInteractable("BottomBar/Mage"), Is.False);
            Assert.That(hud.GetButtonInteractable("BottomBar/Ability"), Is.True);
            Assert.That(hud.GetButtonInteractable("TopBar/Pause"), Is.True);
            Assert.That(hud.GetButtonInteractable("BottomBar/Cancel"), Is.False);
            Assert.That(hud.GetButtonInteractable("BottomBar/NextWave"), Is.True);

            SelectSlot(session, 0);
            InvokePrivate(session, "RefreshUi");
            hud.SetBuildAvailability(true, session.Gold);

            Assert.That(hud.GetButtonInteractable("BottomBar/Archer"), Is.True,
                $"Archer should be available after selecting an empty slot. Phase={session.Phase}, Gold={session.Gold}.");
            Assert.That(hud.GetButtonInteractable("BottomBar/Barracks"), Is.True);
            Assert.That(hud.GetButtonInteractable("BottomBar/Mage"), Is.True);
            Assert.That(hud.GetButtonInteractable("BottomBar/Ability"), Is.True);
            Assert.That(hud.GetButtonInteractable("TopBar/Pause"), Is.True);
            Assert.That(hud.GetButtonInteractable("BottomBar/Cancel"), Is.True);
            Assert.That(hud.GetButtonInteractable("BottomBar/NextWave"), Is.True);
            Transform contextualChoices = hud.transform.Find("SelectedPanel/BuildChoices");
            Assert.That(contextualChoices, Is.Not.Null);
            Assert.That(contextualChoices.gameObject.activeSelf, Is.True);
            Assert.That(contextualChoices.Find("Archer"), Is.Not.Null);
            Assert.That(contextualChoices.Find("Barracks"), Is.Not.Null);
            Assert.That(contextualChoices.Find("Mage"), Is.Not.Null);

            int goldBeforePreview = session.Gold;
            InvokePrivate(session, "TryBuildSelected", Data.OutpostKind.Archer);
            Assert.That(session.Gold, Is.EqualTo(goldBeforePreview), "Previewing must not spend gold.");
            Assert.That(session.PendingBuildKind, Is.EqualTo(Data.OutpostKind.Archer));
            Assert.That(session.SelectionPreviewVisible, Is.True);
            InvokePrivate(session, "TryBuildSelected", Data.OutpostKind.Archer);
            Assert.That(session.Gold, Is.LessThan(goldBeforePreview));
            Assert.That(session.PendingBuildKind, Is.Null);
            yield return null;
            hud.Refresh(session);
            yield return null;

            Assert.That(hud.GetButtonInteractable("SelectedPanel/Upgrade"), Is.True);
            Assert.That(hud.GetButtonInteractable("SelectedPanel/Sell"), Is.True);
            Assert.That(contextualChoices.gameObject.activeSelf, Is.False);

            SelectSlot(session, 1);
            int goldBeforeBarracksPreview = session.Gold;
            InvokePrivate(session, "TryBuildSelected", Data.OutpostKind.Barracks);
            Assert.That(session.Gold, Is.EqualTo(goldBeforeBarracksPreview));
            Assert.That(session.RallyPreviewVisible, Is.True);

            session.TryBuildOutpost(1, Data.OutpostKind.Archer);
            session.TryBuildOutpost(2, Data.OutpostKind.Archer);
            session.TryBuildOutpost(3, Data.OutpostKind.Archer);
            session.TryBuildOutpost(4, Data.OutpostKind.Archer);
            session.TryBuildOutpost(5, Data.OutpostKind.Archer);
            session.TryBuildOutpost(6, Data.OutpostKind.Archer);
            session.TryBuildOutpost(7, Data.OutpostKind.Archer);
            yield return null;
            hud.Refresh(session);
            yield return null;

            Assert.That(hud.GetButtonInteractable("BottomBar/Archer"), Is.False);
            Assert.That(hud.GetButtonInteractable("BottomBar/Barracks"), Is.False);
            Assert.That(hud.GetButtonInteractable("BottomBar/Mage"), Is.False);

            session.TogglePause();
            yield return null;
            hud.Refresh(session);
            yield return null;

            Assert.That(hud.GetButtonInteractable("BottomBar/Ability"), Is.False);
            Assert.That(hud.GetButtonInteractable("TopBar/Pause"), Is.True);
            Assert.That(hud.GetButtonInteractable("BottomBar/Cancel"), Is.True);

            LogAssert.NoUnexpectedReceived();
            Object.Destroy(gameObject);
            yield return null;
        }

        private static void SelectSlot(GameSessionController session, int index)
        {
            var slot = session.GetComponentsInChildren<GreyboxOutpostSlot>(true)
                .FirstOrDefault(candidate => candidate.Index == index);
            Assert.That(slot, Is.Not.Null);
            var method = typeof(GameSessionController).GetMethod("SelectSlot",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(session, new object[] { slot });
        }
    }
}
