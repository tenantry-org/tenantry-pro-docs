# `ITenantPropagator` interface

Namespace: `Tenantry.Pro` · Package: `Tenantry.Pro` · [API reference](README.md)

An extension point: for code that extends the package, such as another package that builds on it. An application rarely needs it.

Carries the tenant into jobs and messages, with tenant ids as text, so a transport needs no tenant key type. Tenantry.Pro's Hangfire, MassTransit, Quartz.NET and Rebus integrations use it; use it to carry the tenant over another bus or job library.

`UsePro` registers it as a singleton. On the sending side, put [`ITenantPropagator.CurrentTenantId`](tenantry-pro-itenantpropagator.md) in the job's or message's headers, under `HeaderName`. On the receiving side, pass that header to [`ITenantPropagator.ResolveAsync`](tenantry-pro-itenantpropagator.md) with the integration's [`TenantPropagationOptions`](tenantry-pro-tenantpropagationoptions.md), then run the work inside [`ITenantPropagator.MakeCurrent`](tenantry-pro-itenantpropagator.md). [`ITenantPropagator.ResolveAsync`](tenantry-pro-itenantpropagator.md) reads the carried tenant id's tenant from the store and asks `ValidateTenantActivity`, but the id itself is not authenticated: any producer that can send work can name any tenant, so only let producers you control send work. For the integrations' options and startup check too, write an [`ITenantPropagationAdapter`](tenantry-pro-itenantpropagationadapter.md).

```csharp
var tenant = await propagator.ResolveAsync(message.Headers["tenantry-tenant-id"], options, "Kafka message", message.Id, ct);
if (tenant.Skip)
    return;

using (propagator.MakeCurrent(tenant))
    await handler.HandleAsync(message, ct);
```

```csharp
[EditorBrowsable(EditorBrowsableState.Advanced)]
public interface ITenantPropagator
```

## Properties

### `CurrentTenantId`

The current tenant's id, as text, or [null](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null) when no tenant is current.

```csharp
string? CurrentTenantId { get; }
```

Value: `string`

## Methods

### `GetTenantIdsAsync(CancellationToken)`

The ids, as text, of every tenant in the store that `ValidateTenantActivity` allows, for work scheduled once for each tenant.

```csharp
ValueTask<IReadOnlyList<string>> GetTenantIdsAsync(CancellationToken cancellationToken = default)
```

Parameters:

- `cancellationToken` `CancellationToken`: Cancels the store read.

Returns: `ValueTask<IReadOnlyList<string>>`

### `MakeCurrent(PropagatedTenant)`

Makes the resolved tenant current until the returned handle is disposed, tags the current activity with `tenant.id` and opens a log scope with `TenantId`. For [`PropagatedTenant.WithoutTenant`](tenantry-pro-propagatedtenant.md), makes no tenant current instead, so the work does not run as a tenant that was current around it.

```csharp
IDisposable MakeCurrent(PropagatedTenant tenant)
```

Parameters:

- `tenant` [`PropagatedTenant`](tenantry-pro-propagatedtenant.md): What [`ITenantPropagator.ResolveAsync`](tenantry-pro-itenantpropagator.md) returned, unless it is [`PropagatedTenant.Skipped`](tenantry-pro-propagatedtenant.md).

Returns: `IDisposable`: A handle that restores the tenant that was current before.

Exceptions:

- `ArgumentException`: `tenant` is [`PropagatedTenant.Skipped`](tenantry-pro-propagatedtenant.md), or its tenant is not an `ITenantDescriptor<TKey>` of the application's tenant key type.

It makes the descriptor in `tenant` current as it is, without reading the store or asking     `ValidateTenantActivity`: pass what [`ITenantPropagator.ResolveAsync`](tenantry-pro-itenantpropagator.md) returned, which read the tenant from the     store and checked it. A descriptor of your own passed through [`PropagatedTenant.Resolved`](tenantry-pro-propagatedtenant.md) is     trusted as given.

Call it in the method that runs the work: the tenant is ambient, and an async method's changes to it end     when that method returns.

### `Resolve(string?, TenantPropagationOptions, string, object?)`

As [`ITenantPropagator.ResolveAsync`](tenantry-pro-itenantpropagator.md), blocking on the tenant lookup, for a host whose filters are synchronous.

```csharp
PropagatedTenant Resolve(string? tenantId, TenantPropagationOptions options, string carrier, object? carrierId)
```

Parameters:

- `tenantId` `string`: The tenant id the job or message carries, or [null](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null).
- `options` [`TenantPropagationOptions`](tenantry-pro-tenantpropagationoptions.md): The integration's policy.
- `carrier` `string`: What carries the tenant, for log messages and errors, such as `Kafka message`.
- `carrierId` `object`: The job's or message's id, for log messages and errors.

Returns: [`PropagatedTenant`](tenantry-pro-propagatedtenant.md): The tenant to make current with [`ITenantPropagator.MakeCurrent`](tenantry-pro-itenantpropagator.md), none, or that the work must not run.

Exceptions:

- `TenantNotResolvedException`: The policy that applies is [`TenantPropagationBehavior.Reject`](tenantry-pro-tenantpropagationbehavior.md).

### `ResolveAsync(string?, TenantPropagationOptions, string, object?, CancellationToken)`

Finds the tenant a job or message carries, applying `options` when it carries none, an id that is not valid, a tenant the store does not have, or one `ValidateTenantActivity` refuses.

```csharp
ValueTask<PropagatedTenant> ResolveAsync(string? tenantId, TenantPropagationOptions options, string carrier, object? carrierId, CancellationToken cancellationToken = default)
```

Parameters:

- `tenantId` `string`: The tenant id the job or message carries, or [null](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null).
- `options` [`TenantPropagationOptions`](tenantry-pro-tenantpropagationoptions.md): The integration's policy.
- `carrier` `string`: What carries the tenant, for log messages and errors, such as `Kafka message`.
- `carrierId` `object`: The job's or message's id, for log messages and errors.
- `cancellationToken` `CancellationToken`: Cancels the tenant lookup.

Returns: `ValueTask<PropagatedTenant>`: The tenant to make current with [`ITenantPropagator.MakeCurrent`](tenantry-pro-itenantpropagator.md), none, or that the work must not run.

Exceptions:

- `TenantNotResolvedException`: The policy that applies is [`TenantPropagationBehavior.Reject`](tenantry-pro-tenantpropagationbehavior.md).
