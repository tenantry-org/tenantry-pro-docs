# `QuartzProBuilderExtensions` class

Namespace: `Tenantry.Pro.Quartz.Extensions` · Package: `Tenantry.Pro.Quartz` · [API reference](README.md)

Extension methods for registering Quartz tenant-propagation services on [`ProBuilder<TKey>`](tenantry-pro-internal-probuilder.md).

```csharp
public static class QuartzProBuilderExtensions
```

## Methods

### `AddQuartzTenantScope<TKey>(ProBuilder<TKey>, Action<TenantPropagationOptions>?)`

Decorates the Quartz `IJobFactory` so every scheduled job runs inside the tenant scope identified by the tenant ID stamped into its job data map (via [`JobDataMapExtensions.WithTenant<TKey>`](tenantry-pro-quartz-extensions-jobdatamapextensions.md)). Jobs whose data map has no (or an unparseable / unknown) tenant run without a scope.

```csharp
public static ProBuilder<TKey> AddQuartzTenantScope<TKey>(this ProBuilder<TKey> builder, Action<TenantPropagationOptions>? configure = null) where TKey : IEquatable<TKey>, IParsable<TKey>
```

Type parameters:

- `TKey`: The tenant identifier type.

Parameters:

- `builder` [`ProBuilder<TKey>`](tenantry-pro-internal-probuilder.md): The Pro builder.
- `configure` `Action<TenantPropagationOptions>`: Optional configuration of the tenant-propagation policy — in particular [`TenantPropagationOptions.OnMissingTenant`](tenantry-pro-backgroundservices-tenantpropagationoptions.md), which controls what happens to a scheduled job that has no resolvable tenant. Defaults to `Warn`.

Returns: [`ProBuilder<TKey>`](tenantry-pro-internal-probuilder.md): The same `builder` for chaining.

Call this AFTER `services.AddQuartz(...)` so the Quartz-registered `IJobFactory` exists and can be decorated. Stamp tenants at schedule time, e.g. `new JobDataMap().WithTenant(tenantId)` passed to `JobBuilder.UsingJobData(...)`.
