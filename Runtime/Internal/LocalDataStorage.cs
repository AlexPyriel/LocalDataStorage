using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace LocalDataStorage.Internal
{
    internal sealed class LocalDataStorage : ILocalDataStorage
    {
        private readonly Dictionary<string, LocalData> _loadedData = new();

        private const string PASS_PHRASE = "LocalDataStorage";
        private const string KEY_REGISTRY_PREFS_KEY = "LocalDataStorage.__keys";
        private const int KEY_SIZE_BITS = 256;
        private const int KEY_SIZE_BYTES = KEY_SIZE_BITS / 8;
        private const int BLOCK_SIZE_BITS = 256;
        private const int SALT_SIZE_BYTES = KEY_SIZE_BYTES;
        private const int IV_SIZE_BYTES = KEY_SIZE_BYTES;
        private const int ENCRYPTION_HEADER_SIZE_BYTES = SALT_SIZE_BYTES + IV_SIZE_BYTES;
        private const int RANDOM_ENTROPY_SIZE_BYTES = 32;
        private const int DERIVATION_ITERATIONS = 1000;

        public void Save<TData>(TData data) where TData : LocalData
        {
            string key = GetKey<TData>();
            string encrypted = null;

            if (data != null)
            {
                string json = JsonUtility.ToJson(data);
                encrypted = Encrypt(json, PASS_PHRASE);
            }

            PlayerPrefs.SetString(key, encrypted);
            RegisterKey(key);
            _loadedData[key] = data;
        }

        public bool TryLoad<TData>(out TData data) where TData : LocalData
        {
            string key = GetKey<TData>();

            if (_loadedData.TryGetValue(key, out LocalData cachedData))
            {
                data = cachedData as TData;
                return data != null;
            }

            string encrypted = PlayerPrefs.GetString(key);
            if (string.IsNullOrEmpty(encrypted))
            {
                _loadedData[key] = null;
                data = null;
                return false;
            }

            try
            {
                string json = Decrypt(encrypted, PASS_PHRASE);
                TData loadedData = JsonUtility.FromJson<TData>(json);
                _loadedData[key] = loadedData;
                data = loadedData;
                return loadedData != null;
            }
            catch (Exception exception)
            {
                Debug.LogError($"{nameof(LocalDataStorage)} Failed to load data for key '{key}'.\n{exception}");
                _loadedData[key] = null;
                data = null;
                return false;
            }
        }

        public void Delete<TData>() where TData : LocalData
        {
            string key = GetKey<TData>();
            PlayerPrefs.DeleteKey(key);
            UnregisterKey(key);
            _loadedData.Remove(key);
        }

        public void DeleteAll()
        {
            foreach (string key in GetRegisteredKeys())
            {
                PlayerPrefs.DeleteKey(key);
            }

            PlayerPrefs.DeleteKey(KEY_REGISTRY_PREFS_KEY);
            _loadedData.Clear();
        }

        private static void RegisterKey(string key)
        {
            HashSet<string> keys = GetRegisteredKeys();
            if (!keys.Add(key))
            {
                return;
            }

            SaveRegisteredKeys(keys);
        }

        private static void UnregisterKey(string key)
        {
            HashSet<string> keys = GetRegisteredKeys();
            if (!keys.Remove(key))
            {
                return;
            }

            SaveRegisteredKeys(keys);
        }

        private static HashSet<string> GetRegisteredKeys()
        {
            string serializedKeys = PlayerPrefs.GetString(KEY_REGISTRY_PREFS_KEY);
            if (string.IsNullOrEmpty(serializedKeys))
            {
                return new HashSet<string>();
            }

            KeyRegistryData registry = JsonUtility.FromJson<KeyRegistryData>(serializedKeys);
            if (registry?.Keys == null || registry.Keys.Length == 0)
            {
                return new HashSet<string>();
            }

            return new HashSet<string>(registry.Keys);
        }

        private static void SaveRegisteredKeys(HashSet<string> keys)
        {
            if (keys.Count == 0)
            {
                PlayerPrefs.DeleteKey(KEY_REGISTRY_PREFS_KEY);
                return;
            }

            KeyRegistryData registry = new()
            {
                Keys = keys.OrderBy(value => value).ToArray(),
            };

            PlayerPrefs.SetString(KEY_REGISTRY_PREFS_KEY, JsonUtility.ToJson(registry));
        }

        private static string GetKey<TData>() where TData : LocalData
        {
            return typeof(TData).ToString();
        }

        private static string Encrypt(string plainText, string passPhrase)
        {
            byte[] saltBytes = GenerateRandomBytes();
            byte[] ivBytes = GenerateRandomBytes();
            byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);

            using Rfc2898DeriveBytes password = new(passPhrase, saltBytes, DERIVATION_ITERATIONS);
            byte[] keyBytes = password.GetBytes(KEY_SIZE_BYTES);

            using RijndaelManaged symmetricKey = new();
            symmetricKey.BlockSize = BLOCK_SIZE_BITS;
            symmetricKey.Mode = CipherMode.CBC;
            symmetricKey.Padding = PaddingMode.PKCS7;

            using ICryptoTransform encryptor = symmetricKey.CreateEncryptor(keyBytes, ivBytes);
            using MemoryStream memoryStream = new();
            using (CryptoStream cryptoStream = new(memoryStream, encryptor, CryptoStreamMode.Write))
            {
                cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                cryptoStream.FlushFinalBlock();
            }

            byte[] cipherTextBytes = saltBytes
                .Concat(ivBytes)
                .Concat(memoryStream.ToArray())
                .ToArray();

            return Convert.ToBase64String(cipherTextBytes);
        }

        private static string Decrypt(string cipherText, string passPhrase)
        {
            byte[] cipherTextBytesWithSaltAndIv = Convert.FromBase64String(cipherText);
            byte[] saltBytes = cipherTextBytesWithSaltAndIv.Take(SALT_SIZE_BYTES).ToArray();
            byte[] ivBytes = cipherTextBytesWithSaltAndIv.Skip(SALT_SIZE_BYTES).Take(IV_SIZE_BYTES).ToArray();
            byte[] cipherTextBytes = cipherTextBytesWithSaltAndIv
                .Skip(ENCRYPTION_HEADER_SIZE_BYTES)
                .Take(cipherTextBytesWithSaltAndIv.Length - ENCRYPTION_HEADER_SIZE_BYTES)
                .ToArray();

            using Rfc2898DeriveBytes password = new(passPhrase, saltBytes, DERIVATION_ITERATIONS);
            byte[] keyBytes = password.GetBytes(KEY_SIZE_BYTES);

            using RijndaelManaged symmetricKey = new();
            symmetricKey.BlockSize = BLOCK_SIZE_BITS;
            symmetricKey.Mode = CipherMode.CBC;
            symmetricKey.Padding = PaddingMode.PKCS7;

            using ICryptoTransform decryptor = symmetricKey.CreateDecryptor(keyBytes, ivBytes);
            using MemoryStream memoryStream = new(cipherTextBytes);
            using CryptoStream cryptoStream = new(memoryStream, decryptor, CryptoStreamMode.Read);
            using StreamReader streamReader = new(cryptoStream, Encoding.UTF8);

            return streamReader.ReadToEnd();
        }

        private static byte[] GenerateRandomBytes()
        {
            byte[] bytes = new byte[RANDOM_ENTROPY_SIZE_BYTES];
            using RNGCryptoServiceProvider rng = new();
            rng.GetBytes(bytes);
            return bytes;
        }

        [Serializable]
        private sealed class KeyRegistryData
        {
            public string[] Keys;
        }
    }
}
