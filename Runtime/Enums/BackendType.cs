namespace LocalDataStorage
{
    /// <summary>
    /// Pluggable storage backend selection for <see cref="ILocalDataStorage"/>. The default backend
    /// is chosen by the installer; a per-type <see cref="StorageAttribute"/> can override it.
    /// </summary>
    public enum BackendType
    {
        /// <summary>
        /// Encrypted blobs stored as <see cref="UnityEngine.PlayerPrefs"/> strings. Default in v1.x; small,
        /// synchronous, visible to Unity's PlayerPrefs tooling. Not suitable for payloads larger than
        /// a few hundred kilobytes or for cross-device cloud sync.
        /// </summary>
        PlayerPrefs = 0,

        /// <summary>
        /// Encrypted blobs stored as individual files under
        /// <c>Application.persistentDataPath/&lt;subPath&gt;/</c>. One file per
        /// <c>(type, slot)</c> pair. Atomic write via <c>tmp -> move</c> + rolling <c>.bak</c> for crash
        /// recovery. Preferred for larger payloads, cross-device cloud sync (Steam Cloud, iCloud,
        /// Google Play) and crash-safe autosaves.
        /// </summary>
        File = 1,
    }
}
