using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using AllinDefense.Core;
using AllinDefense.Data;
using AllinDefense.Presentation;
using AllinDefense.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace AllinDefense.Tests
{
    public sealed class ActorVisualV3Tests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly EnemyArchetype[] StressMix =
        {
            EnemyArchetype.Raider, EnemyArchetype.Raider, EnemyArchetype.Vanguard, EnemyArchetype.Regenerator,
            EnemyArchetype.Support, EnemyArchetype.Raider
        };
        private GameSessionController session;
        private Type review;
        private UnityEngine.Random.State randomState;
        private bool reducedMotion;

        [Serializable] private sealed class Sample
        {
            public string state;
            public int width, height, enemies, drawCalls, setPassCalls, triangles;
            public float medianFrameMs, p95FrameMs;
        }
        [Serializable] private sealed class Report
        {
            public string sampling = "Same chapters, towers and seeded waves; counters = max over 3x5 frames (SetPass = min of the three windows); frame time over 120 frames";
            public List<Sample> samples = new();
        }

        [SetUp]
        public void Setup()
        {
            randomState = UnityEngine.Random.state;
            reducedMotion = FrontendPreferences.ReducedMotion;
        }

        [UnityTest]
        public IEnumerator ActorCapturesAndRenderingCounters()
        {
#if UNITY_EDITOR
            if (Environment.GetEnvironmentVariable("ACTOR_V3_CAPTURE") != "1") yield break;
            string phase = Environment.GetEnvironmentVariable("ACTOR_V3_PHASE") ?? "after";
            review = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("AllinDefense.Editor.FrontendExperienceReview")).First(t => t != null);
            Directory.CreateDirectory("Builds/actors-v3");
            var report = new Report();
            foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(1280, 960) })
            {
                review.GetMethod("SetSize").Invoke(null, new object[] { size.x, size.y });
                yield return null; yield return null;
                foreach ((string state, int chapter, bool boss, bool stress) in new[]
                {
                    ("ch01-wave", 1, false, false), ("ch10-boss", 10, true, false), ("ch30-boss", 30, true, false),
                    ("ch43-wave", 43, false, false), ("ch50-boss", 50, true, false), ("stress60", 1, false, true)
                })
                {
                    UnityEngine.Random.InitState(6006);
                    FrontendPreferences.ReducedMotion = true;
                    session = new GameObject("Actor v3 acceptance").AddComponent<GameSessionController>();
                    session.ConfigurePresentation(Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog"));
                    session.SetCampaignLevel(chapter);
                    session.ConfigureProgress(50);
                    session.Initialize();
                    Time.timeScale = 0;
                    typeof(GameSessionController).GetMethod("EarnGold", Private).Invoke(session, new object[] { 20000 });
                    MethodInfo spawn = typeof(GameSessionController).GetMethod("SpawnEnemy", Private, null,
                        new[] { typeof(EnemyArchetype), typeof(int) }, null);
                    if (stress)
                    {
                        for (int i = 0; i < 10; i++)
                        {
                            foreach (EnemyArchetype archetype in StressMix) spawn.Invoke(session, new object[] { archetype, 0 });
                            session.AdvanceBattle(.6f);
                        }
                    }
                    else
                    {
                        foreach (int slot in new[] { 0, 1, 2, 3 })
                            session.TryBuildOutpost(slot, (OutpostKind)(slot % 3));
                        if (boss)
                            typeof(GameSessionController).GetField("currentWaveIndex", Private).SetValue(session, session.WaveCount - 1);
                        session.StartNextWave();
                        for (int i = 0; i < 80; i++) session.AdvanceBattle(.1f);
                        for (int i = 0; chapter == 43 && session.Enemies.Count < 8 && i < 300; i++) session.AdvanceBattle(.1f);
                        if (boss && !session.Enemies.Any(e => e.Archetype == EnemyArchetype.Boss))
                            spawn.Invoke(session, new object[] { EnemyArchetype.Boss, 0 });
                        for (int i = 0; boss && i < 40; i++) session.AdvanceBattle(.1f);
                    }
                    foreach (GreyboxEnemy enemy in session.Enemies.Where((_, index) => index % 2 == 0).ToArray())
                        if (enemy != null && enemy.Archetype != EnemyArchetype.Boss) enemy.TakeDamage(Mathf.Max(1, enemy.MaxHealth / 3));
                    session.GetComponentInChildren<DefenderHudController>().Refresh(session);
                    if (stress) Assert.That(session.Enemies.Count, Is.GreaterThanOrEqualTo(55), "stress state needs ~60 live enemies");
                    for (int i = 0; i < 12; i++) yield return null;
                    var sample = new Sample { state = state, width = Screen.width, height = Screen.height,
                        enemies = session.Enemies.Count, setPassCalls = int.MaxValue };
                    for (int trial = 0; trial < 3; trial++)
                    {
                        int draw = 0, setPass = 0, triangles = 0;
                        for (int i = 0; i < 5; i++)
                        {
                            yield return null;
                            draw = Mathf.Max(draw, UnityEditor.UnityStats.drawCalls);
                            setPass = Mathf.Max(setPass, UnityEditor.UnityStats.setPassCalls);
                            triangles = Mathf.Max(triangles, UnityEditor.UnityStats.triangles);
                        }
                        sample.setPassCalls = Mathf.Min(sample.setPassCalls, setPass);
                        sample.drawCalls = Mathf.Max(sample.drawCalls, draw);
                        sample.triangles = Mathf.Max(sample.triangles, triangles);
                    }
                    if (stress)
                    {
                        Time.timeScale = 1;
                        var frames = new List<float>();
                        for (int i = 0; i < 120; i++)
                        {
                            yield return null;
                            frames.Add(Time.unscaledDeltaTime * 1000f);
                        }
                        Time.timeScale = 0;
                        frames.Sort();
                        sample.medianFrameMs = frames[frames.Count / 2];
                        sample.p95FrameMs = frames[(int)(frames.Count * .95f)];
                    }
                    Assert.That(sample.drawCalls, Is.GreaterThan(0), "Rendering counters must be live");
                    Debug.Log($"ACTOR_V3_COUNTER phase={phase} state={state} size={size.x}x{size.y} enemies={sample.enemies} draw={sample.drawCalls} setPass={sample.setPassCalls} triangles={sample.triangles} medianMs={sample.medianFrameMs:F2} p95Ms={sample.p95FrameMs:F2}");
                    report.samples.Add(sample);
                    string path = $"Builds/actors-v3/{phase}-{state}-{size.x}x{size.y}.png";
                    ScreenCapture.CaptureScreenshot(path);
                    for (int i = 0; i < 5; i++) yield return null;
                    Assert.That(File.Exists(path), Is.True);
                    Object.Destroy(session.gameObject);
                    yield return null;
                }
            }
            File.WriteAllText($"Builds/actors-v3/{phase}-metrics.json", JsonUtility.ToJson(report, true));
#else
            yield return null;
#endif
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (session != null) Object.Destroy(session.gameObject);
            Time.timeScale = 1;
            UnityEngine.Random.state = randomState;
            FrontendPreferences.ReducedMotion = reducedMotion;
            review?.GetMethod("RestoreSize").Invoke(null, null);
            yield return null;
        }
    }
}
