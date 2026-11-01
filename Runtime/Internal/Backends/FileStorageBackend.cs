using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LocalDataStorage.Internal
{
    /// <summary>
    /// File-based storage backend. One file per key under
    /// <c>Application.persistentDataPath/&lt;subPath&gt;/&lt;key&gt;.sav</c>. Writes are atomic:
    /// payload goes into <c>.sav.tmp</c>, the current <c>.sav</c> is renamed to <c>.sav.bak</c>,
    /// then <c>.sav.tmp</c> is renamed to <c>.sav</c>. A crash between any two steps leaves either
    /// the previous <c>.sav</c> intact or the <c>.sav.bak</c> available as a fallback. On WebGL
    /// the IndexedDB-backed virtual filesystem is explicitly flushed after every write.
    /// </summary>
    internal sealed class FileStorageBackend : IStorageBackend
    {
        private const string SAVE_EXTENSION = ".sav";
        private const string TEMP_EXTENSION = ".sav.tmp";
        private const string BACKUP_EXTENSION = ".sav.bak";

        private readonly string _rootDirectory;

        public FileStorageBackend(string rootDirectory)
        {
            if (string.IsNullOrEmpty(rootDirectory))
                throw new ArgumentException("Root directory must be non-empty.", nameof(rootDirectory));

            _rootDirectory = rootDirectory;
            Directory.CreateDirectory(_rootDirectory);
        }

        /// <summary>Resolves the primary save path for the given key (public for tests).</summary>
        internal string PathFor(string key) => Path.Combine(_rootDirectory, key + SAVE_EXTENSION);

        /// <summary>Resolves the backup path for the given key (public for tests).</summary>
        internal string BackupPathFor(string key) => Path.Combine(_rootDirectory, key + BACKUP_EXTENSION);

        public void Write(string key, byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                Delete(key);
                return;
            }

            string finalPath = PathFor(key);
            string tempPath = Path.Combine(_rootDirectory, key + TEMP_EXTENSION);
            string backupPath = BackupPathFor(key);

            File.WriteAllBytes(tempPath, bytes);

            // Promote current to backup before overwriting.
            if (File.Exists(finalPath))
            {
                if (File.Exists(backupPath))
                    File.Delete(backupPath);

                File.Move(finalPath, backupPath);
            }

            // Atomic rename.
            File.Move(tempPath, finalPath);

            FlushWebGL();
        }

        public byte[] Read(string key)
        {
            string finalPath = PathFor(key);
            string backupPath = BackupPathFor(key);

            if (File.Exists(finalPath))
            {
                try
                {
                    return File.ReadAllBytes(finalPath);
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"[LocalDataStorage] Failed to read '{finalPath}', falling back to .bak: {exception.Message}");
                }
            }

            if (File.Exists(backupPath))
            {
                try
                {
                    byte[] payload = File.ReadAllBytes(backupPath);
                    // Restore primary from backup so subsequent reads succeed without triggering the fallback again.
                    if (!File.Exists(finalPath))
                    {
                        File.Copy(backupPath, finalPath);
                    }
                    return payload;
                }
                catch (Exception exception)
                {
                    Debug.LogError($"[LocalDataStorage] Backup read failed for '{backupPath}': {exception.Message}");
                }
            }

            return null;
        }

        public void Delete(string key)
        {
            string finalPath = PathFor(key);
            string tempPath = Path.Combine(_rootDirectory, key + TEMP_EXTENSION);
            string backupPath = BackupPathFor(key);

            if (File.Exists(finalPath)) File.Delete(finalPath);
            if (File.Exists(tempPath)) File.Delete(tempPath);
            if (File.Exists(backupPath)) File.Delete(backupPath);

            FlushWebGL();
        }

        public IEnumerable<string> EnumerateKeys()
        {
            if (!Directory.Exists(_rootDirectory))
                yield break;

            foreach (string path in Directory.EnumerateFiles(_rootDirectory, "*" + SAVE_EXTENSION, SearchOption.TopDirectoryOnly))
            {
                string name = Path.GetFileName(path);
                yield return name.Substring(0, name.Length - SAVE_EXTENSION.Length);
            }
        }

        public void DeleteAll()
        {
            if (!Directory.Exists(_rootDirectory))
                return;

            foreach (string path in Directory.EnumerateFiles(_rootDirectory, "*.sav*", SearchOption.TopDirectoryOnly))
            {
                try { File.Delete(path); }
                catch (Exception exception) { Debug.LogWarning($"[LocalDataStorage] Failed to delete '{path}': {exception.Message}"); }
            }

            FlushWebGL();
        }

        private static void FlushWebGL()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            // Flush IDBFS so file writes survive tab close. Unity's SyncFs calls Emscripten FS.syncfs(false, cb).
            try { WebGLHelper.FlushIdbfs(); }
            catch { /* older Unity: autoflush is handled by Unity on quit; best-effort here. */ }
#endif
        }
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    internal static class WebGLHelper
    {
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern void GdfLocalDataStorage_SyncFs();

        public static void FlushIdbfs() => GdfLocalDataStorage_SyncFs();
    }
#endif
}
