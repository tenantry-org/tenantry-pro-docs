# `TenantProvisioningStepResult` class

Namespace: `Tenantry.Pro` · Package: `Tenantry.Pro` · [API reference](README.md)

The outcome of one step, in [`TenantProvisioningResult<TKey>.Steps`](tenantry-pro-tenantprovisioningresult.md).

```csharp
public sealed record TenantProvisioningStepResult : IEquatable<TenantProvisioningStepResult>
```

Implements `IEquatable<TenantProvisioningStepResult>`.

## Properties

### `Duration`

How long the step took, including resolving it and disposing its scope.

```csharp
public TimeSpan Duration { get; init; }
```

Value: `TimeSpan`

### `Error`

The exception the step threw, when it [`TenantProvisioningStepStatus.Failed`](tenantry-pro-tenantprovisioningstepstatus.md).

```csharp
public Exception? Error { get; init; }
```

Value: `Exception`

### `Name`

The step's name: the type name of a step or seeder you added, or `CreateDatabase`, `CreateSchema` or `Migrations` for Tenantry.Pro's own.

```csharp
public required string Name { get; init; }
```

Value: `string`

### `Status`

What happened to the step.

```csharp
public required TenantProvisioningStepStatus Status { get; init; }
```

Value: [`TenantProvisioningStepStatus`](tenantry-pro-tenantprovisioningstepstatus.md)
