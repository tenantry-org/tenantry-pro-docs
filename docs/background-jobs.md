# Background jobs & non-HTTP hosts

In an ASP.NET Core request, `UseTenantry()` makes the tenant current. A worker service, console app or hosted
service has no request, so you do it yourself: Tenantry Core's `ITenantScopeFactory<TKey>` opens a DI scope with a
current tenant. It and `ITenantLookup<TKey>` are registered by `AddTenantry`. Core's
[non-HTTP hosts guide](https://github.com/tenantry-org/tenantry-core/blob/master/docs/non-http-hosts.md) covers them;
this page shows how they fit with Pro.

## The factory

```csharp no-compile
public interface ITenantScopeFactory<TKey>
{
    // Activate a tenant you already have a descriptor for.
    ITenantScope<TKey> CreateScope(ITenantDescriptor<TKey> tenant);

    // Look the tenant up in the store by id, then run the work inside its scope.
    Task RunInScopeAsync(TKey tenantId, Func<ITenantScope<TKey>, CancellationToken, Task> work,
        CancellationToken cancellationToken = default);
    Task<TResult> RunInScopeAsync<TResult>(TKey tenantId,
        Func<ITenantScope<TKey>, CancellationToken, Task<TResult>> work,
        CancellationToken cancellationToken = default);
}
```

The returned `ITenantScope<TKey>` is an `IServiceScope` and `IAsyncDisposable`, and exposes:

- `ServiceProvider`: a DI scope in which the tenant is current. Resolve your scoped services from it.
- `Tenant`: the tenant.

Disposing the scope, with `using` or `await using`, disposes its services while the tenant is still
active and then restores whichever tenant was current before it. That holds in loops and when scopes
nest.

There is no `CreateScopeAsync(tenantId)`. The tenant lives in an `AsyncLocal`, and an `async` method
cannot change its caller's value, so a scope opened inside an asynchronous lookup would never be active
for the code that awaited it. Use `RunInScopeAsync` when you only have an id.

## Worker service example

```csharp
public sealed class NightlyRollupWorker(
    ITenantScopeFactory<string> scopeFactory,
    ITenantLookup<string> tenants, // enumerate tenants from the store
    ILogger<NightlyRollupWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        foreach (var tenant in await tenants.GetAllTenantsAsync(stoppingToken))
        {
            // One scope per tenant: scoped services inside see exactly this tenant.
            await using var scope = scopeFactory.CreateScope(tenant);

            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            // ...do tenant-scoped work; db reads/writes target this tenant's database/schema...
            await db.SaveChangesAsync(stoppingToken);

            logger.LogInformation("Rolled up tenant {TenantId}", tenant.TenantId);
        }
    }
}
```

When you only have an id (a queue message, a CLI argument), let the factory look the tenant up and run
the work inside its scope. It throws `TenantNotFoundException` (a `TenantNotResolvedException`) if the store has no
such tenant:

```csharp
await scopeFactory.RunInScopeAsync(message.TenantId, async (scope, ct) =>
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    // ...tenant-scoped work...
    await db.SaveChangesAsync(ct);
}, stoppingToken);
```

## Built-in "for every tenant" base classes

For that loop, Tenantry.Pro has two base classes; you override only the per-tenant work:

- `TenantBackgroundService<TKey>` runs once for every tenant, then completes: a startup or maintenance pass.
- `PeriodicTenantBackgroundService<TKey>` runs at startup and then every `Interval`.

Both go through the tenants one at a time. An exception in one tenant is logged and the others go on. If a periodic sweep fails as a
whole (the tenant store cannot be read, say), the failure is logged and the next sweep runs on schedule.
A one-shot `TenantBackgroundService` whose store fails throws like any `BackgroundService`, which by
default stops the host. Override
`ExecuteForTenantAsync` with the per-tenant work. It gets the tenant's `ITenantScope<TKey>`: `scope.Tenant`
is the tenant, and scoped services resolved from `scope.ServiceProvider` see it. The base class disposes the
scope afterwards. Log through the protected `Logger`, the logger you pass to the constructor.

```csharp
using Tenantry;
using Tenantry.Pro;

public sealed class NightlyRollup(
    ITenantScopeFactory<string> scopeFactory,
    ITenantLookup<string> tenants,
    ILogger<NightlyRollup> logger)
    : PeriodicTenantBackgroundService<string>(scopeFactory, tenants, logger)
{
    protected override TimeSpan Interval => TimeSpan.FromHours(24);

    protected override async Task ExecuteForTenantAsync(ITenantScope<string> scope, CancellationToken ct)
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        // ...tenant-scoped work; db targets this tenant's database/schema...
        await db.SaveChangesAsync(ct);
        Logger.LogInformation("Rolled up tenant {TenantId}", scope.Tenant.TenantId);
    }
}

// Program.cs
builder.Services.AddHostedService<NightlyRollup>();
```

Use `ITenantScopeFactory<TKey>` directly when your loop is not over every tenant, such as a queue of tenant ids. The
[BackgroundWorker sample](../samples/Tenantry.Pro.Samples.BackgroundWorker) runs a periodic sweep in a worker host.

## Suspended tenants

Tenantry Core's `ValidateTenantActivity` says which tenants may have work run for them:

```csharp no-compile
tenant.ValidateTenantActivity(t => t.As<Tenant>().IsActive);   // Tenant is your descriptor type
```

With it, background work leaves out a tenant it refuses:

- `RunInScopeAsync` throws `TenantInactiveException`.
- `TenantBackgroundService` and `PeriodicTenantBackgroundService` skip the tenant.
- Hangfire's `AddOrUpdateForEachTenant` and Quartz.NET's `ForEachTenant` leave it out.
- A job or message for the tenant is handled as one whose tenant the store does not have: `OnUnresolvedTenant`
  decides. Its default, `Reject`, fails it with `TenantInactiveException`, so it waits for the host's retries or
  error queue.

`CreateScope` does not check, because provisioning, offboarding and migrations must reach suspended tenants. A loop of
your own that calls it should ask `ITenantActivity<TKey>.IsActiveAsync` first. Without `ValidateTenantActivity`,
every tenant is active.

## Registering Pro in a non-HTTP host

Use the same `AddTenantry<TKey>` as a web app, from Tenantry core, and add `UsePro` exactly as in a web app.
There are no resolvers, because there is no request: you open scopes yourself with the factory.

```csharp
using Microsoft.EntityFrameworkCore;
using Tenantry;
using Tenantry.Pro;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddTenantry<string>(tenant =>
{
    tenant.UseStore<MyTenantStore>();
    tenant.UseConnectionStrings(opts =>
        opts.GetConnectionString = t =>
            $"Server=.;Database=app_{t.TenantId};Integrated Security=true;TrustServerCertificate=True");
    tenant.UsePro();
    tenant.AddDbContextPerTenantDatabase<AppDbContext>((sp, options) => options.UseSqlServer());
});

builder.Services.AddHostedService<NightlyRollupWorker>();

builder.Build().Run();
```

## Reading tenants from a singleton: `ITenantLookup<TKey>`

A tenant store is often scoped (one that reads a `Tenants` table through EF Core, say), so a singleton such as a
hosted service must not inject `ITenantStore`. Inject `ITenantLookup<TKey>`, which resolves the store in a new scope
for each call:

```csharp no-compile
ValueTask<ITenantDescriptor<TKey>?>           GetTenantAsync(TKey id, CancellationToken ct = default);
ValueTask<ITenantDescriptor<TKey>?>           FindByIdentifierAsync(string identifier, CancellationToken ct = default);
ValueTask<IReadOnlyList<ITenantDescriptor<TKey>>> GetAllTenantsAsync(CancellationToken ct = default);
```

It is the lookup the migration runner and health checks use to enumerate tenants, and the integrations
use to look up the tenant of each job and message. With Tenantry Core's `tenant.CacheTenants()`, its lookups by id
and identifier are served from a cache, so a job or message does not read the store each time
([caching](https://github.com/tenantry-org/tenantry-core/blob/master/docs/tenant-stores.md#caching)); listing every
tenant always reads the store.

## Library integrations

Tenantry.Pro carries the tenant into these libraries' jobs and messages, so you do not open scopes yourself there:

- [Hangfire](hangfire.md): restores the enqueuing request's tenant when a job runs, enqueues for another tenant
  (`WithTenant`), and runs a recurring job for each tenant (`AddOrUpdateForEachTenant`).
- [Quartz.NET](quartz.md): runs a scheduled job inside the tenant stamped into its job data (`WithTenant`), or once
  for each tenant (`ForEachTenant`).
- [MassTransit](masstransit.md) and [Rebus](rebus.md): restore the tenant from a message header on consume, routing
  slips included, and send for another tenant (`WithTenant`).

Each `WithTenant` takes the tenant, or its id. Pass the tenant when you have it: an id of the wrong type (a slug in a
`Guid`-keyed application) compiles, and the job or message then fails when it runs, as a tenant the store does not
have.

Reach for `ITenantScopeFactory<TKey>` when you own the loop: custom `BackgroundService`s, console
tools, one-off maintenance jobs, or a scheduler without a dedicated Tenantry.Pro integration.

To carry the tenant over another bus or job library (NServiceBus, Kafka, Azure Functions), write an adapter on the
public API the integrations are built on. An `ITenantPropagationAdapter`, registered with
`TenantPropagationAdapter.Add` from a `pro.Add…Propagation()` method of your own, gets `TenantPropagationOptions` of
its own, the `ITenantPropagator` that `UsePro` registers, and the integrations' startup check: the application does not
start if the adapter's host side never ran. The host side resolves `TenantPropagationIntegration<TAdapter>`, marks it
wired, and carries the tenant with its `Propagator` and `Options`: put `CurrentTenantId` in a header when you send, and
on receipt resolve the header and run the work inside `Use`:

```csharp
using Tenantry;
using Tenantry.Pro;

// What the startup check says when the host side never ran.
public sealed class QueuePropagation : ITenantPropagationAdapter
{
    public string Registration => "pro.AddQueuePropagation()";

    public string HostSide => "call services.UseQueueTenantry() when you build the queue's handler";
}

public sealed record QueueMessage(string Id, IDictionary<string, string> Headers);

public static class QueuePropagationExtensions
{
    // The adapter's options ("Queue"), the propagator and the startup check.
    public static IProBuilder<TKey> AddQueuePropagation<TKey>(
        this IProBuilder<TKey> pro, Action<TenantPropagationOptions>? configure = null)
        where TKey : IEquatable<TKey>, IParsable<TKey>
    {
        TenantPropagationAdapter.Add<TKey, QueuePropagation>(pro, "Queue", configure);
        return pro;
    }

    // The host side: marks the adapter wired, and runs each message as the tenant it carries.
    public static Func<QueueMessage, Func<Task>, CancellationToken, Task> UseQueueTenantry(this IServiceProvider services)
    {
        var integration = services.GetRequiredService<TenantPropagationIntegration<QueuePropagation>>();
        integration.MarkWired();

        return async (message, handle, ct) =>
        {
            message.Headers.TryGetValue(TenantPropagation.HeaderName, out var tenantId);
            var tenant = await integration.Propagator.ResolveAsync(tenantId, integration.Options, "queue message", message.Id, ct);
            if (tenant.Skip)
                return;

            using (integration.Propagator.Use(tenant))
                await handle();
        };
    }
}
```

A `WithTenant` method of your own puts `TenantPropagationAdapter.FormatTenantId(tenantId)` in the header: it refuses
the ids reserved for no tenant, as the integrations' do. An adapter for a library with several buses, each configured
on its own, implements `FindUnwired` to say which are not wired; one whose library configures itself only when one of
its services is first resolved implements `RunDeferredHostConfiguration`.

The receiver trusts the header as it is, as the integrations do: only let producers you control send to it.
