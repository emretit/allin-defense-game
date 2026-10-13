using System.Runtime.InteropServices;

namespace AllinDefense.UI
{
    public enum UiHapticKind { Light, Medium, Success, Error }

    public interface IUiHapticsBridge
    {
        void Play(UiHapticKind kind);
    }

    public static class UiHaptics
    {
        private sealed class NativeBridge : IUiHapticsBridge
        {
#if UNITY_IOS && !UNITY_EDITOR
            [DllImport("__Internal")] private static extern void AllinUiHaptic(int kind);
#endif
            public void Play(UiHapticKind kind)
            {
#if UNITY_IOS && !UNITY_EDITOR
                AllinUiHaptic((int)kind);
#endif
            }
        }

        public static IUiHapticsBridge Bridge { get; set; } = new NativeBridge();
        public static void Play(UiHapticKind kind) { if (FrontendPreferences.Haptics) Bridge?.Play(kind); }
        public static void Light() => Play(UiHapticKind.Light);
        public static void Medium() => Play(UiHapticKind.Medium);
        public static void Success() => Play(UiHapticKind.Success);
        public static void Error() => Play(UiHapticKind.Error);
    }
}
