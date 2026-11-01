using System;

namespace LocalDataStorage
{
    /// <summary>
    /// Marks a class implementing <see cref="ILocalDataMigration"/> as the migration step for a
    /// specific <see cref="LocalData"/> type between two schema versions. Discovered automatically
    /// at storage startup through reflection over loaded assemblies.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class MigrationForAttribute : Attribute
    {
        /// <summary>Target <see cref="LocalData"/> type.</summary>
        public Type Target { get; }

        /// <summary>Schema version the migration starts from.</summary>
        public int From { get; }

        /// <summary>Schema version the migration produces.</summary>
        public int To { get; }

        public MigrationForAttribute(Type target, int from, int to)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            if (from < 1)
                throw new ArgumentOutOfRangeException(nameof(from), "from version must be at least 1.");
            if (to <= from)
                throw new ArgumentOutOfRangeException(nameof(to), "to must be strictly greater than from.");

            Target = target;
            From = from;
            To = to;
        }
    }
}
