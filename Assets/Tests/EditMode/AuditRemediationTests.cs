using System;
using System.Linq;
using DefenderChronicles.Core;
using DefenderChronicles.Data;
using DefenderChronicles.UI;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.U2D;

namespace DefenderChronicles.Tests
{
    public sealed class AuditRemediationTests
    {
        [Serializable]
        private sealed class RulesDocument
        {
            public string rulesVersion;
            public RuleLevel[] levels;
        }

        [Serializable]
        private sealed class RuleLevel
        {
            public int number;
            public int region;
            public int indexInRegion;
            public string id;
            public int waveCount;
            public int startingGold;
            public int startingLives;
            public float targetMinutes;
            public float difficulty;
            public int valorReward;
            public int heroXpReward;
            public bool isBoss;
            public int pathPreset;
        }

        [Test]
        public void CanonicalCampaignRulesMatchEveryRuntimeLevel()
        {
            TextAsset asset = Resources.Load<TextAsset>("LiveGame/campaign-rules-v1");
            Assert.That(asset, Is.Not.Null);
            RulesDocument document = JsonUtility.FromJson<RulesDocument>(asset.text);
            Assert.That(document.rulesVersion, Is.EqualTo(CampaignRulesVersion.Current));
            Assert.That(document.levels, Has.Length.EqualTo(CampaignCatalog.LevelCount));

            foreach (RuleLevel rule in document.levels)
            {
                CampaignLevelDefinition level = CampaignCatalog.Level(rule.number);
                Assert.That(rule.id, Is.EqualTo(level.id));
                Assert.That(rule.region, Is.EqualTo(level.region));
                Assert.That(rule.indexInRegion, Is.EqualTo(level.indexInRegion));
                Assert.That(rule.waveCount, Is.EqualTo(level.waveCount));
                Assert.That(rule.startingGold, Is.EqualTo(level.startingGold));
                Assert.That(rule.startingLives, Is.EqualTo(level.startingLives));
                Assert.That(rule.targetMinutes, Is.EqualTo(level.targetMinutes).Within(0.001f));
                Assert.That(rule.difficulty, Is.EqualTo(level.difficulty).Within(0.001f));
                Assert.That(rule.valorReward, Is.EqualTo(level.valorReward));
                Assert.That(rule.heroXpReward, Is.EqualTo(level.heroXpReward));
                Assert.That(rule.isBoss, Is.EqualTo(level.isBoss));
                Assert.That(rule.pathPreset, Is.EqualTo(level.pathPreset));
            }
        }

        [Test]
        public void TurkishAndEnglishLocalesAndCoreCopyAreAvailable()
        {
            Assert.That(AssetDatabase.LoadAssetAtPath<Locale>("Assets/Localization/Turkish (tr).asset"), Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<Locale>("Assets/Localization/English (en).asset"), Is.Not.Null);

            GameLanguage original = GameLocalization.CurrentLanguage;
            try
            {
                GameLocalization.SetLanguage(GameLanguage.English);
                Assert.That(GameLocalization.Translate("AYARLAR"), Is.EqualTo("SETTINGS"));
                Assert.That(GameLocalization.Translate("SAVUNMAYI BAŞLAT"), Is.EqualTo("START DEFENCE"));
                GameLocalization.SetLanguage(GameLanguage.Turkish);
                Assert.That(GameLocalization.Translate("AYARLAR"), Is.EqualTo("AYARLAR"));
            }
            finally
            {
                GameLocalization.SetLanguage(original);
            }
        }

        [Test]
        public void MainFontsAreStaticAndExternalNameFallbackStaysDynamic()
        {
            const string required = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789ĞğİıŞşÇçÖöÜü";
            foreach (string path in new[]
                     {
                         "Assets/Art/GeneratedPresentation/Cinzel-SemiBold-DC.asset",
                         "Assets/Art/GeneratedPresentation/NotoSans-DC.asset",
                         "Assets/Art/GeneratedPresentation/NotoSerif-DC.asset"
                     })
            {
                TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                Assert.That(font, Is.Not.Null);
                Assert.That(font.atlasPopulationMode, Is.EqualTo(AtlasPopulationMode.Static));
                Assert.That(font.HasCharacters(required, out uint[] missing), Is.True,
                    $"{path} is missing: {string.Join(",", missing ?? Array.Empty<uint>())}");
            }

            TMP_FontAsset fallback = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                "Assets/Art/GeneratedPresentation/NotoSans-ExternalFallback.asset");
            Assert.That(fallback, Is.Not.Null);
            Assert.That(fallback.atlasPopulationMode, Is.EqualTo(AtlasPopulationMode.Dynamic));
        }

        [Test]
        public void UiAtlasUsesNineSliceSafePacking()
        {
            SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(
                "Assets/Art/UI/DefenderChroniclesUI.spriteatlas");
            Assert.That(atlas, Is.Not.Null);
            SpriteAtlasPackingSettings settings = atlas.GetPackingSettings();
            Assert.That(settings.enableRotation, Is.False);
            Assert.That(settings.enableTightPacking, Is.False);
            Assert.That(settings.padding, Is.GreaterThanOrEqualTo(4));
            Assert.That(atlas.GetPackables().Length, Is.GreaterThanOrEqualTo(9));
        }

        [Test]
        public void MasterMixerHasSafetyCompressor()
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(
                "Assets/Art/GeneratedPresentation/DefenderPresentation.mixer");
            UnityEngine.Object compressor = assets.FirstOrDefault(item =>
                item.GetType().FullName == "UnityEditor.Audio.AudioMixerEffectController" &&
                string.Equals((string)item.GetType().GetProperty("effectName")?.GetValue(item), "Compressor",
                    StringComparison.Ordinal));
            Assert.That(compressor, Is.Not.Null);
        }
    }
}
