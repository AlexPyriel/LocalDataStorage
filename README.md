# GDF Local Data Storage

Typed, encrypted local data storage for Unity. Pluggable backends (PlayerPrefs or crash-safe file),
slot-aware keys, schema migrations, change events, synchronous and UniTask-based async APIs,
DI installers for VContainer / Zenject / Reflex.

Package name: `com.gdf.local-data-storage`
Assembly name: `GDF.LocalDataStorage`

## Features

- Typed save/load through `ILocalDataStorage.Save<T>(T)` / `TryLoad<T>(out T)` / `Delete<T>()` / `DeleteAll()`.
- **Pluggable backends.** `PlayerPrefsStorageBackend` (v1 layout, backward-compatible) and
  `FileStorageBackend` (one `.sav` per key under `Application.persistentDataPath/saves/`, atomic
  `tmp -> move + .bak` writes, WebGL IDBFS auto-flush).
- **Per-type backend override** via `[Storage(BackendType.File)]` on a `LocalData` subclass.
- **Slots.** `Save<T>(string slot, T)`, `TryLoad<T>(string slot, out T)`, `ListSlots<T>()` — multi-slot
  profiles, autosaves and NG+ branches without duplicate types.
- **Schema migrations.** `[DataVersion(N)]` on data + `[MigrationFor(type, from, to)]` on migration
  steps that implement `ILocalDataMigration`. Steps run as a chain against Newtonsoft `JObject`
  before deserialization.
- **Change events.** `DataSaved<Type>`, `DataDeleted<Type>`, `AllDeleted`.
- **R3 adapter** in a separate assembly: `OnDataSaved<T>()` / `OnDataDeleted<T>()` / `OnAllDeleted`
  as R3 `Observable` streams.
- **Async API.** `SaveAsync` / `TryLoadAsync` / `DeleteAsync` / `DeleteAllAsync` via UniTask on the
  thread pool.
- **AES-256 encryption** at rest with per-entry salt and IV, PBKDF2 key derivation. Opt-out via
  `.WithoutEncryption()` for debug workflows.
- **Fluent installer configuration** on VContainer / Zenject / Reflex.
- Backward-compatible with v1.0.0 PlayerPrefs saves — reads legacy payloads automatically.

## Requirements

- Unity `6000.0+`
- `com.cysharp.unitask`
- `com.unity.nuget.newtonsoft-json`
- One of the supported DI containers (optional, required only when using the matching installer):
  - `jp.hadashikick.vcontainer`
  - `com.mathijsbakker.extenject`
  - `com.gustavopsantos.reflex`
- `com.cysharp.r3` is required only when using the `GDF.LocalDataStorage.R3` adapter assembly.

## Installation (UPM via Git)

Add this dependency to `Packages/manifest.json`:

```json
"com.gdf.local-data-storage": "https://github.com/AlexPyriel/LocalDataStorage.git#v2.0.0"
```

For development tracking (not recommended for production), use `#main` instead of a tag.

You can also install it from Unity Package Manager:

- `Window -> Package Manager -> + -> Add package from git URL...`
- `https://github.com/AlexPyriel/LocalDataStorage.git#v2.0.0`

## Usage

### 1. Register the service

VContainer, with the file backend and a custom pass phrase:

```csharp
protected override void Configure(IContainerBuilder builder)
{
    new LocalDataStorageVContainerInstaller()
        .WithFileBackend()                          // or .WithPlayerPrefsBackend()
        .WithEncryption("MyGame-v1-salt")           // or .WithoutEncryption()
        .Install(builder);
}
```

### 2. Define a save payload

```csharp
[DataVersion(2)]
[Serializable]
public sealed class PlayerData : LocalData
{
    public int Coins;
    public int Gems;        // added in v2
}

// Settings is small and read on boot — keep it on PlayerPrefs.
[Storage(BackendType.PlayerPrefs)]
[Serializable]
public sealed class SettingsData : LocalData
{
    public float Volume;
}
```

### 3. Save / load

```csharp
_storage.Save(new PlayerData { Coins = 100, Gems = 5 });

if (_storage.TryLoad(out PlayerData loaded))
    Debug.Log(loaded.Coins);

// Slots
_storage.Save("slot1", new PlayerData { Coins = 200 });
_storage.Save("slot2", new PlayerData { Coins = 300 });
foreach (var slot in _storage.ListSlots<PlayerData>())
    Debug.Log(slot);
```

### 4. Migrate between schema versions

```csharp
[MigrationFor(typeof(PlayerData), from: 1, to: 2)]
public sealed class PlayerData_1_to_2 : ILocalDataMigration
{
    public JObject Migrate(JObject payload)
    {
        payload["Gems"] = 0;        // default value for the new field
        return payload;
    }
}
```

The chain runs automatically on `TryLoad` when the stored version is below the current
`[DataVersion(N)]`. Migration steps are discovered through assembly scanning at startup.

### 5. Observe changes

```csharp
_storage.DataSaved += type => cloudSync.PushAsync(type);

// With R3 (via GDF.LocalDataStorage.R3):
_storage.OnDataSaved<PlayerData>()
    .Debounce(TimeSpan.FromSeconds(1))
    .Subscribe(_ => cloudSync.PushAsync());
```

### 6. Async

```csharp
await _storage.SaveAsync(new PlayerData { Coins = 999 });
var (found, data) = await _storage.TryLoadAsync<PlayerData>();
```

## Assembly Definition Notes

`GDF.LocalDataStorage` is the runtime assembly. The R3 bridge lives in a separate assembly
(`GDF.LocalDataStorage.R3`) so games that do not use R3 incur no reference. DI installers are
in `GDF.LocalDataStorage.Installers.{VContainer,Zenject,Reflex}` with `InternalsVisibleTo` so
they can see the `Internal` orchestrator type without widening the public surface.

## Crash safety

The file backend promises atomic replacement for every write:

1. The payload is written to `<key>.sav.tmp`.
2. The current `<key>.sav` is renamed to `<key>.sav.bak` (previous backup is deleted first).
3. `<key>.sav.tmp` is renamed to `<key>.sav`.

If a crash interrupts step 1 or 2, the previous `<key>.sav` remains intact. If `<key>.sav` is
missing or unreadable on load, the backup is used and the primary is restored from it.

## License

MIT. See `LICENSE.md`.
