# `MigrationResult<TKey>` class

Namespace: `Tenantry.Pro.EfCore.Migrations` · Package: `Tenantry.Pro.EfCore` · [API reference](README.md)

The outcome of running EF Core migrations against a single tenant's database.

```csharp
public sealed record MigrationResult<TKey> : IEquatable<MigrationResult<TKey>>
```

## Type parameters

- `TKey`: The tenant identifier type.

Implements `IEquatable<MigrationResult<TKey>>`.

## Properties

### `AppliedMigrations`

The migrations that were applied during this run. Empty when the database was already up to date or when the migration failed.

```csharp
public required IReadOnlyList<string> AppliedMigrations { get; init; }
```

Value: `IReadOnlyList<string>`

### `Duration`

Wall-clock time taken to run migrations for this tenant.

```csharp
public required TimeSpan Duration { get; init; }
```

Value: `TimeSpan`

### `Error`

The exception that caused migration to fail, or [null](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null) if successful.

```csharp
public Exception? Error { get; init; }
```

Value: `Exception`

### `Succeeded`

True if migrations completed without error.

```csharp
public required bool Succeeded { get; init; }
```

Value: `bool`

### `TenantId`

The tenant whose database was targeted.

```csharp
public required TKey TenantId { get; init; }
```

Value: `TKey`
