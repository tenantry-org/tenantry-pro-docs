# `TenantSendFilter<TKey>` class

Namespace: `Tenantry.Pro.MassTransit.Filters` · Package: `Tenantry.Pro.MassTransit` · [API reference](README.md)

MassTransit pipeline filter that adds the current tenant ID as a message header when a message is sent point-to-point.

Wire this filter into the bus configuration via `cfg.UseTenantryPro<TKey>(ctx)`.

```csharp
public sealed class TenantSendFilter<TKey> : IFilter<SendContext>, IProbeSite where TKey : IEquatable<TKey>, IParsable<TKey>
```

## Type parameters

- `TKey`: The tenant identifier type.

Implements `IFilter<SendContext>`, `IProbeSite`.

## Constructors

### `TenantSendFilter(ITenantContext<TKey>)`

MassTransit pipeline filter that adds the current tenant ID as a message header when a message is sent point-to-point.

```csharp
public TenantSendFilter(ITenantContext<TKey> tenantContext)
```

Parameters:

- `tenantContext` [`ITenantContext<TKey>`](https://tenantry.dev/docs/core/api/tenantry-core-itenantcontext): Supplies the current tenant, written to each sent message's headers.

Wire this filter into the bus configuration via `cfg.UseTenantryPro<TKey>(ctx)`.

## Methods

### `Probe(ProbeContext)`

Adds this filter to MassTransit's diagnostic probe, which describes the send pipeline.

```csharp
public void Probe(ProbeContext context)
```

Parameters:

- `context` `ProbeContext`: The probe context to add to.

### `Send(SendContext, IPipe<SendContext>)`

Sends a context to a filter, such that it can be processed and then passed to the specified output pipe for further processing.

```csharp
public Task Send(SendContext context, IPipe<SendContext> next)
```

Parameters:

- `context` `SendContext`: The pipe context type
- `next` `IPipe<SendContext>`: The next pipe in the pipeline

Returns: `Task`: An awaitable Task
