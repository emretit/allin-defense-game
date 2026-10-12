using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;
using Unity.Services.CloudSave.Model;

namespace AllinDefense.CloudCode;

public sealed class CampaignProgressionModule
{
    public const string StateKey = "campaign_state_v3";
    public const string RulesVersion = "campaign-rules-v2";
    public const int MaximumBatchSize = 20;

    static readonly ConcurrentDictionary<string, SemaphoreSlim> PlayerLocks = new();
    static readonly Lazy<CampaignRulesDocument> Rules = new(LoadRules);

    readonly IGameApiClient gameApiClient;
    readonly ILogger<CampaignProgressionModule> logger;

    public CampaignProgressionModule(IGameApiClient gameApiClient, ILogger<CampaignProgressionModule> logger)
    {
        this.gameApiClient = gameApiClient;
        this.logger = logger;
    }

    [CloudCodeFunction("GetState")]
    public async Task<CampaignSyncResult> GetState(IExecutionContext context, string? legacySnapshotJson = null)
    {
        EnsurePlayer(context);
        SemaphoreSlim gate = PlayerLocks.GetOrAdd(context.PlayerId!, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync();
        try
        {
            (CampaignServerState? state, string? writeLock) = await LoadStateAsync(context);
            if (state == null)
            {
                state = CampaignRulesEngine.MigrateLegacy(legacySnapshotJson, Rules.Value);
                await SaveStateAsync(context, state, writeLock);
                logger.LogInformation("Campaign state initialized. player={PlayerId} migrated={Migrated}",
                    context.PlayerId, !string.IsNullOrWhiteSpace(legacySnapshotJson));
            }
            return new CampaignSyncResult { State = state, RulesVersion = RulesVersion };
        }
        finally
        {
            gate.Release();
        }
    }

    [CloudCodeFunction("SubmitCompletionBatch")]
    public async Task<CampaignSyncResult> SubmitCompletionBatch(
        IExecutionContext context, List<CampaignCompletionClaim>? claims)
    {
        EnsurePlayer(context);
        if (claims == null || claims.Count == 0)
            return await GetState(context);
        if (claims.Count > MaximumBatchSize)
            throw new ArgumentException($"A completion batch cannot exceed {MaximumBatchSize} claims.");

        SemaphoreSlim gate = PlayerLocks.GetOrAdd(context.PlayerId!, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync();
        try
        {
            (CampaignServerState? loadedState, string? writeLock) = await LoadStateAsync(context);
            CampaignServerState state = loadedState ?? new CampaignServerState();
            CampaignSyncResult result = CampaignRulesEngine.ApplyBatch(state, claims, Rules.Value, context.PlayerId);
            await SaveStateAsync(context, result.State, writeLock);
            logger.LogInformation(
                "Campaign batch processed. player={PlayerId} claims={ClaimCount} accepted={Accepted} revision={Revision}",
                context.PlayerId, claims.Count, result.Decisions.Count(item => item.Accepted), result.State.Revision);
            return result;
        }
        finally
        {
            gate.Release();
        }
    }

    async Task<(CampaignServerState? State, string? WriteLock)> LoadStateAsync(IExecutionContext context)
    {
        var response = await gameApiClient.CloudSaveData.GetProtectedItemsAsync(
            context, context.ServiceToken, context.ProjectId, context.PlayerId!, new List<string> { StateKey });
        var item = response.Data?.Results?.FirstOrDefault();
        string? json = item?.Value?.ToString();
        CampaignServerState? state = string.IsNullOrWhiteSpace(json)
            ? null
            : JsonConvert.DeserializeObject<CampaignServerState>(json);
        return (state, item?.WriteLock);
    }

    async Task SaveStateAsync(IExecutionContext context, CampaignServerState state, string? writeLock)
    {
        string json = JsonConvert.SerializeObject(state);
        var body = string.IsNullOrWhiteSpace(writeLock)
            ? new SetItemBody(StateKey, json)
            : new SetItemBody(StateKey, json, writeLock);
        await gameApiClient.CloudSaveData.SetProtectedItemAsync(
            context, context.ServiceToken, context.ProjectId, context.PlayerId!, body);
    }

    static CampaignRulesDocument LoadRules()
    {
        using var stream = typeof(CampaignProgressionModule).Assembly.GetManifestResourceStream("campaign-rules-v2.json")
            ?? throw new InvalidOperationException("Embedded campaign rules are missing from the deployed module.");
        using var reader = new StreamReader(stream);
        CampaignRulesDocument? rules = JsonConvert.DeserializeObject<CampaignRulesDocument>(reader.ReadToEnd());
        if (rules == null || rules.RulesVersion != RulesVersion || rules.Levels.Count != 50)
            throw new InvalidOperationException("Campaign rules are missing, incomplete, or have the wrong version.");
        return rules;
    }

    static void EnsurePlayer(IExecutionContext context)
    {
        if (string.IsNullOrWhiteSpace(context.PlayerId))
            throw new InvalidOperationException("An authenticated player is required.");
    }
}

public static class CampaignRulesEngine
{
    public static CampaignSyncResult ApplyBatch(CampaignServerState state,
        IEnumerable<CampaignCompletionClaim> claims, CampaignRulesDocument rules, string? expectedOwner = null)
    {
        Normalize(state, rules);
        var decisions = new List<CampaignClaimDecision>();
        var seenInBatch = new HashSet<string>(StringComparer.Ordinal);
        foreach (CampaignCompletionClaim claim in claims.OrderBy(item => item?.Sequence ?? long.MaxValue))
        {
            CampaignClaimDecision decision = ApplyClaim(state, claim, rules, seenInBatch, expectedOwner);
            decisions.Add(decision);
        }
        state.Revision++;
        return new CampaignSyncResult { State = state, Decisions = decisions, RulesVersion = rules.RulesVersion };
    }

    public static CampaignServerState MigrateLegacy(string? legacyJson, CampaignRulesDocument rules)
    {
        var state = new CampaignServerState();
        if (string.IsNullOrWhiteSpace(legacyJson)) return state;

        LegacyCampaignSave? legacy;
        try
        {
            legacy = JsonConvert.DeserializeObject<LegacyCampaignSave>(legacyJson);
        }
        catch (JsonException)
        {
            return state;
        }
        if (legacy?.Levels == null) return state;

        int expectedLevel = 1;
        foreach (LegacyLevelProgress incoming in legacy.Levels
                     .Where(item => item != null && item.Level >= 1 && item.Level <= rules.Levels.Count)
                     .GroupBy(item => item.Level)
                     .Select(group => new LegacyLevelProgress
                     {
                         Level = group.Key,
                         Stars = group.Max(item => item.Stars),
                         BestLives = group.Max(item => item.BestLives)
                     })
                     .OrderBy(item => item.Level))
        {
            CampaignRuleLevel rule = rules.Levels[incoming.Level - 1];
            if (incoming.Level != expectedLevel || incoming.Stars <= 0 || incoming.BestLives <= 0)
            {
                state.MigrationIncomplete = true;
                break;
            }
            int lives = Math.Clamp(incoming.BestLives, 1, rule.StartingLives);
            int stars = StarsForLives(lives, rule.StartingLives);
            expectedLevel++;
            state.Levels.Add(new CampaignLevelProgress
            {
                Level = incoming.Level,
                Stars = stars,
                BestLives = lives
            });
            state.Valor += rule.ValorReward + stars * 5;
            state.HeroXp += rule.HeroXpReward;
        }
        int highestCompleted = state.Levels.Count == 0 ? 0 : state.Levels.Max(item => item.Level);
        state.UnlockedLevel = Math.Clamp(highestCompleted + 1, 1, rules.Levels.Count);
        state.Revision = 1;
        return state;
    }

    static CampaignClaimDecision ApplyClaim(CampaignServerState state, CampaignCompletionClaim claim,
        CampaignRulesDocument rules, HashSet<string> seenInBatch, string? expectedOwner)
    {
        var decision = new CampaignClaimDecision { ClaimId = claim?.ClaimId ?? string.Empty };
        if (claim == null || string.IsNullOrWhiteSpace(claim.ClaimId))
            return Reject(decision, "invalid_claim_id");
        if (expectedOwner != null && claim.OwnerPlayerId != expectedOwner)
            return Reject(decision, "owner_mismatch");
        if (!seenInBatch.Add(claim.ClaimId) || state.ProcessedClaimIds.Contains(claim.ClaimId))
            return Accept(decision, "duplicate", 0, 0);
        if (!string.Equals(claim.RulesVersion, rules.RulesVersion, StringComparison.Ordinal) &&
            !(rules.RulesVersion == "campaign-rules-v2" && claim.RulesVersion == "campaign-rules-v1"))
            return Reject(decision, "rules_mismatch");
        if (claim.Level < 1 || claim.Level > rules.Levels.Count)
            return Reject(decision, "invalid_level");

        CampaignRuleLevel rule = rules.Levels[claim.Level - 1];
        if (!string.Equals(claim.LevelId, rule.Id, StringComparison.Ordinal))
            return Reject(decision, "level_id_mismatch");
        if (claim.RemainingLives <= 0 || claim.RemainingLives > rule.StartingLives)
            return Reject(decision, "invalid_lives");
        if (claim.Level > state.UnlockedLevel)
            return Reject(decision, "level_locked");

        CampaignLevelProgress progress = state.Levels.FirstOrDefault(item => item.Level == claim.Level)
                                         ?? new CampaignLevelProgress { Level = claim.Level };
        if (!state.Levels.Contains(progress)) state.Levels.Add(progress);
        int previousStars = progress.Stars;
        int stars = StarsForLives(claim.RemainingLives, rule.StartingLives);
        int improvedStars = Math.Max(0, stars - previousStars);
        int valor = improvedStars * 5 + (previousStars == 0 ? rule.ValorReward : 0);
        int heroXp = previousStars == 0 ? rule.HeroXpReward : 0;
        progress.Stars = Math.Max(previousStars, stars);
        progress.BestLives = Math.Max(progress.BestLives, claim.RemainingLives);
        state.UnlockedLevel = Math.Max(state.UnlockedLevel, Math.Min(rules.Levels.Count, claim.Level + 1));
        state.Valor += valor;
        state.HeroXp += heroXp;
        state.ProcessedClaimIds.Add(claim.ClaimId);
        return Accept(decision, "accepted", valor, heroXp);
    }

    public static int StarsForLives(int lives, int startingLives) =>
        lives <= 0 || startingLives <= 0 ? 0 :
        lives * 4 >= startingLives * 3 ? 3 : lives * 5 >= startingLives * 2 ? 2 : 1;

    static void Normalize(CampaignServerState state, CampaignRulesDocument rules)
    {
        state.SchemaVersion = 4;
        state.Revision = Math.Max(0, state.Revision);
        state.UnlockedLevel = Math.Clamp(state.UnlockedLevel, 1, rules.Levels.Count);
        state.Valor = Math.Max(0, state.Valor);
        state.HeroXp = Math.Max(0, state.HeroXp);
        state.Levels ??= new List<CampaignLevelProgress>();
        state.ProcessedClaimIds ??= new List<string>();
    }

    static CampaignClaimDecision Reject(CampaignClaimDecision decision, string reason)
    {
        decision.Accepted = false;
        decision.Reason = reason;
        return decision;
    }

    static CampaignClaimDecision Accept(CampaignClaimDecision decision, string reason, int valor, int heroXp)
    {
        decision.Accepted = true;
        decision.Reason = reason;
        decision.AwardedValor = valor;
        decision.AwardedHeroXp = heroXp;
        return decision;
    }
}

public sealed class CampaignRulesDocument
{
    [JsonProperty("rulesVersion")] public string RulesVersion { get; set; } = string.Empty;
    [JsonProperty("levels")] public List<CampaignRuleLevel> Levels { get; set; } = new();
}

public sealed class CampaignRuleLevel
{
    [JsonProperty("number")] public int Number { get; set; }
    [JsonProperty("id")] public string Id { get; set; } = string.Empty;
    [JsonProperty("startingLives")] public int StartingLives { get; set; }
    [JsonProperty("valorReward")] public int ValorReward { get; set; }
    [JsonProperty("heroXpReward")] public int HeroXpReward { get; set; }
}

public sealed class CampaignCompletionClaim
{
    [JsonProperty("claimId")] public string ClaimId { get; set; } = string.Empty;
    [JsonProperty("ownerPlayerId")] public string OwnerPlayerId { get; set; } = string.Empty;
    [JsonProperty("sequence")] public long Sequence { get; set; }
    [JsonProperty("levelId")] public string LevelId { get; set; } = string.Empty;
    [JsonProperty("level")] public int Level { get; set; }
    [JsonProperty("stars")] public int Stars { get; set; }
    [JsonProperty("remainingLives")] public int RemainingLives { get; set; }
    [JsonProperty("rulesVersion")] public string RulesVersion { get; set; } = string.Empty;
    [JsonProperty("createdAtUtc")] public string CreatedAtUtc { get; set; } = string.Empty;
}

public sealed class CampaignServerState
{
    [JsonProperty("schemaVersion")] public int SchemaVersion { get; set; } = 4;
    [JsonProperty("migrationIncomplete")] public bool MigrationIncomplete { get; set; }
    [JsonProperty("revision")] public long Revision { get; set; }
    [JsonProperty("unlockedLevel")] public int UnlockedLevel { get; set; } = 1;
    [JsonProperty("valor")] public int Valor { get; set; }
    [JsonProperty("heroXp")] public int HeroXp { get; set; }
    [JsonProperty("levels")] public List<CampaignLevelProgress> Levels { get; set; } = new();
    [JsonProperty("processedClaimIds")] public List<string> ProcessedClaimIds { get; set; } = new();
}

public sealed class CampaignLevelProgress
{
    [JsonProperty("level")] public int Level { get; set; }
    [JsonProperty("stars")] public int Stars { get; set; }
    [JsonProperty("bestLives")] public int BestLives { get; set; }
}

public sealed class CampaignClaimDecision
{
    [JsonProperty("claimId")] public string ClaimId { get; set; } = string.Empty;
    [JsonProperty("accepted")] public bool Accepted { get; set; }
    [JsonProperty("reason")] public string Reason { get; set; } = string.Empty;
    [JsonProperty("awardedValor")] public int AwardedValor { get; set; }
    [JsonProperty("awardedHeroXp")] public int AwardedHeroXp { get; set; }
}

public sealed class CampaignSyncResult
{
    [JsonProperty("state")] public CampaignServerState State { get; set; } = new();
    [JsonProperty("decisions")] public List<CampaignClaimDecision> Decisions { get; set; } = new();
    [JsonProperty("rulesVersion")] public string RulesVersion { get; set; } = CampaignProgressionModule.RulesVersion;
}

public sealed class LegacyCampaignSave
{
    [JsonProperty("levels")] public List<LegacyLevelProgress> Levels { get; set; } = new();
}

public sealed class LegacyLevelProgress
{
    [JsonProperty("level")] public int Level { get; set; }
    [JsonProperty("stars")] public int Stars { get; set; }
    [JsonProperty("bestLives")] public int BestLives { get; set; }
}
