# `TenantPropagationBehavior` enum

Namespace: `Tenantry.Pro` · Package: `Tenantry.Pro` · [API reference](README.md)

What a tenant-propagation integration (Hangfire, MassTransit, Quartz.NET, Rebus) does with a job or message whose tenant it cannot make current. Set with [`TenantPropagationOptions.OnMissingTenant`](tenantry-pro-tenantpropagationoptions.md) and [`TenantPropagationOptions.OnUnresolvedTenant`](tenantry-pro-tenantpropagationoptions.md).

```csharp
public enum TenantPropagationBehavior
```

## Values

| Value | Description |
|-------|-------------|
| `Allow = 0` | Run the job or message handler without a tenant, silently. |
| `Warn = 1` | As [`TenantPropagationBehavior.Allow`](tenantry-pro-tenantpropagationbehavior.md), but log a warning. |
| `Skip = 2` | Do not run the job or message handler, raise no error, and log a warning. What then happens to the job or message depends on the host: Hangfire deletes the job, Quartz.NET counts the run as done, Rebus acknowledges the message, and MassTransit moves it to the endpoint's `_skipped` queue. |
| `Reject = 3` | Throw `TenantNotResolvedException` (`TenantNotFoundException` for a tenant the store does not have, `TenantInactiveException` for one `ValidateTenantActivity` refuses), so the host's retry and error handling take over: a failed job, or a message moved to the error queue. |
