using System.Collections.Generic;
using System.Reflection;
using AllinDefense.Data;
using AllinDefense.UI;
using NUnit.Framework;
using UnityEngine;

namespace AllinDefense.Tests
{
    public sealed class CampaignLocalizationTests
    {
        private const string LanguageKey = "AllinDefense.Frontend.Language";
        private bool hadLanguage;
        private int originalLanguage;

        [SetUp]
        public void SetUp()
        {
            hadLanguage = PlayerPrefs.HasKey(LanguageKey);
            originalLanguage = PlayerPrefs.GetInt(LanguageKey);
        }

        [TearDown]
        public void TearDown()
        {
            if (hadLanguage) PlayerPrefs.SetInt(LanguageKey, originalLanguage);
            else PlayerPrefs.DeleteKey(LanguageKey);
        }

        [Test]
        public void EveryCatalogRegionSubtitleAndChapterHasAnEnglishTranslation()
        {
            PlayerPrefs.SetInt(LanguageKey, 1);
            Assert.That(CampaignCatalog.Regions.Count, Is.EqualTo(5));
            int chapters = 0;
            foreach (var region in CampaignCatalog.Regions)
            {
                AssertTranslated(region.displayName);
                AssertTranslated(region.subtitle);
                foreach (var level in region.levels)
                {
                    AssertTranslated(level.displayName);
                    Assert.That(level.localizedName.Length, Is.LessThanOrEqualTo(22), level.displayName);
                    chapters++;
                }
            }
            Assert.That(chapters, Is.EqualTo(50));
            Assert.That(CampaignCatalog.Level(50).localizedName, Is.EqualTo("The Last Lord"));
            Assert.That(CampaignCatalog.Regions[2].subtitle, Is.EqualTo("Hız ve zamanlama"));
        }

        private const int RegionsWithChapterObjectives = 5;

        [TestCase(GameLanguage.Turkish, 45)]
        [TestCase(GameLanguage.English, 50)]
        public void AuthoredRegionBriefingsHaveDistinctSpecificObjectivesWithinLengthLimit(GameLanguage language, int limit)
        {
            PlayerPrefs.SetInt(LanguageKey, language == GameLanguage.English ? 1 : 0);
            var objectives = new HashSet<string>();
            for (int region = 2; region <= RegionsWithChapterObjectives; region++)
            {
                for (int level = (region - 1) * 10 + 1; level < region * 10; level++)
                {
                    string objective = LearningObjective(level);
                    Assert.That(objective, Is.Not.Null.And.Not.Empty, $"{language} chapter {level}");
                    Assert.That(objective.Length, Is.LessThanOrEqualTo(limit), $"{language} chapter {level}: {objective}");
                    Assert.That(objective, Is.Not.EqualTo(FrontendCopy.ObjectiveRegion.Format(
                        GameLocalization.Translate(CampaignCatalog.Regions[region - 1].subtitle))), $"Chapter {level} needs a specific objective");
                    Assert.That(objectives.Add(objective), Is.True, $"Duplicate objective at chapter {level}");
                    PlayerPrefs.SetInt(LanguageKey, language == GameLanguage.English ? 0 : 1);
                    Assert.That(LearningObjective(level), Is.Not.EqualTo(objective), $"Chapter {level} needs both languages");
                    PlayerPrefs.SetInt(LanguageKey, language == GameLanguage.English ? 1 : 0);
                }
                Assert.That(LearningObjective(region * 10), Is.EqualTo(FrontendCopy.ObjectiveBoss.Format(CampaignCatalog.Level(region * 10).localizedName)));
            }
            for (int level = RegionsWithChapterObjectives * 10 + 1; level <= 49; level++)
            {
                var definition = CampaignCatalog.Level(level);
                string expected = definition.isBoss
                    ? FrontendCopy.ObjectiveBoss.Format(definition.localizedName)
                    : FrontendCopy.ObjectiveRegion.Format(GameLocalization.Translate(CampaignCatalog.Regions[definition.region - 1].subtitle));
                Assert.That(LearningObjective(level), Is.EqualTo(expected), $"Chapter {level} retains its objective");
            }
        }

        [TestCase(GameLanguage.Turkish)]
        [TestCase(GameLanguage.English)]
        public void FinalPanelButtonsUseConsistentUppercase(GameLanguage language)
        {
            PlayerPrefs.SetInt(LanguageKey, language == GameLanguage.English ? 1 : 0);
            foreach (var label in new[] { FrontendCopy.ReturnToMissingStars, FrontendCopy.ReturnToMap })
                Assert.That(label.Value, Is.EqualTo(GameLocalization.Uppercase(label.Value)));
        }

        private static void AssertTranslated(string source)
        {
            Assert.That(GameLocalization.HasTranslation(source), Is.True, source);
            Assert.That(GameLocalization.Translate(source), Is.Not.Null.And.Not.Empty.And.Not.EqualTo(source), source);
        }

        private static string LearningObjective(int level) =>
            (string)typeof(AppFlowController).GetMethod("LearningObjective", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { level });
    }
}
