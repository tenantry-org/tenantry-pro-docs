# `TenantProvisioningOptions` class

Namespace: `Tenantry.Pro` · Package: `Tenantry.Pro` · [API reference](README.md)

How [`ITenantProvisioner<TKey>`](tenantry-pro-itenantprovisioner.md) runs the steps. Set with `pro.ConfigureProvisioning(o => ...)`.

```csharp
public sealed class TenantProvisioningOptions
```

## Properties

### `StopOnFailure`

Whether a failed step stops provisioning, so the steps after it are reported as [`TenantProvisioningStepStatus.NotRun`](tenantry-pro-tenantprovisioningstepstatus.md). Defaults to [true](https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool): seeding a tenant whose migrations failed, say, would fail too. With [false](https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool), every step runs.

```csharp
public bool StopOnFailure { get; set; }
```

Value: `bool`
