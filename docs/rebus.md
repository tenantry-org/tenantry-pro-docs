# Rebus integration

A message sent or published while a tenant is current carries that tenant in a header, and is handled as it: the
handlers, and the scoped services Rebus creates them with, see the tenant in `ITenantContext<TKey>`. A message a
handler sends or publishes carries the same tenant. A message can also be sent or published for a tenant by name.
The [RebusMessaging sample](../samples/Tenantry.Pro.Samples.RebusMessaging) does both, on the in-memory transport.

## Requirements

- `Tenantry.Pro.Rebus` NuGet package
- Rebus 8.4 or later 8.x, with the `Rebus.ServiceProvider` Microsoft DI integration

## Registration

```csharp
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Transport.InMem;

builder.Services.AddTenantry<string>(tenant => tenant
    .ResolveFromHeader("X-Tenant-Id")
    .UseStore<MyTenantStore>()
    .UsePro(pro => pro.AddRebusPropagation()));

builder.Services.AddRebus((configure, sp) => configure
    .Transport(t => t.UseInMemoryTransport(new(), "orders"))
    .Options(o => o.UseTenantry(sp)));
```

`pro.AddRebusPropagation()` registers the integration; `o.UseTenantry(sp)` adds Tenantry's steps to the bus's
pipeline. Calling it again for the same bus has no further effect. Without Rebus.ServiceProvider, pass the built
application's services (`o.UseTenantry(app.Services)`), and configure the bus before the application starts.

If `pro.AddRebusPropagation()` is called but no bus calls `o.UseTenantry(sp)` by the time the application has
started, the application fails to start with an `InvalidOperationException` that names the missing call: its
messages would otherwise be handled without their tenant. (Rebus.ServiceProvider before 10.7.2 configures the bus
just after the application has started on .NET 10, so Tenantry waits up to 5 seconds for it.) With more than one
bus (`AddRebus(…, isDefaultBus: false, key: …)`), call `o.UseTenantry(sp)` in each bus's options: the application
fails to start if any bus added through Rebus.ServiceProvider does not. Rebus keeps no list of its buses, so
Tenantry counts the ones Rebus.ServiceProvider adds; a bus you configure without it is not counted.

## Behaviour

The tenant's id is carried in the `TenantPropagation.HeaderName` header (`tenantry-tenant-id`). The tenant it names is
read from the store and checked against `ValidateTenantActivity` before the handlers run, but the header itself is not
authenticated: only let producers you control send to these queues, or check in the handler that the tenant may send
the message.

| Scenario | Outgoing (send/publish) | Incoming (handle) |
|----------|-------------------------|-------------------|
| A tenant is current | Header added | The handlers run as that tenant |
| No tenant is current | No header | `OnMissingTenant` applies (default `Warn`: the handlers run without a tenant, and a warning is logged) |
| The header names a tenant not in the store or a suspended one, or is not a valid id | Not applicable | `OnUnresolvedTenant` applies (default `Reject`: the message fails, with `TenantNotFoundException` or `TenantInactiveException`) ([Jobs and messages without a tenant](background-jobs.md#jobs-and-messages-without-a-tenant)) |
| Message sent with a tenant in its headers (`WithTenant`) | Header kept | The handlers run as that tenant, whichever tenant was current when it was sent |

While the handlers run as their tenant, their logs carry a `TenantId` scope, and the trace span they run in, if
tracing records one, is tagged `tenant.id` ([Telemetry](telemetry.md#logs-and-traces)).

## Sending or publishing for a tenant

To send or publish on a tenant's behalf from code that runs without one (an administrator's request, a system
task), or for another tenant than the current one, pass the tenant in the message's headers:

```csharp
using Rebus.Bus;

public sealed class OrderReminders(IBus bus)
{
    public Task RemindAsync(string tenantId, Guid orderId) =>
        bus.Publish(new OrderPlaced(orderId), new Dictionary<string, string>().WithTenant(tenantId));
}
```

Tenantry's outgoing step leaves a tenant header the message already has in place. The tenant is looked up when the
message is handled, so `OnUnresolvedTenant` applies to an id the store does not have. `WithTenant` refuses the id
Tenantry reserves for "no tenant" (`Guid.Empty`, `0`, an empty string).

## Messages without a tenant, or with one that cannot be found

`OnMissingTenant` (default `Warn`) and `OnUnresolvedTenant` (default `Reject`) decide what happens to an incoming
message whose tenant cannot be made current
([Jobs and messages without a tenant](background-jobs.md#jobs-and-messages-without-a-tenant)). Set them for Rebus
when you add it:

```csharp
using Tenantry.Pro;

pro.AddRebusPropagation(o => o.OnMissingTenant = TenantPropagationBehavior.Skip);
```

The incoming step runs just before Rebus deserializes the message, inside Rebus's retry step, so a tenant store that
throws is retried like any other failure, and the store lookup is cancelled when the bus stops. A pipeline customised
to drop Rebus's `DeserializeIncomingMessageStep` makes the bus fail to start, rather than run handlers outside their
tenant. Steps Rebus runs before deserializing run without the tenant: loading a data bus attachment
(`HydrateIncomingMessageStep`) and, with `EnableEncryption`, decrypting the message. A data bus storage or an
encryption key provider cannot depend on the message's tenant.

## Accessing the tenant inside a handler

```csharp
using Tenantry;

public sealed class OrderPlacedHandler(ITenantContext<string> tenantContext) : IHandleMessages<OrderPlaced>
{
    public Task Handle(OrderPlaced message)
    {
        var tenantId = tenantContext.CurrentTenantId;   // the tenant the message was sent for
        return Task.CompletedTask;
    }
}
```

Inject `ITenantContext<TKey>` and check `HasTenant` if a handler must handle both messages with a tenant and
messages without one (system events published with no tenant current, say).

## See also

- [MassTransit](masstransit.md): the same for MassTransit.
- [Background jobs & non-HTTP hosts](background-jobs.md): scoping by hand when you own the loop.
