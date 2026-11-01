using VContainer;
using VContainer.Unity;

namespace LocalDataStorage.Installers.VContainer
{
    /// <summary>
    /// Registers LocalDataStorage package services in a VContainer container. Fluent configuration:
    /// <c>new LocalDataStorageVContainerInstaller().WithFileBackend().WithEncryption("my-game").Install(builder)</c>.
    /// </summary>
    public sealed class LocalDataStorageVContainerInstaller : IInstaller
    {
        private LocalDataStorageConfig _config = LocalDataStorageConfig.Default();

        /// <summary>Switches the default backend to <see cref="BackendType.File"/>.</summary>
        /// <param name="subPath">Optional subfolder under <c>Application.persistentDataPath</c>; defaults to <c>saves</c>.</param>
        public LocalDataStorageVContainerInstaller WithFileBackend(string subPath = null)
        {
            _config = _config.WithFileBackend(subPath);
            return this;
        }

        /// <summary>Switches the default backend to <see cref="BackendType.PlayerPrefs"/>.</summary>
        public LocalDataStorageVContainerInstaller WithPlayerPrefsBackend()
        {
            _config = _config.WithPlayerPrefsBackend();
            return this;
        }

        /// <summary>Enables AES encryption using the supplied pass phrase.</summary>
        /// <param name="passPhrase">Pass phrase; omit to keep the current one (default is the legacy "LocalDataStorage" literal).</param>
        public LocalDataStorageVContainerInstaller WithEncryption(string passPhrase = null)
        {
            _config = _config.WithEncryption(passPhrase);
            return this;
        }

        /// <summary>Disables AES encryption. Debug / test workflows only.</summary>
        public LocalDataStorageVContainerInstaller WithoutEncryption()
        {
            _config = _config.WithoutEncryption();
            return this;
        }

        /// <summary>Overrides the file backend root with an absolute path. Primarily for tests.</summary>
        public LocalDataStorageVContainerInstaller WithFileAbsoluteRoot(string absolutePath)
        {
            _config = _config.WithFileAbsoluteRoot(absolutePath);
            return this;
        }

        /// <summary>Replaces the config wholesale. Advanced scenarios only.</summary>
        public LocalDataStorageVContainerInstaller WithConfig(LocalDataStorageConfig config)
        {
            _config = config ?? LocalDataStorageConfig.Default();
            return this;
        }

        /// <inheritdoc />
        public void Install(IContainerBuilder builder)
        {
            builder.RegisterInstance(_config);
            builder
                .Register<Internal.LocalDataStorage>(Lifetime.Singleton)
                .As<ILocalDataStorage>();
        }
    }
}
