using System.Collections;
using AllinDefense.UI;
using AllinDefense.UI.Kit;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AllinDefense.Tests
{
    public sealed class UiKitMotionTests
    {
        private GameObject root;
        private bool reduced;
        private float timeScale;
        [SetUp] public void SetUp()
        {
            reduced = FrontendPreferences.ReducedMotion; timeScale = Time.timeScale;
            FrontendPreferences.ReducedMotion = false;
            root = new GameObject("MotionTest", typeof(RectTransform), typeof(UiMotion), typeof(CanvasGroup));
        }
        [TearDown] public void TearDown()
        {
            Time.timeScale = timeScale; FrontendPreferences.ReducedMotion = reduced;
            Object.DestroyImmediate(root);
        }
        [Test] public void ReducedMotionCompletesEveryPrimitiveSynchronously()
        {
            FrontendPreferences.ReducedMotion = true;
            var motion = root.GetComponent<UiMotion>(); var group = root.GetComponent<CanvasGroup>();
            var rect = (RectTransform)root.transform; int value = 0, completed = 0;
            Assert.That(motion.Fade(group, .3f).IsComplete, Is.True);
            Assert.That(group.alpha, Is.EqualTo(.3f));
            Assert.That(motion.Scale(rect, Vector3.one * 2).IsComplete, Is.True);
            Assert.That(rect.localScale, Is.EqualTo(Vector3.one * 2));
            Assert.That(motion.Slide(rect, Vector2.one * 12).IsComplete, Is.True);
            Assert.That(rect.anchoredPosition, Is.EqualTo(Vector2.one * 12));
            Assert.That(motion.Punch(rect, 2).IsComplete, Is.True);
            Assert.That(rect.localScale, Is.EqualTo(Vector3.one * 2));
            Assert.That(motion.Shake(rect).IsComplete, Is.True);
            Assert.That(rect.anchoredPosition, Is.EqualTo(Vector2.one * 12));
            Assert.That(motion.Count(99, 7, n => value = n).IsComplete, Is.True);
            Assert.That(value, Is.EqualTo(7));
            motion.Animate(_ => { }, 1, () => completed++);
            Assert.That(completed, Is.EqualTo(1));
        }
        [Test] public void ChangingPreferenceFinishesBeforeSetterReturns()
        {
            var handle = root.GetComponent<UiMotion>().Slide((RectTransform)root.transform, Vector2.one * 16);
            FrontendPreferences.ReducedMotion = true;
            Assert.That(handle.IsComplete, Is.True);
            Assert.That(((RectTransform)root.transform).anchoredPosition, Is.EqualTo(Vector2.one * 16));
        }
        [UnityTest] public IEnumerator FadeAndCountFinishWhileGameplayClockIsPaused()
        {
            Time.timeScale = 0;
            var motion = root.GetComponent<UiMotion>(); var group = root.GetComponent<CanvasGroup>(); int number = 0;
            var fade = motion.Fade(group, .2f); var count = motion.Count(0, 99, n => number = n);
            yield return new WaitForSecondsRealtime(UiTokens.SlideDuration + .15f);
            Assert.That(fade.IsComplete, Is.True); Assert.That(count.IsComplete, Is.True);
            Assert.That(group.alpha, Is.EqualTo(.2f).Within(.001f)); Assert.That(number, Is.EqualTo(99));
        }
        [UnityTest] public IEnumerator CancellationStopsMutationAndDoesNotRunCompletion()
        {
            var motion = root.GetComponent<UiMotion>(); float value = -1; int completed = 0;
            var handle = motion.Animate(t => value = t, 1, () => completed++);
            yield return null;
            handle.Cancel(); float stopped = value;
            yield return new WaitForSecondsRealtime(.1f);
            Assert.That(value, Is.EqualTo(stopped)); Assert.That(completed, Is.Zero); Assert.That(handle.IsComplete, Is.False);
        }
        [UnityTest] public IEnumerator PreferenceChangeFinishesAnInFlightAnimation()
        {
            var motion = root.GetComponent<UiMotion>(); var group = root.GetComponent<CanvasGroup>();
            var handle = motion.Fade(group, 0);
            FrontendPreferences.ReducedMotion = true;
            yield return null; yield return null;
            Assert.That(handle.IsComplete, Is.True); Assert.That(group.alpha, Is.Zero);
        }
        [UnityTest] public IEnumerator ReadyHasAtMostTwoPulsesAndRepeatedReadyDoesNotRestart()
        {
            var set = ScriptableObject.CreateInstance<UiDesignSystemAssets>();
            set.regularFont = TMPro.TMP_Settings.defaultFontAsset; set.boldFont = set.regularFont;
            var kit = new UiKit(set);
            var glyph = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * .5f);
            var button = kit.IconButton(root.transform, "Ready", glyph);
            var state = button.GetComponent<UiVisualState>();
            state.SetState(UiState.Ready); Assert.That(state.ReadyPulseCount, Is.EqualTo(2));
            state.SetState(UiState.Ready);
            yield return new WaitForSecondsRealtime(UiTokens.SlideDuration * 2 + .2f);
            var ring = button.transform.Find("Ready.ReadyRing");
            Assert.That(ring.localScale.x, Is.EqualTo(1f).Within(.001f));
            state.SetState(UiState.Ready); yield return null;
            Assert.That(ring.localScale.x, Is.EqualTo(1f).Within(.001f));
            Object.DestroyImmediate(set); Object.DestroyImmediate(glyph);
        }
    }
}
