# `TenantConsumeFilter<TKey>` class

Namespace: `Tenantry.Pro.MassTransit.Filters` · Package: `Tenantry.Pro.MassTransit` · [API reference](README.md)

MassTransit pipeline filter that restores the tenant scope when a message is consumed.

The filter reads the `tenantry-tenant-id` message header set by     [`TenantPublishFilter<TKey>`](tenantry-pro-masstransit-filters-tenantpublishfilter.md) or [`TenantSendFilter<TKey>`](tenantry-pro-masstransit-filters-tenantsendfilter.md),     looks the tenant up in [`ITenantStore<TKey>`](https://tenantry.dev/docs/core/api/tenantry-core-itenantstore), and activates a tenant scope     for the duration of the consumer's execution.

If no header is present, or the tenant cannot be found, the message is processed     without a tenant scope — the consumer sees no active tenant.

Wire this filter into the bus configuration via     `cfg.UseTenantryPro<TKey>(ctx)`.

```csharp
public sealed class TenantConsumeFilter<TKey> : IFilter<ConsumeContext>, IProbeSite where TKey : IEquatable<TKey>, IParsable<TKey>
```

## Type parameters

- `TKey`: The tenant identifier type.

Implements `IFilter<ConsumeContext>`, `IProbeSite`.

## Constructors

### `TenantConsumeFilter(ITenantScope<TKey>, ITenantStoreAccessor<TKey>, MissingTenantBehavior, ILogger<TenantConsumeFilter<TKey>>)`

MassTransit pipeline filter that restores the tenant scope when a message is consumed.

```csharp
public TenantConsumeFilter(ITenantScope<TKey> tenantScope, ITenantStoreAccessor<TKey> storeAccessor, MissingTenantBehavior onMissingTenant, ILogger<TenantConsumeFilter<TKey>> logger)
```

Parameters:

- `tenantScope` [`ITenantScope<TKey>`](https://tenantry.dev/docs/core/api/tenantry-core-itenantscope): Runs each consumer under the message's tenant.
- `storeAccessor` [`ITenantStoreAccessor<TKey>`](https://tenantry.dev/docs/core/api/tenantry-core-itenantstoreaccessor): Looks up the message's tenant before it is consumed.
- `onMissingTenant` [`MissingTenantBehavior`](https://tenantry.dev/docs/core/api/tenantry-core-missingtenantbehavior): What to do with a message that carries no tenant, or one the store does not know.
- `logger` `ILogger<TenantConsumeFilter<TKey>>`: Logs messages consumed without a tenant.

The filter reads the `tenantry-tenant-id` message header set by     [`TenantPublishFilter<TKey>`](tenantry-pro-masstransit-filters-tenantpublishfilter.md) or [`TenantSendFilter<TKey>`](tenantry-pro-masstransit-filters-tenantsendfilter.md),     looks the tenant up in [`ITenantStore<TKey>`](https://tenantry.dev/docs/core/api/tenantry-core-itenantstore), and activates a tenant scope     for the duration of the consumer's execution.

If no header is present, or the tenant cannot be found, the message is processed     without a tenant scope — the consumer sees no active tenant.

Wire this filter into the bus configuration via     `cfg.UseTenantryPro<TKey>(ctx)`.

## Methods

### `Probe(ProbeContext)`

Adds this filter to MassTransit's diagnostic probe, which describes the consume pipeline.

```csharp
public void Probe(ProbeContext context)
```

Parameters:

- `context` `ProbeContext`: The probe context to add to.

### `Send(ConsumeContext, IPipe<ConsumeContext>)`

Sends a context to a filter, such that it can be processed and then passed to the specified output pipe for further processing.

```csharp
public Task Send(ConsumeContext context, IPipe<ConsumeContext> next)
```

Parameters:

- `context` `ConsumeContext`: The pipe context type
- `next` `IPipe<ConsumeContext>`: The next pipe in the pipeline

Returns: `Task`: An awaitable Task
