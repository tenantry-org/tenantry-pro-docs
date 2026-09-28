# Tenantry.Pro documentation

Tenantry.Pro extends [Tenantry](https://github.com/tenantry-org/tenantry-core) with **physical**
tenant isolation — a database per tenant, a schema per tenant, or a mix — and the operational tooling
that physical isolation requires: provisioning, migration orchestration, a tenant-creation lifecycle,
audit logging, health checks, telemetry, and tenant-context propagation into background jobs and
message buses.

Tenantry.Pro always sits **inside** a Tenantry core registration: you call `AddTenantry` (or
`AddTenantryCore` for non-HTTP hosts), keep your resolution and tenant store exactly as core defines
them, and add `tenant.UsePro(pro => { ... })`. Pro changes how each tenant's data is isolated and
operated — not how tenants are identified or stored.

If you are new, start with **[Getting started](getting-started.md)**.

## Guides

### Foundations
1. **[Getting started](getting-started.md)** — install the packages and build a database-per-tenant app end to end.
2. **[Licensing](licensing.md)** — the offline ES256 licence, the 30-day grace period, per-request enforcement, and the startup check.

### Isolation strategies
3. **[Database per tenant](database-per-tenant.md)** — connection-string resolution, async resolvers, caching, and on-demand database provisioning.
4. **[Schema per tenant](schema-per-tenant.md)** — schema-name resolution, EF Core per-schema model caching, and schema provisioning.
5. **[Mixed mode](mixed-mode.md)** — routing individual tenants to a database, a schema, or the shared store.
6. **[Connection-string encryption](connection-string-encryption.md)** — encrypting cached connection strings at rest (AES, custom, or ASP.NET Core Data Protection).
7. **[Database providers](database-providers.md)** — SQL Server, PostgreSQL, and MySQL/MariaDB capabilities and differences.

### Operations
8. **[Migration orchestration](migration-orchestration.md)** — applying EF Core migrations across every tenant database, with per-tenant failure isolation and status tracking.
9. **[Tenant lifecycle](tenant-lifecycle.md)** — the provision → migrate → seed pipeline and `ITenantSeeder`.
10. **[Audit logging](audit-logging.md)** — recording entity changes per tenant via an EF Core interceptor.
11. **[Health checks](health-checks.md)** — verifying tenant database connectivity and pending migrations.
12. **[Telemetry](telemetry.md)** — per-tenant request metrics and the `Tenantry.Pro` meter.

### Background work & messaging
13. **[Background jobs & non-HTTP hosts](background-jobs.md)** — `ITenantScopeFactory` for workers, console apps, and hosted services.
14. **[Hangfire](hangfire.md)**, **[MassTransit](masstransit.md)**, **[Quartz.NET](quartz.md)**, **[Rebus](rebus.md)** — tenant-context propagation across each library.

### Reference
15. **[Troubleshooting](troubleshooting.md)** — common pitfalls, AOT/trimming, and how to diagnose them.

## How the pieces fit together

Tenantry core answers *who is the tenant?* and *which tenants exist?*. Tenantry.Pro adds three more
responsibilities, all configured inside the `tenant.UsePro(...)` lambda:

| Responsibility | Question it answers | Configured with |
|----------------|---------------------|-----------------|
| **Licence** | *Is this deployment licensed for Pro features?* | `pro.WithLicence(key)` |
| **Isolation strategy** | *Where does this tenant's data physically live?* | `pro.UseDatabasePerTenant(...)`, `pro.UseSchemaPerTenant(...)`, `pro.UseMixedMode(...)` |
| **Operations & integrations** | *How are tenant databases provisioned, migrated, observed, and propagated into background work?* | `pro.AddDatabaseProvisioning()`, `pro.WithMigrationOrchestration(...)`, `pro.AddLifecycleManagement()`, `pro.AddAuditLogging()`, `pro.AddTenantMetrics()`, `pro.AddHangfireTenantFilter()`, … |

A database-per-tenant request flows like this:

```
HTTP request
   │
   ▼
UseTenantry()  ──►  Tenantry core resolves the tenant and opens the tenant scope
   │
   ▼
AddDbContext factory  ──►  ITenantConnectionStringResolver<TKey>.Resolve()  ──►  per-tenant connection string
   │                              (optionally cached, optionally encrypted at rest)
   ▼
Your endpoint + EF Core  ──►  reads and writes hit the tenant's own database
```

For schema-per-tenant the connection string is shared and the tenant's **schema** is applied in
`OnModelCreating`, with EF Core caching one compiled model per schema. In a console or worker app
there is no request, so you open the tenant scope yourself with `ITenantScopeFactory<TKey>` —
everything below the scope line behaves identically.

## What Tenantry.Pro does not do

- It does not register your `DbContext` for you — you keep full control of `AddDbContext`.
- It does not invent your connection strings or schema names — you supply the delegates.
- It does not provision or migrate tenants automatically on first request — provisioning and
  migration are explicit: run them as a deployment step, or at startup when you opt in with
  `runAtStartup: true`.
- It does not replace Tenantry core's resolution, storage, or row-level isolation — it composes with them.
- It does not provide an admin dashboard or UI.
