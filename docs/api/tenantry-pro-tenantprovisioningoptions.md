# `TenantProvisioningOptions` class

Namespace: `Tenantry.Pro` · Package: `Tenantry.Pro` · [API reference](README.md)

How [`ITenantProvisioner<TKey>`](tenantry-pro-itenantprovisioner.md) runs the steps. Set with `pro.ConfigureProvisioning(o => ...)`.

```csharp
public sealed class TenantProvisioningOptions
```

## Properties

### `StopOnFailure`

Whether a failed step stops provisioning, so the steps after it are reported as [`TenantLifecycleStepStatus.NotRun`](tenantry-pro-tenantlifecyclestepstatus.md). Defaults to [true](https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool).

```csharp
public bool StopOnFailure { get; set; }
```

Value: `bool`

Seeding a tenant whose migrations failed, say, would fail too. With [false](https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool), every step runs.
