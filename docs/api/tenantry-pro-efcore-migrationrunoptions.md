# `MigrationRunOptions<TKey>` class

Namespace: `Tenantry.Pro.EfCore` · Package: `Tenantry.Pro.EfCore` · [API reference](README.md)

What a migration run covers and when it stops early: the tenants to migrate or leave out, and how many failures to allow. Pass it to [`ITenantMigrationRunner<TKey>.MigrateAsync`](tenantry-pro-efcore-itenantmigrationrunner.md); `migrate-tenants` builds it from `--tenant`, `--exclude` and `--max-failures`.

A database or schema is migrated once for every tenant whose data is in it, so the run selects databases and schemas: one is in the run when one of its tenants is in [`MigrationRunOptions<TKey>.Tenants`](tenantry-pro-efcore-migrationrunoptions.md), and none is in [`MigrationRunOptions<TKey>.ExcludedTenants`](tenantry-pro-efcore-migrationrunoptions.md). Its result names all its tenants, so a run for one tenant in a shared database reports every tenant it migrated.

```csharp
// Three canary tenants first, stopping at the first failure; then the rest.
var canary = await runner.MigrateAsync(new MigrationRunOptions<string> { Tenants = ["acme", "globex", "initech"], MaxFailures = 1 });
if (!canary.HasFailures)
    await runner.MigrateAsync(new MigrationRunOptions<string> { ExcludedTenants = ["acme", "globex", "initech"] });
```

```csharp
public sealed class MigrationRunOptions<TKey> where TKey : IEquatable<TKey>, IParsable<TKey>
```

## Type parameters

- `TKey`: The tenant identifier type.

## Properties

### `ExcludedTenants`

Tenants to leave out: a database or schema with any of them is not migrated, for any of its tenants. Each must be in the store, matched exactly, so a misspelt id does not leave the tenant in the run. Empty by default.

```csharp
public IReadOnlyCollection<TKey> ExcludedTenants { get; init; }
```

Value: `IReadOnlyCollection<TKey>`

### `MaxFailures`

How many databases or schemas may fail before the run stops starting others, or [null](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null) (the default) to go through them all whatever fails. Those in progress when it stops finish; the rest are reported as not attempted ([`MigrationResult<TKey>.Attempted`](tenantry-pro-efcore-migrationresult.md)). At least 1.

```csharp
public int? MaxFailures { get; init; }
```

Value: `int?`

### `StopStarting`

Once cancelled, the run starts no other database or schema: those in progress finish, and the rest are in the report as not attempted ([`MigrationReport<TKey>.Stopped`](tenantry-pro-efcore-migrationreport.md)). Cancel the run's own token instead to abandon those in progress too. `migrate-tenants` cancels it on a first SIGTERM or Ctrl+C. Not cancelled by default.

```csharp
public CancellationToken StopStarting { get; init; }
```

Value: `CancellationToken`

### `Tenants`

The tenants to migrate, or [null](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null) (the default) for every tenant in the store. Each must be in the store.

```csharp
public IReadOnlyCollection<TKey>? Tenants { get; init; }
```

Value: `IReadOnlyCollection<TKey>`
