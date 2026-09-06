#if UNITY_EDITOR
using System;
using System.IO;
using System.Threading.Tasks;
using System.Reflection;
using DefenderChronicles.Core;
using DefenderChronicles.Data;
using DefenderChronicles.Presentation;
using DefenderChronicles.UI;
using UnityEditor;
using UnityEngine;

namespace DefenderChronicles.Editor
{
    public static class ClosedTestVisualReview
    {
        [MenuItem("Defender Chronicles/Closed Test/Close Isolated Editor")]
        public static void Close()
        {
            if (Application.dataPath.Contains("DefenderChronicles-first-region")) EditorApplication.Exit(0);
        }
        [MenuItem("Defender Chronicles/Closed Test/Capture Game UI")]
        public static async void Capture()
        {
            if (!Application.isPlaying) return;
            var view = EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));
            var assembly = typeof(UnityEditor.Editor).Assembly;
            var sizesType = assembly.GetType("UnityEditor.GameViewSizes");
            var singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            var sizes = singleton.GetProperty("instance").GetValue(null);
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var groupType = sizesType.GetProperty("currentGroupType",flags).GetValue(sizes);
            var group = sizesType.GetMethod("GetGroup").Invoke(sizes,new[]{groupType});
            var sizeType = assembly.GetType("UnityEditor.GameViewSize");
            var size = Activator.CreateInstance(sizeType,Enum.ToObject(assembly.GetType("UnityEditor.GameViewSizeType"),1),1920,1080,"Closed test landscape");
            group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{size});
            int total = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group,null);
            view.GetType().GetProperty("selectedSizeIndex",flags).SetValue(view,total-1);
            view.Show(); view.Focus();
            await Task.Delay(1000);
            ScreenCapture.CaptureScreenshot("docs/closed-test/evidence/battle-ui-overlay.png");
            Debug.Log("[CLOSED_TEST_CAPTURE] UI capture requested.");
        }
        [MenuItem("Defender Chronicles/Closed Test/Show Specialization UI")]
        public static void ShowSpecialization()
        {
            var session = GameSessionController.Instance;
            if (session == null) return;
            session.ConfigureProgress(10);
            if (session.TryBuildOutpost(0, OutpostKind.Archer)) session.TryUpgradeOutpost(0);
            var outpost = session.Outposts[0];
            UnityEngine.Object.FindAnyObjectByType<DefenderHudController>().ShowSpecializationChoices(outpost,session);
        }
    }
}
#endif
