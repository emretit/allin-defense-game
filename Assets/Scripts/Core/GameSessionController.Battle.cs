using System;
using System.Collections.Generic;
using DefenderChronicles.Combat;
using DefenderChronicles.Data;
using UnityEngine;

namespace DefenderChronicles.Core
{
    public sealed partial class GameSessionController
    {
        private readonly List<ScheduledSpawn> scheduledSpawns = new();
        private int nextSpawn;
        private double stepAccumulator;
        private float waveElapsed;
        public float BattleElapsedSeconds { get; private set; }
        public float WaveElapsedSeconds => waveElapsed;
        public int AbilityUses { get; private set; }
        public int AbilityTargets { get; private set; }
        private readonly int[] leakedLives = new int[6];

        private readonly struct ScheduledSpawn
        {
            public readonly float time;
            public readonly EnemyArchetype archetype;
            public readonly int order;
            public ScheduledSpawn(float time, EnemyArchetype archetype, int order)
            { this.time = time; this.archetype = archetype; this.order = order; }
        }

        private void ScheduleWave(WaveDefinition wave)
        {
            waveElapsed = 0f;
            scheduledSpawns.Clear();
            nextSpawn = 0;
            int order = 0;
            foreach (var group in wave.groups)
            {
                if (group == null) continue;
                for (int i = 0; i < group.count; i++)
                    scheduledSpawns.Add(new ScheduledSpawn(group.startDelay + i * group.interval,
                        group.enemy, order++));
            }
            scheduledSpawns.Sort((a, b) => a.time == b.time ? a.order.CompareTo(b.order) : a.time.CompareTo(b.time));
        }

        // A single deterministic clock prevents lost attacks/spawns at 8x or on a 30 FPS device.
        public void AdvanceBattle(float elapsed)
        {
            if (!initialized || elapsed <= 0f || Phase is GamePhase.Paused or GamePhase.Victory or GamePhase.Defeat) return;
            stepAccumulator += elapsed;
            while (stepAccumulator + .0000001 >= BattleRules.StepSeconds)
            {
                stepAccumulator -= BattleRules.StepSeconds;
                if (Phase is GamePhase.Paused or GamePhase.Victory or GamePhase.Defeat) { stepAccumulator = 0; break; }
                float delta = BattleRules.StepSeconds;
                if (Phase == GamePhase.Intermission)
                {
                    intermissionRemaining = Mathf.Max(0f, intermissionRemaining - delta);
                    if (intermissionRemaining <= 0f) StartNextWave();
                }
                if (Phase == GamePhase.Wave)
                {
                    BattleElapsedSeconds += delta;
                    waveElapsed += delta;
                    while (nextSpawn < scheduledSpawns.Count && scheduledSpawns[nextSpawn].time <= waveElapsed)
                        SpawnEnemy(scheduledSpawns[nextSpawn++].archetype);
                    if (nextSpawn >= scheduledSpawns.Count) spawning = false;
                }
                for (int i = enemies.Count - 1; i >= 0; i--)
                    if (i < enemies.Count && enemies[i] != null) enemies[i].Tick(delta);
                for (int i = 0; i < outposts.Count; i++) if (outposts[i] != null) outposts[i].Tick(delta);
                for (int i = 0; i < friendlyUnits.Count; i++) if (friendlyUnits[i] != null) friendlyUnits[i].Tick(delta);
                if (hero != null) hero.Tick(delta);
                TryCompleteWave();
            }
        }

        internal void RecordAbility(int targets) { AbilityUses++; AbilityTargets += targets; }
        public EnemyArchetype MostDamagingLeak
        {
            get
            {
                int best = 0;
                for (int i = 1; i < leakedLives.Length; i++) if (leakedLives[i] > leakedLives[best]) best = i;
                return (EnemyArchetype)best;
            }
        }

        public int CountEnemies(Vector3 center, float radius)
        {
            int count = 0;
            foreach (var enemy in enemies)
                if (enemy != null && enemy.IsAlive && (enemy.Position - center).sqrMagnitude <= radius * radius) count++;
            return count;
        }

        internal void SpawnBossReinforcements()
        {
            for (int i = 0; i < 6; i++)
            {
                scheduledSpawns.Add(new ScheduledSpawn(waveElapsed + .5f + i * .6f, EnemyArchetype.Raider,
                    scheduledSpawns.Count));
                plannedThisWave++;
            }
            // Only pending entries may move: already spawned entries retain their indices.
            scheduledSpawns.Sort(nextSpawn, scheduledSpawns.Count - nextSpawn,
                Comparer<ScheduledSpawn>.Create((a, b) => a.time.CompareTo(b.time)));
            spawning = true;
        }
    }
}
