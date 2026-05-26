# Changelog

## [1.0.0] - 2026-05-26

- Initial public release of the package.
- Encrypted `PlayerPrefs`-backed local storage runtime with typed save payloads.
- `TryLoad<TData>(out TData data)`-based read contract.
- Persistent key registry for package-scoped `DeleteAll()` behavior.
- Package-owned installer assemblies for `VContainer`, `Zenject`, and `Reflex`.
- EditMode coverage for roundtrip, delete, cache-clearing, and invalid-payload scenarios.
