# `ITenantMigrator<TKey>` interface

Namespace: `Tenantry.Pro.Lifecycle` · Package: `Tenantry.Pro` · [API reference](README.md)

Runs EF Core migrations for a single tenant's database. Implemented by `Tenantry.Pro.Migrations` and consumed by [`ITenantLifecycleManager<TKey>`](tenantry-pro-lifecycle-itenantlifecyclemanager.md) without exposing the `TContext` type parameter. The EF Core implementation is registered by `WithMigrationOrchestration`, which carries the trimming and Native AOT warnings, so this interface and the lifecycle pipeline do not.

```csharp
public interface ITenantMigrator<TKey> where TKey : IEquatable<TKey>, IParsable<TKey>
```

## Type parameters

- `TKey`: The tenant identifier type.

Derived types: [`MigrationOrchestratorService<TKey, TContext>`](tenantry-pro-efcore-migrations-migrationorchestratorservice.md).

## Methods

### `MigrateAsync(TKey, CancellationToken)`

Applies any pending EF Core migrations to the given tenant's database.

```csharp
Task MigrateAsync(TKey tenantId, CancellationToken cancellationToken = default)
```

Parameters:

- `tenantId` `TKey`: The tenant whose database should be migrated.
- `cancellationToken` `CancellationToken`: A cancellation token.

Returns: `Task`
