using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace LocalDataStorage.Internal
{
    /// <summary>
    /// Orchestrates typed save/load through pluggable <see cref="IStorageBackend"/> implementations,
    /// optional AES encryption, slot-aware keying, schema migrations and change events.
    /// </summary>
    public sealed class LocalDataStorage : ILocalDataStorage
    {
        private const string KEY_SLOT_SEPARATOR = ":";

        private readonly LocalDataStorageConfig _config;
        private readonly IStorageBackend _playerPrefsBackend;
        private readonly IStorageBackend _fileBackend;
        private readonly EncryptionService _encryption;
        private readonly MigrationRegistry _migrations;
        private readonly Dictionary<string, LocalData> _cache = new();

        /// <inheritdoc />
        public event Action<Type> DataSaved;
        /// <inheritdoc />
        public event Action<Type> DataDeleted;
        /// <inheritdoc />
        public event Action AllDeleted;

        /// <summary>Creates the storage with <see cref="LocalDataStorageConfig.Default"/> (PlayerPrefs, encrypted).</summary>
        public LocalDataStorage() : this(LocalDataStorageConfig.Default()) { }

        /// <summary>
        /// Creates the storage for the given configuration. DI containers without a package installer
        /// (Zenject, Reflex) bind <see cref="ILocalDataStorage"/> to this constructor directly.
        /// </summary>
        /// <param name="config">Backend, encryption and file-root settings.</param>
        public LocalDataStorage(LocalDataStorageConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _playerPrefsBackend = new PlayerPrefsStorageBackend();
            _fileBackend = new FileStorageBackend(ResolveFileRoot(config));
            _encryption = config.EncryptionEnabled ? new EncryptionService(config.PassPhrase) : null;
            _migrations = new MigrationRegistry();
        }

        /// <inheritdoc />
        public void Save<TData>(TData data) where TData : LocalData => Save("", data);

        /// <inheritdoc />
        public void Save<TData>(string slot, TData data) where TData : LocalData
        {
            SaveInternal(typeof(TData), slot ?? string.Empty, data);
        }

        /// <inheritdoc />
        public bool TryLoad<TData>(out TData data) where TData : LocalData => TryLoad("", out data);

        /// <inheritdoc />
        public bool TryLoad<TData>(string slot, out TData data) where TData : LocalData
        {
            data = (TData)LoadInternal(typeof(TData), slot ?? string.Empty);
            return data != null;
        }

        /// <inheritdoc />
        public void Delete<TData>() where TData : LocalData => Delete<TData>("");

        /// <inheritdoc />
        public void Delete<TData>(string slot) where TData : LocalData
        {
            Type type = typeof(TData);
            string key = BuildKey(type, slot ?? string.Empty);
            PickBackend(type).Delete(key);
            _cache.Remove(key);
            DataDeleted?.Invoke(type);
        }

        /// <inheritdoc />
        public IEnumerable<string> ListSlots<TData>() where TData : LocalData
        {
            Type type = typeof(TData);
            string prefix = type.FullName;
            IStorageBackend backend = PickBackend(type);

            foreach (string key in backend.EnumerateKeys())
            {
                if (!key.StartsWith(prefix, StringComparison.Ordinal))
                    continue;

                if (key.Length == prefix.Length)
                {
                    yield return string.Empty;
                    continue;
                }

                if (key[prefix.Length] != KEY_SLOT_SEPARATOR[0])
                    continue;

                yield return key.Substring(prefix.Length + 1);
            }
        }

        /// <inheritdoc />
        public void DeleteAll()
        {
            _playerPrefsBackend.DeleteAll();
            _fileBackend.DeleteAll();
            _cache.Clear();
            AllDeleted?.Invoke();
        }

        /// <inheritdoc />
        public UniTask SaveAsync<TData>(TData data, CancellationToken cancellationToken = default) where TData : LocalData =>
            SaveAsync("", data, cancellationToken);

        /// <inheritdoc />
        public async UniTask SaveAsync<TData>(string slot, TData data, CancellationToken cancellationToken = default) where TData : LocalData
        {
            await UniTask.RunOnThreadPool(() => SaveInternal(typeof(TData), slot ?? string.Empty, data), configureAwait: true, cancellationToken: cancellationToken);
        }

        /// <inheritdoc />
        public UniTask<(bool Found, TData Data)> TryLoadAsync<TData>(CancellationToken cancellationToken = default) where TData : LocalData =>
            TryLoadAsync<TData>("", cancellationToken);

        /// <inheritdoc />
        public async UniTask<(bool Found, TData Data)> TryLoadAsync<TData>(string slot, CancellationToken cancellationToken = default) where TData : LocalData
        {
            TData loaded = await UniTask.RunOnThreadPool(
                () => (TData)LoadInternal(typeof(TData), slot ?? string.Empty),
                configureAwait: true,
                cancellationToken: cancellationToken);
            return (loaded != null, loaded);
        }

        /// <inheritdoc />
        public async UniTask DeleteAsync<TData>(string slot = "", CancellationToken cancellationToken = default) where TData : LocalData
        {
            await UniTask.RunOnThreadPool(() => Delete<TData>(slot), configureAwait: true, cancellationToken: cancellationToken);
        }

        /// <inheritdoc />
        public async UniTask DeleteAllAsync(CancellationToken cancellationToken = default)
        {
            await UniTask.RunOnThreadPool(DeleteAll, configureAwait: true, cancellationToken: cancellationToken);
        }

        // ---------- internals ----------

        private void SaveInternal(Type type, string slot, LocalData data)
        {
            string key = BuildKey(type, slot);
            IStorageBackend backend = PickBackend(type);

            if (data == null)
            {
                backend.Delete(key);
                _cache.Remove(key);
                DataDeleted?.Invoke(type);
                return;
            }

            byte[] bytes = SerializePayload(type, data);
            backend.Write(key, bytes);
            _cache[key] = data;
            DataSaved?.Invoke(type);
        }

        private LocalData LoadInternal(Type type, string slot)
        {
            string key = BuildKey(type, slot);
            if (_cache.TryGetValue(key, out LocalData cached))
                return cached;

            IStorageBackend backend = PickBackend(type);
            byte[] raw = backend.Read(key);
            if (raw == null || raw.Length == 0)
            {
                _cache[key] = null;
                return null;
            }

            try
            {
                LocalData loaded = DeserializePayload(type, raw);
                _cache[key] = loaded;
                return loaded;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LocalDataStorage] Failed to load '{key}': {ex}");
                _cache[key] = null;
                return null;
            }
        }

        private byte[] SerializePayload(Type type, LocalData data)
        {
            int version = MigrationRegistry.GetCurrentVersion(type);
            string json = JsonUtility.ToJson(data);
            byte[] jsonBytes = Encoding.UTF8.GetBytes(json);

            // Payload layout (plaintext): [4-byte LE int version][json bytes]
            byte[] withPrefix = new byte[4 + jsonBytes.Length];
            WriteInt32LE(withPrefix, 0, version);
            Buffer.BlockCopy(jsonBytes, 0, withPrefix, 4, jsonBytes.Length);

            return _encryption != null ? _encryption.Encrypt(withPrefix) : withPrefix;
        }

        private LocalData DeserializePayload(Type type, byte[] raw)
        {
            byte[] plain = _encryption != null ? _encryption.Decrypt(raw) : raw;

            int version;
            string json;

            if (LooksLikeLegacyJson(plain))
            {
                // v1 payload without version prefix. Treat as schema version 1.
                version = 1;
                json = Encoding.UTF8.GetString(plain);
            }
            else
            {
                version = ReadInt32LE(plain, 0);
                json = Encoding.UTF8.GetString(plain, 4, plain.Length - 4);
            }

            int currentVersion = MigrationRegistry.GetCurrentVersion(type);
            if (version < currentVersion)
            {
                JObject payload = JObject.Parse(json);
                payload = _migrations.Migrate(type, version, payload);
                json = payload.ToString();
            }
            else if (version > currentVersion)
            {
                Debug.LogWarning($"[LocalDataStorage] Save for {type.Name} is at v{version} but the current schema is v{currentVersion}. Loading as-is; forward migrations are not supported.");
            }

            return (LocalData)JsonUtility.FromJson(json, type);
        }

        private IStorageBackend PickBackend(Type type)
        {
            BackendType backend = _config.DefaultBackend;
            object[] attrs = type.GetCustomAttributes(typeof(StorageAttribute), inherit: false);
            if (attrs.Length > 0)
                backend = ((StorageAttribute)attrs[0]).Backend;

            return backend switch
            {
                BackendType.File => _fileBackend,
                _ => _playerPrefsBackend,
            };
        }

        private static string BuildKey(Type type, string slot)
        {
            return string.IsNullOrEmpty(slot) ? type.FullName : type.FullName + KEY_SLOT_SEPARATOR + slot;
        }

        private static string ResolveFileRoot(LocalDataStorageConfig config)
        {
            if (!string.IsNullOrEmpty(config.FileAbsoluteRoot))
                return config.FileAbsoluteRoot;

            string sub = string.IsNullOrEmpty(config.FileSubPath) ? LocalDataStorageConfig.DEFAULT_FILE_SUB_PATH : config.FileSubPath;
            return Path.Combine(Application.persistentDataPath, sub);
        }

        private static bool LooksLikeLegacyJson(byte[] bytes)
        {
            if (bytes.Length == 0) return false;
            byte first = bytes[0];
            // JSON always starts with '{', '[', or whitespace; versioned payload starts with a low-byte int (v1..v9 → 0x01..0x09 → not whitespace or '{'/'[').
            return first == 0x7B /* '{' */ || first == 0x5B /* '[' */ || first == 0x20 || first == 0x09 || first == 0x0A || first == 0x0D;
        }

        private static void WriteInt32LE(byte[] buffer, int offset, int value)
        {
            buffer[offset + 0] = (byte)(value & 0xFF);
            buffer[offset + 1] = (byte)((value >> 8) & 0xFF);
            buffer[offset + 2] = (byte)((value >> 16) & 0xFF);
            buffer[offset + 3] = (byte)((value >> 24) & 0xFF);
        }

        private static int ReadInt32LE(byte[] buffer, int offset)
        {
            return buffer[offset] | (buffer[offset + 1] << 8) | (buffer[offset + 2] << 16) | (buffer[offset + 3] << 24);
        }
    }
}
