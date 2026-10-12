using System.Collections;
using AllinDefense.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AllinDefense.Tests
{
    public sealed class UiIdentityAndTouchTests
    {
        [Test]
        public void QueriesUseStableIdsAndScopeRepeatedLabelsAfterRename()
        {
            var root = new GameObject("Root", typeof(RectTransform));
            try
            {
                var first = new GameObject("First", typeof(RectTransform)); first.transform.SetParent(root.transform);
                var second = new GameObject("Second", typeof(RectTransform)); second.transform.SetParent(root.transform);
                var firstLabel = new GameObject("Label", typeof(RectTransform)); firstLabel.transform.SetParent(first.transform);
                var secondLabel = new GameObject("Label", typeof(RectTransform)); secondLabel.transform.SetParent(second.transform);
                UiId.AssignHierarchy(root.transform);
                second.name = "Renamed Card";
                secondLabel.name = "Renamed Text";
                Assert.That(UiQuery.FindById(root.transform, "Second/Label"), Is.SameAs(secondLabel.transform));
                Assert.That(UiQuery.FindById(root.transform, "First/Label"), Is.SameAs(firstLabel.transform));
                Assert.That(UiQuery.FindById(root.transform, "Missing"), Is.Null);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [UnityTest]
        public IEnumerator TargetsFollowScalerAndPressedScaleWithoutChangingVisualBoundsOrOverlap()
        {
            var root = new GameObject("Touch Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            try
            {
                var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = root.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920);
                var first = MakeButton(root.transform); var second = MakeButton(root.transform);
                MobileTouchTarget.Attach(first); MobileTouchTarget.Attach(second);
                yield return null;
                foreach (var reference in new[] { new Vector2(1080, 1920), new Vector2(1920, 1080) })
                {
                    scaler.referenceResolution = reference; scaler.matchWidthOrHeight = .5f;
                    first.transform.localScale = new Vector3(.75f, .75f, 1);
                    var points = UiMetrics.PointsPerCanvasUnit(canvas);
                    ((RectTransform)second.transform).anchoredPosition = new Vector2(60 / points.x, 0);
                    yield return null;
                    Canvas.ForceUpdateCanvases();
                    var a = UiQuery.FindById(first, "Touch Target") as RectTransform;
                    var b = UiQuery.FindById(second, "Touch Target") as RectTransform;
                    Assert.That(a.rect.width * .75f * points.x, Is.GreaterThanOrEqualTo(43.999f));
                    Assert.That(a.rect.height * .75f * points.y, Is.GreaterThanOrEqualTo(43.999f));
                    Assert.That(((RectTransform)first.transform).sizeDelta, Is.EqualTo(new Vector2(20, 20)));
                    float right = a.rect.xMax * .75f;
                    float left = ((RectTransform)second.transform).anchoredPosition.x + b.rect.xMin;
                    Assert.That(right, Is.LessThan(left), "Neighbour hit areas must leave a gap");
                }
            }
            finally { Object.Destroy(root); }
        }

        [UnityTest]
        public IEnumerator ResultOnHiddenBattleHudDoesNotStartAnInactiveCoroutine()
        {
            var root = new GameObject("Hidden result session");
            try
            {
                var session = root.AddComponent<AllinDefense.Core.GameSessionController>();
                session.ConfigurePresentation(Resources.Load<AllinDefense.Presentation.DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog"));
                session.Initialize();
                var hud = session.GetComponentInChildren<AllinDefense.Presentation.DefenderHudController>();
                hud.gameObject.SetActive(false);
                hud.ShowResult(true);
                yield return null;
                LogAssert.NoUnexpectedReceived();
            }
            finally { Object.Destroy(root); Time.timeScale = 1; }
        }

        [Test]
        public void AcceptanceDoesNotConfigureOrRestoreOnlineServices()
        {
            const string key = "DC_FRONTEND_ACCEPTANCE";
            string previous = System.Environment.GetEnvironmentVariable(key);
            var root = new GameObject("Offline acceptance services");
            try
            {
                System.Environment.SetEnvironmentVariable(key, "1");
                var services = root.AddComponent<AllinDefense.Core.UnityGameServicesController>();
                Assert.DoesNotThrow(() => services.Configure(null, null));
                var field = typeof(AllinDefense.Core.UnityGameServicesController).GetField("progress",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                Assert.That(field.GetValue(services), Is.Null);
                System.Environment.SetEnvironmentVariable(key, "0");
                var normalFlow = root.AddComponent<AppFlowController>();
                Assert.Throws<System.NullReferenceException>(() => services.Configure(normalFlow, null));
                var flowField = typeof(AllinDefense.Core.UnityGameServicesController).GetField("flow",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                Assert.That(flowField.GetValue(services), Is.SameAs(normalFlow));
            }
            finally
            {
                System.Environment.SetEnvironmentVariable(key, previous);
                Object.DestroyImmediate(root);
            }
        }

        private static Button MakeButton(Transform parent)
        {
            var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            ((RectTransform)go.transform).sizeDelta = new Vector2(20, 20);
            return go.GetComponent<Button>();
        }
    }
}
