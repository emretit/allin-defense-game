using AllinDefense.UI;
using NUnit.Framework;

namespace AllinDefense.Tests
{
    public sealed class UiHapticsTests
    {
        private sealed class Bridge : IUiHapticsBridge
        {
            public int Calls;
            public UiHapticKind Last;
            public void Play(UiHapticKind kind) { Calls++; Last = kind; }
        }
        [TestCase(false, false, 0)]
        [TestCase(false, true, 0)]
        [TestCase(true, false, 4)]
        [TestCase(true, true, 4)]
        public void PreferenceGatesEveryNativeCallIndependentlyOfReducedMotion(bool enabled, bool reduced, int expected)
        {
            var original = UiHaptics.Bridge;
            bool haptics = FrontendPreferences.Haptics, motion = FrontendPreferences.ReducedMotion;
            var bridge = new Bridge();
            try
            {
                UiHaptics.Bridge = bridge; FrontendPreferences.Haptics = enabled; FrontendPreferences.ReducedMotion = reduced;
                UiHaptics.Light(); UiHaptics.Medium(); UiHaptics.Success(); UiHaptics.Error();
                Assert.That(bridge.Calls, Is.EqualTo(expected));
                if (enabled) Assert.That(bridge.Last, Is.EqualTo(UiHapticKind.Error));
            }
            finally { UiHaptics.Bridge = original; FrontendPreferences.Haptics = haptics; FrontendPreferences.ReducedMotion = motion; }
        }
    }
}
