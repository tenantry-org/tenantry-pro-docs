# Getting started

This guide builds a working **database-per-tenant** ASP.NET Core app on SQL Server end to end:
resolve the tenant, give each tenant its own database, provision new databases on demand, and migrate
them all on startup. Schema-per-tenant and the other providers follow the same shape — see the
[schema-per-tenant](schema-per-tenant.md) and [database providers](database-providers.md) guides.

## 1. Install the packages

Tenantry.Pro sits on top of Tenantry core, so you install both. The Pro packages come from a private
feed: set it up first with [Installation](installation.md), which also covers CI and the licence key. For
an ASP.NET Core app using SQL Server:

```bash
dotnet add package Tenantry.AspNetCore            # Tenantry core: resolution + middleware
dotnet add package Tenantry.Pro                    # Pro: strategies + licensing
dotnet add package Tenantry.Pro.EfCore.SqlServer   # provisioning + migration orchestration
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
```

| If you need… | Add |
|--------------|-----|
| Schema-per-tenant model caching | `Tenantry.Pro.EfCore` |
| PostgreSQL / MySQL | `Tenantry.Pro.EfCore.Npgsql` / `Tenantry.Pro.EfCore.MySql` |
| Audit logging | `Tenantry.Pro.EfCore` |
| Health checks | `Tenantry.Pro.HealthChecks` |
| Per-request licence enforcement, telemetry, Data Protection encryption | `Tenantry.Pro.AspNetCore` |

## 2. Define your tenant store and `DbContext`

The tenant **store** is Tenantry core's concept — it answers "which tenants exist?". A production app
typically backs it with a database; here is the shape:

```csharp
using Tenantry.Core;

public sealed class MyTenantStore : ITenantStore<string>
{
    private static readonly ITenantDescriptor<string>[] Tenants =
    [
        new TenantDescriptor<string> { TenantId = "acme",   Name = "Acme" },
        new TenantDescriptor<string> { TenantId = "globex", Name = "Globex" },
    ];

    public ValueTask<ITenantDescriptor<string>?> GetTenantAsync(string tenantId, CancellationToken ct = default)
        => ValueTask.FromResult(Tenants.FirstOrDefault(t => t.TenantId == tenantId));

    public ValueTask<IReadOnlyList<ITenantDescriptor<string>>> GetAllTenantsAsync(CancellationToken ct = default)
        => ValueTask.FromResult<IReadOnlyList<ITenantDescriptor<string>>>(Tenants);
}
```

> Migration orchestration, health checks, and provisioning enumerate tenants through
> `GetAllTenantsAsync`, so make sure your store returns **every** tenant you want operated on.

Your `DbContext` is a plain EF Core context — with database-per-tenant the connection string already
points at the right database, so no per-tenant code is needed inside it:

```csharp
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
}

public sealed class Order
{
    public int Id { get; set; }
    public string Description { get; set; } = "";
}
```

## 3. Register Tenantry and Tenantry.Pro

```csharp
using Microsoft.EntityFrameworkCore;
using Tenantry.AspNetCore.Extensions;
using Tenantry.Pro;
using Tenantry.Pro.EfCore.SqlServer.Extensions;
using Tenantry.Pro.Strategies.DatabasePerTenant;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTenantry<string>(tenant =>
{
    // From Tenantry core: how the tenant is identified, and where tenants are defined.
    tenant.ResolveFromHeader("X-Tenant-Id");
    tenant.UseStore<MyTenantStore>();

    // Tenantry.Pro features.
    tenant.UsePro(pro =>
    {
        pro.WithLicence(builder.Configuration["Tenantry:Licence"]!);

        pro.UseDatabasePerTenant(opts =>
            opts.GetConnectionString = t =>
                $"Server=.;Database=app_{t.TenantId};Integrated Security=true;TrustServerCertificate=True");

        pro.AddDatabaseProvisioning();   // create tenant databases on demand

        pro.WithMigrationOrchestration<string, AppDbContext>(
            cs => new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(cs).Options),
            runAtStartup: false);        // run migrations as a deployment step; see "Multiple instances"
    });
});

// EF Core gets the per-tenant connection string from the resolver, per request.
builder.Services.AddDbContext<AppDbContext>((sp, options) =>
    options.UseSqlServer(sp.GetRequiredService<ITenantConnectionStringResolver<string>>().Resolve()));
```

## 4. Wire the request pipeline

```csharp
var app = builder.Build();

app.UseTenantry();   // Tenantry core: resolves the tenant and opens the scope for the request

app.MapGet("/orders", (AppDbContext db) => db.Orders.ToListAsync());

app.MapPost("/tenants/{id}/provision",
    async (string id, DatabaseProvisioningService<string> provisioning, CancellationToken ct) =>
    {
        await provisioning.ProvisionAsync(id, ct);   // CREATE DATABASE for the new tenant
        return Results.Ok();
    });

app.Run();
```

A request with header `X-Tenant-Id: acme` now reads and writes `app_acme`; `X-Tenant-Id: globex`
hits `app_globex`. Neither can see the other's rows because they are different databases.

## 5. Provide a licence key

Pro needs your licence key, from your [Pro access page](https://tenantry.dev/dashboard/pro), configured
with `pro.WithLicence(...)`. The key does not expire, so you set it once. Without a valid key the
application does not start (`LicenseRequiredException`), so a missing or mistyped key shows up right away.
Store the key outside source control — for example in user secrets or an environment variable bound to
`Tenantry:Licence` — and give it to CI as a secret. See [Licensing](licensing.md) for the full model.

## Where to go next

- **Schema isolation instead of separate databases** → [Schema per tenant](schema-per-tenant.md)
- **Mix dedicated and shared tenants** → [Mixed mode](mixed-mode.md)
- **Create + migrate + seed new tenants in one call** → [Tenant lifecycle](tenant-lifecycle.md)
- **PostgreSQL or MySQL** → [Database providers](database-providers.md)
- **No ASP.NET Core (worker/console)** → [Background jobs & non-HTTP hosts](background-jobs.md)
