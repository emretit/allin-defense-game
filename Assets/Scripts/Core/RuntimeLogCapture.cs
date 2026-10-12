using System;
using System.IO;
using UnityEngine;

namespace AllinDefense.Core
{
    public static class RuntimeLogCapture
    {
        private static string logPath;
        private static bool installed;

        public static string LogPath => logPath;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if (installed) return;
            installed = true;

            Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.Full);
            Application.SetStackTraceLogType(LogType.Exception, StackTraceLogType.Full);
            Application.SetStackTraceLogType(LogType.Assert, StackTraceLogType.Full);

            logPath = Path.Combine(Application.persistentDataPath, "defender-runtime-log.txt");
            try
            {
                File.WriteAllText(logPath, $"ALLIN: Defense runtime log - {DateTime.UtcNow:O}\n");
            }
            catch (Exception)
            {
                logPath = string.Empty;
            }

            Application.logMessageReceived += Capture;
        }

        private static void Capture(string condition, string stackTrace, LogType type)
        {
            if (string.IsNullOrEmpty(logPath) || (type != LogType.Error && type != LogType.Exception && type != LogType.Assert))
                return;

            try
            {
                File.AppendAllText(logPath, $"\n[{DateTime.UtcNow:O}] {type}: {condition}\n{stackTrace}\n");
            }
            catch (Exception)
            {
                // Logging must never break the game.
            }
        }
    }
}
