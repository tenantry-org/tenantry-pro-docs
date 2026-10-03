# `TenantProvisioningResult<TKey>` class

Namespace: `Tenantry.Pro` · Package: `Tenantry.Pro` · [API reference](README.md)

The outcome of [`ITenantProvisioner<TKey>.ProvisionAsync`](tenantry-pro-itenantprovisioner.md).

```csharp
public sealed record TenantProvisioningResult<TKey> : IEquatable<TenantProvisioningResult<TKey>>
```

## Type parameters

- `TKey`: The tenant identifier type.

Implements `IEquatable<TenantProvisioningResult<TKey>>`.

## Properties

### `Duration`

How long provisioning took.

```csharp
public TimeSpan Duration { get; init; }
```

Value: `TimeSpan`

### `Error`

The exception the first failed step threw, or [null](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null) if none failed.

```csharp
public Exception? Error { get; }
```

Value: `Exception`

### `Steps`

Every registered step's outcome, in the order the steps run.

```csharp
public required IReadOnlyList<TenantProvisioningStepResult> Steps { get; init; }
```

Value: `IReadOnlyList<TenantProvisioningStepResult>`

### `Succeeded`

Whether no step failed.

```csharp
public bool Succeeded { get; }
```

Value: `bool`

### `TenantId`

The tenant that was provisioned.

```csharp
public required TKey TenantId { get; init; }
```

Value: `TKey`
