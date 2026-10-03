# `MigrationReport<TKey>` class

Namespace: `Tenantry.Pro.EfCore` · Package: `Tenantry.Pro.EfCore` · [API reference](README.md)

The outcome of a migration run: a result for each database or schema it covered.

```csharp
public sealed record MigrationReport<TKey> : IEquatable<MigrationReport<TKey>>
```

## Type parameters

- `TKey`: The tenant identifier type.

Implements `IEquatable<MigrationReport<TKey>>`.

## Properties

### `Failed`

How many of them the run tried to migrate, and failed.

```csharp
public int Failed { get; }
```

Value: `int`

### `HasFailures`

Whether any of them failed.

```csharp
public bool HasFailures { get; }
```

Value: `bool`

### `NotAttempted`

How many of them the run did not attempt, because it stopped first ([`MigrationReport<TKey>.Stopped`](tenantry-pro-efcore-migrationreport.md)).

```csharp
public int NotAttempted { get; }
```

Value: `int`

### `Results`

A result for each context and each database or schema it connects to for the run's tenants, in the order the contexts were added and the tenants are in the store.

```csharp
public required IReadOnlyList<MigrationResult<TKey>> Results { get; init; }
```

Value: `IReadOnlyList<MigrationResult<TKey>>`

### `Stopped`

Whether the run stopped before attempting them all: [`MigrationRunOptions<TKey>.MaxFailures`](tenantry-pro-efcore-migrationrunoptions.md) were reached, or [`MigrationRunOptions<TKey>.StopStarting`](tenantry-pro-efcore-migrationrunoptions.md) was cancelled (as `migrate-tenants` does when asked to stop).

```csharp
public bool Stopped { get; }
```

Value: `bool`

### `Succeeded`

How many of them were migrated without error.

```csharp
public int Succeeded { get; }
```

Value: `int`

### `Total`

How many databases or schemas the run covered, counting each context separately.

```csharp
public int Total { get; }
```

Value: `int`
