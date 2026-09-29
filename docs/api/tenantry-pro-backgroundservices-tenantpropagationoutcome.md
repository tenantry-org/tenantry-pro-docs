# `TenantPropagationOutcome` enum

Namespace: `Tenantry.Pro.BackgroundServices` · Package: `Tenantry.Pro` · [API reference](README.md)

The action a tenant-propagation integration should take for a single job or message, as decided by [`TenantContextPropagation`](tenantry-pro-backgroundservices-tenantcontextpropagation.md).

```csharp
public enum TenantPropagationOutcome
```

## Values

| Value | Description |
|-------|-------------|
| `RunInScope = 0` | Begin a tenant scope for [`TenantPropagationDecision<TKey>.Tenant`](tenantry-pro-backgroundservices-tenantpropagationdecision.md), then run the handler. |
| `RunWithoutScope = 1` | Run the handler without a tenant scope. |
| `Skip = 2` | Do not run the handler — acknowledge and drop the job/message. |
