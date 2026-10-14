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
    public sealed class BattlefieldLookTests
    {
        private GameSessionController session;
        private Type review;
        private UnityEngine.Random.State randomState;
        private bool reducedMotion;
        private readonly List<GameObject> isolatedPresentationRoots = new();
        private readonly List<GameObject> ownedPresentationLights = new();
#if UNITY_EDITOR
        private bool asyncCompilation;
#endif
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        [Serializable] private sealed class Sample
        {
            public string state;
            public int width, height, drawCalls, setPassCalls, triangles;
            public Vector3[] sceneryCenters;
            public Vector3 cameraPosition;
            public float cameraSize;
        }
        [Serializable] private sealed class Report { public List<Sample> samples = new(); }

        [Serializable] private sealed class BiomeSample
        {
            public int region, chapter, drawCalls, setPassCalls, triangles, materials, sceneryMeshes;
            public float drawCallsPercent, setPassCallsPercent, trianglesPercent;
            public int[] trialSetPassCalls;
        }
        [Serializable] private sealed class BiomeReport
        {
            public string status = "passed";
            public string comparison = "Same chapter-1 wave, camera, towers, enemies and route; only biome dressing and ambient profile change";
            public int allowedSetPassIncrease = 2;
            public string sampling = "Three identical reinstall trials per region, five-frame maxima per trial, minimum SetPass across trials; draw/triangle/material counts must match across trials";
            public List<BiomeSample> regions = new();
            public List<Sample> chapterCaptures = new();
        }

        [SetUp]
        public void Setup()
        {
            randomState = UnityEngine.Random.state;
            reducedMotion = FrontendPreferences.ReducedMotion;
#if UNITY_EDITOR
            asyncCompilation = UnityEditor.ShaderUtil.allowAsyncCompilation;
            UnityEditor.ShaderUtil.allowAsyncCompilation = false;
#endif
        }

        [UnityTest]
        public IEnumerator BiomeCapturesAndRenderingCounters()
        {
#if UNITY_EDITOR
            if (Environment.GetEnvironmentVariable("BIOME_CAPTURE") != "1") yield break;
            review = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("AllinDefense.Editor.FrontendExperienceReview")).First(t => t != null);
            review.GetMethod("SetSize").Invoke(null, new object[] { 1280, 720 });
            yield return null; yield return null;
            string phase = Environment.GetEnvironmentVariable("BIOME_CAPTURE_PHASE") ?? "after";
            var report = new Report();
            foreach (int chapter in phase == "before" ? new[] { 1 } : new[] { 1, 11, 21, 31, 41 })
            {
                UnityEngine.Random.InitState(6006);
                FrontendPreferences.ReducedMotion = true;
                session = new GameObject("Biome look acceptance").AddComponent<GameSessionController>();
                session.ConfigurePresentation(Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog"));
                session.SetCampaignLevel(chapter);
                session.Initialize();
                Time.timeScale = 0;
                session.TryBuildOutpost(0, OutpostKind.Archer);
                session.TryBuildOutpost(1, OutpostKind.Barracks);
                session.StartNextWave();
                for (int i = 0; i < (chapter == 1 ? 100 : 20); i++) session.AdvanceBattle(.1f);
                Assert.That(session.Enemies.Count, Is.GreaterThan(0), "Biome capture must show an active wave");
                yield return new WaitForSecondsRealtime(1f);
                session.GetComponentInChildren<DefenderHudController>().Refresh(session);
                for (int i = 0; i < 12; i++) yield return null;
                var dressing = session.GetComponentInChildren<DefenderBattlefieldDressing>();
                var sample = new Sample { state = $"ch{chapter:00}-wave", width = Screen.width, height = Screen.height,
                    cameraPosition = Camera.main.transform.position, cameraSize = Camera.main.orthographicSize,
                    sceneryCenters = dressing.SceneryFootprints.Select(b => b.center).ToArray() };
                for (int i = 0; i < 5; i++)
                {
                    yield return null;
                    sample.drawCalls = Mathf.Max(sample.drawCalls, UnityEditor.UnityStats.drawCalls);
                    sample.setPassCalls = Mathf.Max(sample.setPassCalls, UnityEditor.UnityStats.setPassCalls);
                    sample.triangles = Mathf.Max(sample.triangles, UnityEditor.UnityStats.triangles);
                }
                Assert.That(sample.drawCalls, Is.GreaterThan(0));
                report.samples.Add(sample);
                string path = $"Builds/biome-{phase}-ch{chapter:00}-wave-1280x720.png";
                ScreenCapture.CaptureScreenshot(path);
                for (int i = 0; i < 5; i++) yield return null;
                Assert.That(File.Exists(path), Is.True);
                Object.Destroy(session.gameObject);
                yield return null;
            }
            File.WriteAllText($"Builds/biome-{phase}-chapter-metrics.json", JsonUtility.ToJson(report, true));
#else
            yield return null;
#endif
        }

        [UnityTest]
        public IEnumerator BiomeProfilesKeepMaterialAndRenderBudgets()
        {
#if UNITY_EDITOR
            review = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("AllinDefense.Editor.FrontendExperienceReview")).First(t => t != null);
            review.GetMethod("SetSize").Invoke(null, new object[] { 1280, 720 });
            yield return null; yield return null;
#endif
            foreach (GameObject root in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)
                .Select(c => c.transform.root.gameObject)
                .Concat(Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Select(l => l.transform.root.gameObject))
                .Where(root => root.activeSelf).Distinct())
            {
                isolatedPresentationRoots.Add(root);
                root.SetActive(false);
            }
            yield return null;
            UnityEngine.Random.InitState(6006);
            FrontendPreferences.ReducedMotion = true;
            var catalog = Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog");
            session = new GameObject("Biome material and render budget").AddComponent<GameSessionController>();
            session.ConfigurePresentation(catalog);
            session.SetCampaignLevel(1);
            session.Initialize();
            ownedPresentationLights.AddRange(Object.FindObjectsByType<Light>(FindObjectsSortMode.None)
                .Where(l => !l.transform.IsChildOf(session.transform)).Select(l => l.gameObject));
            Time.timeScale = 0;
            session.TryBuildOutpost(0, OutpostKind.Archer);
            session.TryBuildOutpost(1, OutpostKind.Barracks);
            session.StartNextWave();
            for (int i = 0; i < 100; i++) session.AdvanceBattle(.1f);
            session.GetComponentInChildren<DefenderHudController>().Refresh(session);
            yield return new WaitForSecondsRealtime(1f);
            RuntimeCampaignLevel level = CampaignLevelFactory.Create(1);
            var report = new BiomeReport();
            var groundColors = new HashSet<Color>();
            for (int region = 0; region < 5; region++)
            {
                var sample = new BiomeSample { region = region + 1, chapter = 1,
                    setPassCalls = int.MaxValue, trialSetPassCalls = new int[3] };
                for (int trial = 0; trial < 3; trial++)
                {
                    var dressing = session.GetComponentInChildren<DefenderBattlefieldDressing>();
                    Transform parent = dressing.transform.parent;
                    Object.Destroy(dressing.gameObject);
                    yield return null;
                    dressing = DefenderBattlefieldDressing.Install(parent, catalog, Camera.main, level.path,
                        region, level.routes, level.outpostSlots);
                    DefenderSceneLook.Apply(Camera.main, region);
                    Renderer[] renderers = dressing.GetComponentsInChildren<Renderer>();
                    int materials = renderers.SelectMany(r => r.sharedMaterials).Distinct().Count();
                    int sceneryMeshes = renderers.Count(r => r.name.StartsWith("Grove "));
                    Color groundColor = renderers.Single(r => r.name == "Continuous Battlefield Ground").sharedMaterial.GetColor("_BaseColor");
                    Assert.That(groundColor, Is.EqualTo(BiomeLook.ForRegion(region).GroundColor));
                    if (trial == 0) Assert.That(groundColors.Add(groundColor), Is.True, "Every biome needs a distinct terrain tone");
                    Assert.That(sceneryMeshes, Is.EqualTo(2));
                    Assert.That(renderers.All(r => r.GetComponent<Collider>() == null), Is.True);
                    foreach (var mesh in dressing.GetComponentsInChildren<MeshFilter>().Where(f => f.name.StartsWith("Grove ")))
                        foreach (Vector3 vertex in mesh.sharedMesh.vertices)
                            Assert.That(dressing.SceneryFootprints.Any(b => { b.Expand(.0001f); return b.Contains(vertex); }),
                                Is.True, $"Region {region + 1}: decoration must stay inside its cleared footprint");
                    for (int i = 0; i < 12; i++) yield return null;
                    int drawCalls = 0, setPassCalls = 0, triangles = 0;
#if UNITY_EDITOR
                    for (int i = 0; i < 5; i++)
                    {
                        yield return null;
                        drawCalls = Mathf.Max(drawCalls, UnityEditor.UnityStats.drawCalls);
                        setPassCalls = Mathf.Max(setPassCalls, UnityEditor.UnityStats.setPassCalls);
                        triangles = Mathf.Max(triangles, UnityEditor.UnityStats.triangles);
                    }
#endif
                    if (trial > 0)
                    {
                        Assert.That(materials, Is.EqualTo(sample.materials));
                        Assert.That(sceneryMeshes, Is.EqualTo(sample.sceneryMeshes));
                        Assert.That(drawCalls, Is.EqualTo(sample.drawCalls));
                        Assert.That(triangles, Is.EqualTo(sample.triangles));
                    }
                    sample.materials = materials;
                    sample.sceneryMeshes = sceneryMeshes;
                    sample.drawCalls = drawCalls;
                    sample.triangles = triangles;
                    sample.trialSetPassCalls[trial] = setPassCalls;
                    sample.setPassCalls = Mathf.Min(sample.setPassCalls, setPassCalls);
                    Debug.Log($"BIOME_TRIAL region={region + 1} trial={trial + 1} draw={drawCalls} setPass={setPassCalls} triangles={triangles} materials={materials}");
                }
                Debug.Log($"BIOME_COUNTER region={region + 1} draw={sample.drawCalls} setPass={sample.setPassCalls} triangles={sample.triangles} materials={sample.materials}");
                if (region > 0)
                {
                    BiomeSample baseline = report.regions[0];
                    Assert.That(sample.materials, Is.EqualTo(baseline.materials));
#if UNITY_EDITOR
                    Assert.That(sample.drawCalls, Is.LessThanOrEqualTo(baseline.drawCalls), "Biome must not add draw calls");
                    Assert.That(sample.setPassCalls, Is.LessThanOrEqualTo(baseline.setPassCalls + 2), "Biome SetPass increase must stay inside the +2 acceptance limit");
                    sample.drawCallsPercent = 100f * (sample.drawCalls - baseline.drawCalls) / Mathf.Max(1, baseline.drawCalls);
                    sample.setPassCallsPercent = 100f * (sample.setPassCalls - baseline.setPassCalls) / Mathf.Max(1, baseline.setPassCalls);
                    sample.trianglesPercent = 100f * (sample.triangles - baseline.triangles) / Mathf.Max(1, baseline.triangles);
#endif
                }
                report.regions.Add(sample);
            }
