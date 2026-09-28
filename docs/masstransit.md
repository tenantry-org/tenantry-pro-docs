# MassTransit Integration

Use this package to propagate the current tenant context across MassTransit message boundaries.
The tenant ID is stamped as a message header when publishing or sending, and the tenant scope
is restored from that header when the message is consumed.

## What Tenantry.Pro.MassTransit Provides

- `TenantPublishFilter<TKey>` — adds the tenant ID header when messages are published
- `TenantSendFilter<TKey>` — adds the tenant ID header when messages are sent point-to-point
- `TenantConsumeFilter<TKey>` — restores the tenant scope from the header when consuming
- `pro.AddMassTransitTenantFilters()` — registers all three filters in DI
- `x.AddTenantryConsumeFilter()` — registers the global consume endpoint callback (TKey resolved from DI)
- `cfg.UseTenantryPro(ctx)` — wires the publish and send filters into the pipeline (TKey resolved from DI)

The message header key is `tenantry-tenant-id` (available as `TenantPublishFilter<TKey>.HeaderKey`).

## Requirements

- `Tenantry.Pro.MassTransit` NuGet package
- MassTransit 8.x

## Registration

```csharp
using MassTransit;
using Tenantry.Pro;
using Tenantry.Pro.MassTransit.Extensions;

// 1. Register Tenantry with MassTransit filters
builder.Services.AddTenantry<string>(tenant =>
{
    tenant.ResolveFromHeader("X-Tenant-Id");
    tenant.UseStore<MyTenantStore>();
    tenant.UsePro(pro =>
    {
        pro.WithLicence(builder.Configuration["Tenantry:Licence"]!);
        pro.AddMassTransitTenantFilters();   // registers filter singletons in DI
    });
});

// 2. Configure MassTransit (TKey is resolved from DI by the non-generic overloads)
builder.Services.AddMassTransit(x =>
{
    x.AddTenantryConsumeFilter();           // global consume callback for all endpoints

    x.AddConsumer<MyOrderConsumer>();

    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host("rabbitmq://localhost");
        cfg.UseTenantryPro(ctx);            // publish + send filters
        cfg.ConfigureEndpoints(ctx);
    });
});
```

## Behaviour

| Scenario | Publish / Send | Consume |
|----------|---------------|---------|
| Active tenant scope | Header `tenantry-tenant-id` added | Scope restored for consumer |
| No tenant scope | Header omitted | Missing-tenant policy applies (default: warn, run without scope) |
| Header present but tenant not in store | — | Missing-tenant policy applies (default: warn, run without scope) |

## Missing-tenant policy

When a consumed message has no resolvable tenant, `AddMassTransitTenantFilters` decides what to do via
`TenantPropagationOptions.OnMissingTenant`:

```csharp
pro.AddMassTransitTenantFilters(o => o.OnMissingTenant = MissingTenantBehavior.Skip);
```

`Allow` runs the consumer without a scope silently; `Warn` (default) does the same and logs; `Reject`
throws (the message goes to MassTransit's retry/error handling); `Skip` acknowledges and drops the
message without running the consumer. The same option exists on every Tenantry.Pro integration and
mirrors core's `EfCoreIsolationOptions.OnMissingTenant`.

## Accessing the tenant inside a consumer

```csharp
public class MyOrderConsumer(ITenantContext<string> tenantContext) : IConsumer<OrderPlaced>
{
    public Task Consume(ConsumeContext<OrderPlaced> context)
    {
        // tenantContext.CurrentTenantId is set from the message header
        var tenantId = tenantContext.CurrentTenantId;
        return Task.CompletedTask;
    }
}
```

## Limitations

- The consume filter restores the scope for the duration of the consumer's execution only.
  Any `IPublishEndpoint`/`ISendEndpoint` calls within the consumer will automatically stamp
  the restored tenant ID on outgoing messages.
- If your consumer publishes additional messages and you want those downstream messages to carry
  the same tenant, ensure the tenant scope is active when publishing (which it will be, as long
  as `AddTenantryConsumeFilter` is registered).
