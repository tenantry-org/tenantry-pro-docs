# Getting started

This guide builds a working **database-per-tenant** ASP.NET Core app on SQL Server end to end:
resolve the tenant, give each tenant its own database, provision new databases on demand, and set up
migrations across every tenant database, run as a deployment step. Schema-per-tenant and the other providers follow the same shape — see the
[schema-per-tenant](schema-per-tenant.md) and [database providers](database-providers.md) guides.

## 1. Install the packages

Tenantry.Pro sits on top of Tenantry core, so you install both. The Pro packages come from a private
feed: set it up first with [Installation](installation.md), which also covers CI and the licence key. For
an ASP.NET Core app using SQL Server:

```bash
dotnet add package Tenantry.AspNetCore            # Tenantry core: resolution + middleware
dotnet add package Tenantry.Pro                    # Pro: strategies + licensing
dotnet add package Tenantry.Pro.EfCore             # provisioning, migrations, health checks
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
```

| If you need… | Add |
|--------------|-----|
| Schema per tenant | `Tenantry.Pro.EfCore` |
| PostgreSQL / MySQL | the EF Core provider (`Npgsql.EntityFrameworkCore.PostgreSQL`, or [a MySQL provider](database-providers.md#mysql--mariadb)) in place of SQL Server's |
| Audit logging | `Tenantry.Pro.EfCore` |
| Health checks | `Tenantry.Pro.EfCore` |
| Per-tenant request metrics | `Tenantry.Pro.AspNetCore` |

## 2. Define your tenant store and `DbContext`

The tenant **store** is Tenantry core's concept — it answers "which tenants exist?". A production app
typically backs it with a database; here is the shape:

```csharp
using Tenantry;

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

> `MigrateTenantAsync` looks the tenant up with `GetTenantAsync`; migration runs, health checks and
> `TenantBackgroundService` enumerate `GetAllTenantsAsync`. So your store must return **every**
> tenant that exists, suspended ones included, from both. Refuse suspended tenants in HTTP requests with an
> access validator instead (see [Tenant lifecycle](tenant-lifecycle.md#when-provisioning-fails)), and check
> the status yourself in background work (see [Background jobs](background-jobs.md#suspended-tenants)).

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
using Tenantry;
using Tenantry.Pro;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTenantry<string>(tenant =>
{
    // From Tenantry core: how the tenant is identified, where tenants are defined, and each tenant's database.
    tenant.ResolveFromHeader("X-Tenant-Id");
    tenant.UseStore<MyTenantStore>();
    tenant.UseConnectionStrings(opts =>
        opts.GetConnectionString = t =>
            $"Server=.;Database=app_{t.TenantId};Integrated Security=true;TrustServerCertificate=True");

    // Tenantry.Pro features.
    tenant.UsePro(pro => pro
        .AddDatabaseProvisioning<AppDbContext>()   // provisioning a new tenant creates its database...
        .AddMigrations<AppDbContext>());           // ...then migrates it, through AppDbContext

    // From Tenantry core: each AppDbContext connects to the current tenant's database.
    tenant.AddDbContextPerTenantDatabase<AppDbContext>((sp, options) => options.UseSqlServer());
});
```

## 4. Wire the request pipeline

```csharp
await using var app = builder.Build();

// dotnet run -- migrate-tenants: migrate every tenant's database as a deployment step, then exit.
if (await app.RunTenantMigrationsIfRequestedAsync(args) is { } exitCode)
    return exitCode;

app.UseTenantry();   // Tenantry core: resolves the tenant and opens the scope for the request

app.MapGet("/orders", (AppDbContext db) => db.Orders.ToListAsync());

app.MapPost("/tenants/{id}/provision",
    async (string id, ITenantStore<string> store, ITenantProvisioner<string> provisioner, CancellationToken ct) =>
    {
        // Add the tenant to your store first; provisioning creates its database and migrates it.
        if (await store.GetTenantAsync(id, ct) is not { } tenant)
            return Results.NotFound();

        var result = await provisioner.ProvisionAsync(tenant, ct);
        return result.Succeeded ? Results.Ok() : Results.StatusCode(StatusCodes.Status500InternalServerError);
    });

app.Run();
return 0;
```

A request with header `X-Tenant-Id: acme` now reads and writes `app_acme`; `X-Tenant-Id: globex`
hits `app_globex`. Neither can see the other's rows because they are different databases. Run the application with
`migrate-tenants` once per deployment, before the new version serves requests, to migrate every tenant's
database (see [Tenant migrations](migration-orchestration.md)).

## 5. Provide a licence key

Pro needs your licence key, from your [Pro access page](https://tenantry.dev/dashboard/pro). `UsePro` reads it
from the `Tenantry:License` setting, so it needs no code. The key does not expire, so you set it once. Without a
valid key the application does not start (`LicenseRequiredException`), so a missing or mistyped key shows up
right away. Store the key outside source control — for example in user secrets, or the `Tenantry__License`
environment variable — and give it to CI as a secret. See [Licensing](licensing.md) for the full model.

## Where to go next

- **Schema isolation instead of separate databases** → [Schema per tenant](schema-per-tenant.md)
- **Mix dedicated and shared tenants** → [Mixed mode](mixed-mode.md)
- **Create + migrate + seed new tenants in one call** → [Tenant lifecycle](tenant-lifecycle.md)
- **PostgreSQL or MySQL** → [Database providers](database-providers.md)
- **No ASP.NET Core (worker/console)** → [Background jobs & non-HTTP hosts](background-jobs.md)
