# `ITenantMigrationRunner<TKey>` interface

Namespace: `Tenantry.Pro.EfCore` · Package: `Tenantry.Pro.EfCore` · [API reference](README.md)

Applies the migrations of every context added with `pro.AddMigrations<TContext>()`, for every tenant, and reads which are applied. Registered by `AddMigrations`, as a singleton.

Each context is created in the tenant's scope, as [`TenantMigrationOptions<TContext>.CreateContext`](tenantry-pro-efcore-tenantmigrationoptions.md)     says, so it connects to the tenant's database, and with schema per tenant uses the tenant's schema. Tenants     whose context connects to the same database and schema are migrated once, together: a shared database, say,     in mixed mode.

One database or schema failing never stops the others: each one's outcome is in the result. Each failure is     logged once, as an error. Databases and schemas are migrated one at a time, unless the context's     [`TenantMigrationOptions<TContext>.MaxConcurrency`](tenantry-pro-efcore-tenantmigrationoptions.md) says more.

```csharp
public interface ITenantMigrationRunner<TKey> where TKey : IEquatable<TKey>, IParsable<TKey>
```

## Type parameters

- `TKey`: The tenant identifier type.

## Methods

### `GetStatusAsync(CancellationToken)`

Reads, for every tenant in the store, which of each context's migrations are applied and which are pending.

```csharp
[RequiresDynamicCode("EF Core migrations use dynamic code generation and are not Native AOT-compatible. Use a migration bundle for AOT applications.")]
[RequiresUnreferencedCode("EF Core migrations use reflection and are not trim-safe. Use a migration bundle for trimmed applications.")]
Task<IReadOnlyList<MigrationStatusEntry<TKey>>> GetStatusAsync(CancellationToken cancellationToken = default)
```

Parameters:

- `cancellationToken` `CancellationToken`: Cancels the read.

Returns: `Task<IReadOnlyList<MigrationStatusEntry<TKey>>>`: An entry for each context and database or schema. One that cannot be read gets an entry with [`MigrationStatusEntry<TKey>.Error`](tenantry-pro-efcore-migrationstatusentry.md) set, and the others are still read.

Exceptions:

- `OperationCanceledException`: `cancellationToken` was cancelled.

Reading needs no licence: it changes nothing.

### `GetTenantStatusAsync(TKey, CancellationToken)`

Reads, for one tenant, which of each context's migrations are applied and which are pending.

```csharp
[RequiresDynamicCode("EF Core migrations use dynamic code generation and are not Native AOT-compatible. Use a migration bundle for AOT applications.")]
[RequiresUnreferencedCode("EF Core migrations use reflection and are not trim-safe. Use a migration bundle for trimmed applications.")]
Task<IReadOnlyList<MigrationStatusEntry<TKey>>> GetTenantStatusAsync(TKey tenantId, CancellationToken cancellationToken = default)
```

Parameters:

- `tenantId` `TKey`: The tenant to read.
- `cancellationToken` `CancellationToken`: Cancels the read.

Returns: `Task<IReadOnlyList<MigrationStatusEntry<TKey>>>`: An entry for each context; one that cannot be read has [`MigrationStatusEntry<TKey>.Error`](tenantry-pro-efcore-migrationstatusentry.md) set.

Exceptions:

- `ArgumentNullException`: `tenantId` is null.
- `ArgumentException`: `tenantId` is one Tenantry reserves for "no tenant": the key type's default (`Guid.Empty`, `0`) or an empty string.
- `TenantNotFoundException`: The tenant store has no tenant `tenantId`.
- `OperationCanceledException`: `cancellationToken` was cancelled.

### `MigrateAllAsync(IProgress<MigrationResult<TKey>>?, CancellationToken)`

Applies each context's pending migrations for every tenant in the store, reporting each database's or schema's result as it completes.

```csharp
[RequiresDynamicCode("EF Core migrations use dynamic code generation and are not Native AOT-compatible. Use a migration bundle for AOT applications.")]
[RequiresUnreferencedCode("EF Core migrations use reflection and are not trim-safe. Use a migration bundle for trimmed applications.")]
Task<MigrationReport<TKey>> MigrateAllAsync(IProgress<MigrationResult<TKey>>? progress, CancellationToken cancellationToken = default)
```

Parameters:

- `progress` `IProgress<MigrationResult<TKey>>`: Gets each result as it completes, with every tenant of its database or schema: `Report` is called once for each, one call at a time, on the thread that completed it. `Progress<T>` runs its handler on the synchronization context it was created on or, without one (a console application, ASP.NET Core), on the thread pool, where handlers can overlap and run out of order. An exception from `Report` is logged, and the run goes on.
- `cancellationToken` `CancellationToken`: Stops the run: the migrations in progress are abandoned and no other is started; those already migrated stay migrated.

Returns: `Task<MigrationReport<TKey>>`: A result for each context and database or schema: the contexts in the order they were added, and for each, the databases and schemas in the order of their first tenant in the store.

Exceptions:

- [`LicenseRequiredException`](tenantry-pro-licenserequiredexception.md): The licence key is missing or invalid; nothing was migrated.
- `OperationCanceledException`: `cancellationToken` was cancelled.

```csharp
var report = await runner.MigrateAllAsync(new Progress<MigrationResult<string>>(result =>
    Console.WriteLine($"{result.Database}: {(result.Succeeded ? "migrated" : "failed")}")));
```

### `MigrateAllAsync(CancellationToken)`

Applies each context's pending migrations for every tenant in the store.

```csharp
[RequiresDynamicCode("EF Core migrations use dynamic code generation and are not Native AOT-compatible. Use a migration bundle for AOT applications.")]
[RequiresUnreferencedCode("EF Core migrations use reflection and are not trim-safe. Use a migration bundle for trimmed applications.")]
Task<MigrationReport<TKey>> MigrateAllAsync(CancellationToken cancellationToken = default)
```

Parameters:

- `cancellationToken` `CancellationToken`: Stops the run before the next database or schema; those already migrated stay migrated.

Returns: `Task<MigrationReport<TKey>>`: A result for each context and database or schema.

Exceptions:

- [`LicenseRequiredException`](tenantry-pro-licenserequiredexception.md): The licence key is missing or invalid; nothing was migrated.
- `OperationCanceledException`: `cancellationToken` was cancelled.

### `MigrateTenantAsync(TKey, CancellationToken)`

Applies each context's pending migrations for one tenant.

```csharp
[RequiresDynamicCode("EF Core migrations use dynamic code generation and are not Native AOT-compatible. Use a migration bundle for AOT applications.")]
[RequiresUnreferencedCode("EF Core migrations use reflection and are not trim-safe. Use a migration bundle for trimmed applications.")]
Task<MigrationReport<TKey>> MigrateTenantAsync(TKey tenantId, CancellationToken cancellationToken = default)
```

Parameters:

- `tenantId` `TKey`: The tenant to migrate.
- `cancellationToken` `CancellationToken`: Stops the run before the next context.

Returns: `Task<MigrationReport<TKey>>`: A result for each context.

Exceptions:

- `ArgumentNullException`: `tenantId` is null.
- `ArgumentException`: `tenantId` is one Tenantry reserves for "no tenant": the key type's default (`Guid.Empty`, `0`) or an empty string.
- [`LicenseRequiredException`](tenantry-pro-licenserequiredexception.md): The licence key is missing or invalid; nothing was migrated.
- `TenantNotFoundException`: The tenant store has no tenant `tenantId`.
- `OperationCanceledException`: `cancellationToken` was cancelled.
