# Database per tenant

Each tenant gets a dedicated database. Reads and writes are physically isolated — a tenant's queries
run against its own database, so there is no shared table and no row-level filter to leak across.
This is the strongest isolation Tenantry.Pro offers, and the natural fit when tenants have very
different data volumes, compliance boundaries, or backup/restore needs.

## What Tenantry.Pro provides

- Per-tenant connection strings through Tenantry Core's `ITenantConnectionStringResolver<TKey>`, which
  `UseDatabasePerTenant` configures. Core resolves them; Pro adds the rest of this list.
- Optional in-memory **caching** of resolved connection strings, with optional **at-rest encryption**
  (see [Connection-string encryption](connection-string-encryption.md)).
- `DatabaseProvisioningService<TKey>` — creates a tenant's database on demand (per provider package).
- Migration orchestration across every tenant database (see [Migration orchestration](migration-orchestration.md)).
- Licence and (where applicable) tenant lookups around provisioning.

## What you provide

- Tenant resolution and a tenant store via `AddTenantry<TKey>(...)` (Tenantry core).
- The **connection-string convention** for each tenant (the `GetConnectionString` delegate).
- Your own `DbContext` registration.
- An explicit tenant-creation flow that calls provisioning when you want databases created.

## Registration

```csharp
using Microsoft.EntityFrameworkCore;
using Tenantry.Pro;
using Tenantry.Pro.EfCore.SqlServer.Extensions;
using Tenantry.Pro.Strategies.DatabasePerTenant;

builder.Services.AddTenantry<string>(tenant =>
{
    tenant.ResolveFromHeader("X-Tenant-Id");
    tenant.UseStore<MyTenantStore>();

    tenant.UsePro(pro =>
    {
        pro.WithLicence(builder.Configuration["Tenantry:Licence"]!);

        pro.UseDatabasePerTenant(opts =>
            opts.GetConnectionString = t =>
                $"Server=.;Database=app_{t.TenantId};Integrated Security=true;TrustServerCertificate=True");

        pro.AddDatabaseProvisioning();   // optional — see "Provisioning" below
    });
});

// Resolve the per-tenant connection string when each AppDbContext is created.
builder.Services.AddDbContext<AppDbContext>((sp, options) =>
    options.UseSqlServer(sp.GetRequiredService<ITenantConnectionStringResolver<string>>().Resolve()));
```

The `GetConnectionString` delegate receives the resolved `ITenantDescriptor<TKey>` — use
`t.TenantId`, `t.Name`, or any custom property you carry on your descriptor. It is called when each
`DbContext` is created (i.e. per request), so it must be **fast and deterministic**: compute the
string from tenant properties; do not call external services inside it. For external lookups, use the
async resolver and caching (below).

## Resolving connection strings

Inject `ITenantConnectionStringResolver<TKey>` (from `Tenantry.Core`) wherever you build a context or
open a connection. It is a singleton. The parameterless overloads use the current tenant and throw
`TenantNotResolvedException` when there is none, so ensure `app.UseTenantry()` runs before any code that
resolves a connection string. The overloads that take a tenant work without one; the migration
orchestrator, provisioning services and health checks use them.

```csharp
string cs       = resolver.Resolve();                    // current tenant, sync
string csAsync  = await resolver.ResolveAsync(ct);        // current tenant, async
string forAcme  = await resolver.ResolveAsync(acme, ct);  // a given tenant
```

