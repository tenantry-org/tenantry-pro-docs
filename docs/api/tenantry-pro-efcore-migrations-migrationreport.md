# `MigrationReport<TKey>` class

Namespace: `Tenantry.Pro.EfCore.Migrations` · Package: `Tenantry.Pro.EfCore` · [API reference](README.md)

The aggregate outcome of running EF Core migrations across all tenant databases.

```csharp
public sealed record MigrationReport<TKey> : IEquatable<MigrationReport<TKey>>
```

## Type parameters

- `TKey`: The tenant identifier type.

Implements `IEquatable<MigrationReport<TKey>>`.

## Properties

### `Failed`

Number of tenants whose migrations failed.

```csharp
public int Failed { get; }
```

Value: `int`

### `HasFailures`

True if at least one tenant migration failed.

```csharp
public bool HasFailures { get; }
```

Value: `bool`

### `Results`

Per-tenant migration results, one entry per tenant in the store.

```csharp
public required IReadOnlyList<MigrationResult<TKey>> Results { get; init; }
```

Value: `IReadOnlyList<MigrationResult<TKey>>`

### `Succeeded`

Number of tenants whose migrations completed successfully.

```csharp
public int Succeeded { get; }
```

Value: `int`

### `Total`

Total number of tenants targeted.

```csharp
public int Total { get; }
```

Value: `int`
