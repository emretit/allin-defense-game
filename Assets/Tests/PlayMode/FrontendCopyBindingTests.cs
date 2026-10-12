using System.Collections;
using System.Reflection;
using AllinDefense.Presentation;
using AllinDefense.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace AllinDefense.Tests
{
    public sealed class FrontendCopyBindingTests
    {
        [UnityTest]
        public IEnumerator RevisedTurkishLabelKeepsEnglishOnLiveLanguageChangesAndReenable()
        {
            GameLanguage previous = GameLocalization.CurrentLanguage;
            var parent = new GameObject("Copy Test");
            try
            {
                foreach (GameLanguage initial in new[] { GameLanguage.Turkish, GameLanguage.English })
                {
                    GameLocalization.SetLanguage(initial);
                    // No legacy dictionary entry exists for this deliberately revised wording.
                    var revised = new LocalizedCopy("YENİ AYAR BAŞLIĞI", FrontendCopy.SettingsTitle.English);
                    Assert.That(GameLocalization.HasTranslation(revised.Turkish), Is.False);
                    var method = typeof(AppFlowController).GetMethod("CreateText", BindingFlags.Static | BindingFlags.NonPublic);
                    var catalog = Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog");
                    var text = (TextMeshProUGUI)method.Invoke(null, new object[]
                    {
                        "Stable Test Name", parent.transform, revised, 24f, catalog.bodyFont,
                        FontStyles.Normal, TextAlignmentOptions.Center, Color.white
                    });
                    Assert.That(text.GetComponent<LocalizedTmpText>(), Is.Not.Null);
                    Assert.That(text.name, Is.EqualTo("Stable Test Name"));
                    Assert.That(text.text, Is.EqualTo(initial == GameLanguage.English ? "SETTINGS" : revised.Turkish));
                    GameLocalization.SetLanguage(GameLanguage.English);
                    Assert.That(text.text, Is.EqualTo("SETTINGS"));
                    GameLocalization.SetLanguage(GameLanguage.Turkish);
                    Assert.That(text.text, Is.EqualTo("YENİ AYAR BAŞLIĞI"));
                    text.gameObject.SetActive(false);
                    GameLocalization.SetLanguage(GameLanguage.English);
                    text.gameObject.SetActive(true);
                    Assert.That(text.text, Is.EqualTo("SETTINGS"));
                    Object.Destroy(text.gameObject);
                    yield return null;
                }
            }
            finally
            {
                Object.Destroy(parent);
                GameLocalization.SetLanguage(previous);
            }
        }
    }
}
