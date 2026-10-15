using AllinDefense.Data;
using AllinDefense.UI;
using NUnit.Framework;

namespace AllinDefense.Tests
{
    public sealed class BossNameTests
    {
        [TestCase(10, "Bölge Lordu", "Region Lord")]
        [TestCase(20, "Geçit Efendisi", "Lord of the Pass")]
        [TestCase(30, "Buz Hükümdarı", "Frost Sovereign")]
        [TestCase(40, "Derinliklerin Lordu", "Lord of the Depths")]
        [TestCase(50, "Son Lord", "The Last Lord")]
        public void AuthoredBossNameIsLocalisedForEachRegion(int chapter, string turkish, string english)
        {
            var language = GameLocalization.CurrentLanguage;
            try
            {
                GameLocalization.SetLanguage(GameLanguage.Turkish);
                Assert.That(CampaignCatalog.BossName(chapter), Is.EqualTo(turkish));
                Assert.That(CampaignCatalog.BossName(chapter - 9), Is.EqualTo(turkish));
                GameLocalization.SetLanguage(GameLanguage.English);
                Assert.That(CampaignCatalog.BossName(chapter), Is.EqualTo(english));
            }
            finally { GameLocalization.SetLanguage(language); }
        }
    }
}
