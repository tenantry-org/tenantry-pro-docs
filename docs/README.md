# Tenantry.Pro documentation

[Tenantry](https://github.com/tenantry-org/tenantry-core) isolates tenants in a shared database or gives
each tenant its own database. Tenantry.Pro extends it with a **schema per tenant** and **mixed mode**, and
the operational tooling that many tenant databases require: provisioning, migrations,
connection-string caching, audit logging, health checks, telemetry, and tenant-context propagation into
background jobs and message buses.

Tenantry.Pro always sits **inside** a Tenantry core registration: you call `AddTenantry` (or
`AddTenantry` for non-HTTP hosts), keep your resolution and tenant store exactly as core defines
them, and add `tenant.UsePro(pro => { ... })`. Pro changes how each tenant's data is isolated and
operated — not how tenants are identified or stored.

If you are new, start with **[Installation](installation.md)**, then **[Getting started](getting-started.md)**.

## Guides

### Foundations
1. **[Installation](installation.md)** — the private package feed, its credentials, CI, and the licence key.
2. **[Getting started](getting-started.md)** — install the packages and build a database-per-tenant app end to end.
3. **[Licensing](licensing.md)** — the offline ES256 licence key, which does not expire, and the startup check.

### Isolation strategies
4. **[Database per tenant](database-per-tenant.md)** — connection strings, caching, pooling, and database provisioning.
5. **[Schema per tenant](schema-per-tenant.md)** — schema-name resolution, EF Core per-schema model caching, and schema provisioning.
6. **[Mixed mode](mixed-mode.md)** — tenants on their own database, their own schema, or the shared store, in one application.
7. **[Database providers](database-providers.md)** — SQL Server, PostgreSQL, and MySQL/MariaDB capabilities and differences.

### Operations
8. **[Tenant migrations](migration-orchestration.md)** — applying EF Core migrations to every tenant database and schema, once per database or schema, with each one's failure kept to itself, and reading their status.
9. **[Tenant lifecycle](tenant-lifecycle.md)** — provisioning a new tenant: create its database or schema, migrate, seed, and your own steps.
10. **[Audit logging](audit-logging.md)** — recording entity changes per tenant via an EF Core interceptor.
11. **[Health checks](health-checks.md)** — verifying tenant database connectivity and pending migrations.
12. **[Telemetry](telemetry.md)** — the tenant on ASP.NET Core's request metrics.

### Background work & messaging
13. **[Background jobs & non-HTTP hosts](background-jobs.md)** — `ITenantScopeFactory` for workers, console apps, and hosted services.
14. **[Hangfire](hangfire.md)**, **[MassTransit](masstransit.md)**, **[Quartz.NET](quartz.md)**, **[Rebus](rebus.md)** — tenant-context propagation across each library.

### Reference
15. **[Testing](testing.md)** — the licence key in tests and CI, and testing provisioning, migrations, jobs and messages.
16. **[Compatibility](compatibility.md)** — supported .NET, EF Core, database and library versions, and dependency ranges.
17. **[Troubleshooting](troubleshooting.md)** — common pitfalls, AOT/trimming, and how to diagnose them.
18. **[API reference](api/README.md)** — every public type and member, generated from the XML documentation comments.

## How the pieces fit together

Tenantry core answers *who is the tenant?* and *which tenants exist?*. Tenantry.Pro adds three more
responsibilities, configured inside the `tenant.UsePro(...)` lambda (a database per tenant's connection strings
are Tenantry core's, set beside it):

| Responsibility | Question it answers | Configured with |
|----------------|---------------------|-----------------|
| **Licence** | *Is this deployment licensed for Pro features?* | the `Tenantry:License` setting, or `pro.UseLicenseKey(key)` |
| **Isolation strategy** | *Where does this tenant's data physically live?* | `pro.UseSchemaPerTenant(...)`, `pro.UseMixedMode(...)` (a database per tenant is Tenantry core's `tenant.UseConnectionStrings(...)`) |
| **Operations & integrations** | *How are tenant databases provisioned, migrated, observed, and propagated into background work?* | `pro.AddDatabaseProvisioning<TContext>()`, `pro.AddMigrations<TContext>()`, `pro.AddSeeder<T>()`, `pro.CacheConnectionStrings()`, `pro.AddAuditLogging()`, `pro.AddTenantMetrics()`, `pro.AddHangfirePropagation()`, … |

A database-per-tenant request flows like this:

```
HTTP request
   │
   ▼
UseTenantry()  ──►  Tenantry core resolves the tenant and opens the tenant scope
   │
   ▼
AddDbContextPerTenantDatabase  ──►  ITenantConnectionStringProvider<TKey>  ──►  per-tenant connection string
   │                                        (optionally cached by Pro)
   ▼
Your endpoint + EF Core  ──►  reads and writes hit the tenant's own database
```

For schema-per-tenant the connection string is shared and the tenant's **schema** becomes the model's
default schema for every context that uses `UseTenantry()`, with EF Core caching one compiled model per
schema. In a console or worker app
there is no request, so you open the tenant scope yourself with `ITenantScopeFactory<TKey>` —
everything below the scope line behaves identically.

## What Tenantry.Pro does not do

- It does not register your `DbContext` for you — you register it with Tenantry core's
  `AddDbContextPerTenantDatabase` (a database per tenant) or EF Core's `AddDbContext`, and keep full control of
  its options.
- It does not invent your connection strings or schema names — you supply the delegates.
- It does not provision or migrate tenants automatically on first request — provisioning and
  migration are explicit: run them as a deployment step, or at startup when you opt in with
  `o.OnStartup`.
- It does not replace Tenantry core's resolution, storage, or shared-database isolation — it composes with them.
- It does not provide an admin dashboard or UI.
