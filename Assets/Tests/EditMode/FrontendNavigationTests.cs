using AllinDefense.UI;
using NUnit.Framework;
using UnityEngine;

namespace AllinDefense.Tests
{
    public sealed class FrontendNavigationTests
    {
        [Test]
        public void RotationWaitRequiresStableDimensionsAndSafeArea()
        {
            var wait = new FrontendOrientationWait(0);
            var size = new Vector2Int(1920, 1080);
            var area = new Rect(100, 20, 1720, 1040);
            Assert.That(wait.Tick(.1, true, size, area), Is.False);
            Assert.That(wait.Tick(.2, true, size, area), Is.False);
            Assert.That(wait.Tick(.3, true, size, new Rect(100, 20, 1720, 1000)), Is.False);
            Assert.That(wait.Tick(.4, true, size, area), Is.False);
            Assert.That(wait.Tick(.5, true, size, area), Is.False);
            Assert.That(wait.Tick(.6, true, size, area), Is.True);
            Assert.That(wait.Ready, Is.True);
        }

        [Test]
        public void RotationTimeoutAndInvalidSafeAreaCannotLeaveAnInfiniteWait()
        {
            var wait = new FrontendOrientationWait(0);
            Assert.That(wait.Tick(3, false, new Vector2Int(1080, 1920), new Rect(0, 0, 1080, 1920)), Is.True);
            Assert.That(wait.TimedOut, Is.True);
            Assert.That(wait.Ready, Is.False);
            wait = new FrontendOrientationWait(0);
            Assert.That(wait.Tick(3, true, new Vector2Int(1920, 1080), new Rect(0, 0, 1080, 1920)), Is.True);
            Assert.That(wait.Ready, Is.False);
        }

        [TestCase(1080, 1920, false)]
        [TestCase(1080, 2340, false)]
        [TestCase(1536, 2048, true)]
        [TestCase(700, 1000, true)]
        public void TabletColumnsFollowAvailableSafeArea(float width, float height, bool columns)
        {
            Assert.That(FrontendResponsiveLayout.UseColumns(new Vector2(width, height)), Is.EqualTo(columns));
        }
    }
}
