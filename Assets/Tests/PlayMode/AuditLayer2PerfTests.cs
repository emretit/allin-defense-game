using System;
using System.Collections;
using System.Collections.Generic;
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
    public sealed class AuditLayer2PerfTests
    {
        private readonly List<Object> owned = new();
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        private T Own<T>(T value) where T : Object { owned.Add(value); return value; }
        private static T Field<T>(object target, string name) =>
            (T)target.GetType().GetField(name, Private).GetValue(target);
        private static T Method<T>(object target, string name) where T : Delegate =>
            (T)target.GetType().GetMethod(name, Private).CreateDelegate(typeof(T), target);

        [UnityTest]
        public IEnumerator AudioCueSelectionAndRecordReuseDoNotAllocate()
        {
            var catalog = Own(ScriptableObject.CreateInstance<DefenderPresentationCatalog>());
            var clip = Own(AudioClip.Create("Perf cue", 44100 * 10, 1, 44100, false));
            catalog.audioCues = new[] { new AudioCueEntry { cue = PresentationCue.EnemyHit, clip = clip, concurrency = 8 } };
            var controller = Own(new GameObject("Perf audio")).AddComponent<DefenderAudioController>();
            controller.Initialize(catalog);
            yield return null;
            var sources = Field<List<AudioSource>>(controller, "sfxSources");
            var play = Method<Action<PresentationCue, PresentationAudioCueSpec>>(controller, "PlayNow");
            var spec = new PresentationAudioCueSpec("Test", 8, PresentationAudioPriority.Normal);
            void Cycle()
            {
                foreach (var source in sources) source.Stop();
                play(PresentationCue.EnemyHit, spec);
            }
            Cycle();
            Assert.That((TestDelegate)Cycle, UnityEngine.TestTools.Constraints.ConstraintExtensions.AllocatingGCMemory(Is.Not),
                "Warmed cue lookup, finished cleanup, selection and voice registration");
            Assert.That(controller.ActiveVoiceCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator AudioGroupStealingPreservesPriorityAgeAndTieOrder()
        {
            var catalog = Own(ScriptableObject.CreateInstance<DefenderPresentationCatalog>());
            for (int i = 0; i < 4; i++)
            {
                var entry = new AudioCueEntry { cue = (PresentationCue)i,
                    clip = Own(AudioClip.Create("Perf priority " + i, 44100 * 10, 1, 44100, false)), concurrency = 8 };
                Array.Resize(ref catalog.audioCues, i + 1);
                catalog.audioCues[i] = entry;
            }
            var controller = Own(new GameObject("Perf priorities")).AddComponent<DefenderAudioController>();
            controller.Initialize(catalog);
            yield return null;
            var play = Method<Action<PresentationCue, PresentationAudioCueSpec>>(controller, "PlayNow");
            var sources = Field<List<AudioSource>>(controller, "sfxSources");
            var normal = new PresentationAudioCueSpec("Group", 2, PresentationAudioPriority.Normal);
            play((PresentationCue)0, normal);
            play((PresentationCue)1, normal);
            play((PresentationCue)2, normal);
            Assert.That(sources[0].clip, Is.SameAs(catalog.audioCues[0].clip));
            Assert.That(sources[1].clip, Is.SameAs(catalog.audioCues[1].clip));
            play((PresentationCue)2, new PresentationAudioCueSpec("Group", 2, PresentationAudioPriority.High));
            Assert.That(sources[0].clip, Is.SameAs(catalog.audioCues[2].clip), "Equal priority/age uses dictionary insertion order");
            play((PresentationCue)3, new PresentationAudioCueSpec("Group", 2, PresentationAudioPriority.High));
            Assert.That(sources[1].clip, Is.SameAs(catalog.audioCues[3].clip), "Lower priority wins even when inserted later");
        }

        [UnityTest]
        public IEnumerator AudioGlobalStealingAndClipLimitsPreserveOrderWithoutAllocating()
        {
            var catalog = Own(ScriptableObject.CreateInstance<DefenderPresentationCatalog>());
            catalog.audioCues = new AudioCueEntry[13];
            for (int i = 0; i < catalog.audioCues.Length; i++)
                catalog.audioCues[i] = new AudioCueEntry { cue = (PresentationCue)i, concurrency = 1,
                    clip = Own(AudioClip.Create("Global cue " + i, 44100 * 10, 1, 44100, false)) };
            var controller = Own(new GameObject("Perf global audio")).AddComponent<DefenderAudioController>();
            controller.Initialize(catalog);
            yield return null;
            var play = Method<Action<PresentationCue, PresentationAudioCueSpec>>(controller, "PlayNow");
            var sources = Field<List<AudioSource>>(controller, "sfxSources");
            for (int i = 0; i < 10; i++)
                play((PresentationCue)i, new PresentationAudioCueSpec("Group " + i, 4,
                    i < 8 ? PresentationAudioPriority.Normal : PresentationAudioPriority.Ambient));
            var voices = Field<IDictionary>(controller, "activeVoices");
            foreach (AudioSource source in sources)
                voices[source].GetType().GetField("startedAt").SetValue(voices[source], 0f);
            var incoming = new PresentationAudioCueSpec("Incoming", 4, PresentationAudioPriority.Normal);
            Assert.That((TestDelegate)(() => play((PresentationCue)10, incoming)),
                UnityEngine.TestTools.Constraints.ConstraintExtensions.AllocatingGCMemory(Is.Not));
            Assert.That(sources[8].clip, Is.SameAs(catalog.audioCues[10].clip));
            Assert.That(sources[9].clip, Is.SameAs(catalog.audioCues[9].clip));
            play((PresentationCue)10, incoming); // Clip concurrency wins before any other selection.
            Assert.That(controller.ActiveVoiceCount, Is.EqualTo(10));
            Assert.That(sources[9].clip, Is.SameAs(catalog.audioCues[9].clip));
            play((PresentationCue)11, incoming);
            Assert.That(sources[9].clip, Is.SameAs(catalog.audioCues[11].clip));
            foreach (AudioSource source in sources)
                voices[source].GetType().GetField("priority").SetValue(voices[source], PresentationAudioPriority.Critical);
            Assert.That((TestDelegate)(() => play((PresentationCue)12, incoming)),
                UnityEngine.TestTools.Constraints.ConstraintExtensions.AllocatingGCMemory(Is.Not));
            foreach (AudioSource source in sources)
                Assert.That(source.clip, Is.Not.SameAs(catalog.audioCues[12].clip));
        }

        [UnityTest]
        public IEnumerator AudioSelectionIgnoresFinishedVoicesForClipAndGroupLimits()
        {
            var catalog = Own(ScriptableObject.CreateInstance<DefenderPresentationCatalog>());
            var clip = Own(AudioClip.Create("Finished cue", 44100 * 10, 1, 44100, false));
            catalog.audioCues = new[] { new AudioCueEntry { cue = PresentationCue.EnemyHit, clip = clip, concurrency = 1 } };
            var controller = Own(new GameObject("Perf finished audio")).AddComponent<DefenderAudioController>();
            controller.Initialize(catalog);
            yield return null;
            var play = Method<Action<PresentationCue, PresentationAudioCueSpec>>(controller, "PlayNow");
            var select = Method<Func<AudioCueEntry, PresentationAudioCueSpec, AudioSource>>(controller, "SelectVoice");
            var sources = Field<List<AudioSource>>(controller, "sfxSources");
            var spec = new PresentationAudioCueSpec("Finished", 1, PresentationAudioPriority.Normal);
            play(PresentationCue.EnemyHit, spec);
            sources[0].Stop(); // A voice can finish between the cleanup and selection queries.
            Assert.That(select(catalog.audioCues[0], spec), Is.SameAs(sources[0]));
        }

        private static Renderer VisibleRenderer(GameObject actor)
        {
            foreach (var renderer in actor.GetComponentsInChildren<Renderer>(true))
                if (renderer.enabled && renderer.sharedMaterial != null) return renderer;
            return null;
        }

        private GameSessionController BareSession() =>
            Own(new GameObject("Perf session")).AddComponent<GameSessionController>();

        private GreyboxEnemy Enemy(GameSessionController session, Vector3 position, int health = 100)
        {
            var enemy = Own(new GameObject("Perf enemy")).AddComponent<GreyboxEnemy>();
            enemy.transform.position = position;
            enemy.Configure(session, EnemyArchetype.Raider, new EnemyStats(health, 1f, 0, 1, 1, Color.red),
                new[] { Vector3.zero, Vector3.forward * 20 });
            Field<List<GreyboxEnemy>>(session, "enemies").Add(enemy);
            return enemy;
        }

        private delegate void DamageArea(Vector3 position, float radius, int damage, float slowFactor,
            float slowDuration, AllinDefense.Combat.DamageType damageType);

        [Test]
        public void AreaDamageSnapshotDoesNotAllocateAndPreservesDeathsAndOrder()
        {
            var session = BareSession();
            var first = Enemy(session, Vector3.zero);
            var second = Enemy(session, Vector3.right);
            var outside = Enemy(session, Vector3.right * 5);
            var damage = Method<DamageArea>(session, "DamageEnemies");
            void Hit() => damage(Vector3.zero, 2f, 1, .5f, 3f, AllinDefense.Combat.DamageType.Magic);
            Hit();
            Assert.That((TestDelegate)Hit,
                UnityEngine.TestTools.Constraints.ConstraintExtensions.AllocatingGCMemory(Is.Not));
            Assert.That(first.Health, Is.EqualTo(98));
            Assert.That(second.Health, Is.EqualTo(98));
            Assert.That(outside.Health, Is.EqualTo(100));
            Assert.That(second.SlowFactor, Is.EqualTo(.5f));
            damage(Vector3.zero, 2f, 100, 1f, 0f, AllinDefense.Combat.DamageType.Magic);
            Assert.That(first.IsAlive, Is.False);
            Assert.That(second.IsAlive, Is.False);
            Assert.That(session.Enemies.Count, Is.EqualTo(1), "Removing the first victim must not skip the next");
            Assert.That(session.Enemies[0], Is.SameAs(outside));
        }

        [Test]
        public void NestedAreaDamageKeepsIndependentSnapshotsAndExcludesLaterSpawns()
        {
            var session = BareSession();
            session.ConfigurePresentation(Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog"));
            var first = Enemy(session, Vector3.zero, 1);
            var second = Enemy(session, Vector3.right, 50);
            GreyboxEnemy added = null;
            var damage = Method<DamageArea>(session, "DamageEnemies");
            session.PresentationSignaled += signal =>
            {
                if (signal.cue != PresentationCue.EnemyDefeated || signal.source != first.gameObject) return;
                added = Enemy(session, Vector3.right, 100);
                damage(Vector3.zero, 2f, 2, 1f, 0f, AllinDefense.Combat.DamageType.Magic);
            };
            damage(Vector3.zero, 2f, 10, 1f, 0f, AllinDefense.Combat.DamageType.Magic);
            Assert.That(first.IsAlive, Is.False);
            Assert.That(second.Health, Is.EqualTo(38), "Nested and outer hits must each apply once");
            Assert.That(added.Health, Is.EqualTo(98), "Only the nested snapshot includes the later spawn");
            Assert.That(session.Enemies.Count, Is.EqualTo(2));
            Assert.That(Field<int>(session, "damageSnapshotDepth"), Is.Zero);
        }

        [Test]
        public void OutpostDisruptionUsesPropertyBlockWithoutMaterialInstances()
        {
            var session = BareSession();
            var material = Own(new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "Perf material" });
            material.color = new Color(.7f, .4f, .2f);
            for (int cycle = 0; cycle < 3; cycle++)
            {
                var go = Own(GameObject.CreatePrimitive(PrimitiveType.Cube));
                var renderer = go.GetComponent<Renderer>();
                renderer.sharedMaterial = material;
                var outpost = go.AddComponent<GreyboxOutpost>();
                outpost.Configure(session, null, OutpostKind.Archer, new OutpostStats(1, 1, 1, 1f, 1f));
                outpost.DisableTemporarily(.5f);
                Assert.That(renderer.sharedMaterial, Is.SameAs(material));
                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block, 0);
                Assert.That(block.GetColor("_BaseColor"), Is.EqualTo(new Color(.25f, .25f, .25f)));
                Method<Action<float>>(outpost, "Tick")(.5f);
                renderer.GetPropertyBlock(block, 0);
                Assert.That(block.GetColor("_BaseColor"), Is.EqualTo(material.color));
                Assert.That(renderer.sharedMaterial, Is.SameAs(material));
                outpost.Sell();
            }
            Assert.That(material.color, Is.EqualTo(new Color(.7f, .4f, .2f)));
        }

        [Test]
        public void IntermissionLabelUpdatesOnlyWhenDisplayedSecondChanges()
        {
            var session = BareSession();
            var label = Own(new GameObject("Perf label", typeof(RectTransform))).AddComponent<UnityEngine.UI.Text>();
            typeof(GameSessionController).GetField("nextWaveButtonLabel", Private).SetValue(session, label);
            var remaining = typeof(GameSessionController).GetField("intermissionRemaining", Private);
            var update = Method<Action>(session, "RefreshIntermissionLabel");
            remaining.SetValue(session, 7.9f);
            update();
            string text = label.text;
            remaining.SetValue(session, 7.1f);
            Assert.That((TestDelegate)(() => update()),
                UnityEngine.TestTools.Constraints.ConstraintExtensions.AllocatingGCMemory(Is.Not));
            Assert.That(label.text, Is.SameAs(text));
            remaining.SetValue(session, 7f);
            update();
            Assert.That(label.text, Does.EndWith("7"));
        }

        [UnityTest]
        public IEnumerator VfxMeshesAreReusedOnInitializationAndDestroyedWithPool()
        {
            var catalog = Own(ScriptableObject.CreateInstance<DefenderPresentationCatalog>());
            var host = Own(new GameObject("Perf VFX"));
            var pool = host.AddComponent<DefenderVfxPool>();
            pool.Initialize(catalog, 1);
            var diamond = Field<Mesh>(pool, "diamondMesh");
            var ring = Field<Mesh>(pool, "ringMesh");
            pool.Initialize(catalog, 1);
            Assert.That(Field<Mesh>(pool, "diamondMesh"), Is.SameAs(diamond));
            Assert.That(Field<Mesh>(pool, "ringMesh"), Is.SameAs(ring));
            Object.Destroy(host);
            yield return null;
            yield return null;
            Assert.That(diamond == null, Is.True);
            Assert.That(ring == null, Is.True);
        }

        private GameSessionController ProductionSession(Vector3 position = default)
        {
            var session = BareSession();
            session.transform.position = position;
            session.ConfigurePresentation(Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog"));
            session.SetCampaignLevel(14);
            session.Initialize();
            Method<Action<GamePhase>>(session, "SetPhase")(GamePhase.Paused);
            return session;
        }

        [UnityTest]
        public IEnumerator EveryEnemyArchetypeReusesItsInstanceAndResetsAllState()
        {
            var session = ProductionSession();
            Assert.That(session.TryBuildOutpost(0, OutpostKind.Barracks), Is.False, "Paused build remains disallowed");
            Method<Action<GamePhase>>(session, "SetPhase")(GamePhase.Preparing);
            Assert.That(session.TryBuildOutpost(0, OutpostKind.Barracks), Is.True);
            Method<Action<GamePhase>>(session, "SetPhase")(GamePhase.Paused);
            var guard = session.Outposts[0].Soldiers[0];
            var spawn = (Action<EnemyArchetype, int>)typeof(GameSessionController)
                .GetMethod("SpawnEnemy", Private, null, new[] { typeof(EnemyArchetype), typeof(int) }, null)
                .CreateDelegate(typeof(Action<EnemyArchetype, int>), session);
            for (int kind = 0; kind < 6; kind++)
            {
                spawn((EnemyArchetype)kind, 0);
                var enemy = session.Enemies[0];
                int fullHealth = enemy.Health;
                int childCount = enemy.GetComponentsInChildren<Transform>(true).Length;
                var renderer = VisibleRenderer(enemy.gameObject);
                Material shared = renderer.sharedMaterial;
                var canvas = enemy.GetComponentInChildren<Canvas>();
                var fill = Field<Transform>(enemy, "healthFill");
                var animator = enemy.GetComponentInChildren<Animator>();
                enemy.TakeDamage(1);
                enemy.ApplySlow(.5f, 20f);
                enemy.Stun(20f);
                foreach (string field in new[] { "attackTimer", "supportTimer", "siegeTimer", "regenerationTimer", "siegeWindup" })
                    typeof(GreyboxEnemy).GetField(field, Private).SetValue(enemy, 13f);
                typeof(GreyboxEnemy).GetField("targetWaypoint", Private).SetValue(enemy, 99);
                typeof(GreyboxEnemy).GetField("bossPhase", Private).SetValue(enemy, 3);
                typeof(GreyboxEnemy).GetField("siegeTarget", Private).SetValue(enemy, session.Outposts[0]);
                var phase = enemy.GetComponent<BossPhaseVisual>();
                if (phase != null) phase.SetPhase(3);
                var block = new MaterialPropertyBlock();
                block.SetColor("_BaseColor", Color.cyan);
                renderer.SetPropertyBlock(block);
                renderer.SetPropertyBlock(block, 0);
                canvas.enabled = false;
                fill.gameObject.GetComponent<UnityEngine.UI.Image>().color = Color.cyan;
                animator.SetTrigger(PresentationActorAnimationContract.DefeatState);
                animator.speed = 0f;
                animator.enabled = false;
                enemy.TakeDamage(int.MaxValue, AllinDefense.Combat.DamageType.Magic);
                Assert.That(enemy.IsAlive, Is.False);
                yield return null;
                yield return null;
                Assert.That(enemy != null, Is.True, "Retired actor must remain owned by the session pool");
                typeof(GreyboxFriendlyUnit).GetField("target", Private).SetValue(guard, enemy);
                spawn((EnemyArchetype)kind, 1);
                Assert.That(session.Enemies[0], Is.SameAs(enemy));
                Assert.That(enemy.Health, Is.EqualTo(fullHealth));
                Assert.That(enemy.IsAlive, Is.True);
                Assert.That(enemy.State, Is.EqualTo(EnemyCombatState.Moving));
                Assert.That(enemy.StunRemaining, Is.Zero);
                Assert.That(enemy.SlowFactor, Is.EqualTo(1f));
                Assert.That(Field<AllinDefense.Combat.SlowEffect>(enemy, "slow").Remaining, Is.Zero);
                Assert.That(Field<int>(enemy, "targetWaypoint"), Is.EqualTo(1));
                Assert.That(Field<int>(enemy, "bossPhase"), Is.EqualTo(1));
                foreach (string field in new[] { "attackTimer", "supportTimer", "siegeTimer", "regenerationTimer", "siegeWindup" })
                    Assert.That(Field<float>(enemy, field), Is.Zero, field);
                Assert.That(Field<GreyboxOutpost>(enemy, "siegeTarget") == null, Is.True);
                Assert.That(Field<GreyboxEnemy>(guard, "target") == null, Is.True);
                Assert.That(Field<IReadOnlyList<Vector3>>(enemy, "path"), Is.SameAs(session.Routes[1]));
                Assert.That(enemy.Position, Is.EqualTo(session.Routes[1][0]));
                Assert.That(enemy.GetComponentsInChildren<Transform>(true).Length, Is.EqualTo(childCount));
                Assert.That(canvas.enabled, Is.True);
                Assert.That(fill.localScale, Is.EqualTo(Vector3.one));
                Assert.That(fill.localPosition, Is.EqualTo(Vector3.zero));
                Assert.That(fill.GetComponent<UnityEngine.UI.Image>().color, Is.Not.EqualTo(Color.cyan));
                Assert.That(renderer.sharedMaterial, Is.SameAs(shared));
                renderer.GetPropertyBlock(block);
                Assert.That(block.GetColor("_BaseColor"), Is.Not.EqualTo(Color.cyan));
                renderer.GetPropertyBlock(block, 0);
                Assert.That(block.GetColor("_BaseColor"), Is.Not.EqualTo(Color.cyan));
                Assert.That(animator.enabled, Is.True);
                Assert.That(animator.speed, Is.EqualTo(1f));
                Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName(PresentationActorAnimationContract.IdleState), Is.True);
                if (phase != null) Assert.That(phase.CurrentPhase, Is.EqualTo(1));
                enemy.TakeDamage(int.MaxValue, AllinDefense.Combat.DamageType.Magic);
                yield return null;
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator DefeatEchoPoolReusesAndResetsVisualsWithoutAllocating()
        {
            var session = ProductionSession(new Vector3(5f, 0f, 2f));
            var director = session.GetComponentInChildren<DefenderPresentationDirector>();
            var pool = Field<Component>(director, "defeatEchoPool");
            var play = (Action<PresentationActorKind, Vector3>)pool.GetType().GetMethod("Play")
                .CreateDelegate(typeof(Action<PresentationActorKind, Vector3>), pool);
            play(PresentationActorKind.Raider, Vector3.right);
            DefeatEcho first = null;
            foreach (var echo in pool.GetComponentsInChildren<DefeatEcho>(true))
                if (echo.gameObject.activeSelf) { first = echo; break; }
            Assert.That(first, Is.Not.Null);
            int count = pool.GetComponentsInChildren<DefeatEcho>(true).Length;
            yield return new WaitForSeconds(.7f);
            Assert.That(first.gameObject.activeSelf, Is.False);
            var renderer = VisibleRenderer(first.gameObject);
            var block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", Color.cyan);
            renderer.SetPropertyBlock(block);
            first.GetComponent<Animator>().speed = 0f;
            Assert.That((TestDelegate)(() => play(PresentationActorKind.Raider, Vector3.forward)),
                UnityEngine.TestTools.Constraints.ConstraintExtensions.AllocatingGCMemory(Is.Not));
            Assert.That(first.gameObject.activeSelf, Is.True);
            Assert.That(first.transform.position, Is.EqualTo(Vector3.forward));
            Assert.That(Field<float>(first, "elapsed"), Is.Zero);
            Assert.That(first.GetComponent<Animator>().speed, Is.EqualTo(1f));
            renderer.GetPropertyBlock(block);
            Assert.That(block.GetColor("_BaseColor"), Is.Not.EqualTo(Color.cyan));
            Assert.That(pool.GetComponentsInChildren<DefeatEcho>(true).Length, Is.EqualTo(count));
            yield return null;
            Assert.That(first.transform.parent.position, Is.EqualTo(Vector3.forward), "Anchor retains death placement while the defeat animation moves its root");
            Object.Destroy(session.gameObject);
            yield return null;
            Assert.That(first == null, Is.True, "Session teardown owns inactive echoes too");
        }

        [UnityTest]
        public IEnumerator Chapter10ControlledEditorAllocationSample()
        {
            var session = BareSession();
            session.ConfigurePresentation(Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog"));
            session.SetCampaignLevel(10);
            session.Initialize();
            Method<Action<int>>(session, "EarnGold")(10000);
            for (int slot = 0; slot < session.SlotCount; slot++)
                Assert.That(session.TryBuildOutpost(slot, slot % 3 == 0 ? OutpostKind.Barracks : OutpostKind.Archer), Is.True);
            session.enabled = false;
            var lateUpdate = typeof(GameSessionController).GetMethod("LateUpdate", Private);
            Action recycle = lateUpdate != null ? (Action)lateUpdate.CreateDelegate(typeof(Action), session) : null;
            session.StartNextWave();
            for (int warm = 0; warm < 30; warm++)
            {
                session.AdvanceBattle(.1f);
                if (recycle != null) recycle();
                yield return null;
            }
            const int frames = 300;
            var samples = new long[frames];
            using (var recorder = Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Memory,
                "GC Allocated In Frame", 1))
            {
                Assert.That(recorder.Valid, Is.True);
                for (int frame = 0; frame < frames; frame++)
                {
                    session.AdvanceBattle(.1f);
                    if (recycle != null) recycle();
                    yield return null;
                    samples[frame] = recorder.LastValue;
                }
            }
            long total = 0;
            foreach (long sample in samples) total += sample;
            Array.Sort(samples);
            Debug.Log($"[K2_EDITOR_GC] level=10 frames={frames} simulatedStep=0.1 meanBytes={total / (double)frames:F1} p95Bytes={samples[(int)Math.Ceiling(frames * .95) - 1]} battleSeconds={session.BattleElapsedSeconds:F2} peakEnemies={session.PeakEnemyCount}");
        }

        [Test]
        public void OutpostTintAffectsOnlyThePrimaryMaterialAndPreservesOtherOverrides()
        {
            var session = BareSession();
            var primary = Own(new Material(Shader.Find("Universal Render Pipeline/Lit")));
            var secondary = Own(new Material(primary));
            primary.color = Color.red;
            secondary.color = Color.blue;
            var go = Own(GameObject.CreatePrimitive(PrimitiveType.Cube));
            var renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterials = new[] { primary, secondary };
            var block = new MaterialPropertyBlock();
            block.SetFloat("_Smoothness", .3f);
            renderer.SetPropertyBlock(block);
            var outpost = go.AddComponent<GreyboxOutpost>();
            outpost.Configure(session, null, OutpostKind.Archer, new OutpostStats(1, 1, 1, 1f, 1f));
            outpost.DisableTemporarily(.5f);
            renderer.GetPropertyBlock(block, 0);
            Assert.That(block.GetColor("_BaseColor"), Is.EqualTo(new Color(.25f, .25f, .25f)));
            Assert.That(block.GetFloat("_Smoothness"), Is.EqualTo(.3f));
            renderer.GetPropertyBlock(block, 1);
            Assert.That(block.HasColor("_BaseColor"), Is.False);
            renderer.GetPropertyBlock(block);
            Assert.That(block.HasColor("_BaseColor"), Is.False, "Global block must not tint secondary slots");
            Assert.That(block.GetFloat("_Smoothness"), Is.EqualTo(.3f));
            Method<Action<float>>(outpost, "Tick")(.5f);
            renderer.GetPropertyBlock(block, 0);
            Assert.That(block.GetColor("_BaseColor"), Is.EqualTo(Color.red));
            Assert.That(renderer.sharedMaterials[0], Is.SameAs(primary));
            Assert.That(renderer.sharedMaterials[1], Is.SameAs(secondary));
            Assert.That(secondary.color, Is.EqualTo(Color.blue));
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            for (int i = owned.Count - 1; i >= 0; i--)
                if (owned[i] != null) Object.Destroy(owned[i]);
            owned.Clear();
            Time.timeScale = 1f;
            yield return null;
        }
    }
}
