# `TenantDeprovisioningResult<TKey>` class

Namespace: `Tenantry.Pro` · Package: `Tenantry.Pro` · [API reference](README.md)

The outcome of offboarding a tenant: a result for each deprovisioning step, in the order they ran.

```csharp
public sealed record TenantDeprovisioningResult<TKey> : IEquatable<TenantDeprovisioningResult<TKey>>
```

## Type parameters

- `TKey`: The tenant identifier type.

Implements `IEquatable<TenantDeprovisioningResult<TKey>>`.

## Properties

### `Duration`

How long the steps took.

```csharp
public TimeSpan Duration { get; init; }
```

Value: `TimeSpan`

### `Error`

The failed step's exception, or [null](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null) when none failed.

```csharp
public Exception? Error { get; }
```

Value: `Exception`

### `Steps`

Each step's outcome, as provisioning reports its steps.

```csharp
public required IReadOnlyList<TenantLifecycleStepResult> Steps { get; init; }
```

Value: `IReadOnlyList<TenantLifecycleStepResult>`

### `Succeeded`

Whether no step failed. Remove the tenant from the store only then.

```csharp
public bool Succeeded { get; }
```

Value: `bool`

### `TenantId`

The tenant offboarded.

```csharp
public required TKey TenantId { get; init; }
```

Value: `TKey`
