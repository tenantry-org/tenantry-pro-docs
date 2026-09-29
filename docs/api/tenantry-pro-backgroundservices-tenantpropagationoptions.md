# `TenantPropagationOptions` class

Namespace: `Tenantry.Pro.BackgroundServices` · Package: `Tenantry.Pro` · [API reference](README.md)

Configures how a tenant-context propagation integration (Hangfire, MassTransit, Quartz, Rebus) behaves when a job or message has no resolvable tenant — because none was attached, the attached value could not be parsed as `TKey`, or the tenant was not found in the store.

```csharp
public sealed class TenantPropagationOptions
```

## Properties

### `OnMissingTenant`

What happens when a job or message runs with no resolvable tenant.

- `Allow` — run the handler without a tenant scope, silently.
- `Warn` — run the handler without a tenant scope and log a warning. **Default.**
- `Reject` — throw, so the host's retry/error handling takes over.
- `Skip` — acknowledge and drop the job/message without running the handler.

Set `Allow` for deployments where jobs legitimately run without a tenant (global maintenance, system events) and you do not want warnings.

```csharp
public MissingTenantBehavior OnMissingTenant { get; set; }
```

Value: [`MissingTenantBehavior`](https://tenantry.dev/docs/core/api/tenantry-core-missingtenantbehavior)
