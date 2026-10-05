# `MigrationResult<TKey>` class

Namespace: `Tenantry.Pro.EfCore` · Package: `Tenantry.Pro.EfCore` · [API reference](README.md)

The outcome of applying a context's migrations to one database or schema: the one the context connects to for the tenants in [`MigrationResult<TKey>.TenantIds`](tenantry-pro-efcore-migrationresult.md).

```csharp
public sealed record MigrationResult<TKey> : IEquatable<MigrationResult<TKey>>
```

## Type parameters

- `TKey`: The tenant identifier type.

Implements `IEquatable<MigrationResult<TKey>>`.

## Properties

### `AppliedMigrations`

The migrations in the database's history after the run that were not in it before, in order; empty when there was nothing to apply.

```csharp
public required IReadOnlyList<string> AppliedMigrations { get; init; }
```

Value: `IReadOnlyList<string>`

The history does not say who applied a migration, so when another runner applies migrations to the same database while this one runs, a migration may be listed in either result, or in both. After a failure it lists those committed before it, which depends on the EF Core version and database (the Tenant migrations guide's failure model); check with [`TenantMigrationRunnerExtensions.GetTenantStatusAsync<TKey>`](tenantry-pro-efcore-tenantmigrationrunnerextensions.md).

### `Attempted`

Whether the run tried to migrate this database or schema. [false](https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool) when it stopped first: [`MigrationRunOptions<TKey>.MaxFailures`](tenantry-pro-efcore-migrationrunoptions.md) were reached, or `migrate-tenants` was asked to stop. Its migrations were not touched, and [`MigrationResult<TKey>.Error`](tenantry-pro-efcore-migrationresult.md) is [null](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null).

```csharp
public bool Attempted { get; init; }
```

Value: `bool`

### `ContextType`

The context whose migrations were applied.

```csharp
public required Type ContextType { get; init; }
```

Value: `Type`

### `Database`

The database's name, as the context's connection gives it, or [null](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null) when the context could not be created for the tenant (then [`MigrationResult<TKey>.Error`](tenantry-pro-efcore-migrationresult.md) says why), or was not created because the run had stopped (then the result names one tenant and is not [`MigrationResult<TKey>.Attempted`](tenantry-pro-efcore-migrationresult.md)).

```csharp
public string? Database { get; init; }
```

Value: `string`

### `Duration`

How long applying the migrations took.

```csharp
public required TimeSpan Duration { get; init; }
```

Value: `TimeSpan`

### `Error`

Why the migrations could not be applied, or [null](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null) when they were.

```csharp
public Exception? Error { get; init; }
```

Value: `Exception`

### `Schema`

The default schema of the context's model for these tenants: their own, with schema per tenant (`UseSchemaPerTenant`), or [null](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null) when their tables are in the database's default schema.

```csharp
public string? Schema { get; init; }
```

Value: `string`

### `Succeeded`

Whether the migrations were applied without error. [false](https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool) for one the run did not attempt ([`MigrationResult<TKey>.Attempted`](tenantry-pro-efcore-migrationresult.md)).

```csharp
public required bool Succeeded { get; init; }
```

Value: `bool`

### `TenantIds`

The tenants whose data is in this database or schema, in the tenant store's order, whether or not the run named them all. Tenants that share one (a shared database, or one schema for several tenants) share a result: the migrations were applied once for all of them.

```csharp
public required IReadOnlyList<TKey> TenantIds { get; init; }
```

Value: `IReadOnlyList<TKey>`
