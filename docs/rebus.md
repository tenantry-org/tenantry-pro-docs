# Rebus integration

Use `Tenantry.Pro.Rebus` to propagate the current tenant context across Rebus message boundaries. The
tenant ID is stamped as a message header when a message is sent or published, and the tenant scope is
restored from that header when the message is handled.

## What it provides

- `TenantOutgoingStep<TKey>` — adds the tenant ID header to outgoing messages.
- `TenantIncomingStep<TKey>` — restores the tenant scope from the header before a handler runs.
- `pro.AddRebusTenantSteps<TKey>()` — registers both steps in DI.
- `options.UseTenantryPro(sp)` — wires the steps into the Rebus pipeline.

The header key is `tenantry-tenant-id` (exposed as `TenantOutgoingStep<TKey>.HeaderKey`).

## Requirements

- `Tenantry.Pro.Rebus` NuGet package
- Rebus 8.x with the `Rebus.ServiceProvider` Microsoft DI integration

## Registration

```csharp
using Rebus.Config;
using Tenantry.Pro;
using Tenantry.Pro.Rebus.Extensions;

// 1. Register Tenantry with the Rebus steps.
builder.Services.AddTenantry<string>(tenant =>
{
    tenant.ResolveFromHeader("X-Tenant-Id");
    tenant.UseStore<MyTenantStore>();
    tenant.UsePro(pro =>
    {
        pro.WithLicence(builder.Configuration["Tenantry:Licence"]!);
        pro.AddRebusTenantSteps<string>();   // registers the outgoing + incoming steps in DI
    });
});

// 2. Configure Rebus and decorate the pipeline with the Tenantry steps.
builder.Services.AddRebus((configure, sp) => configure
    .Transport(t => t.UseInMemoryTransport(new(), "orders"))
    .Options(o => o.UseTenantryPro(sp)));   // wires the steps into the pipeline
```

`UseTenantryPro(sp)` resolves the registered configurator; if you forget
`pro.AddRebusTenantSteps<TKey>()` it throws `InvalidOperationException` telling you to add the steps
before `AddRebus`.

## Behaviour

| Scenario | Outgoing (send/publish) | Incoming (handle) |
|----------|-------------------------|-------------------|
| Active tenant scope | Header `tenantry-tenant-id` added | Scope restored for the handler |
| No tenant scope | Header omitted | Missing-tenant policy applies (default: warn, run without scope) |
| Header present but unparseable as `TKey` | — | Missing-tenant policy applies (default: warn, run without scope) |
| Header present but tenant not in store | — | Missing-tenant policy applies (default: warn, run without scope) |

## Missing-tenant policy

When an incoming message has no resolvable tenant, `AddRebusTenantSteps` decides what to do via
`TenantPropagationOptions.OnMissingTenant`:

```csharp
pro.AddRebusTenantSteps<string>(o => o.OnMissingTenant = MissingTenantBehavior.Skip);
```

`Allow` runs the handler without a scope silently; `Warn` (default) does the same and logs; `Reject`
throws (Rebus applies its retry/error handling); `Skip` drops the message without running the handler.
The same option exists on every Tenantry.Pro integration and mirrors core's
`EfCoreIsolationOptions.OnMissingTenant`.

The incoming step restores the scope for the duration of the handler. Any messages your handler sends
or publishes while the scope is active automatically carry the same tenant header downstream.

## Accessing the tenant inside a handler

```csharp
public sealed class OrderPlacedHandler(ITenantContext<string> tenantContext) : IHandleMessages<OrderPlaced>
{
    public Task Handle(OrderPlaced message)
    {
        var tenantId = tenantContext.CurrentTenantId;   // restored from the message header
        return Task.CompletedTask;
    }
}
```

Inject `ITenantContext<TKey>` and check `HasTenant` if a handler must cope with both tenant-scoped and
scopeless messages (e.g. system events published without a tenant).

## See also

- [MassTransit](masstransit.md) — the same pattern for MassTransit.
- [Background jobs & non-HTTP hosts](background-jobs.md) — manual scoping when you own the loop.
