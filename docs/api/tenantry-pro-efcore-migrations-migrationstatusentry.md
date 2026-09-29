# `MigrationStatusEntry<TKey>` class

Namespace: `Tenantry.Pro.EfCore.Migrations` · Package: `Tenantry.Pro.EfCore` · [API reference](README.md)

A point-in-time snapshot of which EF Core migrations have been applied to a specific tenant's database.

```csharp
public sealed record MigrationStatusEntry<TKey> : IEquatable<MigrationStatusEntry<TKey>>
```

## Type parameters

- `TKey`: The tenant identifier type.

Implements `IEquatable<MigrationStatusEntry<TKey>>`.

## Properties

### `AppliedMigrations`

Migrations already applied to this tenant's database.

```csharp
public required IReadOnlyList<string> AppliedMigrations { get; init; }
```

Value: `IReadOnlyList<string>`

### `IsUpToDate`

True when there are no pending migrations.

```csharp
public bool IsUpToDate { get; }
```

Value: `bool`

### `PendingMigrations`

Migrations defined in the assembly that have not yet been applied.

```csharp
public required IReadOnlyList<string> PendingMigrations { get; init; }
```

Value: `IReadOnlyList<string>`

### `TenantId`

The tenant whose database was inspected.

```csharp
public required TKey TenantId { get; init; }
```

Value: `TKey`
