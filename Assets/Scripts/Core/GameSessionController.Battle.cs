using System;
using System.Collections.Generic;
using AllinDefense.Combat;
using AllinDefense.Data;
using AllinDefense.Presentation;
using UnityEngine;

namespace AllinDefense.Core
{
    public sealed partial class GameSessionController
    {
        public bool NextWaveHasBoss => waves != null && currentWaveIndex < waves.Length && waves[currentWaveIndex].bossWave;
        public bool CanBuildOnSelection => CanChangeDefence && selectedSlot != null && selectedSlot.Occupant == null;
        internal void RecordSaveFailure() => telemetry.Record(this,"save_error","local_write_failed");
        private int unlockedCampaignLevel;
        public int UnlockedCampaignLevel => unlockedCampaignLevel > 0 ? unlockedCampaignLevel : CampaignLevelNumber;
        public void ConfigureProgress(int unlockedLevel) => unlockedCampaignLevel = Mathf.Clamp(unlockedLevel, 1, 50);
        public bool CanBuild(OutpostKind kind) => TowerProgression.CanBuild(kind, UnlockedCampaignLevel);
        private bool CanChangeDefence => initialized && Phase is GamePhase.Preparing or GamePhase.Intermission or GamePhase.Wave;

        public bool TrySpecializeOutpost(int slotIndex, OutpostSpecialization branch)
        {
            if (!CanChangeDefence || slotIndex < 0 || slotIndex >= slots.Count) return false;
            var outpost = slots[slotIndex]?.Occupant;
            if (outpost == null || !outpost.CanSpecialize || !TowerProgression.Matches(outpost.Kind, branch) ||
                UnlockedCampaignLevel < TowerProgression.UnlockLevel(outpost.Kind) || Gold < TowerProgression.Cost(outpost.Kind)) return false;
            if (!outpost.Specialize(branch)) return false;
            SpendGold(TowerProgression.Cost(outpost.Kind));
            telemetry.Record(this,"specialize",branch.ToString(),TowerProgression.Cost(outpost.Kind));
            ReplaceOutpostVisual(outpost);
            TowerBranchVisual.Apply(outpost);
            RefreshSelectedPanel();
            EmitPresentation(PresentationCue.OutpostUpgraded, outpost.Position, outpost.gameObject, outpost: outpost.Kind);
            return true;
        }

        internal GreyboxEnemy FindPriorityEnemy(Vector3 position, float range)
        {
            GreyboxEnemy best = null;
            int bestPriority = -1;
            foreach (var enemy in enemies)
            {
                if (enemy == null || !enemy.IsAlive || (enemy.Position - position).sqrMagnitude > range * range) continue;
                int priority = enemy.Archetype == EnemyArchetype.Support ? 2 : enemy.Archetype == EnemyArchetype.Siege ? 1 : 0;
                if (best != null && (priority < bestPriority || priority == bestPriority && enemy.RouteProgress <= best.RouteProgress)) continue;
                best = enemy; bestPriority = priority;
            }
            return best;
        }

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
            public readonly int routeId;
            public ScheduledSpawn(float time, EnemyArchetype archetype, int order, int routeId)
            { this.time = time; this.archetype = archetype; this.order = order; this.routeId = routeId; }
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
                        group.enemy, order++, group.routeId));
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
                    {
                        ScheduledSpawn spawn = scheduledSpawns[nextSpawn++];
                        SpawnEnemy(spawn.archetype, spawn.routeId);
                    }
                    if (nextSpawn >= scheduledSpawns.Count) spawning = false;
                }
                for (int i = enemies.Count - 1; i >= 0; i--)
                {
                    if (i < enemies.Count && enemies[i] != null) enemies[i].Tick(delta);
                    if (Phase is GamePhase.Victory or GamePhase.Defeat) { stepAccumulator = 0; return; }
                }
                for (int i = 0; i < outposts.Count; i++)
                {
                    if (outposts[i] != null) outposts[i].Tick(delta);
                    if (Phase is GamePhase.Victory or GamePhase.Defeat) { stepAccumulator = 0; return; }
                }
                for (int i = 0; i < friendlyUnits.Count; i++)
                {
                    if (friendlyUnits[i] != null) friendlyUnits[i].Tick(delta);
                    if (Phase is GamePhase.Victory or GamePhase.Defeat) { stepAccumulator = 0; return; }
                }
                if (hero != null) hero.Tick(delta);
                if (Phase is GamePhase.Victory or GamePhase.Defeat) { stepAccumulator = 0; return; }
                TryCompleteWave();
            }
        }

        internal void RecordAbility(int targets) { AbilityUses++; AbilityTargets += targets; telemetry.Record(this,"ability","ShieldBash",targets); }
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
                    scheduledSpawns.Count, 0));
                plannedThisWave++;
            }
            // Only pending entries may move: already spawned entries retain their indices.
            scheduledSpawns.Sort(nextSpawn, scheduledSpawns.Count - nextSpawn,
                Comparer<ScheduledSpawn>.Create((a, b) => a.time.CompareTo(b.time)));
            spawning = true;
        }
    }
}
