using System;

namespace LocalDataStorage
{
    /// <summary>
    /// Immutable configuration passed to <see cref="Internal.LocalDataStorage"/>. Built through the
    /// fluent methods on the DI installers so hosts can declare backend, encryption and file-root
    /// policy in one place.
    /// </summary>
    public sealed class LocalDataStorageConfig
    {
        /// <summary>Default pass phrase used when the host does not override it; matches the v1.0.0 value for backward compatibility.</summary>
        public const string DEFAULT_PASS_PHRASE = "LocalDataStorage";

        /// <summary>Default subfolder under <c>Application.persistentDataPath</c> used by <see cref="BackendType.File"/>.</summary>
        public const string DEFAULT_FILE_SUB_PATH = "saves";

        /// <summary>Backend used for types without a <see cref="StorageAttribute"/> override.</summary>
        public BackendType DefaultBackend { get; }

        /// <summary>Whether payloads are encrypted at rest. When <see langword="false"/> blobs are stored as plain JSON.</summary>
        public bool EncryptionEnabled { get; }

        /// <summary>Pass phrase used for AES-256 derivation. Ignored when <see cref="EncryptionEnabled"/> is <see langword="false"/>.</summary>
        public string PassPhrase { get; }

        /// <summary>Subfolder (relative to <c>Application.persistentDataPath</c>) or absolute path used by the file backend.</summary>
        public string FileSubPath { get; }

        /// <summary>When set, overrides <see cref="FileSubPath"/> with an absolute directory. Primarily for tests.</summary>
        public string FileAbsoluteRoot { get; }

        private LocalDataStorageConfig(
            BackendType defaultBackend,
            bool encryptionEnabled,
            string passPhrase,
            string fileSubPath,
            string fileAbsoluteRoot)
        {
            DefaultBackend = defaultBackend;
            EncryptionEnabled = encryptionEnabled;
            PassPhrase = passPhrase ?? DEFAULT_PASS_PHRASE;
            FileSubPath = fileSubPath ?? DEFAULT_FILE_SUB_PATH;
            FileAbsoluteRoot = fileAbsoluteRoot;
        }

        /// <summary>v1.0.0 compatible defaults: PlayerPrefs backend + AES encryption + legacy pass phrase.</summary>
        public static LocalDataStorageConfig Default() => new(
            defaultBackend: BackendType.PlayerPrefs,
            encryptionEnabled: true,
            passPhrase: DEFAULT_PASS_PHRASE,
            fileSubPath: DEFAULT_FILE_SUB_PATH,
            fileAbsoluteRoot: null);

        /// <summary>Returns a copy with <see cref="DefaultBackend"/> set to <see cref="BackendType.PlayerPrefs"/>.</summary>
        public LocalDataStorageConfig WithPlayerPrefsBackend() =>
            new(BackendType.PlayerPrefs, EncryptionEnabled, PassPhrase, FileSubPath, FileAbsoluteRoot);

        /// <summary>Returns a copy with <see cref="DefaultBackend"/> set to <see cref="BackendType.File"/> and the given <paramref name="subPath"/>.</summary>
        /// <param name="subPath">Subfolder under <c>Application.persistentDataPath</c>; defaults to <c>saves</c>. Pass <see langword="null"/> to keep the current value.</param>
        public LocalDataStorageConfig WithFileBackend(string subPath = null) =>
            new(BackendType.File, EncryptionEnabled, PassPhrase, subPath ?? FileSubPath, FileAbsoluteRoot);

        /// <summary>Returns a copy with <see cref="EncryptionEnabled"/> set and the provided pass phrase (or current one when <see langword="null"/>).</summary>
        /// <param name="passPhrase">Optional pass phrase override. Pass <see langword="null"/> to keep the current value.</param>
        public LocalDataStorageConfig WithEncryption(string passPhrase = null) =>
            new(DefaultBackend, encryptionEnabled: true, passPhrase ?? PassPhrase, FileSubPath, FileAbsoluteRoot);

        /// <summary>Returns a copy with encryption disabled. Debug and test workflows only.</summary>
        public LocalDataStorageConfig WithoutEncryption() =>
            new(DefaultBackend, encryptionEnabled: false, PassPhrase, FileSubPath, FileAbsoluteRoot);

        /// <summary>Returns a copy with an absolute file root (bypasses <c>persistentDataPath</c>). Used by tests.</summary>
        public LocalDataStorageConfig WithFileAbsoluteRoot(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath))
                throw new ArgumentException("Absolute path must be non-empty.", nameof(absolutePath));

            return new(DefaultBackend, EncryptionEnabled, PassPhrase, FileSubPath, absolutePath);
        }
    }
}