Do not resolve inside an `AddDbContextPool` or `AddPooledDbContextFactory` options callback: it runs
once, so every pooled context would keep the first tenant's database. For pooling, use
`AddTenantDbContextPool` (see [DbContext pooling](#dbcontext-pooling)).

### Async / external connection strings

When the connection string lives in a secrets vault or a config database, set
`GetConnectionStringAsync` instead of (or in addition to) `GetConnectionString`:

```csharp
pro.UseDatabasePerTenant(opts =>
{
    opts.GetConnectionStringAsync = async (t, ct) =>
        await secretsClient.GetSecretAsync($"connstr-{t.TenantId}", ct);

    opts.CacheConnectionStrings = true;                 // strongly recommended for external lookups
    opts.CacheDuration = TimeSpan.FromMinutes(30);
});
```

If both delegates are set, `ResolveAsync` prefers the async one. If **only** the async delegate is
configured, the synchronous `Resolve()` throws — call `ResolveAsync` in that case.

## Caching

Set `CacheConnectionStrings = true` to cache resolved strings in memory for `CacheDuration` (default
30 minutes). This is the right choice whenever resolution is expensive (an external lookup). When
caching is enabled you can also encrypt the cached values **at rest** — see
[Connection-string encryption](connection-string-encryption.md). Encryption only applies to cached
values, so it has no effect unless caching is on.

| Option | Default | Meaning |
|--------|---------|---------|
| `GetConnectionString` | — | Synchronous, fast, deterministic delegate (`ITenantDescriptor<TKey>` → string). One of this or the async form is required. |
| `GetConnectionStringAsync` | — | Async delegate for external lookups. Preferred by `ResolveAsync`. |
| `CacheConnectionStrings` | `false` | Cache resolved strings in memory. |
| `CacheDuration` | 30 min | How long a cached entry is valid. |
| `Encryption` | `None` | At-rest encryption mode for cached values. |

## DbContext pooling

EF Core keeps a pooled context's connection string when the context returns to the pool, so the next
lease would silently use the previous tenant's database. Tenantry Core's `AddTenantDbContextPool` (in
`Tenantry.EfCore`) pools contexts safely with a database per tenant: configure the provider
**without** a connection string, and each lease is connected to the current tenant's database. It uses
the same `ITenantConnectionStringResolver`, so Pro's caching and encryption apply.

```csharp
using Tenantry.EfCore.Extensions;

builder.Services.AddTenantDbContextPool<AppDbContext, string>((sp, options) =>
    options.UseSqlServer().AddTenantInterceptors(sp));
```

- It registers a scoped `AppDbContext` and `IDbContextFactory<AppDbContext>`, both leasing from one pool.
  Use it instead of `AddDbContext`, `AddDbContextPool` or `AddPooledDbContextFactory` for that context.
- Leasing without a current tenant throws `TenantNotResolvedException`.
- Before a pooled context opens a connection, and again before every command it runs, a guard checks that
  the connection was set for this lease and belongs to the tenant that is current now. A context leased some
  other way, kept and used after switching to another tenant, or whose connection or connection string your
  code replaced, throws `TenantIsolationViolationException` instead of touching the wrong database. That
  includes a context whose connection is still open, whether you opened it or a transaction did. The EF Core
  integration guide in Tenantry.Core lists what the guard cannot see.
- The context needs a constructor that takes only its options; `MultiTenantDbContext<TKey>` provides
  one.
- The scoped context resolves the connection string synchronously, so it needs `GetConnectionString`.
  With only `GetConnectionStringAsync`, create contexts with
  `IDbContextFactory<AppDbContext>.CreateDbContextAsync()`. Caching applies either way.

Core tests it with one pooled instance serving two tenant databases in turn, and with concurrent leases,
on SQLite, SQL Server, PostgreSQL and MySQL.

## Provisioning a new tenant

`AddDatabaseProvisioning()` (from the provider package, e.g. `Tenantry.Pro.EfCore.SqlServer`)
registers `DatabaseProvisioningService<TKey>`. It does **not** run automatically — Tenantry.Pro never
creates databases on first request. Call it from your tenant-creation flow:

```csharp
public sealed class TenantAdminService(DatabaseProvisioningService<string> provisioning)
{
    public Task CreateDatabaseAsync(string tenantId, CancellationToken ct) =>
        provisioning.ProvisionAsync(tenantId, ct);   // idempotent CREATE DATABASE
}
```

`ProvisionAsync`:

1. checks the licence (logs an error if it is missing or out of grace; throws `LicenseRequiredException`
   instead under `LicenseEnforcement.Throw`);
2. looks the tenant up in your store, resolves its connection string, and extracts the database name;
3. issues a provider-specific, **idempotent** `CREATE DATABASE` against a server/admin connection
   derived from the tenant connection string.

It creates the database only — running migrations and seeding is separate. To do all three in one
call, see [Tenant lifecycle](tenant-lifecycle.md). Provisioning is available for SQL Server,
PostgreSQL, and MySQL/MariaDB; see [Database providers](database-providers.md).

## Migrating tenant databases

Use `pro.WithMigrationOrchestration<TKey, TContext>(...)` to apply EF Core migrations across every
tenant database — on demand, from an admin endpoint, or automatically at startup. See
[Migration orchestration](migration-orchestration.md).

## Limitations

- The `GetConnectionString` delegate must be deterministic and fast; use `GetConnectionStringAsync` +
  caching for anything that hits the network.
- Provisioning and migration are explicit (or opt-in at startup) — never implicit on first request.
- This strategy targets relational databases supported by an EF Core provider with a Tenantry.Pro
  provisioning package (SQL Server, PostgreSQL, MySQL/MariaDB).

## See also

- [Connection-string encryption](connection-string-encryption.md)
- [Migration orchestration](migration-orchestration.md) · [Tenant lifecycle](tenant-lifecycle.md)
- [Health checks](health-checks.md) — probe every tenant database for connectivity and pending migrations.
- [Database providers](database-providers.md)
