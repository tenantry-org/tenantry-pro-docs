# Background jobs & non-HTTP hosts

In an ASP.NET Core request, `UseTenantry()` opens the tenant scope for you. A worker service, console
app, scheduled job, or `IHostedService` has no request — so there is nothing to open the scope, and
scoped tenant-aware services (your `DbContext`, repositories) have no tenant to bind to. Tenantry
Core's `ITenantScopeFactory<TKey>` fills that gap: it creates a combined **DI scope + tenant scope** in
one call.

`ITenantScopeFactory<TKey>` and `ITenantStoreAccessor<TKey>` live in `Tenantry.Core` and are registered
by `AddTenantry`/`AddTenantryCore` (and therefore wherever you call `UsePro()`) — no extra configuration.
Core's [non-HTTP hosts guide](https://github.com/tenantry-org/tenantry-core/blob/main/docs/non-http-hosts.md)
covers them in full; this page shows how they fit with Pro.

## The factory

```csharp
public interface ITenantScopeFactory<TKey>
{
    // Activate a tenant you already have a descriptor for.
    ITenantServiceScope<TKey> CreateScope(ITenantDescriptor<TKey> tenant);

    // Look the tenant up in the store by id, then run the work inside its scope.
    Task RunInScopeAsync(TKey tenantId, Func<ITenantServiceScope<TKey>, CancellationToken, Task> work,
        CancellationToken cancellationToken = default);
    Task<TResult> RunInScopeAsync<TResult>(TKey tenantId,
        Func<ITenantServiceScope<TKey>, CancellationToken, Task<TResult>> work,
        CancellationToken cancellationToken = default);
}
```

The returned `ITenantServiceScope<TKey>` is an `IServiceScope` and `IAsyncDisposable`, and exposes:

- `ServiceProvider` — a DI scope in which the tenant is active. Resolve your scoped services **from
  here**.
- `Tenant` — the active tenant descriptor.

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
    ITenantStoreAccessor<string> tenants,        // enumerate tenants from the store
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
the work inside its scope. It throws `TenantNotResolvedException` if the store has no such tenant:

```csharp
await scopeFactory.RunInScopeAsync(message.TenantId, async (scope, ct) =>
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    // ...tenant-scoped work...
    await db.SaveChangesAsync(ct);
}, stoppingToken);
```

## Built-in "for every tenant" base classes

The loop above — enumerate tenants, scope into each, isolate per-tenant failures — is common enough
that Tenantry.Pro ships two base classes so you only write the per-tenant body:

- **`TenantBackgroundService<TKey>`** — runs once for every tenant, then completes. Good for a one-shot
  startup or maintenance pass.
- **`PeriodicTenantBackgroundService<TKey>`** — runs the sweep at startup and then every `Interval`.

Both sweep tenants sequentially and **isolate per-tenant failures**: an exception in one tenant is
logged and the sweep continues, so one bad tenant never stops the rest. Override
`ExecuteForTenantAsync` with the per-tenant work — it runs inside the tenant's scope, so scoped
services resolved from the supplied provider see the tenant.

```csharp
public sealed class NightlyRollup(
    ITenantScopeFactory<string> scopeFactory,
    ITenantStoreAccessor<string> tenants,
    ILogger<NightlyRollup> logger)
    : PeriodicTenantBackgroundService<string>(scopeFactory, tenants, logger)
{
    protected override TimeSpan Interval => TimeSpan.FromHours(24);

    protected override async Task ExecuteForTenantAsync(
        ITenantDescriptor<string> tenant, IServiceProvider scopedServices, CancellationToken ct)
    {
        var db = scopedServices.GetRequiredService<AppDbContext>();
        // ...tenant-scoped work; db targets this tenant's database/schema...
        await db.SaveChangesAsync(ct);
    }
}

// Program.cs
builder.Services.AddHostedService<NightlyRollup>();
```

Reach for the raw `ITenantScopeFactory<TKey>` (above) only when your loop doesn't fit the
"every tenant" shape — e.g. processing a queue of specific tenant ids.

## Registering Pro in a non-HTTP host

Use Tenantry core's `AddTenantryCore<TKey>` (instead of `AddTenantry`) and add `UsePro` exactly as in
a web app. There are no resolvers because there is no request — you open scopes yourself with the
factory.

```csharp
using Tenantry.Core;              // ITenantConnectionStringResolver
using Tenantry.Core.Extensions;   // AddTenantryCore
using Tenantry.Pro;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddTenantryCore<string>(tenant =>
{
    tenant.UseStore<MyTenantStore>();

    tenant.UsePro(pro =>
    {
        pro.WithLicence(builder.Configuration["Tenantry:Licence"]!);
        pro.UseDatabasePerTenant(opts =>
            opts.GetConnectionString = t =>
                $"Server=.;Database=app_{t.TenantId};Integrated Security=true;TrustServerCertificate=True");
    });
});

builder.Services.AddDbContext<AppDbContext>((sp, options) =>
    options.UseSqlServer(sp.GetRequiredService<ITenantConnectionStringResolver<string>>().Resolve()));

builder.Services.AddHostedService<NightlyRollupWorker>();

builder.Build().Run();
```

## Reading tenants from a singleton: `ITenantStoreAccessor<TKey>`

A custom tenant store is **scoped** (for example an EF-backed store reading a `Tenants` table). A
singleton — a hosted service, a Pro provisioning/migration service — must not inject `ITenantStore`
directly: that is a captive dependency. Inject `ITenantStoreAccessor<TKey>` instead. It opens a fresh
DI scope per call to resolve the store, so it is correct whether the store is singleton or scoped:

```csharp
ValueTask<ITenantDescriptor<TKey>?>           GetTenantAsync(TKey id, CancellationToken ct = default);
ValueTask<IReadOnlyList<ITenantDescriptor<TKey>>> GetAllTenantsAsync(CancellationToken ct = default);
```

This is the same accessor the migration orchestrator, provisioning services and health checks use to
enumerate tenants.

## Library integrations

For specific background-processing and messaging libraries, Tenantry.Pro ships dedicated integrations
that capture and restore the tenant scope automatically — you usually do **not** need to call the
scope factory by hand:

- [Hangfire](hangfire.md) — restores the enqueuing request's tenant when a job runs.
- [Quartz.NET](quartz.md) — runs a scheduled job inside the tenant stamped into its job data.
- [MassTransit](masstransit.md) / [Rebus](rebus.md) — restore the tenant from a message header on consume.

Reach for `ITenantScopeFactory<TKey>` when you own the loop: custom `BackgroundService`s, console
tools, one-off maintenance jobs, or a scheduler without a dedicated Tenantry.Pro integration.
