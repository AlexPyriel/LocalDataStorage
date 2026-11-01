using System;

namespace LocalDataStorage
{
    /// <summary>
    /// Overrides the installer-configured default backend for a single <see cref="LocalData"/> type.
    /// Attach to a concrete <c>LocalData</c>-derived class to pin it to a specific
    /// <see cref="BackendType"/> regardless of global installer configuration — useful when most
    /// game data lives on the file backend but a small type (e.g. volatile settings) is better served
    /// by PlayerPrefs.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class StorageAttribute : Attribute
    {
        /// <summary>Backend used for this specific type.</summary>
        public BackendType Backend { get; }

        /// <summary>Creates the override.</summary>
        /// <param name="backend">Backend to force for this type.</param>
        public StorageAttribute(BackendType backend)
        {
            Backend = backend;
        }
    }
}
