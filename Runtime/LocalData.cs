using System;

namespace LocalDataStorage
{
    /// <summary>
    /// Serves as the serializable base type for payloads persisted through <see cref="ILocalDataStorage" />.
    /// </summary>
    [Serializable]
    public abstract class LocalData
    {
    }
}
