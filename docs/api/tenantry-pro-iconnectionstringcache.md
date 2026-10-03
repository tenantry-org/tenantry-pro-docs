# `IConnectionStringCache<TKey>` interface

Namespace: `Tenantry.Pro` · Package: `Tenantry.Pro` · [API reference](README.md)

Removes cached connection strings, so the next read calls the connection-string delegates again. Use it when a tenant's connection details change before its cached connection string expires.

`UsePro` always registers it, as a singleton. Without `pro.CacheConnectionStrings(...)` nothing is cached, so it has nothing to remove.

```csharp
public interface IConnectionStringCache<in TKey> where TKey : IEquatable<TKey>, IParsable<TKey>
```

## Type parameters

- `TKey`: The tenant identifier type.

## Methods

### `Invalidate(TKey)`

Removes the cached connection string of the tenant `tenantId`.

```csharp
void Invalidate(TKey tenantId)
```

Parameters:

- `tenantId` `TKey`: The tenant whose connection string to remove.

### `InvalidateAll()`

Removes every tenant's cached connection string.

```csharp
void InvalidateAll()
```
