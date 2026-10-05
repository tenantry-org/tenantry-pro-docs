# MassTransit Integration

A message published or sent while a tenant is current carries that tenant in a header, and is consumed as it:
the consumer, and the scoped services MassTransit creates it with, see the tenant in `ITenantContext<TKey>`. A
message the consumer publishes or sends carries the same tenant, and so does a routing slip's every activity. A
message can also be published or sent for a tenant by name. The
[MassTransitMessaging sample](../samples/Tenantry.Pro.Samples.MassTransitMessaging) runs on the in-memory transport.

## Requirements

- `Tenantry.Pro.MassTransit` NuGet package
- MassTransit 8.1 or later 8.x

## Registration

```csharp
using MassTransit;

builder.Services.AddTenantry<string>(tenant => tenant
    .ResolveFromHeader("X-Tenant-Id")
    .UseStore<MyTenantStore>()
    .UsePro(pro => pro.AddMassTransitPropagation()));

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<MyOrderConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("rabbitmq://localhost");
        cfg.UseTenantry(context);
        cfg.ConfigureEndpoints(context);
    });
});
```

`pro.AddMassTransitPropagation()` registers the integration; `cfg.UseTenantry(context)` adds Tenantry's publish,
send, consume and routing-slip activity filters to the bus, and calling it again for the same bus has no further
effect. It covers every receive endpoint, whether `ConfigureEndpoints` or your own `cfg.ReceiveEndpoint(…)`
configures it, before or after the call: consumers and sagas (through MassTransit's scoped filters, which it
creates in each message's scope), handlers (`e.Handler<T>(…)`), and routing-slip activities, as they execute and as
they compensate.

If `pro.AddMassTransitPropagation()` is called but the bus configuration never calls `cfg.UseTenantry(context)`,
the application fails to start with an `InvalidOperationException` that names the missing call: its messages
would otherwise be consumed without their tenant. With more than one bus (MassTransit's MultiBus,
`AddMassTransit<TBus>`), call `cfg.UseTenantry(context)` in each bus's configuration: the application fails to
start if any bus does not, and the error names the buses.

## Behaviour

The tenant's id is carried in the `TenantPropagation.HeaderName` header (`tenantry-tenant-id`). The tenant it names
is read from the store and checked against `ValidateTenantActivity` before the consumer runs, but the header itself is
not authenticated: only let producers you control publish to these endpoints, or check in the consumer that the tenant
may send the message.

| Scenario | Publish / Send | Consume |
|----------|---------------|---------|
| A tenant is current | Header added | The consumer runs as that tenant |
| No tenant is current | No header | `OnMissingTenant` applies (default `Warn`: the consumer runs without a tenant, and a warning is logged) |
| The header names a tenant not in the store or a suspended one, or is not a valid id | Not applicable | `OnUnresolvedTenant` applies (default `Reject`: the message faults, with `TenantNotFoundException` or `TenantInactiveException`) ([Jobs and messages without a tenant](background-jobs.md#jobs-and-messages-without-a-tenant)) |

A message a consumer publishes or sends carries the consumed message's tenant even when it is sent after the
consumer returns, as MassTransit's in-memory outbox (`UseInMemoryOutbox`) sends it: MassTransit copies the consumed
message's headers onto the messages published or sent from it.

While a consumer runs as its tenant, its logs carry a `TenantId` scope, and, if tracing records spans, the
message's receive span is tagged `tenant.id`. The span MassTransit starts for the consumer is a child of it and is
not tagged; a routing-slip activity's span is ([Telemetry](telemetry.md#logs-and-traces)).

## Publishing or sending for a tenant

To publish or send on a tenant's behalf from code that runs without one (an administrator's request, a system
task), or for another tenant than the current one, set the tenant in the callback `Publish` or `Send` takes:

```csharp
using MassTransit;

public sealed class OrderReminders(IPublishEndpoint publishEndpoint)
{
    public Task RemindAsync(string tenantId, Guid orderId) =>
        publishEndpoint.Publish(new OrderPlaced(orderId), context => context.WithTenant(tenantId));
}
```

The message is consumed as that tenant whichever tenant is current, also from a consumer, and also through the
in-memory outbox: MassTransit runs the callback after the bus's filters, Tenantry's among them. The tenant is looked up when the
message is consumed, so `OnUnresolvedTenant` applies to an id the store does not have. `WithTenant` refuses the id
Tenantry reserves for "no tenant" (`Guid.Empty`, `0`, an empty string).

## Routing slips

A routing slip (MassTransit Courier) carries the tenant that was current when it was executed (`bus.Execute`). Each
activity executes, and compensates, as that tenant: the activity, and the scoped services MassTransit creates it
with, see it in `ITenantContext<TKey>`. Each activity passes the tenant on to the next, and to the events the
routing slip publishes. To execute a routing slip for a tenant by name, send it to its first activity, as
`Execute` does, with `WithTenant`:

```csharp
using MassTransit;
using MassTransit.Courier.Contracts;

public sealed class Fulfilment(ISendEndpointProvider sendEndpoints)
{
    public async Task StartAsync(string tenantId, RoutingSlip routingSlip)
    {
        var firstActivity = await sendEndpoints.GetSendEndpoint(routingSlip.GetNextExecuteAddress()!);
        await firstActivity.Send(routingSlip, context => context.WithTenant(tenantId));
    }
}
```

`OnMissingTenant` and `OnUnresolvedTenant` apply to each activity as to a consumer, except that an activity that
does not run (`Skip`) returns no result, so MassTransit faults the routing slip: the activities before it
compensate, and `RoutingSlipFaulted` is published. Under `Reject`, the routing slip faults in the same way. When an
activity compensates and its tenant cannot be made current (the store no longer has it, say), `Skip` and `Reject`
both make MassTransit record the compensation as failed (`RoutingSlipActivityCompensationFailed`) and stop: the
activities before it are not compensated.

## Batch consumers

A batch consumer (`IConsumer<Batch<T>>`) runs as one tenant for the whole batch, so a batch must hold messages of
one tenant only. A batch whose messages carry different tenants, or some a tenant and some none, fails without being
consumed. Group the consumer's batches by the tenant header, and each batch is consumed as its tenant (messages
without a tenant are batched together):

```csharp
using MassTransit;
using Tenantry.Pro;

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<OrderBatchConsumer>(c => c.Options<BatchOptions>(o => o
        .SetMessageLimit(100)
        .GroupBy<OrderPlaced, string>(m => m.Headers.Get<string>(TenantPropagation.HeaderName)!)));

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.UseTenantry(context);
        cfg.ConfigureEndpoints(context);
    });
});

public class OrderBatchConsumer : IConsumer<Batch<OrderPlaced>>
{
    public Task Consume(ConsumeContext<Batch<OrderPlaced>> context) => Task.CompletedTask;   // runs as the batch's tenant
}
```

## Messages without a tenant, or with one that cannot be found

`OnMissingTenant` (default `Warn`) and `OnUnresolvedTenant` (default `Reject`) decide what happens to a consumed
message whose tenant cannot be made current
([Jobs and messages without a tenant](background-jobs.md#jobs-and-messages-without-a-tenant)). Set them for
MassTransit when you add it:

```csharp
using Tenantry.Pro;

pro.AddMassTransitPropagation(o => o.OnMissingTenant = TenantPropagationBehavior.Skip);
```

## Accessing the tenant inside a consumer

```csharp
using Tenantry;

public class MyOrderConsumer(ITenantContext<string> tenantContext) : IConsumer<OrderPlaced>
{
    public Task Consume(ConsumeContext<OrderPlaced> context)
    {
        // The tenant the message was published for
        var tenantId = tenantContext.CurrentTenantId;
        return Task.CompletedTask;
    }
}
```
