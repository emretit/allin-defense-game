using System;
using System.IO;
using System.Linq;
using System.Reflection;
using AllinDefense.UI;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace AllinDefense.Tests
{
    public sealed class UiFoundationTests
    {
        [Test]
        public void LiteralCopyNeverAccidentallyTranslatesAsAUiLabel()
        {
            GameLanguage previous = GameLocalization.CurrentLanguage;
            var parent = new GameObject("Literal Test");
            try
            {
                GameLocalization.SetLanguage(GameLanguage.English);
                var text = CreateText(parent.transform, "AYARLAR");
                Assert.That(text.text, Is.EqualTo("AYARLAR"));
                Assert.That(text.GetComponent<LocalizedTmpText>(), Is.Null);
                GameLocalization.SetLanguage(GameLanguage.Turkish);
                Assert.That(text.text, Is.EqualTo("AYARLAR"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(parent);
                GameLocalization.SetLanguage(previous);
            }
        }

        [Test]
        public void DefaultHudBuildKeepsExistingPrefabAndGuidUntouched()
        {
            const string path = "Assets/Art/GeneratedPresentation/DefenderHud.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(existing, Is.Not.Null);
            byte[] before = File.ReadAllBytes(path);
            string guid = AssetDatabase.AssetPathToGUID(path);
            Type builder = AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType(
                "AllinDefense.Editor.DefenderPresentationAssetBuilder")).First(type => type != null);
            MethodInfo method = builder.GetMethod("CreateHudPrefab", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            Assert.That(method.GetParameters().Last().DefaultValue, Is.False);
            var result = method.Invoke(null, new object[] { null, null, null, null, false });
            Assert.That(result, Is.SameAs(existing));
            Assert.That(File.ReadAllBytes(path), Is.EqualTo(before));
            Assert.That(AssetDatabase.AssetPathToGUID(path), Is.EqualTo(guid));
        }

        private static TextMeshProUGUI CreateText(Transform parent, LocalizedCopy copy)
        {
            MethodInfo method = typeof(AppFlowController).GetMethod("CreateText", BindingFlags.Static | BindingFlags.NonPublic);
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Art/GeneratedPresentation/NotoSans-DC.asset");
            return (TextMeshProUGUI)method.Invoke(null, new object[]
            {
                "Stable Test Name", parent, copy, 24f, font, FontStyles.Normal,
                TextAlignmentOptions.Center, Color.white
            });
        }
    }
}
