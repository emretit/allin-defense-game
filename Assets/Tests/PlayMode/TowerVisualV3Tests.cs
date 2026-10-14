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
    public sealed class TowerVisualV3Tests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private GameSessionController session;
        private Type review;
        private UnityEngine.Random.State randomState;
        private bool reducedMotion;

        [Serializable] private sealed class Sample
        {
            public string state;
            public int width, height, towers, drawCalls, setPassCalls, triangles;
            public int[] trialSetPassCalls;
        }
        [Serializable] private sealed class Report
        {
            public string sampling = "Same chapter, slots, tower kinds/levels/branches and wave; three five-frame windows per state, minimum SetPass across windows";
            public List<Sample> samples = new();
        }

        private static readonly (OutpostKind kind, int level, OutpostSpecialization branch)[] EarlyTowers =
        {
            (OutpostKind.Archer, 1, OutpostSpecialization.None), (OutpostKind.Barracks, 1, OutpostSpecialization.None),
            (OutpostKind.Mage, 1, OutpostSpecialization.None), (OutpostKind.Archer, 2, OutpostSpecialization.None),
            (OutpostKind.Barracks, 2, OutpostSpecialization.None), (OutpostKind.Mage, 2, OutpostSpecialization.None)
        };
        private static readonly (OutpostKind kind, int level, OutpostSpecialization branch)[] BranchTowersA =
        {
            (OutpostKind.Archer, 2, OutpostSpecialization.RapidFire), (OutpostKind.Barracks, 2, OutpostSpecialization.ShieldGuard),
            (OutpostKind.Mage, 2, OutpostSpecialization.Blast), (OutpostKind.Archer, 1, OutpostSpecialization.None),
            (OutpostKind.Barracks, 1, OutpostSpecialization.None), (OutpostKind.Mage, 1, OutpostSpecialization.None)
        };
        private static readonly (OutpostKind kind, int level, OutpostSpecialization branch)[] BranchTowersB =
        {
            (OutpostKind.Archer, 2, OutpostSpecialization.Sharpshooter), (OutpostKind.Barracks, 2, OutpostSpecialization.AssaultGuard),
            (OutpostKind.Mage, 2, OutpostSpecialization.Frost), (OutpostKind.Archer, 2, OutpostSpecialization.None),
            (OutpostKind.Barracks, 2, OutpostSpecialization.None), (OutpostKind.Mage, 2, OutpostSpecialization.None)
        };

        [SetUp]
        public void Setup()
        {
            randomState = UnityEngine.Random.state;
            reducedMotion = FrontendPreferences.ReducedMotion;
        }

        [UnityTest]
        public IEnumerator SpecializingSwapsToTheBranchModelOnTheSharedPalette()
        {
            var catalog = Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog");
            Material shared = catalog.towerPaletteMaterial.material;
            foreach (OutpostKind kind in new[] { OutpostKind.Archer, OutpostKind.Barracks, OutpostKind.Mage })
            {
                session = new GameObject("Tower v3 branch swap").AddComponent<GameSessionController>();
                session.ConfigurePresentation(catalog);
                session.SetCampaignLevel(10);
                session.ConfigureProgress(50);
                session.Initialize();
                Time.timeScale = 0;
                typeof(GameSessionController).GetMethod("EarnGold", Private).Invoke(session, new object[] { 5000 });
                Assert.That(session.TryBuildOutpost(0, kind), Is.True);
                Assert.That(session.TryUpgradeOutpost(0), Is.True);
                OutpostSpecialization branch = TowerProgression.Branch(kind, 1);
                Assert.That(session.TrySpecializeOutpost(0, branch), Is.True);
                yield return null;
                GreyboxOutpost tower = session.Outposts[0];
                MeshFilter[] meshes = tower.GetComponentsInChildren<MeshFilter>()
                    .Where(filter => filter.GetComponent<Renderer>() != null && filter.GetComponent<Renderer>().enabled).ToArray();
                Mesh expected = catalog.OutpostPrefab(kind, 2, branch).GetComponentInChildren<MeshFilter>().sharedMesh;
                Assert.That(meshes.Select(filter => filter.sharedMesh), Has.Member(expected), $"{branch} model");
                Assert.That(meshes.Select(filter => filter.sharedMesh),
                    Has.No.Member(catalog.OutpostPrefab(kind, 2).GetComponentInChildren<MeshFilter>().sharedMesh));
                Assert.That(tower.transform.GetComponentsInChildren<Transform>().Any(t => t.name == "Specialization crest"), Is.False);
                Assert.That(meshes.Single(filter => filter.sharedMesh == expected).GetComponent<Renderer>().sharedMaterial, Is.SameAs(shared));
                Object.Destroy(session.gameObject);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator TowerCapturesAndRenderingCounters()
        {
#if UNITY_EDITOR
            if (Environment.GetEnvironmentVariable("TOWER_V3_CAPTURE") != "1") yield break;
            string phase = Environment.GetEnvironmentVariable("TOWER_V3_PHASE") ?? "after";
            review = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("AllinDefense.Editor.FrontendExperienceReview")).First(t => t != null);
            Directory.CreateDirectory("Builds/towers-v3");
            var report = new Report();
            foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(1280, 960) })
            {
                review.GetMethod("SetSize").Invoke(null, new object[] { size.x, size.y });
                yield return null; yield return null;
                foreach ((string state, int chapter, bool wave, var towers) in new[]
                {
                    ("ch01-prep", 1, false, EarlyTowers), ("ch01-wave", 1, true, EarlyTowers),
                    ("ch10-boss", 10, true, BranchTowersA), ("ch43-wave", 43, true, BranchTowersB)
                })
                {
                    UnityEngine.Random.InitState(6006);
                    FrontendPreferences.ReducedMotion = true;
                    session = new GameObject("Tower v3 acceptance").AddComponent<GameSessionController>();
                    session.ConfigurePresentation(Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog"));
                    session.SetCampaignLevel(chapter);
                    session.ConfigureProgress(50);
                    session.Initialize();
                    Time.timeScale = 0;
                    typeof(GameSessionController).GetMethod("EarnGold", Private).Invoke(session, new object[] { 20000 });
                    int built = 0;
                    for (int slot = 0; slot < towers.Length; slot++)
                    {
                        var (kind, level, branch) = towers[slot];
                        if (!session.TryBuildOutpost(slot, kind)) continue;
                        if (level > 1) Assert.That(session.TryUpgradeOutpost(slot), Is.True, $"{state} upgrade {kind}");
                        if (branch != OutpostSpecialization.None)
                            Assert.That(session.TrySpecializeOutpost(slot, branch), Is.True, $"{state} {branch}");
                        built++;
                    }
                    Assert.That(built, Is.GreaterThanOrEqualTo(5), $"{state} must show the tower set");
                    if (wave)
                    {
                        if (state == "ch10-boss")
                            typeof(GameSessionController).GetField("currentWaveIndex", Private).SetValue(session, session.WaveCount - 1);
                        session.StartNextWave();
                        for (int i = 0; i < 100; i++) session.AdvanceBattle(.1f);
                        if (state == "ch10-boss" && !session.Enemies.Any(e => e.Archetype == EnemyArchetype.Boss))
                            typeof(GameSessionController).GetMethod("SpawnEnemy", Private, null, new[] { typeof(EnemyArchetype), typeof(int) }, null)
                                .Invoke(session, new object[] { EnemyArchetype.Boss, 0 });
                    }
                    session.GetComponentInChildren<DefenderHudController>().Refresh(session);
                    for (int i = 0; i < 12; i++) yield return null;
                    var sample = new Sample { state = state, width = Screen.width, height = Screen.height, towers = built,
                        setPassCalls = int.MaxValue, trialSetPassCalls = new int[3] };
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
                        sample.trialSetPassCalls[trial] = setPass;
                        sample.setPassCalls = Mathf.Min(sample.setPassCalls, setPass);
                        sample.drawCalls = Mathf.Max(sample.drawCalls, draw);
                        sample.triangles = Mathf.Max(sample.triangles, triangles);
                    }
                    Assert.That(sample.drawCalls, Is.GreaterThan(0), "Rendering counters must be live");
                    Debug.Log($"TOWER_V3_COUNTER phase={phase} state={state} size={size.x}x{size.y} towers={built} draw={sample.drawCalls} setPass={sample.setPassCalls} triangles={sample.triangles}");
                    report.samples.Add(sample);
                    string path = $"Builds/towers-v3/{phase}-{state}-{size.x}x{size.y}.png";
                    ScreenCapture.CaptureScreenshot(path);
                    for (int i = 0; i < 5; i++) yield return null;
                    Assert.That(File.Exists(path), Is.True);
                    Object.Destroy(session.gameObject);
                    yield return null;
                }
            }
            File.WriteAllText($"Builds/towers-v3/{phase}-metrics.json", JsonUtility.ToJson(report, true));
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
