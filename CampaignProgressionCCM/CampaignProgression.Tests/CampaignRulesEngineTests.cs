using DefenderChronicles.CloudCode;
using Newtonsoft.Json;
using NUnit.Framework;

namespace CampaignProgression.Tests;

public sealed class CampaignRulesEngineTests
{
    CampaignRulesDocument rules = null!;

    [SetUp]
    public void SetUp()
    {
        string path = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory,
            "../../../../../Assets/Resources/LiveGame/campaign-rules-v1.json"));
        rules = JsonConvert.DeserializeObject<CampaignRulesDocument>(File.ReadAllText(path))!;
    }

    [Test]
    public void DuplicateClaimCannotAwardTwice()
    {
        var state = new CampaignServerState();
        var claim = Claim(1, 3, "claim-one");

        CampaignSyncResult first = CampaignRulesEngine.ApplyBatch(state, new[] { claim }, rules);
        CampaignSyncResult second = CampaignRulesEngine.ApplyBatch(state, new[] { claim }, rules);

        Assert.That(first.Decisions.Single().AwardedValor, Is.GreaterThan(0));
        Assert.That(second.Decisions.Single().AwardedValor, Is.Zero);
        Assert.That(second.State.Valor, Is.EqualTo(first.State.Valor));
    }

    [Test]
    public void OrderedOfflineBatchCanUnlockFollowingLevel()
    {
        var state = new CampaignServerState();

        CampaignSyncResult result = CampaignRulesEngine.ApplyBatch(state,
            new[] { Claim(1, 2, "claim-one"), Claim(2, 1, "claim-two", 2) }, rules);

        Assert.That(result.Decisions.All(item => item.Accepted), Is.True);
        Assert.That(result.State.UnlockedLevel, Is.EqualTo(3));
        Assert.That(result.State.Levels, Has.Count.EqualTo(2));
    }

    [Test]
    public void LockedLevelAndInvalidLivesAreRejected()
    {
        var state = new CampaignServerState();
        CampaignCompletionClaim locked = Claim(3, 2, "locked");
        CampaignCompletionClaim lives = Claim(1, 2, "lives");
        lives.RemainingLives = rules.Levels[0].StartingLives + 1;

        CampaignSyncResult result = CampaignRulesEngine.ApplyBatch(state, new[] { locked, lives }, rules);

        Assert.That(result.Decisions.Select(item => item.Reason),
            Is.EqualTo(new[] { "level_locked", "invalid_lives" }));
        Assert.That(result.State.Valor, Is.Zero);
    }

    [Test]
    public void LegacyMigrationRecomputesCurrencyFromValidatedStars()
    {
        string legacy = "{\"valor\":999999,\"heroXp\":999999,\"levels\":[{\"level\":1,\"stars\":3,\"bestLives\":999}]}";

        CampaignServerState state = CampaignRulesEngine.MigrateLegacy(legacy, rules);

        Assert.That(state.Valor, Is.EqualTo(rules.Levels[0].ValorReward + 15));
        Assert.That(state.HeroXp, Is.EqualTo(rules.Levels[0].HeroXpReward));
        Assert.That(state.Levels.Single().BestLives, Is.EqualTo(rules.Levels[0].StartingLives));
        Assert.That(state.UnlockedLevel, Is.EqualTo(2));
    }

    CampaignCompletionClaim Claim(int level, int stars, string id, long sequence = 1) => new()
    {
        ClaimId = id,
        Sequence = sequence,
        Level = level,
        LevelId = rules.Levels[level - 1].Id,
        Stars = stars,
        RemainingLives = rules.Levels[level - 1].StartingLives,
        RulesVersion = rules.RulesVersion
    };
}
