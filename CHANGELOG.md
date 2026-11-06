# Changelog

## [3.0.0] - 2026-11-06

Breaking release for projects on Zenject or Reflex. VContainer projects upgrade with no code changes.

### Changed

- `LocalDataStorage.Internal.LocalDataStorage` is `public sealed`, so any DI container can bind it.

### Removed

- Zenject installer (`LocalDataStorageZenjectInstaller`) and Reflex installer
  (`LocalDataStorageReflexInstaller`). The README section "Using with Zenject / Reflex" shows the
  replacement bindings.

## [2.0.0] - 2026-11-01

Breaking release. Keeps the v1 synchronous API intact — single-arg
`Save<T>(T)` / `TryLoad<T>(out T)` / `Delete<T>()` / `DeleteAll()` still work and
read existing v1 PlayerPrefs saves unchanged. Downstream packages that only used
these calls recompile against v2 with no code changes.

### Added

- **Pluggable storage backends.** `IStorageBackend` abstraction with two
  built-in implementations: `PlayerPrefsStorageBackend` (v1 layout) and
  `FileStorageBackend` (one `.sav` per key under
  `Application.persistentDataPath/saves/`, atomic `tmp -> move + .bak` writes,
  WebGL IDBFS auto-flush).
- **`[Storage(BackendType.File)]` attribute** to override the installer-level
  default backend for a single `LocalData` type.
- **Slots.** `Save<T>(string slot, T)` / `TryLoad<T>(string slot, out T)` /
  `Delete<T>(string slot)` / `ListSlots<T>()` for multi-slot workflows
  (profiles, autosaves). The empty slot is the default slot used by the
  single-arg overloads.
- **Schema migrations.** `[DataVersion(N)]` on the data class and
  `[MigrationFor(typeof(T), from, to)]` on `ILocalDataMigration`
  implementations. Steps are discovered on startup and run as a chain against
  a Newtonsoft `JObject` before deserialization. Classes without the attribute
  are implicitly version 1.
- **Change events.** `event Action<Type> DataSaved`, `event Action<Type>
  DataDeleted`, `event Action AllDeleted`.
- **R3 adapter.** New `GDF.LocalDataStorage.R3` assembly exposes `OnDataSaved`,
  `OnDataSaved<T>()`, `OnDataDeleted`, `OnDataDeleted<T>()`, `OnAllDeleted` as
  R3 `Observable<T>` streams for debouncing, filtering and lifetime binding.
- **Async API.** `SaveAsync<T>` / `TryLoadAsync<T>` / `DeleteAsync<T>` /
  `DeleteAllAsync` run serialization and encryption on the thread pool via
  UniTask. Returns `(bool Found, T Data)` so callers can `await` directly.
- **Fluent installer configuration.** `.WithFileBackend(subPath)`,
  `.WithPlayerPrefsBackend()`, `.WithEncryption(passPhrase)`,
  `.WithoutEncryption()` on the VContainer installer; equivalent fields on the
  Zenject and Reflex installers.

### Changed

- `AES encryption` can be disabled through `.WithoutEncryption()` for debug or
  tests. Default remains AES-256 with the legacy `"LocalDataStorage"` pass
  phrase (configurable).
- Payload layout now carries a 4-byte little-endian version prefix before the
  JSON. v1 payloads (no prefix, JSON starting with `{` / `[`) are detected
  automatically and treated as schema version 1 for the migration chain.
- Package now depends on `com.cysharp.unitask` (async API) and
  `com.unity.nuget.newtonsoft-json` (migration payload manipulation).

### Migration guide (v1 → v2)

1. Bump the dependency in `Packages/manifest.json` to
   `"com.gdf.local-data-storage": "https://github.com/AlexPyriel/LocalDataStorage.git#v2.0.0"`.
2. Existing `Save<T>` / `TryLoad<T>` / `Delete<T>` / `DeleteAll` calls keep
   working without modifications.
3. Existing PlayerPrefs saves are read as schema v1 and migrated on load (no
   explicit migration if the schema has not changed).
4. If the installer previously passed no configuration, the defaults (PlayerPrefs
   backend + AES + legacy pass phrase) match v1 behavior exactly.
5. To opt into the file backend:
   `new LocalDataStorageVContainerInstaller().WithFileBackend().Install(builder);`
   Mix with `[Storage(BackendType.PlayerPrefs)]` on individual types to keep a
   subset on PlayerPrefs.

## [1.0.0] - 2026-05-26

- Initial public release of the package.
- Encrypted `PlayerPrefs`-backed local storage runtime with typed save payloads.
- `TryLoad<TData>(out TData data)`-based read contract.
- Persistent key registry for package-scoped `DeleteAll()` behavior.
- Package-owned installer assemblies for `VContainer`, `Zenject`, and `Reflex`.
- EditMode coverage for roundtrip, delete, cache-clearing, and invalid-payload scenarios.
