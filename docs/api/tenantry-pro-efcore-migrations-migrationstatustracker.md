# `MigrationStatusTracker<TKey, TContext>` class

Namespace: `Tenantry.Pro.EfCore.Migrations` · Package: `Tenantry.Pro.EfCore` · [API reference](README.md)

Queries the applied and pending EF Core migrations for each tenant's database.

This service does not require a licence — it is read-only and performs no provisioning or schema changes.

```csharp
public sealed class MigrationStatusTracker<TKey, TContext> where TKey : IEquatable<TKey>, IParsable<TKey> where TContext : DbContext
```

## Type parameters

- `TKey`: The tenant identifier type.
- `TContext`: The consumer's `DbContext` type.

## Methods

### `GetStatusAsync(CancellationToken)`

Returns the migration status for every tenant in the store.

```csharp
[RequiresDynamicCode("EF Core migrations use dynamic code generation and are not AOT-safe. Use a migration bundle for AOT scenarios.")]
[RequiresUnreferencedCode("EF Core migrations use reflection and are not trim-safe. Use a migration bundle for trim scenarios.")]
public Task<IReadOnlyList<MigrationStatusEntry<TKey>>> GetStatusAsync(CancellationToken cancellationToken = default)
```

Parameters:

- `cancellationToken` `CancellationToken`: A cancellation token.

Returns: `Task<IReadOnlyList<MigrationStatusEntry<TKey>>>`

### `GetTenantStatusAsync(TKey, CancellationToken)`

Returns the migration status for a single tenant's database.

```csharp
[RequiresDynamicCode("EF Core migrations use dynamic code generation and are not AOT-safe. Use a migration bundle for AOT scenarios.")]
[RequiresUnreferencedCode("EF Core migrations use reflection and are not trim-safe. Use a migration bundle for trim scenarios.")]
public Task<MigrationStatusEntry<TKey>> GetTenantStatusAsync(TKey tenantId, CancellationToken cancellationToken = default)
```

Parameters:

- `tenantId` `TKey`: The tenant to inspect.
- `cancellationToken` `CancellationToken`: A cancellation token.

Returns: `Task<MigrationStatusEntry<TKey>>`

Exceptions:

- `InvalidOperationException`: The tenant was not found in the store.
