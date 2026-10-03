# `MigrationReport<TKey>` class

Namespace: `Tenantry.Pro.EfCore` · Package: `Tenantry.Pro.EfCore` · [API reference](README.md)

The outcome of a migration run: a result for each database or schema it migrated.

```csharp
public sealed record MigrationReport<TKey> : IEquatable<MigrationReport<TKey>>
```

## Type parameters

- `TKey`: The tenant identifier type.

Implements `IEquatable<MigrationReport<TKey>>`.

## Properties

### `Failed`

How many of them failed.

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

### `Results`

A result for each context and each database or schema it connects to for the tenants, in the order the contexts were added and the tenants are in the store.

```csharp
public required IReadOnlyList<MigrationResult<TKey>> Results { get; init; }
```

Value: `IReadOnlyList<MigrationResult<TKey>>`

### `Succeeded`

How many of them were migrated without error.

```csharp
public int Succeeded { get; }
```

Value: `int`

### `Total`

How many databases or schemas the run migrated, counting each context separately.

```csharp
public int Total { get; }
```

Value: `int`
