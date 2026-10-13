using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using AllinDefense.Core;
using AllinDefense.Data;
using AllinDefense.Presentation;
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
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        [Serializable] private sealed class Sample
        {
            public string state;
            public int width, height, drawCalls, setPassCalls, triangles;
            public Vector3 cameraPosition;
            public float cameraSize;
        }
        [Serializable] private sealed class Report { public List<Sample> samples = new(); }

        [SetUp]
        public void Setup() => randomState = UnityEngine.Random.state;

        [UnityTest]
        public IEnumerator BattlefieldCapturesAndRenderingCountersAtAcceptanceRatios()
        {
#if UNITY_EDITOR
            review = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("AllinDefense.Editor.FrontendExperienceReview")).First(t => t != null);
            string phase = Environment.GetEnvironmentVariable("STEP6_CAPTURE_PHASE") ?? "after";
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
                    }
                    session.GetComponentInChildren<DefenderHudController>().Refresh(session);
                    for (int i = 0; i < 12; i++) yield return null;
                    Assert.That(Screen.width, Is.EqualTo(size.x));
                    Assert.That(Screen.height, Is.EqualTo(size.y));
                    var camera = Camera.main;
                    var sample = new Sample { state = state, width = Screen.width, height = Screen.height,
                        cameraPosition = camera.transform.position, cameraSize = camera.orthographicSize };
                    for (int frame = 0; frame < 5; frame++)
                    {
                        yield return null;
                        sample.drawCalls = Mathf.Max(sample.drawCalls, UnityEditor.UnityStats.drawCalls);
                        sample.setPassCalls = Mathf.Max(sample.setPassCalls, UnityEditor.UnityStats.setPassCalls);
                        sample.triangles = Mathf.Max(sample.triangles, UnityEditor.UnityStats.triangles);
                    }
                    Assert.That(sample.drawCalls, Is.GreaterThan(0), "Rendering counters must be live");
                    report.samples.Add(sample);
                    string path = $"Builds/step6-{phase}-{state}-{size.x}x{size.y}.png";
                    ScreenCapture.CaptureScreenshot(path);
                    for (int i = 0; i < 5; i++) yield return null;
                    Assert.That(File.Exists(path), Is.True);
                    Object.Destroy(session.gameObject);
                    yield return null;
                }
            }
            File.WriteAllText($"Builds/step6-{phase}-metrics.json", JsonUtility.ToJson(report, true));
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

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (session != null) Object.Destroy(session.gameObject);
            Time.timeScale = 1;
            UnityEngine.Random.state = randomState;
            review?.GetMethod("RestoreSize").Invoke(null, null);
            yield return null;
        }
    }
}
