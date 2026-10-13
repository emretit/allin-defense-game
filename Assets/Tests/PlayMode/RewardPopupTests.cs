using System.Collections;
using System.Linq;
using System.Reflection;
using AllinDefense.Core;
using AllinDefense.Data;
using AllinDefense.Presentation;
using AllinDefense.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace AllinDefense.Tests
{
    public sealed class RewardPopupTests
    {
        private GameObject root;
        private DefenderRewardPopupPool pool;
        private Camera camera;
        private bool motion;
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static DefenderPresentationCatalog Catalog => Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog");

        [SetUp] public void Setup()
        {
            motion = FrontendPreferences.ReducedMotion;
            FrontendPreferences.ReducedMotion = false;
            Time.timeScale = 1;
            root = new GameObject("Reward test");
            camera = new GameObject("Reward camera").AddComponent<Camera>();
            camera.transform.SetParent(root.transform);
            camera.transform.position = new Vector3(0, 4, -10);
            camera.transform.LookAt(Vector3.zero);
            pool = root.AddComponent<DefenderRewardPopupPool>();
            pool.Initialize(Catalog, camera);
        }

        [UnityTearDown] public IEnumerator Cleanup()
        {
            Object.Destroy(root);
            yield return null;
            FrontendPreferences.ReducedMotion = motion;
            Time.timeScale = 1;
        }

        private PresentationSignal Signal(int reward = 17, PresentationActorKind actor = PresentationActorKind.Raider) =>
            new(PresentationCue.EnemyDefeated, Vector3.zero, actor: actor, value: reward);
        private TMP_Text ActiveText => pool.GetComponentsInChildren<TMP_Text>().Single();

        [Test] public void KilledEnemyCarriesItsRewardAndCreatesPopupAboveItsPosition()
        {
            var session = root.AddComponent<GameSessionController>();
            session.ConfigurePresentation(Catalog);
            session.SetCampaignLevel(1);
            session.Initialize();
            Time.timeScale = 0;
            session.GetType().GetMethods(Private).Single(m => m.Name == "SpawnEnemy" && m.GetParameters().Length == 1).Invoke(session, new object[] { EnemyArchetype.Raider });
            var enemy = session.Enemies.Last();
            var director = session.GetComponentInChildren<DefenderPresentationDirector>();
            int before = session.Gold;
            int reward = enemy.Reward;
            Vector3 position = enemy.Position;
            enemy.TakeDamage(100000);
            Assert.That(session.Gold, Is.EqualTo(before + reward));
            var text = director.Rewards.GetComponentsInChildren<TMP_Text>().Single();
            Assert.That(text.text, Is.EqualTo("+" + reward));
            var rect = (RectTransform)text.transform.parent;
            Assert.That(rect.anchoredPosition.y, Is.GreaterThan(Camera.main.WorldToScreenPoint(position).y));
        }

        [UnityTest] public IEnumerator PopupRisesFadesAndReturnsToPool()
        {
            pool.Play(Signal());
            var rect = (RectTransform)ActiveText.transform.parent;
            float start = rect.anchoredPosition.y;
            yield return new WaitForSeconds(.6f);
            Assert.That(rect.anchoredPosition.y, Is.GreaterThan(start));
            Assert.That(rect.GetComponent<CanvasGroup>().alpha, Is.InRange(0.01f, .99f));
            yield return new WaitForSeconds(.25f);
            Assert.That(pool.ActiveCount, Is.Zero);
            Assert.That(pool.GetComponentsInChildren<TMP_Text>(true).Length, Is.EqualTo(DefenderRewardPopupPool.Capacity));
        }

        [Test] public void CapacityRecyclesOldestWithoutGrowingOrAllocatingPerKill()
        {
            pool.Play(Signal(1));
            var oldest = ActiveText;
            for (int i = 2; i <= DefenderRewardPopupPool.Capacity; i++) pool.Play(Signal(i));
            pool.Play(Signal(99));
            Assert.That(oldest.text, Is.EqualTo("+99"));
            Assert.That(pool.ActiveCount, Is.EqualTo(DefenderRewardPopupPool.Capacity));
            Assert.That(pool.GetComponentsInChildren<TMP_Text>(true).Length, Is.EqualTo(DefenderRewardPopupPool.Capacity));
            var signal = Signal(308);
            pool.Play(Signal(int.MaxValue));
            Assert.That(pool.GetComponentsInChildren<TMP_Text>().Any(t => t.text == "+2147483647"), Is.True);
            pool.Play(signal);
            Assert.That((TestDelegate)(() => pool.Play(signal)),
                UnityEngine.TestTools.Constraints.ConstraintExtensions.AllocatingGCMemory(Is.Not));
        }

        [UnityTest] public IEnumerator ReducedMotionKeepsPositionFixedAndStillExpires()
        {
            FrontendPreferences.ReducedMotion = true;
            pool.Play(Signal());
            var rect = (RectTransform)ActiveText.transform.parent;
            Vector2 position = rect.anchoredPosition;
            yield return new WaitForSeconds(.6f);
            Assert.That(rect.anchoredPosition, Is.EqualTo(position));
            Assert.That(rect.GetComponent<CanvasGroup>().alpha, Is.LessThan(1));
            yield return new WaitForSeconds(.25f);
            Assert.That(pool.ActiveCount, Is.Zero);
        }

        [Test] public void ZeroRewardAndOtherCuesDoNotCreatePopups()
        {
            pool.Play(Signal(0));
            pool.Play(new PresentationSignal(PresentationCue.UnitDefeated, Vector3.zero, value: 20));
            Assert.That(pool.ActiveCount, Is.Zero);
        }

        [Test] public void BossRewardIsLargerAndHigher()
        {
            pool.Play(Signal());
            var normal = (RectTransform)ActiveText.transform.parent;
            pool.Play(Signal(300, PresentationActorKind.Boss));
            var boss = pool.GetComponentsInChildren<TMP_Text>().Single(t => t.text == "+300");
            var rect = (RectTransform)boss.transform.parent;
            Assert.That(rect.localScale.x, Is.GreaterThan(normal.localScale.x));
            Assert.That(rect.anchoredPosition.y, Is.GreaterThan(normal.anchoredPosition.y));
        }

        [UnityTest] public IEnumerator ScaledTimePausesAndDoubleSpeedExpiresSooner()
        {
            pool.Play(Signal());
            var rect = (RectTransform)ActiveText.transform.parent;
            Vector2 position = rect.anchoredPosition;
            Time.timeScale = 0;
            yield return new WaitForSecondsRealtime(.9f);
            Assert.That(pool.ActiveCount, Is.EqualTo(1));
            Assert.That(rect.anchoredPosition, Is.EqualTo(position));
            Time.timeScale = 2;
            yield return new WaitForSecondsRealtime(.45f);
            Assert.That(pool.ActiveCount, Is.Zero);
        }
    }
}
