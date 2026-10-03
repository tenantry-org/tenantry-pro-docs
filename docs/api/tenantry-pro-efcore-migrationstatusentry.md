# `MigrationStatusEntry<TKey>` class

Namespace: `Tenantry.Pro.EfCore` · Package: `Tenantry.Pro.EfCore` · [API reference](README.md)

Which of a context's migrations a database or schema has applied, and which are pending: the database or schema the context connects to for the tenants in [`MigrationStatusEntry<TKey>.TenantIds`](tenantry-pro-efcore-migrationstatusentry.md).

```csharp
public sealed record MigrationStatusEntry<TKey> : IEquatable<MigrationStatusEntry<TKey>>
```

## Type parameters

- `TKey`: The tenant identifier type.

Implements `IEquatable<MigrationStatusEntry<TKey>>`.

## Properties

### `AppliedMigrations`

The migrations already applied. Empty when [`MigrationStatusEntry<TKey>.Error`](tenantry-pro-efcore-migrationstatusentry.md) is set.

```csharp
public required IReadOnlyList<string> AppliedMigrations { get; init; }
```

Value: `IReadOnlyList<string>`

### `ContextType`

The context whose migrations were read.

```csharp
public required Type ContextType { get; init; }
```

Value: `Type`

### `Database`

The database's name, as the context's connection gives it, or [null](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null) when the context could not be created for the tenant.

```csharp
public string? Database { get; init; }
```

Value: `string`

### `Error`

Why the status could not be read (the database is unreachable, say), or [null](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null) when it was.

```csharp
public Exception? Error { get; init; }
```

Value: `Exception`

### `IsUpToDate`

Whether the status was read and no migration is pending.

```csharp
public bool IsUpToDate { get; }
```

Value: `bool`

### `PendingMigrations`

The context's migrations not yet applied. Empty when [`MigrationStatusEntry<TKey>.Error`](tenantry-pro-efcore-migrationstatusentry.md) is set.

```csharp
public required IReadOnlyList<string> PendingMigrations { get; init; }
```

Value: `IReadOnlyList<string>`

### `Schema`

The default schema of the context's model for these tenants: their own, with schema per tenant, or [null](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null) for the database's default schema.

```csharp
public string? Schema { get; init; }
```

Value: `string`

### `TenantIds`

The tenants whose data is in this database or schema, in the tenant store's order.

```csharp
public required IReadOnlyList<TKey> TenantIds { get; init; }
```

Value: `IReadOnlyList<TKey>`
