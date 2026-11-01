using System.Collections.Generic;

namespace LocalDataStorage
{
    /// <summary>
    /// Low-level key-value storage backend. Operates on raw byte blobs; encryption, JSON
    /// serialization and migration happen in <see cref="ILocalDataStorage"/> before/after a backend
    /// call. Implementations must make <see cref="Write"/> safe against crashes mid-write (atomic
    /// semantics or best-effort .bak recovery).
    /// </summary>
    public interface IStorageBackend
    {
        /// <summary>Writes raw bytes under the key. On failure the previous value should remain readable (atomic replace).</summary>
        /// <param name="key">Opaque storage key (produced by the storage from type + slot).</param>
        /// <param name="bytes">Raw payload. Length-0 or null is treated as a delete.</param>
        void Write(string key, byte[] bytes);

        /// <summary>Reads raw bytes stored under the key. Returns <see langword="null"/> when no entry exists.</summary>
        /// <param name="key">Opaque storage key.</param>
        /// <returns>Payload bytes or <see langword="null"/> when missing / unreadable.</returns>
        byte[] Read(string key);

        /// <summary>Removes the entry under the key. No-op when no entry exists.</summary>
        /// <param name="key">Opaque storage key.</param>
        void Delete(string key);

        /// <summary>Returns every key currently stored. Used by <c>DeleteAll</c> and <c>ListSlots&lt;T&gt;</c>.</summary>
        IEnumerable<string> EnumerateKeys();

        /// <summary>Removes every entry stored by this backend. May be destructive — only clears what this backend owns.</summary>
        void DeleteAll();
    }
}
