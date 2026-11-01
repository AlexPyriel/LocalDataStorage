using System;

namespace LocalDataStorage
{
    /// <summary>
    /// Declares the current schema version of a <see cref="LocalData"/>-derived type. Payloads loaded
    /// from disk whose stored version is below this value are passed through the registered migration
    /// chain (see <c>MigrationForAttribute</c>) before deserialization. Classes without the attribute
    /// are implicitly version <c>1</c>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class DataVersionAttribute : Attribute
    {
        /// <summary>Current schema version (1 or higher).</summary>
        public int Version { get; }

        /// <summary>Declares the schema version.</summary>
        /// <param name="version">Positive integer representing the current schema version.</param>
        public DataVersionAttribute(int version)
        {
            if (version < 1)
                throw new ArgumentOutOfRangeException(nameof(version), "Data version must be at least 1.");

            Version = version;
        }
    }
}
