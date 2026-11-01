using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace LocalDataStorage.Internal
{
    /// <summary>
    /// Discovers <see cref="ILocalDataMigration"/> implementations through the
    /// <see cref="MigrationForAttribute"/> and runs the chain for a given <c>(type, fromVersion, toVersion)</c>.
    /// </summary>
    internal sealed class MigrationRegistry
    {
        // (target type, from version) -> step instance.
        private readonly Dictionary<(Type Target, int From), ILocalDataMigration> _steps = new();

        public MigrationRegistry()
        {
            Discover();
        }

        /// <summary>Reads the <see cref="DataVersionAttribute"/> on the type, defaulting to 1.</summary>
        public static int GetCurrentVersion(Type type)
        {
            if (type == null) return 1;
            if (type.GetCustomAttributes(typeof(DataVersionAttribute), inherit: false) is object[] attrs && attrs.Length > 0)
                return ((DataVersionAttribute)attrs[0]).Version;
            return 1;
        }

        /// <summary>
        /// Walks the migration chain for <paramref name="target"/> starting at <paramref name="fromVersion"/>
        /// up to the current version declared by <see cref="DataVersionAttribute"/>. Returns the
        /// migrated JSON payload or the original one when no steps apply.
        /// </summary>
        public JObject Migrate(Type target, int fromVersion, JObject payload)
        {
            int currentVersion = GetCurrentVersion(target);
            int cursor = fromVersion;
            JObject working = payload;

            while (cursor < currentVersion)
            {
                if (!_steps.TryGetValue((target, cursor), out ILocalDataMigration step))
                {
                    Debug.LogWarning($"[LocalDataStorage] No migration registered for {target.Name} from v{cursor}; stopping chain.");
                    break;
                }

                working = step.Migrate(working);
                cursor++;
            }

            return working;
        }

        private void Discover()
        {
            foreach (System.Reflection.Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try { types = assembly.GetTypes(); }
                catch (System.Reflection.ReflectionTypeLoadException ex) { types = ex.Types; }

                for (int i = 0; i < types.Length; i++)
                {
                    Type type = types[i];
                    if (type == null) continue;
                    if (!typeof(ILocalDataMigration).IsAssignableFrom(type)) continue;
                    if (type.IsAbstract || type.IsInterface) continue;

                    object[] attrs = type.GetCustomAttributes(typeof(MigrationForAttribute), inherit: false);
                    if (attrs.Length == 0) continue;

                    MigrationForAttribute attr = (MigrationForAttribute)attrs[0];
                    if (attr.To - attr.From != 1)
                    {
                        Debug.LogError($"[LocalDataStorage] Migration {type.Name} spans v{attr.From} -> v{attr.To}; only single-step migrations are supported. Split into multiple classes.");
                        continue;
                    }

                    ILocalDataMigration instance;
                    try { instance = (ILocalDataMigration)Activator.CreateInstance(type); }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[LocalDataStorage] Failed to instantiate migration {type.Name}: {ex.Message}");
                        continue;
                    }

                    var key = (attr.Target, attr.From);
                    if (_steps.ContainsKey(key))
                    {
                        Debug.LogWarning($"[LocalDataStorage] Duplicate migration for {attr.Target.Name} v{attr.From}; ignoring {type.Name}.");
                        continue;
                    }

                    _steps[key] = instance;
                }
            }
        }
    }
}
