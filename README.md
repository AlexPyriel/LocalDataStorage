# GDF Local Data Storage

Local data storage package for Unity projects.

Package name: `com.gdf.local-data-storage`  
Assembly name: `GDF.LocalDataStorage`

## Features

- Public API: `ILocalDataStorage` and `LocalData`.
- Default runtime implementation: `Internal/LocalDataStorage.cs`.
- JSON serialization through `UnityEngine.JsonUtility`.
- Encrypted persistence in `PlayerPrefs`.
- In-memory cache for repeated reads within the same session.
- Optional installer assemblies for `VContainer`, `Zenject`, and `Reflex`.

## Requirements

- Unity `6000.0+`
- `com.unity.modules.jsonserialize`

## Installation (UPM via Git)

Add this dependency to `Packages/manifest.json`:

```json
"com.gdf.local-data-storage": "https://github.com/AlexPyriel/LocalDataStorage.git#v1.0.0"
```

For development tracking (not recommended for production), use `#main` instead of a tag.

You can also install it from Unity Package Manager:

- `Window -> Package Manager -> + -> Add package from git URL...`
- `https://github.com/AlexPyriel/LocalDataStorage.git#v1.0.0`

## Usage

Define a serializable save model by inheriting from `LocalData`, then resolve `ILocalDataStorage` from your DI container.

```csharp
using System;
using LocalDataStorage;

[Serializable]
public sealed class PlayerProgressData : LocalData
{
    public int Level;
    public int Coins;
}

ILocalDataStorage storage = ...;
storage.Save(new PlayerProgressData { Level = 3, Coins = 125 });

if (storage.TryLoad(out PlayerProgressData progress))
{
    storage.Delete<PlayerProgressData>();
}
```

`DeleteAll()` clears only entries managed by the package and resets the in-memory cache.

### VContainer

```csharp
new LocalDataStorage.Installers.VContainer.LocalDataStorageVContainerInstaller().Install(builder);
```

### Zenject

```csharp
LocalDataStorage.Installers.Zenject.LocalDataStorageZenjectInstaller.Install(Container);
```

### Reflex

Add `LocalDataStorageReflexInstaller` to your Reflex project scope and let Reflex discover it as an `IInstaller`.

## Current Limitations

- The default backend persists data through `PlayerPrefs`.
- Package-owned keys are tracked through a persistent registry stored in `PlayerPrefs`.
- Storage keys are derived from `typeof(TData).ToString()`.
- The package currently exposes a single built-in encrypted runtime implementation.

## Assembly Definition Notes

`GDF.LocalDataStorage` is a dedicated runtime assembly.

The package follows the standard UPM layout with runtime code under `Runtime/`, optional DI integrations under
`Installers/`, and package tests under `Tests/`.

EditMode coverage is intended for save/load roundtrips, delete behavior, cache clearing, and invalid payload handling.
