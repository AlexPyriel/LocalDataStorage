using System;
using System.Collections.Generic;
using UnityEngine;

namespace LocalDataStorage.Internal
{
    /// <summary>
    /// <see cref="IStorageBackend"/> that stores base64-encoded blobs in <see cref="PlayerPrefs"/>.
    /// Preserves the on-disk layout used by v1.0.0 — reading an existing v1 PlayerPrefs save under
    /// the type's full name key still works through this backend.
    /// </summary>
    internal sealed class PlayerPrefsStorageBackend : IStorageBackend
    {
        private const string KEY_REGISTRY_PREFS_KEY = "LocalDataStorage.__keys";

        public void Write(string key, byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                Delete(key);
                return;
            }

            PlayerPrefs.SetString(key, Convert.ToBase64String(bytes));
            PlayerPrefs.Save();
            RegisterKey(key);
        }

        public byte[] Read(string key)
        {
            string serialized = PlayerPrefs.GetString(key);
            if (string.IsNullOrEmpty(serialized))
                return null;

            try
            {
                return Convert.FromBase64String(serialized);
            }
            catch (FormatException)
            {
                return null;
            }
        }

        public void Delete(string key)
        {
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
            UnregisterKey(key);
        }

        public IEnumerable<string> EnumerateKeys()
        {
            return GetRegisteredKeys();
        }

        public void DeleteAll()
        {
            foreach (string key in GetRegisteredKeys())
                PlayerPrefs.DeleteKey(key);

            PlayerPrefs.DeleteKey(KEY_REGISTRY_PREFS_KEY);
            PlayerPrefs.Save();
        }

        private static void RegisterKey(string key)
        {
            HashSet<string> keys = GetRegisteredKeys();
            if (keys.Add(key))
                SaveRegisteredKeys(keys);
        }

        private static void UnregisterKey(string key)
        {
            HashSet<string> keys = GetRegisteredKeys();
            if (keys.Remove(key))
                SaveRegisteredKeys(keys);
        }

        private static HashSet<string> GetRegisteredKeys()
        {
            string serialized = PlayerPrefs.GetString(KEY_REGISTRY_PREFS_KEY);
            if (string.IsNullOrEmpty(serialized))
                return new HashSet<string>();

            KeyRegistryData registry = JsonUtility.FromJson<KeyRegistryData>(serialized);
            if (registry?.Keys == null || registry.Keys.Length == 0)
                return new HashSet<string>();

            return new HashSet<string>(registry.Keys);
        }

        private static void SaveRegisteredKeys(HashSet<string> keys)
        {
            if (keys.Count == 0)
            {
                PlayerPrefs.DeleteKey(KEY_REGISTRY_PREFS_KEY);
                PlayerPrefs.Save();
                return;
            }

            string[] sorted = new string[keys.Count];
            keys.CopyTo(sorted);
            Array.Sort(sorted, StringComparer.Ordinal);
            PlayerPrefs.SetString(KEY_REGISTRY_PREFS_KEY, JsonUtility.ToJson(new KeyRegistryData { Keys = sorted }));
            PlayerPrefs.Save();
        }

        [Serializable]
        private sealed class KeyRegistryData
        {
            public string[] Keys;
        }
    }
}
