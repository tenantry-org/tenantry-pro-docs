# `IConnectionStringCache<TKey>` interface

Namespace: `Tenantry.Pro.Strategies.DatabasePerTenant` · Package: `Tenantry.Pro` · [API reference](README.md)

Allows invalidating cached connection strings for a specific tenant. Inject this when a tenant's database connection details change and the in-memory cache needs to be cleared before the TTL expires.

```csharp
public interface IConnectionStringCache<in TKey> where TKey : IEquatable<in TKey>, IParsable<in TKey>
```

## Type parameters

- `TKey`: The tenant identifier type.

## Methods

### `Invalidate(TKey)`

Removes the cached connection string for `tenantId`. The next call to `Resolve` / `ResolveAsync` will re-resolve from the delegate.

```csharp
void Invalidate(TKey tenantId)
```

Parameters:

- `tenantId` `TKey`: The identifier of the tenant whose cached connection string to remove.
