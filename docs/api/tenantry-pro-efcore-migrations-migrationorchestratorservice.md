# `MigrationOrchestratorService<TKey, TContext>` class

Namespace: `Tenantry.Pro.EfCore.Migrations` · Package: `Tenantry.Pro.EfCore` · [API reference](README.md)

Runs pending EF Core migrations across all tenant databases discovered via [`ITenantStore<TKey>`](https://tenantry.dev/docs/core/api/tenantry-core-itenantstore).

Register via `pro.WithMigrationOrchestration<AppDbContext>(connectionString => ...)`     inside `tenant.UsePro(...)`. Inject this service in an admin endpoint, a background job,     or enable `runAtStartup: true` to run migrations automatically on each deployment.

**Failure isolation:** A failure migrating one tenant's database never aborts     migrations for other tenants. All per-tenant outcomes are captured in the returned     [`MigrationReport<TKey>`](tenantry-pro-efcore-migrations-migrationreport.md).

**Concurrency:** Migrations are applied sequentially, not in parallel, to avoid     overwhelming the database server under CI or production conditions.

```csharp
public sealed class MigrationOrchestratorService<TKey, TContext> : ITenantMigrator<TKey> where TKey : IEquatable<TKey>, IParsable<TKey> where TContext : DbContext
```

## Type parameters

- `TKey`: The tenant identifier type.
- `TContext`: The consumer's `DbContext` type.

Implements [`ITenantMigrator<TKey>`](tenantry-pro-lifecycle-itenantmigrator.md).

## Methods

### `MigrateAllAsync(CancellationToken)`

Applies pending EF Core migrations to every tenant database in the store.

```csharp
[RequiresDynamicCode("EF Core migrations use dynamic code generation and are not AOT-safe. Use a migration bundle for AOT scenarios.")]
[RequiresUnreferencedCode("EF Core migrations use reflection and are not trim-safe. Use a migration bundle for trim scenarios.")]
public Task<MigrationReport<TKey>> MigrateAllAsync(CancellationToken cancellationToken = default)
```

Parameters:

- `cancellationToken` `CancellationToken`: A cancellation token.

Returns: `Task<MigrationReport<TKey>>`: A [`MigrationReport<TKey>`](tenantry-pro-efcore-migrations-migrationreport.md) containing per-tenant results. A tenant whose migration fails is captured in the report and the run continues with the next tenant.

Exceptions:

- `OperationCanceledException`: `cancellationToken` was cancelled. The run stops at once and the remaining tenants are not migrated; tenants already migrated stay migrated.
- [`LicenseRequiredException`](tenantry-pro-exceptions-licenserequiredexception.md): Thrown before any migration is attempted if the licence key is missing or invalid.

### `MigrateAsync(TKey, CancellationToken)`

Applies pending EF Core migrations to a single tenant's database. This is the non-generic bridge implementation of [`ITenantMigrator<TKey>`](tenantry-pro-lifecycle-itenantmigrator.md) used by `ITenantLifecycleManager`.

```csharp
[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Registered only by WithMigrationOrchestration, which requires unreferenced and dynamic code.")]
[UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Registered only by WithMigrationOrchestration, which requires unreferenced and dynamic code.")]
public Task MigrateAsync(TKey tenantId, CancellationToken cancellationToken = default)
```

Parameters:

- `tenantId` `TKey`: The identifier of the tenant whose database to migrate.
- `cancellationToken` `CancellationToken`: Cancels the migration.

Returns: `Task`

### `MigrateTenantAsync(TKey, CancellationToken)`

Applies pending EF Core migrations to a single named tenant's database.

```csharp
[RequiresDynamicCode("EF Core migrations use dynamic code generation and are not AOT-safe. Use a migration bundle for AOT scenarios.")]
[RequiresUnreferencedCode("EF Core migrations use reflection and are not trim-safe. Use a migration bundle for trim scenarios.")]
public Task<MigrationResult<TKey>> MigrateTenantAsync(TKey tenantId, CancellationToken cancellationToken = default)
```

Parameters:

- `tenantId` `TKey`: The tenant to migrate.
- `cancellationToken` `CancellationToken`: A cancellation token.

Returns: `Task<MigrationResult<TKey>>`: A [`MigrationResult<TKey>`](tenantry-pro-efcore-migrations-migrationresult.md) for the specified tenant. On migration failure the result has [`MigrationResult<TKey>.Succeeded`](tenantry-pro-efcore-migrations-migrationresult.md) = false rather than throwing.

Exceptions:

- `OperationCanceledException`: `cancellationToken` was cancelled.
- [`LicenseRequiredException`](tenantry-pro-exceptions-licenserequiredexception.md): The licence key is missing or invalid.
- `InvalidOperationException`: The tenant was not found in the store.
