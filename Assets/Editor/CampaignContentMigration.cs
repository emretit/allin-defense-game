using System;
using System.IO;
using System.Linq;
using System.Reflection;
using AllinDefense.Core;
using AllinDefense.Data;
using UnityEngine;

namespace AllinDefense.Editor
{
    // One-time migration evidence; not a balance contract for future authored content.
    public static class CampaignContentMigration
    {
        [Serializable] public sealed class Region { public string contentVersion; public Level[] levels; }
        [Serializable] public sealed class Level
        {
            public int level, startingGold, targetMinSeconds, targetMaxSeconds;
            public float healthMultiplier, speedMultiplier, attackMultiplier, baseDamageMultiplier;
            public int[] killRewards;
            public string bossProfileId, priorityTargeting;
            public WaveDefinition[] waves;
        }
        [Serializable] public sealed class Snapshot { public Entry[] levels; }
        [Serializable] public sealed class Entry
        {
            public int level, startingGold; public float targetMinutes;
            public WaveDefinition[] waves; public Stats[] stats;
        }
        [Serializable] public sealed class Stats
        {
            public int maxHealth, reward, baseDamage, attackDamage; public float speed;
            public Stats(EnemyStats s)
            { maxHealth=s.maxHealth; reward=s.reward; baseDamage=s.baseDamage; attackDamage=s.attackDamage; speed=s.speed; }
        }
        private static Snapshot Capture()
        {
            var entries = new Entry[50];
            var root = new GameObject("Content migration probe");
            try
            {
                var session = root.AddComponent<GameSessionController>();
                var stats = typeof(GameSessionController).GetMethod("GetScaledStats", BindingFlags.Instance | BindingFlags.NonPublic);
                for (int n=1; n<=50; n++)
                {
                    var runtime = CampaignLevelFactory.Create(n);
                    session.SetCampaignLevel(n);
                    entries[n-1] = new Entry { level=n, startingGold=runtime.campaign.startingGold,
                        targetMinutes=runtime.campaign.targetMinutes, waves=runtime.waves,
                        stats=Enumerable.Range(0,6).Select(e=>new Stats((EnemyStats)stats.Invoke(session,new object[]{(EnemyArchetype)e}))).ToArray() };
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
            return new Snapshot { levels=entries };
        }
        public static void ExportLegacy()
        {
            if (Resources.Load<TextAsset>("FirstRegionCampaign") == null)
                throw new InvalidOperationException("ExportLegacy must only run on the pre-migration source revision.");
            Directory.CreateDirectory("Builds/content-0a");
            if (File.Exists("Builds/content-0a/legacy.json")) throw new InvalidOperationException("Legacy capture already exists; do not overwrite migration evidence.");
            var snapshot = Capture();
            File.WriteAllText("Builds/content-0a/legacy.json", JsonUtility.ToJson(snapshot,true));
            for (int r=1; r<=5; r++)
            {
                var region = new Region { contentVersion=r==1 ? "first-region-v2" : $"region-{r:00}-v1", levels=new Level[10] };
                for (int i=0; i<10; i++)
                {
                    int n=(r-1)*10+i+1;
                    var old=snapshot.levels[n-1];
                    float p=Mathf.Clamp01((n-1f)/49f);
                    var first=r==1 ? FirstRegionCampaign.Level(n) : null;
                    region.levels[i]=new Level { level=n, startingGold=old.startingGold,
                        targetMinSeconds=first?.targetMinSeconds ?? Mathf.RoundToInt(old.targetMinutes*60f),
                        targetMaxSeconds=first?.targetMaxSeconds ?? Mathf.RoundToInt(old.targetMinutes*60f),
                        healthMultiplier=first?.healthMultiplier ?? Mathf.Lerp(1f,2.7f,p),
                        speedMultiplier=r==1 ? 1f : Mathf.Lerp(1f,1.16f,p),
                        attackMultiplier=r==1 ? 1f : Mathf.Lerp(1f,1.85f,p),
                        baseDamageMultiplier=r==1 ? 1f : Mathf.Lerp(1f,1.6f,p),
                        killRewards=old.stats.Select(s=>s.reward).ToArray(), bossProfileId=n%10==0 ? "region-lord" : "",
                        priorityTargeting=n==10 ? "furthestProgress" : "nearestExit", waves=old.waves };
                }
                File.WriteAllText($"Assets/Resources/Campaign/region-{r:00}.json",JsonUtility.ToJson(region,true));
            }
            Debug.Log("Captured original 50-level runtime, stats and exported region JSON.");
        }
        public static void VerifyMigration()
        {
            var before=JsonUtility.FromJson<Snapshot>(File.ReadAllText("Builds/content-0a/legacy.json"));
            var after=Capture();
            // Preview text is presentation, outside the combat-data equality contract.
            foreach(var snapshot in new[]{before,after})
                foreach(var entry in snapshot.levels)
                    foreach(var wave in entry.waves) wave.preview=null;
            File.WriteAllText("Builds/content-0a/migrated.json",JsonUtility.ToJson(after,true));
            bool equal=JsonUtility.ToJson(before)==JsonUtility.ToJson(after);
            int waves=after.levels.Skip(10).Sum(l=>l.waves.Length);
            int groups=after.levels.Skip(10).Sum(l=>l.waves.Sum(w=>w.groups.Length));
            File.WriteAllText("Builds/content-0a/equality.json", $"{{\"status\":\"{(equal ? "passed" : "failed")}\",\"levelsCompared\":50,\"progressiveLevels\":40,\"progressiveWaves\":{waves},\"progressiveGroups\":{groups},\"progressiveStats\":240,\"comparison\":\"exact serialized float and integer equality: wave ids, bonuses, boss flags, groups, routes, final timings, all six scaled stats, starting gold and target minutes\"}}");
            if(!equal) throw new InvalidOperationException("Migration changed original combat data.");
            Debug.Log("Migration equality passed for all 50 levels.");
        }
    }
}
