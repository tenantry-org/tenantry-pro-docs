# Database per tenant

Each tenant gets a dedicated database. Reads and writes are physically isolated — a tenant's queries
run against its own database, so there is no shared table and no row-level filter to leak across.
This is the strongest isolation Tenantry offers, and the natural fit when tenants have very
different data volumes, compliance boundaries, or backup/restore needs.

## What Tenantry provides

Tenantry Core (free) connects each tenant to its database:

- `UseConnectionStrings` — your delegate that returns a tenant's connection string, read through
  `ITenantConnectionStringProvider<TKey>`.
- `AddDbContextPerTenantDatabase<TContext>` (in `Tenantry.EfCore`) — registers your `DbContext`, pooled or not, and
  connects each one to the current tenant's database.

Tenantry.Pro adds what running many databases takes:

- **Caching** of connection strings, for a delegate that reads a secrets store (`pro.CacheConnectionStrings()`).
- **Provisioning**: creating a new tenant's database, migrating it and seeding it in one call (see
  [Tenant lifecycle](tenant-lifecycle.md)).
- **Migrations** for every tenant database (see [Tenant migrations](migration-orchestration.md)).
- **Health checks** for every tenant database (see [Health checks](health-checks.md)).

## Registration

```csharp
using Microsoft.EntityFrameworkCore;

builder.Services.AddTenantry<string>(tenant =>
{
    tenant.ResolveFromHeader("X-Tenant-Id");
    tenant.UseStore<MyTenantStore>();

    tenant.UseConnectionStrings(opts =>
        opts.GetConnectionString = t =>
            $"Server=.;Database=app_{t.TenantId};Integrated Security=true;TrustServerCertificate=True");

    tenant.UsePro(pro => pro.AddDatabaseProvisioning<AppDbContext>());   // optional — see "Provisioning" below

    // Connects each AppDbContext to the current tenant's database. Configure the provider without a
    // connection string.
    tenant.AddDbContextPerTenantDatabase<AppDbContext>((sp, options) => options.UseSqlServer());
});
```

