using LocalDataStorage.Internal;
using VContainer;
using VContainer.Unity;

namespace LocalDataStorage.Installers.VContainer
{
    /// <summary>
    /// Registers LocalDataStorage package services in a VContainer container.
    /// </summary>
    public sealed class LocalDataStorageVContainerInstaller : IInstaller
    {
        /// <summary>
        /// Adds the package runtime services to the provided container builder.
        /// </summary>
        /// <param name="builder">Container builder used for service registrations.</param>
        public void Install(IContainerBuilder builder)
        {
            builder
                .Register<Internal.LocalDataStorage>(Lifetime.Singleton)
                .As<ILocalDataStorage>();
        }
    }
}
