using System.Collections.Generic;
using AllinDefense.Data;
using UnityEngine;

namespace AllinDefense.Core
{
    public sealed partial class GameSessionController
    {
        private readonly List<GreyboxEnemy>[] enemyPools =
        {
            new(), new(), new(), new(), new(), new()
        };
        private readonly List<GreyboxEnemy> pendingEnemyReturns = new();
        private readonly int[] prewarmEnemyCounts = new int[6];
        private Transform enemyPoolRoot;

        private void EnsureEnemyPoolRoot()
        {
            if (enemyPoolRoot != null) return;
            var host = new GameObject("Enemy Pool");
            host.transform.SetParent(transform, false);
            host.SetActive(false);
            enemyPoolRoot = host.transform;
        }

        private void PrewarmEnemies(WaveDefinition wave)
        {
            EnsureEnemyPoolRoot();
            System.Array.Clear(prewarmEnemyCounts, 0, prewarmEnemyCounts.Length);
            if (wave.groups != null)
                foreach (var group in wave.groups)
                    prewarmEnemyCounts[(int)group.enemy] += Mathf.Max(0, group.count);
            if (wave.bossWave) prewarmEnemyCounts[(int)EnemyArchetype.Raider] += 6;
            int capacity = 0;
            for (int kind = 0; kind < enemyPools.Length; kind++)
            {
                int wanted = prewarmEnemyCounts[kind];
                if (presentationDirector != null)
                    presentationDirector.PrewarmDefeatEcho(ActorKind((EnemyArchetype)kind), wanted);
                capacity += wanted;
                List<GreyboxEnemy> pool = enemyPools[kind];
                for (int i = pool.Count - 1; i >= 0; i--)
                    if (pool[i] == null) pool.RemoveAt(i);
                int existing = pool.Count;
                foreach (var enemy in enemies)
                    if (enemy != null && (int)enemy.Archetype == kind) existing++;
                foreach (var enemy in pendingEnemyReturns)
                    if (enemy != null && (int)enemy.Archetype == kind) existing++;
                while (existing++ < wanted)
                {
                    var enemy = CreateEnemy((EnemyArchetype)kind, GetScaledStats((EnemyArchetype)kind), path);
                    enemy.gameObject.SetActive(false);
                    enemy.transform.SetParent(enemyPoolRoot, true);
                    pool.Add(enemy);
                }
            }
            if (enemies.Capacity < capacity) enemies.Capacity = capacity;
            if (pendingEnemyReturns.Capacity < capacity) pendingEnemyReturns.Capacity = capacity;
            foreach (var snapshot in damageSnapshots)
                if (snapshot.Capacity < capacity) snapshot.Capacity = capacity;
        }

        private GreyboxEnemy RentEnemy(EnemyArchetype archetype, EnemyStats stats, IReadOnlyList<Vector3> spawnPath)
        {
            List<GreyboxEnemy> pool = enemyPools[(int)archetype];
            GreyboxEnemy enemy = null;
            while (pool.Count > 0 && enemy == null)
            {
                int last = pool.Count - 1;
                enemy = pool[last];
                pool.RemoveAt(last);
            }
            if (enemy == null) return CreateEnemy(archetype, stats, spawnPath);
            // A guard must never inherit a target solely because its dead actor is reused.
            foreach (var guard in friendlyUnits)
                if (guard != null) guard.ForgetTarget(enemy);
            enemy.transform.SetParent(transform, true);
            float size = archetype == EnemyArchetype.Boss ? 1.7f : archetype == EnemyArchetype.Siege ? 1.25f : .85f;
            enemy.transform.position = spawnPath[0] + (presentationCatalog != null ? Vector3.zero : Vector3.up * (size * .6f));
            enemy.gameObject.SetActive(true);
            enemy.Configure(this, archetype, stats, spawnPath);
            return enemy;
        }

        internal void ReturnEnemy(GreyboxEnemy enemy)
        {
            EnsureEnemyPoolRoot();
            enemy.gameObject.SetActive(false);
            pendingEnemyReturns.Add(enemy);
        }

        private void LateUpdate()
        {
            // Keep retired identities unavailable until damage/death callbacks and simulation finish.
            foreach (var enemy in pendingEnemyReturns)
            {
                if (enemy == null) continue;
                enemy.transform.SetParent(enemyPoolRoot, true);
                enemyPools[(int)enemy.Archetype].Add(enemy);
            }
            pendingEnemyReturns.Clear();
        }
    }
}