The `GetConnectionString` delegate receives the resolved `ITenantDescriptor<TKey>` — use
`t.TenantId`, `t.Name`, or your own tenant type's properties with `t.As<AppTenant>()`
([your own tenant type](https://github.com/tenantry-org/tenantry-core/blob/master/docs/core-concepts.md#your-own-tenant-type)). It is called when each
`DbContext` is created (i.e. per request), so it must be **fast and deterministic**: compute the
string from tenant properties; do not call external services inside it. For external lookups, use the
async delegate and caching (below).

## Resolving connection strings

Contexts registered with `AddDbContextPerTenantDatabase` get the current tenant's connection string for you. For
code that opens its own connections, inject Tenantry Core's `CurrentTenantConnectionString<TKey>`. It throws
`TenantNotResolvedException` when there is none, so ensure
`app.UseTenantry()` runs before any code that reads a connection string. `ITenantConnectionStringProvider<TKey>`
takes the tenant explicitly and works without a current one. Both are singletons. Tenantry.Pro's provisioning,
migrations and health checks go through the tenant's context instead, created in its scope, so they connect as
`AddDbContextPerTenantDatabase` does.

```csharp
string cs       = current.Get();                          // current tenant, sync
string csAsync  = await current.GetAsync(ct);             // current tenant, async
string forAcme  = await provider.GetAsync(acme, ct);      // a given tenant
```

Do not read a connection string inside an `AddDbContextPool` or `AddPooledDbContextFactory` options callback
yourself: it runs once, so every pooled context would keep the first tenant's database (see
[DbContext pooling](#dbcontext-pooling)).

### Async / external connection strings

When the connection string lives in a secrets vault or a config database, set
`GetConnectionStringAsync` instead of (or in addition to) `GetConnectionString`, and cache it:

```csharp
builder.Services.AddTenantry<string>(tenant => tenant
    .UseStore<MyTenantStore>()
    .UseConnectionStrings(opts => opts.GetConnectionStringAsync = async (t, ct) =>
        await secretsClient.GetSecretAsync($"connstr-{t.TenantId}", ct))
    .UsePro(pro => pro.CacheConnectionStrings(o => o.Duration = TimeSpan.FromMinutes(30))));
```

If both delegates are set, `GetAsync` prefers the async one. If **only** the async delegate is
configured, the synchronous `Get()` throws — call `GetAsync` in that case.

## Caching

`pro.CacheConnectionStrings()` keeps each tenant's connection string in memory for `Duration` (30 minutes by
default), so the delegates run once per tenant per period instead of for every context. Use it whenever reading
a connection string is expensive (an external lookup).

- It wraps the tenants' `ITenantConnectionStringProvider<TKey>`: the one `UseConnectionStrings` registers, called
  before or after `UsePro`, or one of your own registered before `UsePro`. Everything that reads connection strings
  is then cached: `AddDbContextPerTenantDatabase`, `CurrentTenantConnectionString`, provisioning, migrations and
  health checks. The application fails to start if there are no connection strings to cache. A provider registered
  after `UsePro` (a test's replacement, say) takes the cache's place, unless it wraps it, as a decorator does; a
  warning at startup says so.
- When a tenant's connection details change, inject `IConnectionStringCache<TKey>` and call
  `Invalidate(tenantId)`, or `InvalidateAll()` after rotating every tenant's credentials. A connection string
  read while you invalidate is not cached.
- `Duration` must be positive, or the application does not start. `TimeSpan.MaxValue` caches until you invalidate.
- Caching does not change which reads work: with only `GetConnectionStringAsync`, `Get()` throws even when the
  cache holds the tenant's connection string.

Cached connection strings live in the application's memory, like any other configuration it has read.

## DbContext pooling

EF Core keeps a pooled context's connection string when the context returns to the pool, so the next
lease would silently use the previous tenant's database. Tenantry Core's `AddDbContextPerTenantDatabase` (in
`Tenantry.EfCore`) pools contexts safely with a database per tenant when you pass `pooled: true`: configure the
provider **without** a connection string, and each lease is connected to the current tenant's database. It uses
the same `ITenantConnectionStringProvider`, so Pro's caching applies.

```csharp
builder.Services.AddTenantry<string>(tenant => tenant
    .UseStore<MyTenantStore>()
    .UseConnectionStrings(opts => opts.GetConnectionString = t => $"Server=.;Database=app_{t.TenantId}")
    .UsePro(pro => pro.CacheConnectionStrings())
    .AddDbContextPerTenantDatabase<AppDbContext>((sp, options) => options.UseSqlServer(), pooled: true));
```

- It registers a scoped `AppDbContext` and `IDbContextFactory<AppDbContext>`, and applies `UseTenantry()`.
  Use it instead of `AddDbContext`, `AddDbContextPool` or `AddPooledDbContextFactory` for that context.
- Creating a context without a current tenant throws `TenantNotResolvedException`.
- Before a context opens a connection, and again before every command it runs, a guard checks that the
  connection was set for this context (and lease) and belongs to the tenant that is current now. A context kept
  and used after switching to another tenant, or whose connection or connection string your code replaced,
  throws `TenantIsolationViolationException` instead of touching the wrong database. That includes a context
  whose connection is still open, whether you opened it or a transaction did. The EF Core integration guide in
  Tenantry.Core lists what the guard cannot see.
- A pooled context needs a constructor that takes only its options.
- The scoped context resolves the connection string synchronously, so it needs `GetConnectionString`.
  With only `GetConnectionStringAsync`, create contexts with
  `IDbContextFactory<AppDbContext>.CreateDbContextAsync()`. Caching applies either way.

Core tests it with one pooled instance serving two tenant databases in turn, and with concurrent leases,
on SQLite, SQL Server, PostgreSQL and MySQL.

## Provisioning a new tenant

`AddDatabaseProvisioning<TContext>()` adds creating the tenant's database to tenant provisioning, as its first
step. It does **not** run automatically — Tenantry.Pro never creates databases on first request. Add the tenant
to your store, then provision it:

```csharp
using Tenantry;
using Tenantry.Pro;

public sealed class TenantAdminService(ITenantProvisioner<string> provisioner)
{
    public async Task<bool> CreateAsync(ITenantDescriptor<string> tenant, CancellationToken ct) =>
        (await provisioner.ProvisionAsync(tenant, ct)).Succeeded;   // create the database, migrate, seed
}
```

The `CreateDatabase` step resolves `TContext` in the tenant's scope, so with `AddDbContextPerTenantDatabase`
the context is connected to the tenant's database, and creates that database through the EF Core provider's
own database creator, unless it exists. So it works with whichever provider the context uses. Migrations and
seeders run after it, in the same call; see [Tenant lifecycle](tenant-lifecycle.md) and
[Database providers](database-providers.md).

Calls for the same tenant at the same time (a double submit, a redelivered message) all succeed: a call whose
`CREATE DATABASE` fails because another call created the database first waits for that database to come
online (up to 30 seconds) and succeeds. A database that never appears, because creating it failed for
another reason, fails the step with the error the database reported. A call that fails once the database
exists (after `CREATE DATABASE`, say) also succeeds, because it cannot tell that from another call creating
it, so it logs the failure as a warning: if no other call ran, check the database.

The context keeps the credentials of the tenant's connection string, so the login your application connects
with needs permission to create databases: `CREATE ANY DATABASE` (or the `dbcreator` role) on SQL Server,
`CREATEDB` on PostgreSQL, `CREATE` on MySQL. If the application should not hold that permission, give
provisioning a context with a privileged login, from the tenant's scope:

```csharp
pro.AddDatabaseProvisioning<AppDbContext>(o => o.CreateContext = sp =>
{
    var tenant = sp.GetRequiredService<ITenantContext<string>>().CurrentTenant!;
    return new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
        .UseSqlServer($"Server=.;Database=app_{tenant.TenantId};User Id=provisioner;Password={adminPassword};TrustServerCertificate=True")
        .Options);
});
```

or provision from a separate process, such as an admin tool or a job, whose connection strings use a
privileged login. Either way, give the application's login access to each database created, since Tenantry
grants nothing: on SQL Server a user for the login in the new database with the roles it needs
(`db_datareader`, `db_datawriter`); on PostgreSQL privileges on the tables and sequences the migrations
create (`GRANT … ON ALL TABLES IN SCHEMA` and `… ON ALL SEQUENCES IN SCHEMA`, and `ALTER DEFAULT PRIVILEGES` for
the tables later migrations add); on MySQL a `GRANT` on the new database. A provisioning step of your own
(`pro.AddProvisioningStep<T>()`) can do this: your steps run after the database is created and migrated.

## Migrating tenant databases

Use `pro.AddMigrations<TContext>()` to apply EF Core migrations to every tenant database, through the context
`AddDbContextPerTenantDatabase` connects to each tenant's database: as a deployment step
(`app.RunTenantMigrationsIfRequestedAsync(args)`), from an admin endpoint, at startup if you opt in, and to each new
tenant's database when it is provisioned. See [Tenant migrations](migration-orchestration.md).

## Limitations

- The `GetConnectionString` delegate must be deterministic and fast; use `GetConnectionStringAsync` +
  caching for anything that hits the network.
- Provisioning and migration are explicit (or opt-in at startup) — never implicit on first request.
- Provisioning needs a relational EF Core provider; SQL Server, PostgreSQL and MySQL/MariaDB are tested
  ([Database providers](database-providers.md)).

## See also

- [Tenant migrations](migration-orchestration.md) · [Tenant lifecycle](tenant-lifecycle.md)
- [Health checks](health-checks.md) — probe every tenant database for connectivity and pending migrations.
- [Mixed mode](mixed-mode.md) — some tenants on their own database, others sharing.
- [Database providers](database-providers.md)
