using AllinDefense.UI.Kit;
using NUnit.Framework;
using UnityEngine;

namespace AllinDefense.Tests
{
    public sealed class UiPointSizeTests
    {
        [TestCase(true)]
        [TestCase(false)]
        public void AnimationScaleIsIgnoredOnlyWhenRequested(bool ignore)
        {
            var canvas = new GameObject("Canvas", typeof(Canvas));
            var parent = new GameObject("Scaled parent", typeof(RectTransform));
            var button = new GameObject("Button", typeof(RectTransform), typeof(UiPointSize));
            try
            {
                parent.transform.SetParent(canvas.transform, false);
                parent.transform.localScale = Vector3.one * 2;
                button.transform.SetParent(parent.transform, false);
                var size = button.GetComponent<UiPointSize>();
                Assert.That(size.IgnoreOwnScale, Is.False);
                size.IgnoreOwnScale = ignore; size.Configure(60);
                float width = size.minWidth, height = size.minHeight;
                button.transform.localScale = Vector3.one * .975f; size.Refresh();
                Assert.That(size.minWidth, Is.EqualTo(ignore ? width : width / .975f).Within(.001f));
                Assert.That(size.minHeight, Is.EqualTo(ignore ? height : height / .975f).Within(.001f));
                parent.transform.localScale = Vector3.one; size.Refresh();
                Assert.That(size.minWidth, Is.EqualTo((ignore ? width : width / .975f) * 2).Within(.001f));
            }
            finally { Object.DestroyImmediate(canvas); }
        }
    }
}
