# `TenantLifecycleStepStatus` enum

Namespace: `Tenantry.Pro` · Package: `Tenantry.Pro` · [API reference](README.md)

What happened to a provisioning or deprovisioning step.

```csharp
public enum TenantLifecycleStepStatus
```

## Values

| Value | Description |
|-------|-------------|
| `Succeeded = 0` | The step ran and completed. |
| `Failed = 1` | The step threw; [`TenantLifecycleStepResult.Error`](tenantry-pro-tenantlifecyclestepresult.md) has the exception. |
| `Skipped = 2` | The step does not apply to the tenant ([`ITenantProvisioningStep<TKey>.AppliesTo`](tenantry-pro-itenantprovisioningstep.md), [`ITenantDeprovisioningStep<TKey>.AppliesTo`](tenantry-pro-itenantdeprovisioningstep.md)). |
| `NotRun = 3` | The step did not run because an earlier step failed (when provisioning, with [`TenantProvisioningOptions.StopOnFailure`](tenantry-pro-tenantprovisioningoptions.md); deprovisioning always stops). |
