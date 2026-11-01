using LocalDataStorage.Internal;
using Zenject;

namespace LocalDataStorage.Installers.Zenject
{
    /// <summary>
    /// Registers LocalDataStorage package services in a Zenject container. Build the config via the
    /// static <c>Configure</c> entry points and bind as usual.
    /// </summary>
    public sealed class LocalDataStorageZenjectInstaller : Installer<LocalDataStorageConfig, LocalDataStorageZenjectInstaller>
    {
        [Inject]
        private LocalDataStorageConfig _config;

        /// <summary>Convenience factory producing an installer bound to a default config.</summary>
        public static LocalDataStorageConfig DefaultConfig() => LocalDataStorageConfig.Default();

        /// <inheritdoc />
        public override void InstallBindings()
        {
            Container.BindInstance(_config);
            Container
                .Bind<ILocalDataStorage>()
                .To<LocalDataStorage>()
                .AsSingle();
        }
    }
}
