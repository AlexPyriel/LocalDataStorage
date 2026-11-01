using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace LocalDataStorage
{
    /// <summary>
    /// Provides typed local persistence for serializable save payloads with optional slots,
    /// crash-safe file storage, schema migrations and change notifications. Save payload types
    /// derive from <see cref="LocalData"/> and may carry <see cref="StorageAttribute"/> and
    /// <see cref="DataVersionAttribute"/>.
    /// </summary>
    public interface ILocalDataStorage
    {
        /// <summary>
        /// Raised after a payload is successfully persisted. The argument is the concrete runtime
        /// type of the saved payload. Fired on the thread that completed the save (for async
        /// variants this may be a thread-pool thread).
        /// </summary>
        event Action<Type> DataSaved;

        /// <summary>Raised after a payload is deleted. The argument is the deleted type.</summary>
        event Action<Type> DataDeleted;

        /// <summary>Raised once after <see cref="DeleteAll"/> clears every stored entry.</summary>
        event Action AllDeleted;

        /// <summary>Persists the payload in the default slot. Passing <see langword="null"/> clears the stored value.</summary>
        /// <typeparam name="TData">Serializable payload type used as the storage key.</typeparam>
        /// <param name="data">Payload instance to store.</param>
        void Save<TData>(TData data) where TData : LocalData;

        /// <summary>Persists the payload in the named slot. Passing <see langword="null"/> clears the stored value for that slot.</summary>
        /// <typeparam name="TData">Serializable payload type used as the storage key.</typeparam>
        /// <param name="slot">Slot name. An empty slot is the default slot used by the single-arg overload.</param>
        /// <param name="data">Payload instance to store.</param>
        void Save<TData>(string slot, TData data) where TData : LocalData;

        /// <summary>Loads the payload from the default slot.</summary>
        /// <typeparam name="TData">Serializable payload type used as the storage key.</typeparam>
        /// <param name="data">Loaded payload instance when the operation succeeds; otherwise <see langword="null"/>.</param>
        /// <returns><see langword="true"/> when a valid stored payload exists; otherwise <see langword="false"/>.</returns>
        bool TryLoad<TData>(out TData data) where TData : LocalData;

        /// <summary>Loads the payload from the named slot.</summary>
        /// <typeparam name="TData">Serializable payload type used as the storage key.</typeparam>
        /// <param name="slot">Slot name. An empty slot is the default slot.</param>
        /// <param name="data">Loaded payload instance when the operation succeeds; otherwise <see langword="null"/>.</param>
        /// <returns><see langword="true"/> when a valid stored payload exists; otherwise <see langword="false"/>.</returns>
        bool TryLoad<TData>(string slot, out TData data) where TData : LocalData;

        /// <summary>Removes the default slot payload.</summary>
        /// <typeparam name="TData">Serializable payload type used as the storage key.</typeparam>
        void Delete<TData>() where TData : LocalData;

        /// <summary>Removes the payload stored under the named slot.</summary>
        /// <typeparam name="TData">Serializable payload type used as the storage key.</typeparam>
        /// <param name="slot">Slot name.</param>
        void Delete<TData>(string slot) where TData : LocalData;

        /// <summary>Enumerates slot names populated for the given type (including the default slot as an empty string).</summary>
        /// <typeparam name="TData">Serializable payload type.</typeparam>
        IEnumerable<string> ListSlots<TData>() where TData : LocalData;

        /// <summary>Removes every payload managed by the storage. Fires <see cref="AllDeleted"/>.</summary>
        void DeleteAll();

        /// <summary>Async equivalent of <see cref="Save{TData}(TData)"/>. Serialization and encryption run on the thread pool.</summary>
        UniTask SaveAsync<TData>(TData data, CancellationToken cancellationToken = default) where TData : LocalData;

        /// <summary>Async equivalent of <see cref="Save{TData}(string, TData)"/>.</summary>
        UniTask SaveAsync<TData>(string slot, TData data, CancellationToken cancellationToken = default) where TData : LocalData;

        /// <summary>Async equivalent of <see cref="TryLoad{TData}(out TData)"/>. Returns a tuple so callers can keep <c>await</c>.</summary>
        UniTask<(bool Found, TData Data)> TryLoadAsync<TData>(CancellationToken cancellationToken = default) where TData : LocalData;

        /// <summary>Async equivalent of <see cref="TryLoad{TData}(string, out TData)"/>.</summary>
        UniTask<(bool Found, TData Data)> TryLoadAsync<TData>(string slot, CancellationToken cancellationToken = default) where TData : LocalData;

        /// <summary>Async equivalent of <see cref="Delete{TData}(string)"/>.</summary>
        UniTask DeleteAsync<TData>(string slot = "", CancellationToken cancellationToken = default) where TData : LocalData;

        /// <summary>Async equivalent of <see cref="DeleteAll"/>.</summary>
        UniTask DeleteAllAsync(CancellationToken cancellationToken = default);
    }
}
