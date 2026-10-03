# `ITenantMigrationRunner<TKey>` interface

Namespace: `Tenantry.Pro.EfCore` · Package: `Tenantry.Pro.EfCore` · [API reference](README.md)

Applies the migrations of every context added with `pro.AddMigrations<TContext>()`, for every tenant, and reads which are applied. Registered by `AddMigrations`, as a singleton.

Each context is created in the tenant's scope, as [`TenantMigrationOptions<TContext>.CreateContext`](tenantry-pro-efcore-tenantmigrationoptions.md)     says, so it connects to the tenant's database, and with schema per tenant uses the tenant's schema. Tenants     whose context connects to the same database and schema are migrated once, together: a shared database, say,     in mixed mode.

One database or schema failing never stops the others: each one's outcome is in the result. Each failure is     logged once, as an error. Databases and schemas are migrated one at a time, unless the context's     [`TenantMigrationOptions<TContext>.MaxConcurrency`](tenantry-pro-efcore-tenantmigrationoptions.md) says more.

With schema per tenant and no contexts listed in [`SchemaPerTenantOptions<TKey>.Contexts`](tenantry-pro-efcore-schemapertenantoptions.md), a run     first builds the options of the contexts the application registers, as a starting host does, and throws     `InvalidOperationException`, having migrated nothing, when more than one uses     `UseTenantry()`.

```csharp
public interface ITenantMigrationRunner<TKey> where TKey : IEquatable<TKey>, IParsable<TKey>
```

## Type parameters

- `TKey`: The tenant identifier type.

## Methods

### `GetStatusAsync(MigrationRunOptions<TKey>?, CancellationToken)`

Reads, for the tenants `options` selects (every tenant in the store, without options), which of each context's migrations are applied and which are pending. [`TenantMigrationRunnerExtensions`](tenantry-pro-efcore-tenantmigrationrunnerextensions.md) has `GetTenantStatusAsync`, for one tenant.

```csharp
[RequiresDynamicCode("EF Core migrations use dynamic code generation and are not Native AOT-compatible. Use a migration bundle for AOT applications.")]
[RequiresUnreferencedCode("EF Core migrations use reflection and are not trim-safe. Use a migration bundle for trimmed applications.")]
Task<IReadOnlyList<MigrationStatusEntry<TKey>>> GetStatusAsync(MigrationRunOptions<TKey>? options = null, CancellationToken cancellationToken = default)
```

Parameters:

- `options` [`MigrationRunOptions<TKey>`](tenantry-pro-efcore-migrationrunoptions.md): The tenants to read or leave out ([`MigrationRunOptions<TKey>.Tenants`](tenantry-pro-efcore-migrationrunoptions.md), [`MigrationRunOptions<TKey>.ExcludedTenants`](tenantry-pro-efcore-migrationrunoptions.md)), or [null](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null) for every tenant. Its other settings are for runs.
- `cancellationToken` `CancellationToken`: Cancels the read.

Returns: `Task<IReadOnlyList<MigrationStatusEntry<TKey>>>`: An entry for each context and database or schema. One that cannot be read gets an entry with [`MigrationStatusEntry<TKey>.Error`](tenantry-pro-efcore-migrationstatusentry.md) set, and the others are still read.

Exceptions:

- `ArgumentNullException`: `options` names a null tenant.
- `ArgumentException`: `options` names no tenant, or a tenant id Tenantry reserves.
- `TenantNotFoundException`: A tenant `options` names is not in the store.
- `OperationCanceledException`: `cancellationToken` was cancelled.

Reading needs no licence: it changes nothing.

### `MigrateAsync(MigrationRunOptions<TKey>?, IProgress<MigrationResult<TKey>>?, CancellationToken)`

Applies each context's pending migrations for the tenants `options` selects (every tenant in the store, without options), stopping early when they say so, and reports each database's or schema's result as it completes. [`TenantMigrationRunnerExtensions`](tenantry-pro-efcore-tenantmigrationrunnerextensions.md) has the shorter forms: `MigrateAllAsync` and `MigrateTenantAsync`.

```csharp
[RequiresDynamicCode("EF Core migrations use dynamic code generation and are not Native AOT-compatible. Use a migration bundle for AOT applications.")]
[RequiresUnreferencedCode("EF Core migrations use reflection and are not trim-safe. Use a migration bundle for trimmed applications.")]
Task<MigrationReport<TKey>> MigrateAsync(MigrationRunOptions<TKey>? options = null, IProgress<MigrationResult<TKey>>? progress = null, CancellationToken cancellationToken = default)
```

Parameters:

- `options` [`MigrationRunOptions<TKey>`](tenantry-pro-efcore-migrationrunoptions.md): The tenants to migrate or leave out, how many failures to allow, and a token that stops starting others; or [null](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null) for every tenant.
- `progress` `IProgress<MigrationResult<TKey>>`: Gets each result as it completes, with every tenant of its database or schema: `Report` is called once for each, one call at a time, on the thread that completed it. `Progress<T>` runs its handler on the synchronization context it was created on or, without one (a console application, ASP.NET Core), on the thread pool, where handlers can overlap and run out of order. An exception from `Report` is logged, and the run goes on. Results the run did not attempt are not reported to it.
- `cancellationToken` `CancellationToken`: Stops the run: the migrations in progress are abandoned and no other is started; those already migrated stay migrated. To let those in progress finish, cancel [`MigrationRunOptions<TKey>.StopStarting`](tenantry-pro-efcore-migrationrunoptions.md) instead.

Returns: `Task<MigrationReport<TKey>>`: A result for each context and selected database or schema, attempted or not: the contexts in the order they were added, and for each, the databases and schemas in the order of their first tenant in the store.

Exceptions:

- `ArgumentNullException`: `options` names a null tenant.
- `ArgumentException`: `options` names no tenant to migrate (an empty [`MigrationRunOptions<TKey>.Tenants`](tenantry-pro-efcore-migrationrunoptions.md)), a tenant id Tenantry reserves for "no tenant", or a [`MigrationRunOptions<TKey>.MaxFailures`](tenantry-pro-efcore-migrationrunoptions.md) below 1.
- [`LicenseRequiredException`](tenantry-pro-licenserequiredexception.md): The licence key is missing or invalid; nothing was migrated.
- `TenantNotFoundException`: A tenant in [`MigrationRunOptions<TKey>.Tenants`](tenantry-pro-efcore-migrationrunoptions.md) or [`MigrationRunOptions<TKey>.ExcludedTenants`](tenantry-pro-efcore-migrationrunoptions.md) is not in the store.
- `OperationCanceledException`: `cancellationToken` was cancelled.

The run creates every tenant's context to find which tenants share a database or schema, then migrates the selected ones, so a run for a few tenants takes about as long to plan as a run for all.

```csharp
var report = await runner.MigrateAsync(progress: new Progress<MigrationResult<string>>(result =>
    Console.WriteLine($"{result.Database}: {(result.Succeeded ? "migrated" : "failed")}")));
```
