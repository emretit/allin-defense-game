using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace DefenderChronicles.Core
{
    public static class ClosedTestBuild { public const string Id = "ct-20260906-01"; }
    public sealed class BattleTelemetry
    {
        [Serializable] private sealed class Row
        {
            public string build = ClosedTestBuild.Id;
            public string run, eventId, detail;
            public bool automated;
            public int level, wave, gold, lives, value;
            public float battleSeconds;
        }
        private readonly List<string> pending = new();
        private readonly string run = Guid.NewGuid().ToString("N");
        private readonly bool automated = Application.isEditor || Application.isBatchMode ||
            Environment.GetEnvironmentVariable("DC_PERFORMANCE_ACCEPTANCE") == "1" ||
            Environment.GetEnvironmentVariable("DC_SOAK_ACCEPTANCE") == "1" ||
            Environment.GetEnvironmentVariable("DC_CLOSED_TEST_AUTOPLAY") == "1" ||
            IsDevelopmentAutomation();
        private static bool IsDevelopmentAutomation()
        {
            if (!Debug.isDebugBuild) return false;
            foreach (string name in new[] { "DC_SMOKE", "DC_SMOKE_DEFEAT", "DC_SLOT_REGRESSION", "DC_ABILITY_CAPTURE", "DC_PAUSE_CAPTURE", "DC_ACTOR_CAPTURE", "DC_AUDIO_ACCEPTANCE", "DC_VIS812_CAPTURE", "DC_VIS812_CAPTURE_DEFEAT" })
                if (Environment.GetEnvironmentVariable(name) == "1") return true;
            return Array.Exists(Environment.GetCommandLineArgs(), a => a.Equals("-dc-smoke", StringComparison.OrdinalIgnoreCase));
        }
        public void Record(GameSessionController session, string eventId, string detail = "", int value = 0)
        {
            pending.Add(JsonUtility.ToJson(new Row { run = run, automated = automated,
                eventId = eventId, detail = detail, value = value, level = session.CampaignLevelNumber,
                wave = session.CurrentWaveIndex + 1, gold = session.Gold, lives = session.Lives,
                battleSeconds = session.BattleElapsedSeconds }));
            if (pending.Count >= 512) Flush();
        }
        public void Flush()
        {
            if (pending.Count == 0) return;
            try
            {
                string folder = Path.Combine(Application.persistentDataPath,"closed-test","telemetry",automated ? "automated" : "players");
                Directory.CreateDirectory(folder);
                File.AppendAllLines(Path.Combine(folder,run+".jsonl"),pending);
                pending.Clear();
            }
            catch (IOException error)
            {
                Debug.LogWarning("Telemetry write deferred: "+error.Message);
                if (pending.Count > 1024) pending.RemoveRange(0,pending.Count-1024);
            }
        }
    }
}
