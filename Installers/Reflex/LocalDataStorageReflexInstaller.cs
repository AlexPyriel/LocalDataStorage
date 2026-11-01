using LocalDataStorage.Internal;
using Reflex.Core;
using UnityEngine;

namespace LocalDataStorage.Installers.Reflex
{
    /// <summary>
    /// Registers LocalDataStorage package services in a Reflex container. Configure through the
    /// serialized fields in the inspector; the installer forwards them into a
    /// <see cref="LocalDataStorageConfig"/> before binding.
    /// </summary>
    public sealed class LocalDataStorageReflexInstaller : MonoBehaviour, IInstaller
    {
        [SerializeField]
        [Tooltip("Backend used for types without a [Storage] override.")]
        private BackendType _defaultBackend = BackendType.PlayerPrefs;

        [SerializeField]
        [Tooltip("Enable AES-256 encryption at rest. Disable only for debug / tests.")]
        private bool _encryptionEnabled = true;

        [SerializeField]
        [Tooltip("Pass phrase used for AES derivation. Leave empty to use the default literal.")]
        private string _passPhrase = LocalDataStorageConfig.DEFAULT_PASS_PHRASE;

        [SerializeField]
        [Tooltip("Subfolder under Application.persistentDataPath used by the file backend.")]
        private string _fileSubPath = LocalDataStorageConfig.DEFAULT_FILE_SUB_PATH;

        /// <inheritdoc />
        public void InstallBindings(ContainerBuilder builder)
        {
            LocalDataStorageConfig config = _defaultBackend == BackendType.File
                ? LocalDataStorageConfig.Default().WithFileBackend(_fileSubPath)
                : LocalDataStorageConfig.Default().WithPlayerPrefsBackend();

            config = _encryptionEnabled
                ? config.WithEncryption(string.IsNullOrEmpty(_passPhrase) ? null : _passPhrase)
                : config.WithoutEncryption();

            builder.AddSingleton(config, typeof(LocalDataStorageConfig));
            builder.AddSingleton(
                typeof(LocalDataStorage),
                typeof(ILocalDataStorage));
        }
    }
}
