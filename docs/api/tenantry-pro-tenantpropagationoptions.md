# `TenantPropagationOptions` class

Namespace: `Tenantry.Pro` · Package: `Tenantry.Pro` · [API reference](README.md)

What a tenant-propagation integration (Hangfire, MassTransit, Quartz.NET, Rebus) does with a job or message that carries no tenant, or a tenant it cannot find. Set for each integration when it is added, as in `pro.AddRebusPropagation(o => o.OnMissingTenant = TenantPropagationBehavior.Reject)`.

```csharp
public sealed class TenantPropagationOptions
```

## Properties

### `OnMissingTenant`

What happens to a job or message that carries no tenant, such as one created outside a request or by a scheduler. The default is [`TenantPropagationBehavior.Warn`](tenantry-pro-tenantpropagationbehavior.md): it runs without a tenant, and a warning is logged.

```csharp
public TenantPropagationBehavior OnMissingTenant { get; set; }
```

Value: [`TenantPropagationBehavior`](tenantry-pro-tenantpropagationbehavior.md)

### `OnUnresolvedTenant`

What happens to a job or message that carries a tenant id that is not a valid id of the tenant key type, that the tenant store does not have (a tenant deleted since, or an id from another system), or whose tenant `ValidateTenantActivity` refuses (a suspended one). The default is [`TenantPropagationBehavior.Reject`](tenantry-pro-tenantpropagationbehavior.md): it fails, so the host's retry and error handling take over, and it never runs as no tenant.

```csharp
public TenantPropagationBehavior OnUnresolvedTenant { get; set; }
```

Value: [`TenantPropagationBehavior`](tenantry-pro-tenantpropagationbehavior.md)