#if UNITY_EDITOR
            if (Environment.GetEnvironmentVariable("BIOME_CAPTURE") == "1")
            {
                const string chapterPath = "Builds/biome-after-chapter-metrics.json";
                if (File.Exists(chapterPath)) report.chapterCaptures = JsonUtility.FromJson<Report>(File.ReadAllText(chapterPath)).samples;
                File.WriteAllText("Builds/biome-metrics.json", JsonUtility.ToJson(report, true));
            }
#endif
        }

        [UnityTest]
        public IEnumerator BattlefieldCapturesAndRenderingCountersAtAcceptanceRatios()
        {
#if UNITY_EDITOR
            review = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("AllinDefense.Editor.FrontendExperienceReview")).First(t => t != null);
            string phase = Environment.GetEnvironmentVariable("STEP6_CAPTURE_PHASE") ?? "after";
            bool environment = Environment.GetEnvironmentVariable("ENV_CAPTURE") == "1";
            string environmentPrefix = Environment.GetEnvironmentVariable("ENV_CAPTURE_PREFIX") ?? "env";
            bool battleUi = Environment.GetEnvironmentVariable("BATTLE_UI_CAPTURE") == "1";
            var report = new Report();
            Directory.CreateDirectory("Builds");
            foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(1560, 720), new Vector2Int(1280, 960) })
            {
                review.GetMethod("SetSize").Invoke(null, new object[] { size.x, size.y });
                yield return null; yield return null;
                foreach (string state in new[] { "chapter1-prep", "chapter1-wave", "chapter10-boss" })
                {
                    UnityEngine.Random.InitState(6006);
                    session = new GameObject("Battlefield look acceptance").AddComponent<GameSessionController>();
                    session.ConfigurePresentation(Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog"));
                    session.SetCampaignLevel(state == "chapter10-boss" ? 10 : 1);
                    session.Initialize();
                    Time.timeScale = 0;
                    if (state != "chapter1-prep")
                    {
                        session.TryBuildOutpost(0, OutpostKind.Archer);
                        session.TryBuildOutpost(1, OutpostKind.Barracks);
                        if (state == "chapter10-boss") typeof(GameSessionController).GetField("currentWaveIndex", Private).SetValue(session, session.WaveCount - 1);
                        session.StartNextWave();
                        for (int i = 0; i < 100; i++) session.AdvanceBattle(.1f);
                        if (state == "chapter10-boss" && !session.Enemies.Any(e => e.Archetype == EnemyArchetype.Boss))
                            typeof(GameSessionController).GetMethod("SpawnEnemy", Private, null, new[] { typeof(EnemyArchetype), typeof(int) }, null).Invoke(session, new object[] { EnemyArchetype.Boss, 0 });
                        Assert.That(session.Enemies.Count, Is.GreaterThan(0));
                        if (battleUi)
                        {
                            session.Enemies[0].TakeDamage(1);
                            typeof(GameSessionController).GetMethod("EnemyReachedKeep", Private).Invoke(session, new object[] { session.Enemies[0], 1 });
                        }
                    }
                    session.GetComponentInChildren<DefenderHudController>().Refresh(session);
                    for (int i = 0; i < 12; i++) yield return null;
                    Assert.That(Screen.width, Is.EqualTo(size.x));
                    Assert.That(Screen.height, Is.EqualTo(size.y));
                    var camera = Camera.main;
                    var sample = new Sample { state = state, width = Screen.width, height = Screen.height,
                        cameraPosition = camera.transform.position, cameraSize = camera.orthographicSize,
                        sceneryCenters = session.GetComponentInChildren<DefenderBattlefieldDressing>().SceneryFootprints.Select(b => b.center).ToArray() };
                    var dressing = session.GetComponentInChildren<DefenderBattlefieldDressing>();
                    Assert.That(dressing.SceneryFootprints.Count, Is.InRange(25, 40));
                    foreach (Bounds footprint in dressing.SceneryFootprints)
                        Assert.That(DefenderBattlefieldDressing.DecorationOverlapsHud(footprint, camera, dressing.KeepHudExclusion), Is.False);
                    var plate = session.GetComponentInChildren<DefenderKeepPlate>().Plate;
                    var plateCorners = new Vector3[4];
                    plate.GetWorldCorners(plateCorners);
                    var actualPlate = Rect.MinMaxRect(plateCorners[0].x / Screen.width, plateCorners[0].y / Screen.height,
                        plateCorners[2].x / Screen.width, plateCorners[2].y / Screen.height);
                    foreach (Bounds footprint in dressing.SceneryFootprints)
                        Assert.That(DefenderBattlefieldDressing.DecorationOverlapsHud(footprint, camera, actualPlate), Is.False,
                            "Scenery must also clear the actual rendered keep plate");
                    for (int frame = 0; frame < 5; frame++)
                    {
                        yield return null;
                        sample.drawCalls = Mathf.Max(sample.drawCalls, UnityEditor.UnityStats.drawCalls);
                        sample.setPassCalls = Mathf.Max(sample.setPassCalls, UnityEditor.UnityStats.setPassCalls);
                        sample.triangles = Mathf.Max(sample.triangles, UnityEditor.UnityStats.triangles);
                    }
                    Assert.That(sample.drawCalls, Is.GreaterThan(0), "Rendering counters must be live");
                    report.samples.Add(sample);
                    string path = environment ? $"Builds/{environmentPrefix}-{phase}-{state}-{size.x}x{size.y}.png" : battleUi ? $"Builds/battle-ui-{phase}-{state}-{size.x}x{size.y}.png" : $"Builds/step6-{phase}-{state}-{size.x}x{size.y}.png";
                    ScreenCapture.CaptureScreenshot(path);
                    for (int i = 0; i < 5; i++) yield return null;
                    Assert.That(File.Exists(path), Is.True);
                    if (Environment.GetEnvironmentVariable("ENV_COUNTER_DIAGNOSTIC") == "1" &&
                        state == "chapter1-wave" && size.x == 1280 && size.y == 960)
                    {
                        Canvas[] canvases = session.GetComponentsInChildren<Canvas>().Where(c => c.enabled).ToArray();
                        foreach (Canvas canvas in canvases) canvas.enabled = false;
                        for (int i = 0; i < 12; i++) yield return null;
                        int worldPasses = 0;
                        for (int i = 0; i < 5; i++) { yield return null; worldPasses = Mathf.Max(worldPasses, UnityEditor.UnityStats.setPassCalls); }
                        Renderer[] scenery = dressing.GetComponentsInChildren<Renderer>().Where(r => r.name.StartsWith("Grove ")).ToArray();
                        foreach (Renderer renderer in scenery) renderer.enabled = false;
                        for (int i = 0; i < 12; i++) yield return null;
                        int bareWorldPasses = 0;
                        for (int i = 0; i < 5; i++) { yield return null; bareWorldPasses = Mathf.Max(bareWorldPasses, UnityEditor.UnityStats.setPassCalls); }
                        Debug.Log($"ENV2_COUNTER_DIAGNOSTIC full={sample.setPassCalls} withoutHud={worldPasses} withoutHudAndGroves={bareWorldPasses}");
                        foreach (Renderer renderer in scenery) renderer.enabled = true;
                        foreach (Canvas canvas in canvases) canvas.enabled = true;
                    }
                    Object.Destroy(session.gameObject);
                    yield return null;
                }
            }
            File.WriteAllText(environment ? $"Builds/{environmentPrefix}-{phase}-metrics.json" : battleUi ? $"Builds/battle-ui-{phase}-metrics.json" : $"Builds/step6-{phase}-metrics.json", JsonUtility.ToJson(report, true));
#else
            yield return null;
#endif
        }

        [UnityTest]
        public IEnumerator SlotSelectionChangesOnlyItsGoldRingAndTerrainCoversViewport()
        {
            session = new GameObject("Battlefield material contract").AddComponent<GameSessionController>();
            session.ConfigurePresentation(Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog"));
            session.SetCampaignLevel(1);
            session.Initialize();
            yield return null;
            var slots = session.GetComponentsInChildren<GreyboxOutpostSlot>().OrderBy(slot => slot.Index).ToArray();
            var first = slots[0].transform.Find("SlotGoldRing").GetComponent<Renderer>();
            var second = slots[1].transform.Find("SlotGoldRing").GetComponent<Renderer>();
            Assert.That(first.sharedMaterial, Is.SameAs(second.sharedMaterial));
            slots[0].SetSelected(true);
            Assert.That(first.bounds.size.x, Is.GreaterThan(1.7f), "Selection halo must clear the occupied tower base");
            Assert.That(first.sharedMaterial.GetColor("_EmissionColor").maxColorComponent, Is.GreaterThan(1));
            Assert.That(second.sharedMaterial.GetColor("_EmissionColor"), Is.EqualTo(Color.black));
            slots[0].SetSelected(false);
            Assert.That(first.transform.localScale, Is.EqualTo(Vector3.one));
            Assert.That(first.sharedMaterial.GetColor("_EmissionColor"), Is.EqualTo(Color.black));
            Assert.That(slots[0].GetComponentsInChildren<Collider>().Length, Is.EqualTo(1));
            Assert.That(slots[0].GetComponent<DefenderSlotVisual>().ShowsBuildPlus, Is.True);
            Assert.That(session.TryBuildOutpost(0, OutpostKind.Archer), Is.True);
            Assert.That(slots[0].GetComponent<DefenderSlotVisual>().ShowsBuildPlus, Is.False);
            var ground = session.GetComponentsInChildren<Renderer>().Single(r => r.name == "Continuous Battlefield Ground");
            var texture = ground.sharedMaterial.GetTexture("_BaseMap");
            Assert.That(texture.width, Is.LessThanOrEqualTo(256));
            Assert.That(texture.height, Is.LessThanOrEqualTo(256));
            Assert.That(ground.GetComponent<Collider>(), Is.Null);
            var plane = new Plane(Vector3.up, ground.bounds.center);
            var camera = Camera.main;
            foreach (float aspect in new[] { 16f / 9f, 19.5f / 9f, 4f / 3f })
            {
                camera.aspect = aspect;
                camera.orthographicSize = AdaptiveBattlefieldCamera.CalculateOrthographicSize(new Vector2(720 * aspect, 720), new Rect(0, 0, 720 * aspect, 720));
                foreach (var corner in new[] { Vector3.zero, Vector3.right, Vector3.up, new Vector3(1, 1, 0) })
                {
                    var ray = camera.ViewportPointToRay(corner);
                    Assert.That(plane.Raycast(ray, out float distance), Is.True);
                    Vector3 point = ray.GetPoint(distance);
                    Assert.That(point.x, Is.InRange(ground.bounds.min.x, ground.bounds.max.x));
                    Assert.That(point.z, Is.InRange(ground.bounds.min.z, ground.bounds.max.z));
                }
            }
            camera.ResetAspect();
        }

        [UnityTest]
        public IEnumerator SceneryClustersAreDeterministicAndClearGameplayAndLandmarks()
        {
            foreach (int levelNumber in new[] { 1, 10, 14, 24, 34, 43 })
            {
                Vector3[][] previous = null;
                for (int repeat = 0; repeat < 2; repeat++)
                {
                    UnityEngine.Random.InitState(100 + repeat);
                    session = new GameObject("Scenery repeatability").AddComponent<GameSessionController>();
                    session.ConfigurePresentation(Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog"));
                    session.SetCampaignLevel(levelNumber);
                    session.Initialize();
                    yield return null;
                    RuntimeCampaignLevel level = CampaignLevelFactory.Create(levelNumber);
                    var dressing = session.GetComponentInChildren<DefenderBattlefieldDressing>();
                    var clusters = dressing.GetComponentsInChildren<MeshFilter>().Where(f => f.name.StartsWith("Grove ")).ToArray();
                    Assert.That(clusters.Length, Is.EqualTo(2), "Shared scenery palette and existing emissive material only");
                    Assert.That(dressing.SceneryFootprints.Count, Is.InRange(25, 40));
                    foreach (Bounds bounds in dressing.SceneryFootprints)
                    {
                        Assert.That(DefenderBattlefieldDressing.DecorationOverlapsGameplay(bounds, level.outpostSlots, level.routes), Is.False);
                        Assert.That(DefenderBattlefieldDressing.DecorationOverlapsLandmarks(bounds, level.path), Is.False);
                        Assert.That(DefenderBattlefieldDressing.DecorationOverlapsHud(bounds, Camera.main, dressing.KeepHudExclusion), Is.False);
                    }
                    Assert.That(dressing.SceneryFootprints.Select(b => Mathf.FloorToInt(b.center.z / 2)).Distinct().Count(),
                        Is.GreaterThanOrEqualTo(6), "Groves must fill the field instead of two rows");
                    foreach (var cluster in clusters)
                    {
                        Assert.That(cluster.GetComponent<Collider>(), Is.Null);
                        var outside = cluster.sharedMesh.vertices.Where(v => !dressing.SceneryFootprints.Any(b =>
                        {
                            b.Expand(.0001f);
                            return b.Contains(v);
                        })).ToArray();
                        Assert.That(outside.Length, Is.Zero,
                            cluster.name + " outside tested placement footprints: " + string.Join(", ", outside.Take(4)));
                    }
                    Assert.That(session.GetComponentsInChildren<Renderer>().Where(r => r.name.StartsWith("GateBase")).All(r => !r.enabled), Is.True);
                    Assert.That(session.GetComponentsInChildren<Renderer>().Where(r => r.name.StartsWith("Gate")).All(r =>
                        r.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.Off), Is.True);
                    Vector3[][] current = clusters.Select(f => f.sharedMesh.vertices).ToArray();
                    if (previous != null)
                    {
                        Assert.That(current.Length, Is.EqualTo(previous.Length));
                        for (int i = 0; i < current.Length; i++) CollectionAssert.AreEqual(previous[i], current[i]);
                    }
                    previous = current;
                    Object.Destroy(session.gameObject);
                    yield return null;
                }
            }
        }

        [UnityTest]
        public IEnumerator SlotGlowRespectsReducedMotionAndCalmsDuringWaves()
        {
            session = new GameObject("Slot glow states").AddComponent<GameSessionController>();
            session.ConfigurePresentation(Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog"));
            session.SetCampaignLevel(1);
            session.Initialize();
            FrontendPreferences.ReducedMotion = true;
            yield return null;
            var ring = session.GetComponentsInChildren<Renderer>().First(r => r.name == "SlotGoldRing");
            var properties = new MaterialPropertyBlock();
            ring.GetPropertyBlock(properties);
            Color preparation = properties.GetColor("_EmissionColor");
            yield return new WaitForSecondsRealtime(.05f);
            ring.GetPropertyBlock(properties);
            Assert.That(properties.GetColor("_EmissionColor"), Is.EqualTo(preparation));
            FrontendPreferences.ReducedMotion = false;
            session.StartNextWave();
            yield return null;
            ring.GetPropertyBlock(properties);
            Color wave = properties.GetColor("_EmissionColor");
            Assert.That(wave.maxColorComponent, Is.LessThan(preparation.maxColorComponent));
            yield return new WaitForSecondsRealtime(.05f);
            ring.GetPropertyBlock(properties);
            Assert.That(properties.GetColor("_EmissionColor"), Is.EqualTo(wave));
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (session != null) Object.Destroy(session.gameObject);
            foreach (GameObject light in ownedPresentationLights)
                if (light != null) Object.Destroy(light);
            ownedPresentationLights.Clear();
            foreach (GameObject root in isolatedPresentationRoots)
                if (root != null) root.SetActive(true);
            isolatedPresentationRoots.Clear();
#if UNITY_EDITOR
            UnityEditor.ShaderUtil.allowAsyncCompilation = asyncCompilation;
#endif
            Time.timeScale = 1;
            UnityEngine.Random.state = randomState;
            FrontendPreferences.ReducedMotion = reducedMotion;
            review?.GetMethod("RestoreSize").Invoke(null, null);
            yield return null;
        }
    }
}
