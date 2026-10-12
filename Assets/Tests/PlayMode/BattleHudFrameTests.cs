using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using AllinDefense.Core;
using AllinDefense.Data;
using AllinDefense.Presentation;
using AllinDefense.UI;
using AllinDefense.UI.Kit;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace AllinDefense.Tests
{
    public sealed class BattleHudFrameTests
    {
        private GameSessionController session;
        private DefenderHudController hud;
        private bool reducedMotion;
        private float music, effects;
        private string checkpointPath;
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static object Call(object target, string method, params object[] args) => target.GetType().GetMethods(Private).First(m => m.Name == method && m.GetParameters().Length == args.Length).Invoke(target, args);
        private void Create(int level = 1)
        {
            session = new GameObject("Frame test").AddComponent<GameSessionController>();
            session.ConfigurePresentation(Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog"));
            session.SetCampaignLevel(level); session.Initialize();
            hud = session.GetComponentInChildren<DefenderHudController>();
            Time.timeScale = 0;
        }
        [SetUp] public void Setup()
        { reducedMotion = FrontendPreferences.ReducedMotion; music = FrontendPreferences.MusicVolume; effects = FrontendPreferences.SfxVolume; }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (session != null) Object.Destroy(session.gameObject);
            yield return null;
            FrontendPreferences.ReducedMotion = reducedMotion;
            FrontendPreferences.MusicVolume = music; FrontendPreferences.SfxVolume = effects;
            Time.timeScale = 1;
            if (checkpointPath != null && System.IO.File.Exists(checkpointPath)) System.IO.File.Delete(checkpointPath);
#if UNITY_EDITOR
            typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView");
            var review = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("AllinDefense.Editor.FrontendExperienceReview")).FirstOrDefault(t => t != null);
            review?.GetMethod("RestoreSize").Invoke(null, null);
#endif
        }
        [UnityTest] public IEnumerator ActualBossWaveControlsPhaseAndLiveBossOwnsNotchedHealthBar()
        {
            Create(1); hud.Refresh(session);
            Assert.That(UiQuery.FindById<TMP_Text>(hud, "PhaseChip.Label").text, Is.EqualTo(FrontendCopy.HudPreparing.Value));
            typeof(GameSessionController).GetField("currentWaveIndex", Private).SetValue(session, session.WaveCount - 1);
            Call(session, "SetPhase", GamePhase.Wave); hud.Refresh(session);
            Assert.That(session.NextWaveHasBoss, Is.False);
            Assert.That(UiQuery.FindById<TMP_Text>(hud, "PhaseChip.Label").text, Does.Not.Contain("BOSS"));
            Object.Destroy(session.gameObject); yield return null;
            Create(10);
            typeof(GameSessionController).GetField("currentWaveIndex", Private).SetValue(session, session.WaveCount - 1);
            hud.Refresh(session);
            Assert.That(session.NextWaveHasBoss, Is.True);
            Assert.That(UiQuery.FindById<TMP_Text>(hud, "PhaseChip.Label").text, Is.EqualTo(FrontendCopy.BossPhase.Value));
            var bar = UiQuery.FindById(hud, "BossBar"); Assert.That(bar.gameObject.activeSelf, Is.False);
            Call(session, "SpawnEnemy", EnemyArchetype.Boss, 0);
            FrontendPreferences.ReducedMotion = true; hud.Refresh(session);
            Assert.That(bar.gameObject.activeSelf, Is.True); Assert.That(bar.localScale, Is.EqualTo(Vector3.one));
            Assert.That(UiQuery.FindById<TMP_Text>(hud, "BossName").text, Is.EqualTo(FirstRegionCampaign.EnemyName(EnemyArchetype.Boss)));
            foreach (var notch in new[] { ("Notch33", .33f), ("Notch66", .66f) })
                Assert.That(((RectTransform)UiQuery.FindById(hud, notch.Item1)).anchorMin.x, Is.EqualTo(notch.Item2));
            var boss = session.Enemies.First(e => e.Archetype == EnemyArchetype.Boss);
            typeof(GreyboxEnemy).GetField("health", Private).SetValue(boss, boss.MaxHealth / 2);
            hud.Refresh(session);
            Assert.That(UiQuery.FindById<Image>(hud, "BossFill").fillAmount, Is.EqualTo(.5f).Within(.01));
            Object.Destroy(boss.gameObject); yield return null; hud.Refresh(session);
            Assert.That(bar.gameObject.activeSelf, Is.False);
        }
        [UnityTest] public IEnumerator AbilityCooldownReturnsToReadyAndGoldDeltaExpiresWhilePaused()
        {
            Create(); hud.Refresh(session);
            var radial = hud.GetComponentInChildren<UiRadialCooldown>();
            Assert.That(radial.IsReady, Is.True);
            typeof(GreyboxHero).GetProperty("AbilityRemaining").SetValue(session.Hero, session.Hero.AbilityCooldownDuration); hud.Refresh(session);
            Assert.That(radial.IsReady, Is.False); Assert.That(radial.Fill, Is.GreaterThan(.9f));
            typeof(GreyboxHero).GetProperty("AbilityRemaining").SetValue(session.Hero, 0f);
            hud.Refresh(session); Assert.That(radial.IsReady, Is.True);
            var gold = UiQuery.FindById<UiStatPill>(hud, "Gold");
            Call(session, "EarnGold", 12); hud.Refresh(session);
            Assert.That(gold.DeltaText, Is.EqualTo("+12"));
            hud.Refresh(session);
            Assert.That(gold.DeltaText, Is.EqualTo("+12"));
            yield return new WaitForSecondsRealtime(.45f);
            Assert.That(gold.DeltaText, Is.Empty);
        }
        [UnityTest] public IEnumerator PauseActionsUseNavigationCallbacksConfirmMapAndPersistAudio()
        {
            Create(7); int restart = 0, map = 0;
            session.ConfigureBattleNavigation(() => restart = session.CampaignLevelNumber, () => map++);
            session.TogglePause(); hud.Refresh(session); yield return null;
            var overlay = UiQuery.FindById<UiOverlay>(hud, "PauseOverlay");
            Assert.That(overlay.IsOpen, Is.True); Assert.That(overlay.DismissOnScrim, Is.False);
            overlay.OnPointerClick(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current) { position = new Vector2(-100, -100) });
            Assert.That(overlay.IsOpen, Is.True);
            Assert.That(session.TryBuildOutpost(0, OutpostKind.Archer), Is.False);
            Assert.That(overlay.Body.GetComponentsInChildren<Button>().Length, Is.EqualTo(5));
            UiQuery.FindById<UiSwitch>(hud, "MusicSwitch").SetValue(false);
            UiQuery.FindById<UiSwitch>(hud, "EffectsSwitch").SetValue(false);
            Assert.That(FrontendPreferences.MusicVolume, Is.Zero); Assert.That(FrontendPreferences.SfxVolume, Is.Zero);
            UiQuery.FindById<Button>(hud, "PauseRestart").onClick.Invoke();
            Assert.That(restart, Is.EqualTo(7));
            UiQuery.FindById<Button>(hud, "MainMenu").onClick.Invoke(); Assert.That(map, Is.Zero);
            Assert.That(hud.MapConfirmationOpen, Is.True);
            Canvas.ForceUpdateCanvases();
            var confirmationTitle = UiQuery.FindById<TMP_Text>(hud, "MapConfirmation.Title");
            confirmationTitle.ForceMeshUpdate();
            Assert.That(confirmationTitle.isTextOverflowing, Is.False);
            FrontendPreferences.ReducedMotion = true;
            UiQuery.FindById<Button>(hud, "KeepPlaying").onClick.Invoke(); yield return null;
            Assert.That(hud.MapConfirmationOpen, Is.False); Assert.That(map, Is.Zero);
            UiQuery.FindById<Button>(hud, "MainMenu").onClick.Invoke();
            UiQuery.FindById<Button>(hud, "ConfirmMap").onClick.Invoke(); Assert.That(map, Is.EqualTo(1));
            UiQuery.FindById<Button>(hud, "Resume").onClick.Invoke();
            Assert.That(session.Phase, Is.EqualTo(GamePhase.Preparing));
        }
        [UnityTest] public IEnumerator PauseRestartClearsOnlyTheBattleCheckpointAndReloadsTheSameLevel()
        {
            checkpointPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "hud-restart-" + Guid.NewGuid().ToString("N") + ".json");
            var store = new CampaignProgressStore(checkpointPath);
            store.Data.unlockedLevel = 7;
            session = new GameObject("Restart frame test").AddComponent<GameSessionController>();
            session.ConfigureCampaignStore(store);
            session.ConfigurePresentation(Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog"));
            session.SetCampaignLevel(7); session.Initialize();
            hud = session.GetComponentInChildren<DefenderHudController>();
            Call(session, "SaveWaveCheckpoint");
            Assert.That(store.Data.checkpoint, Is.Not.Null);
            int reloadedLevel = 0;
            session.ConfigureBattleNavigation(() => reloadedLevel = session.CampaignLevelNumber, () => { });
            session.TogglePause(); hud.Refresh(session);
            UiQuery.FindById<Button>(hud, "PauseRestart").onClick.Invoke();
            Assert.That(reloadedLevel, Is.EqualTo(7));
            Assert.That(store.Data.checkpoint, Is.Null);
            Assert.That(store.Data.unlockedLevel, Is.EqualTo(7));
            Assert.That(new CampaignProgressStore(checkpointPath).Data.checkpoint, Is.Null);
            Object.Destroy(session.gameObject); yield return null;
            session = new GameObject("Reloaded frame test").AddComponent<GameSessionController>();
            session.ConfigureCampaignStore(store); session.SetCampaignLevel(reloadedLevel); session.Initialize();
            Assert.That(session.CheckpointRestored, Is.False);
            Assert.That(session.CurrentWaveIndex, Is.Zero);
            Assert.That(session.CampaignLevelNumber, Is.EqualTo(7));
        }
        [UnityTest] public IEnumerator LargeGoldDeltaKeepsBothValuesVisible()
        {
            Create(); yield return null; yield return null;
            FrontendPreferences.ReducedMotion = true;
            Call(session, "EarnGold", 100000); hud.Refresh(session);
            Canvas.ForceUpdateCanvases();
            var gold = UiQuery.FindById<UiStatPill>(hud, "Gold");
            foreach (var text in gold.GetComponentsInChildren<TMP_Text>())
            {
                text.ForceMeshUpdate();
                Assert.That(text.isTextOverflowing, Is.False, text.name);
                Assert.That(text.textInfo.meshInfo.Sum(mesh => mesh.vertexCount), Is.GreaterThan(0), text.name);
            }
            yield return null;
        }
        [UnityTest] public IEnumerator WaveCtaReflectsActualRewardAndWaveSegmentCount()
        {
            Create(); hud.Refresh(session);
            Assert.That(UiQuery.FindById<Button>(hud, "NextWave").GetComponentInChildren<TMP_Text>().text, Is.EqualTo(FrontendCopy.HudFirstWave.Value));
            typeof(GameSessionController).GetField("currentWaveIndex", Private).SetValue(session, 1);
            Call(session, "SetPhase", GamePhase.Intermission); hud.Refresh(session);
            Assert.That(session.EarlyWaveReward, Is.Zero);
            Assert.That(UiQuery.FindById<Button>(hud, "NextWave").GetComponentInChildren<TMP_Text>().text, Is.EqualTo(FrontendCopy.HudNextWave.Value));
            Assert.That(DefenderHudController.WaveActionLabel(false, 15), Is.EqualTo(FrontendCopy.EarlyWaveReward.Format(15)));
            yield return null;
            var progress = hud.GetComponentInChildren<UiProgressSegments>();
            Assert.That(progress.transform.childCount, Is.EqualTo(session.WaveCount));
        }
        private static void AssertFootprintClear(Camera camera, Rect[] hudRects, params Vector3[] corners)
        {
            Vector2 min = new(float.MaxValue, float.MaxValue), max = new(float.MinValue, float.MinValue);
            foreach (var corner in corners)
            {
                Vector3 point = camera.WorldToScreenPoint(corner);
                Assert.That(point.z, Is.GreaterThan(0));
                min = Vector2.Min(min, point); max = Vector2.Max(max, point);
            }
            var footprint = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            Assert.That(min.x, Is.GreaterThanOrEqualTo(0)); Assert.That(min.y, Is.GreaterThanOrEqualTo(0));
            Assert.That(max.x, Is.LessThanOrEqualTo(Screen.width)); Assert.That(max.y, Is.LessThanOrEqualTo(Screen.height));
            Assert.That(hudRects.Any(rect => rect.Overlaps(footprint)), Is.False, $"World rectangle {footprint} intersects HUD at {Screen.width}x{Screen.height}");
        }
        [UnityTest] public IEnumerator LandscapeRatiosKeepFrameBelowEighteenPercentAndBattlefieldClear()
        {
#if UNITY_EDITOR
            var review = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("AllinDefense.Editor.FrontendExperienceReview")).First(t => t != null);
            foreach (var size in new[] { new Vector2Int(800, 450), new Vector2Int(844, 390), new Vector2Int(1024, 768) })
            {
                review.GetMethod("SetSize").Invoke(null, new object[] { size.x, size.y });
                yield return null; yield return null;
                Create(); yield return null; hud.Refresh(session); Canvas.ForceUpdateCanvases(); yield return null;
                var layout = hud.GetComponent<DefenderHudResponsiveLayout>(); layout.RefreshLayout(); Canvas.ForceUpdateCanvases();
                var rects = layout.FrameScreenRects();
                foreach (var text in hud.GetComponentsInChildren<TMP_Text>().Where(t => !string.IsNullOrEmpty(t.text)))
                {
                    text.ForceMeshUpdate();
                    Assert.That(text.textInfo.meshInfo.Sum(m => m.vertexCount), Is.GreaterThan(0), text.name + " must render glyphs");
                    Assert.That(text.isTextOverflowing, Is.False, text.name + " overflows");
                }
                float coverage = rects.Sum(r => r.width * r.height) / (Screen.width * Screen.height);
                Assert.That(coverage, Is.LessThanOrEqualTo(.18f));
                var camera = Camera.main;
                foreach (var route in session.Routes)
                    for (int segment = 1; segment < route.Length; segment++)
                    {
                        var corners = new System.Collections.Generic.List<Vector3>();
                        foreach (var endpoint in new[] { route[segment - 1], route[segment] })
                            foreach (var padding in new[] { Vector3.left, Vector3.right, Vector3.forward, Vector3.back })
                                corners.Add(endpoint + padding * .8f);
                        AssertFootprintClear(camera, rects, corners.ToArray());
                    }
                var points = session.Routes.SelectMany(route => route).ToList();
                foreach (var route in session.Routes)
                    for (int segment = 1; segment < route.Length; segment++)
                        for (int sample = 0; sample <= 16; sample++)
                        {
                            Vector3 centre = Vector3.Lerp(route[segment - 1], route[segment], sample / 16f);
                            points.Add(centre + Vector3.right * .8f); points.Add(centre - Vector3.right * .8f);
                            points.Add(centre + Vector3.forward * .8f); points.Add(centre - Vector3.forward * .8f);
                        }
                var slots = session.GetComponentsInChildren<GreyboxOutpostSlot>();
                Assert.That(slots.Length, Is.EqualTo(8));
                points.AddRange(slots.Select(slot => slot.transform.position));
                foreach (var slot in slots)
                    AssertFootprintClear(camera, rects,
                        slot.transform.position + new Vector3(-.65f, 0, -.65f), slot.transform.position + new Vector3(.65f, 0, .65f),
                        slot.transform.position + new Vector3(-.65f, 0, .65f), slot.transform.position + new Vector3(.65f, 0, -.65f));
                foreach (var slot in slots)
                    foreach (var corner in new[] { new Vector3(-.65f, 0, -.65f), new Vector3(.65f, 0, .65f), new Vector3(-.65f, 0, .65f), new Vector3(.65f, 0, -.65f) })
                        points.Add(slot.transform.position + corner);
                var keep = session.GetComponentsInChildren<Renderer>().Where(r => r.enabled && r.transform.IsChildOf(session.GetComponentsInChildren<Transform>().First(t => t.name == "Defender Keep Licensed"))).ToArray();
                Assert.That(keep.Length, Is.GreaterThan(0));
                foreach (var part in keep)
                {
                    var corners = new System.Collections.Generic.List<Vector3>();
                    foreach (float x in new[] { -1f, 1f }) foreach (float y in new[] { -1f, 1f }) foreach (float z in new[] { -1f, 1f })
                        corners.Add(part.bounds.center + Vector3.Scale(part.bounds.extents, new Vector3(x, y, z)));
                    AssertFootprintClear(camera, rects, corners.ToArray());
                    points.AddRange(corners);
                }
                points.Add(session.Path[session.Path.Count - 1]);
                foreach (var point in points)
                {
                    Vector3 pixel = camera.WorldToScreenPoint(point);
                    Assert.That(new Rect(0, 0, Screen.width, Screen.height).Contains(pixel), Is.True, $"Offscreen {point} at {pixel}");
                    Assert.That(rects.Any(rect => rect.Contains(pixel)), Is.False, $"HUD intersection {point} at {pixel}, {Screen.width}x{Screen.height}");
                }
                Debug.Log($"[HUD_RATIO] {Screen.width}x{Screen.height}: coverage={coverage:P2}, routes={session.Routes.Count}, slots={slots.Length}, keep=visible, intersections=0");
                System.IO.Directory.CreateDirectory("Builds/HudFrame");
                ScreenCapture.CaptureScreenshot($"Builds/HudFrame/preparation-{Screen.width}x{Screen.height}.png");
                yield return null;
                Object.Destroy(session.gameObject); yield return null;
            }
#endif
        }
    }
}
