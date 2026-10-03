# `TenantProvisioningStepStatus` enum

Namespace: `Tenantry.Pro` · Package: `Tenantry.Pro` · [API reference](README.md)

What happened to a provisioning step.

```csharp
public enum TenantProvisioningStepStatus
```

## Values

| Value | Description |
|-------|-------------|
| `Succeeded = 0` | The step ran and completed. |
| `Failed = 1` | The step threw; [`TenantProvisioningStepResult.Error`](tenantry-pro-tenantprovisioningstepresult.md) has the exception. |
| `Skipped = 2` | The step does not apply to the tenant ([`ITenantProvisioningStep<TKey>.AppliesTo`](tenantry-pro-itenantprovisioningstep.md)). |
| `NotRun = 3` | The step did not run because an earlier step failed ([`TenantProvisioningOptions.StopOnFailure`](tenantry-pro-tenantprovisioningoptions.md)). |
