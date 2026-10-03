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

The migrations this run applied, in the order they were applied. Empty when there was nothing to apply.

```csharp
public required IReadOnlyList<string> AppliedMigrations { get; init; }
```

Value: `IReadOnlyList<string>`

A migration counts when this run wrote its row in the migration history and the row was committed (or written outside a transaction, for a migration whose operations suppress it). So a migration another process applied is not listed, whether this run waited for it (EF Core's migration lock), failed on it, or skipped it when an execution strategy retried the run. When the run failed, this lists the migrations it committed before the failure, if any: EF Core 8 and 10+ commit each migration separately, while EF Core 9 commits them together, so on SQL Server and PostgreSQL a failure usually leaves none. MySQL commits each DDL statement itself, so earlier migrations stay applied on every EF Core version; on EF Core 9 this list cannot show them. The migration that failed is never listed, although on MySQL it can leave some of its own changes in place. Check the database with [`ITenantMigrationRunner<TKey>.GetTenantStatusAsync`](tenantry-pro-efcore-itenantmigrationrunner.md) after a failure.

### `ContextType`

The context whose migrations were applied.

```csharp
public required Type ContextType { get; init; }
```

Value: `Type`

### `Database`

The database's name, as the context's connection gives it, or [null](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null) when the context could not be created for the tenant (then [`MigrationResult<TKey>.Error`](tenantry-pro-efcore-migrationresult.md) says why).

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

Whether the migrations were applied without error.

```csharp
public required bool Succeeded { get; init; }
```

Value: `bool`

### `TenantIds`

The tenants whose data is in this database or schema, in the tenant store's order. Tenants that share one (a shared database, or one schema for several tenants) share a result: the migrations were applied once for all of them.

```csharp
public required IReadOnlyList<TKey> TenantIds { get; init; }
```

Value: `IReadOnlyList<TKey>`
