# `TenantLifecycleStepResult` class

Namespace: `Tenantry.Pro` · Package: `Tenantry.Pro` · [API reference](README.md)

The outcome of one step, in [`TenantProvisioningResult<TKey>.Steps`](tenantry-pro-tenantprovisioningresult.md), or of an offboarding step, in [`TenantDeprovisioningResult<TKey>.Steps`](tenantry-pro-tenantdeprovisioningresult.md).

```csharp
public sealed record TenantLifecycleStepResult : IEquatable<TenantLifecycleStepResult>
```

Implements `IEquatable<TenantLifecycleStepResult>`.

## Properties

### `Duration`

How long the step took, including resolving it and disposing its scope.

```csharp
public TimeSpan Duration { get; init; }
```

Value: `TimeSpan`

### `Error`

The exception the step threw, when it [`TenantLifecycleStepStatus.Failed`](tenantry-pro-tenantlifecyclestepstatus.md).

```csharp
public Exception? Error { get; init; }
```

Value: `Exception`

### `Name`

The step's name: the type name of a step or seeder you added, or for Tenantry.Pro's own, `CreateDatabase`, `CreateSchema` or `Migrations` when provisioning, and `DropDatabase`, `DropSchema`, `DeleteSharedData` or `ClearCaches` when deprovisioning.

```csharp
public required string Name { get; init; }
```

Value: `string`

### `Status`

What happened to the step.

```csharp
public required TenantLifecycleStepStatus Status { get; init; }
```

Value: [`TenantLifecycleStepStatus`](tenantry-pro-tenantlifecyclestepstatus.md)
