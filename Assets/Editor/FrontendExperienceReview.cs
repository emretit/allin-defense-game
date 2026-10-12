#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using AllinDefense.UI;
using UnityEditor;
using UnityEngine;

namespace AllinDefense.Editor
{
    // Reproducible Game View captures; sizes are real backbuffers, not resized screenshots.
    public static class FrontendExperienceReview
    {
        private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        private static int originalIndex = -1;
        private static object group;
        private static object customSize;
        private static int customIndex;

        public static void SetSize(int width, int height)
        {
            var assembly = typeof(UnityEditor.Editor).Assembly;
            var viewType = assembly.GetType("UnityEditor.GameView");
            var view = EditorWindow.GetWindow(viewType);
            var selected = viewType.GetProperty("selectedSizeIndex", Flags);
            if (customSize == null)
            {
                originalIndex = (int)selected.GetValue(view);
                var sizesType = assembly.GetType("UnityEditor.GameViewSizes");
                var singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
                var sizes = singleton.GetProperty("instance", BindingFlags.Static | BindingFlags.Public).GetValue(null);
                group = sizesType.GetProperty("currentGroup", Flags).GetValue(sizes);
                var sizeType = assembly.GetType("UnityEditor.GameViewSize");
                var kind = assembly.GetType("UnityEditor.GameViewSizeType");
                customSize = Activator.CreateInstance(sizeType, new object[] { Enum.ToObject(kind, 1), width, height, "Frontend review" });
                customIndex = (int)group.GetType().GetMethod("GetCustomCount").Invoke(group, null);
                group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { customSize });
            }
            customSize.GetType().GetProperty("width", Flags).SetValue(customSize, width);
            customSize.GetType().GetProperty("height", Flags).SetValue(customSize, height);
            int builtIn = (int)group.GetType().GetMethod("GetBuiltinCount").Invoke(group, null);
            selected.SetValue(view, builtIn + customIndex);
            view.Repaint();
        }

        public static void RestoreSize()
        {
            if (customSize == null) return;
            var viewType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView");
            viewType.GetProperty("selectedSizeIndex", Flags).SetValue(EditorWindow.GetWindow(viewType), originalIndex);
            int builtIn = (int)group.GetType().GetMethod("GetBuiltinCount").Invoke(group, null);
            group.GetType().GetMethod("RemoveCustomSize").Invoke(group, new object[] { builtIn + customIndex });
            customSize = null;
        }

        public static string[] OverflowingText()
        {
            var flow = UnityEngine.Object.FindAnyObjectByType<AppFlowController>();
            Canvas.ForceUpdateCanvases();
            return flow.GetComponentsInChildren<TMPro.TMP_Text>()
                .Where(t => { t.ForceMeshUpdate(); return t.isTextOverflowing; })
                .Select(t => t.name + ": " + t.text).ToArray();
        }

        [Serializable]
        private sealed class CaptureReport
        {
            public int width;
            public int height;
            public string screen;
            public string language;
            public string[] overflowingText;
        }

        public static void Capture(string path)
        {
            var flow = UnityEngine.Object.FindAnyObjectByType<AppFlowController>();
            flow.StartCoroutine(CaptureFrame(flow, path));
        }

        public static void CaptureSuite(string folder)
        {
            var flow = UnityEngine.Object.FindAnyObjectByType<AppFlowController>();
            flow.StartCoroutine(CaptureScreens(flow, folder));
        }

        private static IEnumerator CaptureScreens(AppFlowController flow, string folder)
        {
            GameLanguage original = FrontendPreferences.Language;
            var show = flow.GetType().GetMethod("ShowEvidenceScreen", Flags);
            foreach (var size in new[] { new Vector2Int(1080, 1920), new Vector2Int(1080, 2340), new Vector2Int(1536, 2048) })
            {
                SetSize(size.x, size.y);
                yield return new WaitForSecondsRealtime(.3f);
                foreach (var language in new[] { GameLanguage.Turkish, GameLanguage.English })
                {
                    FrontendPreferences.Language = language;
                    foreach (var screen in new[] { "welcome", "menu", "campaign", "login", "settings" })
                    {
                        if (screen == "settings") flow.GetType().GetMethod("OpenSettings", Flags).Invoke(flow, null);
                        else show.Invoke(flow, new object[] { screen });
                        yield return CaptureFrame(flow, Path.Combine(folder, $"{screen}-{size.x}x{size.y}-{language}.png"));
                        if (screen == "settings") flow.GetType().GetMethod("CloseSettings", Flags).Invoke(flow, null);
                    }
                }
            }
            FrontendPreferences.Language = original;
            RestoreSize();
            File.WriteAllText(Path.Combine(folder, "complete.txt"), "30 captures completed");
        }

        private static IEnumerator CaptureFrame(AppFlowController flow, string path)
        {
            yield return new WaitForSecondsRealtime(.25f);
            yield return new WaitForEndOfFrame();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            ScreenCapture.CaptureScreenshot(path);
            // Query inside the player loop: Screen dimensions in editor eval can reflect the dock instead.
            var report = new CaptureReport { width = Screen.width, height = Screen.height,
                screen = flow.CurrentScreen.ToString(), language = GameLocalization.CurrentLanguage.ToString(),
                overflowingText = OverflowingText() };
            File.WriteAllText(Path.ChangeExtension(path, ".json"), JsonUtility.ToJson(report, true));
        }
    }
}
#endif
