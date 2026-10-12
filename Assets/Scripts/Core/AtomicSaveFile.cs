using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace AllinDefense.Core
{
    public enum SaveWriteStage { TemporaryFlushed, BeforeReplace, Committed }

    /// <summary>Checksummed replacement with recovery from the last valid main, temporary or backup file.</summary>
    public static class AtomicSaveFile
    {
        [Serializable] private sealed class Envelope
        {
            public int format = 1;
            public string payload;
            public string checksum;
        }

        public static void Write(string path, string payload, Action<SaveWriteStage> faultPoint = null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            string temporary = path + ".tmp";
            var envelope = new Envelope { payload = payload, checksum = Hash(payload) };
            byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(envelope));
            using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(true);
            }
            faultPoint?.Invoke(SaveWriteStage.TemporaryFlushed);
            faultPoint?.Invoke(SaveWriteStage.BeforeReplace);
            if (File.Exists(path))
            {
                try { File.Replace(temporary, path, path + ".bak"); }
                catch (PlatformNotSupportedException)
                {
                    // The old complete file remains recoverable even if this platform lacks replace().
                    File.Copy(path, path + ".bak", true);
                    File.Copy(temporary, path, true);
                    File.Delete(temporary);
                }
            }
            else File.Move(temporary, path);
            faultPoint?.Invoke(SaveWriteStage.Committed);
        }

        public static bool TryRead(string path, Func<string, bool> validate, out string payload,
            out bool recovered)
        {
            payload = null;
            recovered = false;
            foreach (string candidate in new[] { path, path + ".tmp", path + ".bak" })
            {
                if (!File.Exists(candidate)) continue;
                try
                {
                    string json = File.ReadAllText(candidate);
                    string content = json;
                    if (json.Contains("\"checksum\""))
                    {
                        var envelope = JsonUtility.FromJson<Envelope>(json);
                        if (envelope == null || envelope.format != 1 || envelope.payload == null ||
                            envelope.checksum != Hash(envelope.payload)) continue;
                        content = envelope.payload;
                    }
                    if (!validate(content)) continue;
                    payload = content;
                    recovered = candidate != path;
                    return true;
                }
                catch (Exception error) when (error is IOException || error is ArgumentException)
                { /* Try the next complete version. Never replace a corrupt save with an empty one here. */ }
            }
            return false;
        }

        public static string Hash(string value)
        {
            using var sha = SHA256.Create();
            byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(value ?? string.Empty));
            var result = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes) result.Append(b.ToString("x2"));
            return result.ToString();
        }
    }
}
