# `TenantProvisioningResult<TKey>` class

Namespace: `Tenantry.Pro.Lifecycle` · Package: `Tenantry.Pro` · [API reference](README.md)

Describes the outcome of a [`ITenantLifecycleManager<TKey>.ProvisionAsync`](tenantry-pro-lifecycle-itenantlifecyclemanager.md) call.

```csharp
public sealed record TenantProvisioningResult<TKey> : IEquatable<TenantProvisioningResult<TKey>>
```

## Type parameters

- `TKey`: The tenant identifier type.

Implements `IEquatable<TenantProvisioningResult<TKey>>`.

## Properties

### `CompletedUpTo`

The last pipeline stage that completed successfully. Use this to determine where to resume after a partial failure. [`TenantProvisioningStep.Complete`](tenantry-pro-lifecycle-tenantprovisioningstep.md) when the entire pipeline succeeded. [`TenantProvisioningStep.None`](tenantry-pro-lifecycle-tenantprovisioningstep.md) when the pipeline failed before any step ran.

```csharp
public required TenantProvisioningStep CompletedUpTo { get; init; }
```

Value: [`TenantProvisioningStep`](tenantry-pro-lifecycle-tenantprovisioningstep.md)

### `Duration`

Total wall-clock time taken to run the entire provisioning pipeline.

```csharp
public TimeSpan Duration { get; init; }
```

Value: `TimeSpan`

### `Error`

The first exception thrown during the pipeline, or [null](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null) when successful. When `TenantLifecycleOptions.StopOnFailure` is [false](https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool), this captures the first failure even if later steps were attempted.

```csharp
public Exception? Error { get; init; }
```

Value: `Exception`

### `Succeeded`

[true](https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool) if every configured pipeline step completed without error; [false](https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool) if any step failed.

```csharp
public required bool Succeeded { get; init; }
```

Value: `bool`

### `TenantId`

The tenant that was provisioned.

```csharp
public required TKey TenantId { get; init; }
```

Value: `TKey`
