# `TenantPublishFilter<TKey>` class

Namespace: `Tenantry.Pro.MassTransit.Filters` · Package: `Tenantry.Pro.MassTransit` · [API reference](README.md)

MassTransit pipeline filter that adds the current tenant ID as a message header when a message is published.

Wire this filter into the bus configuration via `cfg.UseTenantryPro<TKey>(ctx)`.

```csharp
public sealed class TenantPublishFilter<TKey> : IFilter<PublishContext>, IProbeSite where TKey : IEquatable<TKey>, IParsable<TKey>
```

## Type parameters

- `TKey`: The tenant identifier type.

Implements `IFilter<PublishContext>`, `IProbeSite`.

## Constructors

### `TenantPublishFilter(ITenantContext<TKey>)`

MassTransit pipeline filter that adds the current tenant ID as a message header when a message is published.

```csharp
public TenantPublishFilter(ITenantContext<TKey> tenantContext)
```

Parameters:

- `tenantContext` [`ITenantContext<TKey>`](https://tenantry.dev/docs/core/api/tenantry-core-itenantcontext): Supplies the current tenant, written to each published message's headers.

Wire this filter into the bus configuration via `cfg.UseTenantryPro<TKey>(ctx)`.

## Fields

### `HeaderKey`

The MassTransit message header key used to carry the tenant identifier.

```csharp
public const string HeaderKey = "tenantry-tenant-id"
```

Returns: `string`

## Methods

### `Probe(ProbeContext)`

Adds this filter to MassTransit's diagnostic probe, which describes the publish pipeline.

```csharp
public void Probe(ProbeContext context)
```

Parameters:

- `context` `ProbeContext`: The probe context to add to.

### `Send(PublishContext, IPipe<PublishContext>)`

Sends a context to a filter, such that it can be processed and then passed to the specified output pipe for further processing.

```csharp
public Task Send(PublishContext context, IPipe<PublishContext> next)
```

Parameters:

- `context` `PublishContext`: The pipe context type
- `next` `IPipe<PublishContext>`: The next pipe in the pipeline

Returns: `Task`: An awaitable Task
