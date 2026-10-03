# `TenantMigrationRunnerExtensions` class

Namespace: `Tenantry.Pro.EfCore` · Package: `Tenantry.Pro.EfCore` · [API reference](README.md)

Shorter forms of [`ITenantMigrationRunner<TKey>`](tenantry-pro-efcore-itenantmigrationrunner.md)'s runs and reads: every tenant, or one.

```csharp
public static class TenantMigrationRunnerExtensions
```

## Methods

### `GetTenantStatusAsync<TKey>(ITenantMigrationRunner<TKey>, TKey, CancellationToken)`

Reads, for one tenant, which of each context's migrations are applied and which are pending.

```csharp
[RequiresDynamicCode("EF Core migrations use dynamic code generation and are not Native AOT-compatible. Use a migration bundle for AOT applications.")]
[RequiresUnreferencedCode("EF Core migrations use reflection and are not trim-safe. Use a migration bundle for trimmed applications.")]
public static Task<IReadOnlyList<MigrationStatusEntry<TKey>>> GetTenantStatusAsync<TKey>(this ITenantMigrationRunner<TKey> runner, TKey tenantId, CancellationToken cancellationToken = default) where TKey : IEquatable<TKey>, IParsable<TKey>
```

Type parameters:

- `TKey`: The tenant identifier type.

Parameters:

- `runner` [`ITenantMigrationRunner<TKey>`](tenantry-pro-efcore-itenantmigrationrunner.md): The runner.
- `tenantId` `TKey`: The tenant to read.
- `cancellationToken` `CancellationToken`: Cancels the read.

Returns: `Task<IReadOnlyList<MigrationStatusEntry<TKey>>>`: An entry for each context; one that cannot be read has [`MigrationStatusEntry<TKey>.Error`](tenantry-pro-efcore-migrationstatusentry.md) set.

Exceptions:

- `ArgumentNullException`: `runner` or `tenantId` is null.
- `ArgumentException`: `tenantId` is one Tenantry reserves for "no tenant": the key type's default (`Guid.Empty`, `0`) or an empty string.
- `TenantNotFoundException`: The tenant store has no tenant `tenantId`.
- `OperationCanceledException`: `cancellationToken` was cancelled.

### `MigrateAllAsync<TKey>(ITenantMigrationRunner<TKey>, IProgress<MigrationResult<TKey>>?, CancellationToken)`

Applies each context's pending migrations for every tenant in the store, reporting each database's or schema's result as it completes, as [`ITenantMigrationRunner<TKey>.MigrateAsync`](tenantry-pro-efcore-itenantmigrationrunner.md) describes.

```csharp
[RequiresDynamicCode("EF Core migrations use dynamic code generation and are not Native AOT-compatible. Use a migration bundle for AOT applications.")]
[RequiresUnreferencedCode("EF Core migrations use reflection and are not trim-safe. Use a migration bundle for trimmed applications.")]
public static Task<MigrationReport<TKey>> MigrateAllAsync<TKey>(this ITenantMigrationRunner<TKey> runner, IProgress<MigrationResult<TKey>>? progress, CancellationToken cancellationToken = default) where TKey : IEquatable<TKey>, IParsable<TKey>
```

Type parameters:

- `TKey`: The tenant identifier type.

Parameters:

- `runner` [`ITenantMigrationRunner<TKey>`](tenantry-pro-efcore-itenantmigrationrunner.md): The runner.
- `progress` `IProgress<MigrationResult<TKey>>`: Gets each result as it completes.
- `cancellationToken` `CancellationToken`: Stops the run: the migrations in progress are abandoned and no other is started; those already migrated stay migrated.

Returns: `Task<MigrationReport<TKey>>`: A result for each context and database or schema.

Exceptions:

- `ArgumentNullException`: `runner` is null.
- [`LicenseRequiredException`](tenantry-pro-licenserequiredexception.md): The licence key is missing or invalid; nothing was migrated.
- `OperationCanceledException`: `cancellationToken` was cancelled.

### `MigrateAllAsync<TKey>(ITenantMigrationRunner<TKey>, CancellationToken)`

Applies each context's pending migrations for every tenant in the store.

```csharp
[RequiresDynamicCode("EF Core migrations use dynamic code generation and are not Native AOT-compatible. Use a migration bundle for AOT applications.")]
[RequiresUnreferencedCode("EF Core migrations use reflection and are not trim-safe. Use a migration bundle for trimmed applications.")]
public static Task<MigrationReport<TKey>> MigrateAllAsync<TKey>(this ITenantMigrationRunner<TKey> runner, CancellationToken cancellationToken = default) where TKey : IEquatable<TKey>, IParsable<TKey>
```

Type parameters:

- `TKey`: The tenant identifier type.

Parameters:

- `runner` [`ITenantMigrationRunner<TKey>`](tenantry-pro-efcore-itenantmigrationrunner.md): The runner.
- `cancellationToken` `CancellationToken`: Stops the run: the migrations in progress are abandoned and no other is started; those already migrated stay migrated.

Returns: `Task<MigrationReport<TKey>>`: A result for each context and database or schema.

Exceptions:

- `ArgumentNullException`: `runner` is null.
- [`LicenseRequiredException`](tenantry-pro-licenserequiredexception.md): The licence key is missing or invalid; nothing was migrated.
- `OperationCanceledException`: `cancellationToken` was cancelled.

### `MigrateTenantAsync<TKey>(ITenantMigrationRunner<TKey>, TKey, CancellationToken)`

Applies each context's pending migrations for one tenant: [`ITenantMigrationRunner<TKey>.MigrateAsync`](tenantry-pro-efcore-itenantmigrationrunner.md) with [`MigrationRunOptions<TKey>.Tenants`](tenantry-pro-efcore-migrationrunoptions.md) set to it alone.

```csharp
[RequiresDynamicCode("EF Core migrations use dynamic code generation and are not Native AOT-compatible. Use a migration bundle for AOT applications.")]
[RequiresUnreferencedCode("EF Core migrations use reflection and are not trim-safe. Use a migration bundle for trimmed applications.")]
public static Task<MigrationReport<TKey>> MigrateTenantAsync<TKey>(this ITenantMigrationRunner<TKey> runner, TKey tenantId, CancellationToken cancellationToken = default) where TKey : IEquatable<TKey>, IParsable<TKey>
```

Type parameters:

- `TKey`: The tenant identifier type.

Parameters:

- `runner` [`ITenantMigrationRunner<TKey>`](tenantry-pro-efcore-itenantmigrationrunner.md): The runner.
- `tenantId` `TKey`: The tenant to migrate.
- `cancellationToken` `CancellationToken`: Stops the run: the migrations in progress are abandoned and no other is started; those already migrated stay migrated.

Returns: `Task<MigrationReport<TKey>>`: A result for each context. In a database or schema the tenant shares, the result names every tenant it migrated.

Exceptions:

- `ArgumentNullException`: `runner` or `tenantId` is null.
- `ArgumentException`: `tenantId` is one Tenantry reserves for "no tenant": the key type's default (`Guid.Empty`, `0`) or an empty string.
- [`LicenseRequiredException`](tenantry-pro-licenserequiredexception.md): The licence key is missing or invalid; nothing was migrated.
- `TenantNotFoundException`: The tenant store has no tenant `tenantId`.
- `OperationCanceledException`: `cancellationToken` was cancelled.
