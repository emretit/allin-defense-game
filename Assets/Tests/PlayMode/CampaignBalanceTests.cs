using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using AllinDefense.Core;
using AllinDefense.Data;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace AllinDefense.Tests
{
    [Category("RegionBalance")]
    public sealed class CampaignBalanceTests
    {
        internal static readonly int[] ApprovedRegions = { 1, 2 };
        private static readonly int[] Regions = { 1, 2, 3, 4, 5 };
        private UnityEngine.Random.State randomState;

        [Serializable] private sealed class Sample
        {
            public int level, strategy;
            public string plan;
            public CampaignBalanceBot.Outcome result;
        }
        [Serializable] private sealed class RegionReport
        {
            public int region;
            public bool approved;
            public string status, contentVersion, measuredUtc;
            public double wallSeconds;
            public List<Sample> samples = new();
            public List<string> failures = new();
        }
        [Serializable] private sealed class Summary
        {
            public string measuredUtc;
            public int[] approvedRegions = ApprovedRegions;
            public List<RegionReport> regions = new();
        }
        private static readonly List<RegionReport> reports = new();

        [OneTimeSetUp] public void Begin() => reports.Clear();
        [SetUp] public void Setup() { randomState = UnityEngine.Random.state; Time.timeScale = 0; }

        // UnityTest uses ValueSource for its five parameter cases; TestCase is unsupported for coroutines.
        [UnityTest]
        public IEnumerator RealResourceCampaign([ValueSource(nameof(Regions))] int region)
        {
            var watch = Stopwatch.StartNew();
            var report = new RegionReport { region = region, approved = ApprovedRegions.Contains(region),
                status = "running", contentVersion = CampaignContent.Region(region).contentVersion,
                measuredUtc = DateTime.UtcNow.ToString("O") };
            try
            {
                foreach (var definition in CampaignContent.Region(region).levels)
                for (int plan = 0; plan < 4; plan++)
                {
                    CampaignBalanceBot.Outcome outcome = null;
                    UnityEngine.Random.InitState(2300 + definition.level);
                    if (plan < 2)
                        yield return CampaignBalanceBot.RunBalance(definition.level, plan, (_, value) => outcome = value, true);
                    else
                        yield return CampaignBalanceBot.RunLearning(definition.level, 1, plan == 2 ? 3 : float.PositiveInfinity,
                            plan == 2, value => outcome = value, plan == 3, true);
                    report.samples.Add(new Sample { level = definition.level, strategy = plan,
                        plan = plan < 2 ? $"active-{plan}" : plan == 2 ? "slow-3s" : "passive-archers", result = outcome });
                    Debug.Log($"[REGION_BALANCE] region={region} level={definition.level} plan={plan} phase={outcome.phase} stars={outcome.stars} seconds={outcome.seconds:F2} lives={outcome.lives} peak={outcome.peak}");
                    if (outcome.phase != "Victory" && outcome.phase != "Defeat")
                        report.failures.Add($"Level {definition.level}, plan {plan}: simulation timed out ({outcome.phase}).");
                    if (!report.approved) continue;
                    if (outcome.peak > 60) report.failures.Add($"Level {definition.level}, plan {plan}: peak {outcome.peak} exceeds 60.");
                    if (plan < 2)
                    {
                        int minimumLives = definition.level <= 3 ? 15 : 8;
                        if (!outcome.victory || outcome.lives < minimumLives || outcome.seconds < definition.targetMinSeconds || outcome.seconds > definition.targetMaxSeconds)
                            report.failures.Add($"Level {definition.level}, active {plan}: {outcome.phase}, {outcome.seconds:F2}s, {outcome.lives} lives (>= {minimumLives}), target {definition.targetMinSeconds}-{definition.targetMaxSeconds}.");
                    }
                    else if (plan == 2 && outcome.stars < 1)
                        report.failures.Add($"Level {definition.level}: slow decisions must earn at least one star.");
                    else if (plan == 3 && definition.level % 10 == 0 && outcome.victory)
                        report.failures.Add($"Level {definition.level}: passive archers must lose the boss chapter.");
                }
                report.status = report.failures.Count > 0 ? "failed" : report.approved ? "passed" : "measurement-only";
            }
            finally
            {
                watch.Stop(); report.wallSeconds = watch.Elapsed.TotalSeconds;
                if (report.status == "running") report.status = "error";
                string folder = Path.Combine(Application.dataPath, "../Builds/balance");
                Directory.CreateDirectory(folder);
                File.WriteAllText(Path.Combine(folder, $"region-{region:00}.json"), JsonUtility.ToJson(report, true));
                reports.RemoveAll(r => r.region == region); reports.Add(report);
                File.WriteAllText(Path.Combine(folder, "regions-summary.json"), JsonUtility.ToJson(new Summary
                    { measuredUtc = DateTime.UtcNow.ToString("O"), regions = reports.OrderBy(r => r.region).ToList() }, true));
            }
            Assert.That(report.failures, Is.Empty, string.Join("; ", report.failures));
        }

        [UnityTearDown] public IEnumerator Cleanup()
        {
            foreach (var session in Object.FindObjectsByType<GameSessionController>(FindObjectsSortMode.None)) Object.Destroy(session.gameObject);
            UnityEngine.Random.state = randomState; Time.timeScale = 1; yield return null;
        }
    }
}
