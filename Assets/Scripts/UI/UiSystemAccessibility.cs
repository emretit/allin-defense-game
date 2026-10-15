using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace AllinDefense.UI
{
    public interface IUiSystemAccessibilitySource
    {
        bool ReducedMotion { get; }
        event Action Changed;
    }

    public static class UiSystemAccessibility
    {
        private sealed class NativeSource : IUiSystemAccessibilitySource
        {
            public bool ReducedMotion
            {
                get
                {
#if UNITY_IOS && !UNITY_EDITOR
                    return AllinUiReducedMotion() != 0;
#else
                    return false;
#endif
                }
            }
            public event Action Changed;
            internal void Notify() => Changed?.Invoke();
        }
#if UNITY_IOS && !UNITY_EDITOR
        private delegate void NativeChanged();
        private static readonly NativeChanged nativeChanged = OnNativeChanged;
        [DllImport("__Internal")] private static extern int AllinUiReducedMotion();
        [DllImport("__Internal")] private static extern void AllinUiObserveReducedMotion(NativeChanged changed);
        [AOT.MonoPInvokeCallback(typeof(NativeChanged))]
        private static void OnNativeChanged() => native.Notify();
#endif
        private static readonly NativeSource native = new();
        private static IUiSystemAccessibilitySource source = native;
        static UiSystemAccessibility() { source.Changed += Notify; }
        public static event Action Changed;
        public static IUiSystemAccessibilitySource Source
        {
            get => source;
            set
            {
                source.Changed -= Notify;
                source = value ?? native;
                source.Changed += Notify;
                Notify();
            }
        }
        public static bool ReducedMotion => source.ReducedMotion;
        private static void Notify() => Changed?.Invoke();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
#if UNITY_IOS && !UNITY_EDITOR
            AllinUiObserveReducedMotion(nativeChanged);
#endif
        }
    }
}
