namespace LocalDataStorage
{
    /// <summary>
    /// Provides typed local persistence for serializable save payloads.
    /// </summary>
    public interface ILocalDataStorage
    {
        /// <summary>
        /// Persists the provided payload under the type-specific storage key.
        /// </summary>
        /// <typeparam name="TData">Serializable payload type used as the storage key.</typeparam>
        /// <param name="data">Payload instance to store. Passing <see langword="null" /> clears the stored value for the type.</param>
        public void Save<TData>(TData data) where TData : LocalData;

        /// <summary>
        /// Attempts to load the payload stored for the requested type.
        /// </summary>
        /// <typeparam name="TData">Serializable payload type used as the storage key.</typeparam>
        /// <param name="data">Loaded payload instance when the operation succeeds; otherwise <see langword="null" />.</param>
        /// <returns><see langword="true" /> when a valid stored payload exists; otherwise <see langword="false" />.</returns>
        public bool TryLoad<TData>(out TData data) where TData : LocalData;

        /// <summary>
        /// Removes the payload stored for the requested type.
        /// </summary>
        /// <typeparam name="TData">Serializable payload type used as the storage key.</typeparam>
        public void Delete<TData>() where TData : LocalData;

        /// <summary>
        /// Removes every payload managed by the underlying local storage backend.
        /// </summary>
        public void DeleteAll();
    }
}
