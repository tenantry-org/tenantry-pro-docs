# `TenantProvisioningStep` enum

Namespace: `Tenantry.Pro.Lifecycle` · Package: `Tenantry.Pro` · [API reference](README.md)

Represents the pipeline stages in the tenant provisioning lifecycle. Used by [`TenantProvisioningResult<TKey>.CompletedUpTo`](tenantry-pro-lifecycle-tenantprovisioningresult.md) to indicate which stage completed successfully before a failure occurred (or before the pipeline finished).

```csharp
public enum TenantProvisioningStep
```

## Values

| Value | Description |
|-------|-------------|
| `None = 0` | No steps have completed. Either the pipeline was not started or failed before the first step. |
| `DatabaseProvisioned = 1` | A dedicated database was successfully created for the tenant. |
| `SchemaProvisioned = 2` | A dedicated schema was successfully created for the tenant within the shared database. |
| `MigrationsApplied = 3` | EF Core migrations were successfully applied to the tenant's database. |
| `DataSeeded = 4` | Seed data was successfully written to the tenant's database. |
| `Complete = 5` | All pipeline stages completed successfully. |
