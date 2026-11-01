using Newtonsoft.Json.Linq;

namespace LocalDataStorage
{
    /// <summary>
    /// Converts a stored payload JSON from one schema version to the next. Discovered automatically
    /// through the <c>MigrationForAttribute</c> attribute scan at storage startup.
    /// </summary>
    public interface ILocalDataMigration
    {
        /// <summary>
        /// Mutates the raw JSON object in place or returns a new one. The returned <see cref="JObject"/>
        /// is passed to the next migration in the chain, or deserialized directly when it is the last step.
        /// </summary>
        /// <param name="payload">Parsed JSON of the stored data (already stripped of the version prefix).</param>
        /// <returns>Migrated JSON object.</returns>
        JObject Migrate(JObject payload);
    }
}
