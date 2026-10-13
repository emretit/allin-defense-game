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
    public sealed class BattleUiPolishTests
    {
        private GameSessionController session;
        private DefenderHudController hud;
        private bool reducedMotion;
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static void Invoke(object target, string name, params object[] values) =>
            target.GetType().GetMethod(name, Private).Invoke(target, values);
        private GreyboxEnemy Spawn(EnemyArchetype type)
        {
            typeof(GameSessionController).GetMethod("SpawnEnemy", Private, null, new[] { typeof(EnemyArchetype), typeof(int) }, null)
                .Invoke(session, new object[] { type, 0 });
            return session.Enemies[session.Enemies.Count - 1];
        }
        private void Create()
        {
            session = new GameObject("Battle UI polish").AddComponent<GameSessionController>();
            session.ConfigurePresentation(Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog"));
            session.SetCampaignLevel(10);
            session.Initialize();
            hud = session.GetComponentInChildren<DefenderHudController>();
            hud.Refresh(session);
            Time.timeScale = 0;
        }
        [SetUp] public void Setup() => reducedMotion = FrontendPreferences.ReducedMotion;

        [UnityTest]
        public IEnumerator KeepPlateTracksLivesCriticalColourAndWorldAnchor()
        {
            FrontendPreferences.ReducedMotion = true;
            Create(); yield return null;
            var plate = hud.GetComponent<DefenderKeepPlate>();
            Assert.That(plate.Amount.text, Is.EqualTo($"{session.Lives}/{session.StartingLivesForLevel}"));
            plate.Amount.ForceMeshUpdate();
            Assert.That(plate.Amount.textInfo.meshInfo.Sum(mesh => mesh.vertexCount), Is.GreaterThan(0));
            Invoke(session, "LoseLives", session.Lives - 5);
            plate.Refresh();
            Assert.That(plate.Amount.text, Is.EqualTo($"5/{session.StartingLivesForLevel}"));
            Assert.That(plate.Fill.fillAmount, Is.EqualTo(5f / session.StartingLivesForLevel).Within(.001));
            Assert.That(plate.Fill.color, Is.EqualTo(UiTokens.Danger));
            Assert.That(plate.Amount.color, Is.EqualTo(UiTokens.Danger));
            Assert.That(plate.Plate.localScale, Is.EqualTo(Vector3.one));
            var update = (Action)typeof(DefenderKeepPlate).GetMethod("LateUpdate", Private).CreateDelegate(typeof(Action), plate);
            update();
            Assert.That((TestDelegate)(() => update()), UnityEngine.TestTools.Constraints.ConstraintExtensions.AllocatingGCMemory(Is.Not));
            var anchor = (RectTransform)plate.Plate.parent;
            Vector2 before = anchor.anchoredPosition;
            Camera.main.orthographicSize *= 2;
            update();
            Assert.That(anchor.anchoredPosition, Is.Not.EqualTo(before));
        }

        [UnityTest]
        public IEnumerator BreachShowsPooledDangerAmountAndReturnsWithoutAllocation()
        {
            Create();
            var enemy = Spawn(EnemyArchetype.Raider);
            var pool = session.GetComponentInChildren<DefenderPresentationDirector>().Rewards;
            Invoke(session, "EnemyReachedKeep", enemy, 2);
            Assert.That(pool.ActiveDamageCount, Is.EqualTo(1));
            var text = pool.GetComponentsInChildren<TMP_Text>().Single(t => t.text == "-2");
            Assert.That(text.color, Is.EqualTo(UiTokens.Danger));
            Time.timeScale = 1;
            yield return new WaitForSeconds(DefenderRewardPopupPool.Lifetime + .15f);
            Assert.That(pool.ActiveDamageCount, Is.Zero);
            Assert.That(text.gameObject.activeInHierarchy, Is.False);
            var signal = new PresentationSignal(PresentationCue.UiError, session.Path[session.Path.Count - 1], value: 2);
            pool.Play(signal);
            Assert.That((TestDelegate)(() => pool.Play(signal)),
                UnityEngine.TestTools.Constraints.ConstraintExtensions.AllocatingGCMemory(Is.Not));
            Assert.That(pool.GetComponentsInChildren<TMP_Text>(true).Length, Is.EqualTo(DefenderRewardPopupPool.Capacity));
        }

        [UnityTest]
        public IEnumerator EnemyBarsRevealOnDamageBossStaysVisibleAndTrailUsesSimulationTime()
        {
            Create();
            var enemy = Spawn(EnemyArchetype.Raider);
            var boss = Spawn(EnemyArchetype.Boss);
            Assert.That(enemy.HealthBarVisible, Is.False);
            Assert.That(boss.HealthBarVisible, Is.True);
            Assert.That(enemy.GetComponentsInChildren<Canvas>(true).Length, Is.EqualTo(1));
            enemy.TakeDamage(Mathf.Max(1, enemy.MaxHealth / 2));
            Assert.That(enemy.HealthBarVisible, Is.True);
            var trail = (Transform)typeof(GreyboxEnemy).GetField("healthTrail", Private).GetValue(enemy);
            var fill = (Transform)typeof(GreyboxEnemy).GetField("healthFill", Private).GetValue(enemy);
            Assert.That(trail.localScale.x, Is.GreaterThan(fill.localScale.x));
            enemy.Stun(10);
            yield return null;
            Assert.That(trail.localScale.x, Is.EqualTo(1));
            var tick = (Action<float>)typeof(GreyboxEnemy).GetMethod("Tick", Private).CreateDelegate(typeof(Action<float>), enemy);
            tick(.2f);
            Assert.That(trail.localScale.x, Is.GreaterThan(fill.localScale.x));
            Assert.That((TestDelegate)(() => tick(.2f)), UnityEngine.TestTools.Constraints.ConstraintExtensions.AllocatingGCMemory(Is.Not));
            Assert.That(trail.localScale.x, Is.EqualTo(fill.localScale.x).Within(.001));
            Assert.That(boss.GetComponentInChildren<Canvas>().transform.position.y, Is.GreaterThan(boss.GetComponentInChildren<Renderer>().bounds.max.y));
            Assert.That(boss.GetComponentInChildren<Canvas>().GetComponent<RectTransform>().rect.width,
                Is.GreaterThan(enemy.GetComponentInChildren<Canvas>().GetComponent<RectTransform>().rect.width));
        }

        private static Rect ScreenRect(RectTransform rect)
        {
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }
        [UnityTest]
        public IEnumerator BottomChromeAndKeepStayInsideSafeAreaAtAllAcceptanceRatios()
        {
#if UNITY_EDITOR
            var review = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("AllinDefense.Editor.FrontendExperienceReview")).First(t => t != null);
            foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(1560, 720), new Vector2Int(1280, 960), new Vector2Int(844, 390) })
            {
                review.GetMethod("SetSize").Invoke(null, new object[] { size.x, size.y });
                yield return null; yield return null;
                Create();
                hud.SetMessage(GameLocalization.Text("Kaleye sızma: Yağmacı", "Keep breached: Raider"), true);
                typeof(GreyboxHero).GetProperty("AbilityRemaining").SetValue(session.Hero, 4f);
                hud.Refresh(session);
                for (int i = 0; i < 5; i++) yield return null;
                var cooldown = hud.GetComponentInChildren<UiRadialCooldown>();
                foreach (var rect in new[] { (RectTransform)hud.transform.Find("MessageChrome"), cooldown.ActionRect, cooldown.BadgeRect,
                    (RectTransform)hud.transform.Find("BottomBar/Hero"), hud.GetComponent<DefenderKeepPlate>().Plate })
                {
                    Rect bounds = ScreenRect(rect), safe = Screen.safeArea;
                    Assert.That(bounds.xMin, Is.GreaterThanOrEqualTo(safe.xMin), rect.name);
                    Assert.That(bounds.yMin, Is.GreaterThanOrEqualTo(safe.yMin), rect.name);
                    Assert.That(bounds.xMax, Is.LessThanOrEqualTo(safe.xMax), rect.name);
                    Assert.That(bounds.yMax, Is.LessThanOrEqualTo(safe.yMax - (rect.name == "Keep Plate" ? 56 : 0)), rect.name);
                }
                var keep = session.GetComponentsInChildren<Transform>().Single(t => t.name == "Defender Keep Licensed");
                var renderers = keep.GetComponentsInChildren<Renderer>();
                Bounds model = renderers[0].bounds;
                foreach (var renderer in renderers) model.Encapsulate(renderer.bounds);
                Vector2 min = new(float.MaxValue, float.MaxValue), max = new(float.MinValue, float.MinValue);
                for (int corner = 0; corner < 8; corner++)
                {
                    Vector3 point = model.center + Vector3.Scale(model.extents,
                        new Vector3((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1));
                    Vector2 screen = Camera.main.WorldToScreenPoint(point);
                    min = Vector2.Min(min, screen); max = Vector2.Max(max, screen);
                }
                Assert.That(ScreenRect(hud.GetComponent<DefenderKeepPlate>().Plate).Overlaps(Rect.MinMaxRect(min.x, min.y, max.x, max.y)), Is.False);
                Assert.That(ScreenRect(cooldown.ActionRect).Overlaps(ScreenRect(cooldown.BadgeRect)), Is.False);
                Assert.That(ScreenRect(cooldown.ActionRect).width, Is.GreaterThanOrEqualTo(UiMetrics.MinTouchPt));
                Assert.That(hud.transform.Find("Message").GetComponent<TMP_Text>().textWrappingMode, Is.EqualTo(TextWrappingModes.NoWrap));
                Object.Destroy(session.gameObject); yield return null;
            }
#else
            yield return null;
#endif
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (session != null) Object.Destroy(session.gameObject);
            FrontendPreferences.ReducedMotion = reducedMotion; Time.timeScale = 1;
#if UNITY_EDITOR
            var review = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("AllinDefense.Editor.FrontendExperienceReview")).FirstOrDefault(t => t != null);
            review?.GetMethod("RestoreSize").Invoke(null, null);
#endif
            yield return null;
        }
    }
}
