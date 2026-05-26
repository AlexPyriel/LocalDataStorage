using LocalDataStorage.Internal;
using Zenject;

namespace LocalDataStorage.Installers.Zenject
{
    /// <summary>
    /// Registers LocalDataStorage package services in a Zenject container.
    /// </summary>
    public sealed class LocalDataStorageZenjectInstaller : Installer<LocalDataStorageZenjectInstaller>
    {
        /// <summary>
        /// Adds the package runtime services to the current container.
        /// </summary>
        public override void InstallBindings()
        {
            Container
                .Bind<ILocalDataStorage>()
                .To<LocalDataStorage>()
                .AsSingle();
        }
    }
}
