using LocalDataStorage.Internal;
using Reflex.Core;
using UnityEngine;

namespace LocalDataStorage.Installers.Reflex
{
    /// <summary>
    /// Registers LocalDataStorage package services in a Reflex container.
    /// </summary>
    public sealed class LocalDataStorageReflexInstaller : MonoBehaviour, IInstaller
    {
        /// <summary>
        /// Adds the package runtime services to the current Reflex container.
        /// </summary>
        /// <param name="builder">Container builder used for service registrations.</param>
        public void InstallBindings(ContainerBuilder builder)
        {
            builder.AddSingleton(
                typeof(LocalDataStorage),
                typeof(ILocalDataStorage));
        }
    }
}
