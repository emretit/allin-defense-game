using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DefenderChronicles.Core;
using DefenderChronicles.Data;
using DefenderChronicles.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace DefenderChronicles.Tests
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
            float slowDuration, DefenderChronicles.Combat.DamageType damageType);

        [Test]
        public void AreaDamageSnapshotDoesNotAllocateAndPreservesDeathsAndOrder()
        {
            var session = BareSession();
            var first = Enemy(session, Vector3.zero);
            var second = Enemy(session, Vector3.right);
            var outside = Enemy(session, Vector3.right * 5);
            var damage = Method<DamageArea>(session, "DamageEnemies");
            void Hit() => damage(Vector3.zero, 2f, 1, .5f, 3f, DefenderChronicles.Combat.DamageType.Magic);
            Hit();
            Assert.That((TestDelegate)Hit,
                UnityEngine.TestTools.Constraints.ConstraintExtensions.AllocatingGCMemory(Is.Not));
            Assert.That(first.Health, Is.EqualTo(98));
            Assert.That(second.Health, Is.EqualTo(98));
            Assert.That(outside.Health, Is.EqualTo(100));
            Assert.That(second.SlowFactor, Is.EqualTo(.5f));
            damage(Vector3.zero, 2f, 100, 1f, 0f, DefenderChronicles.Combat.DamageType.Magic);
            Assert.That(first.IsAlive, Is.False);
            Assert.That(second.IsAlive, Is.False);
            Assert.That(session.Enemies.Count, Is.EqualTo(1), "Removing the first victim must not skip the next");
            Assert.That(session.Enemies[0], Is.SameAs(outside));
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
