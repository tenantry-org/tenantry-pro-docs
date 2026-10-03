# Tenantry.Pro documentation

Tenantry.Pro adds a schema per tenant, mixed mode and the tooling many tenant databases need to
[Tenantry](https://github.com/tenantry-org/tenantry-core). It runs inside a Tenantry registration: keep
`AddTenantry`, your resolvers and your store, and add `tenant.UsePro(...)`.

New to it? Start with [Installation](installation.md), then [Getting started](getting-started.md).

## Guides

### Foundations
1. [Installation](installation.md): the private package feed, its credentials, CI and the licence key.
2. [Getting started](getting-started.md): build a database-per-tenant app end to end.
3. [Licensing](licensing.md): the licence key, checked offline at startup, which does not expire.

### Isolation strategies
4. [Database per tenant](database-per-tenant.md): connection strings, caching, pooling and database provisioning.
5. [Schema per tenant](schema-per-tenant.md): schema names, EF Core's model per schema, and schema provisioning.
6. [Mixed mode](mixed-mode.md): each tenant on its own database, its own schema or the shared database.
7. [Database providers](database-providers.md): SQL Server, PostgreSQL and MySQL, and what was tested.

### Operations
8. [Tenant migrations](migration-orchestration.md): migrating every tenant database and schema, and their status.
9. [Tenant lifecycle](tenant-lifecycle.md): provisioning a tenant (create, migrate, seed, your own steps) and
   offboarding one.
10. [Audit logging](audit-logging.md): recording each tenant's entity changes.
11. [Health checks](health-checks.md): tenant database connectivity and pending migrations.
12. [Telemetry](telemetry.md): the tenant on ASP.NET Core's request metrics, and Pro's log events.

### Background work & messaging
13. [Background jobs & non-HTTP hosts](background-jobs.md): work for each tenant in workers and console apps.
14. [Hangfire](hangfire.md), [MassTransit](masstransit.md), [Quartz.NET](quartz.md), [Rebus](rebus.md): the tenant
    in each library's jobs and messages.

### Reference
15. [Testing](testing.md): the licence key in tests and CI, and testing provisioning, migrations, jobs and messages.
16. [Compatibility](compatibility.md): supported .NET, EF Core, database and library versions.
17. [Troubleshooting](troubleshooting.md): common problems, and trimming and AOT.
18. [API reference](api/README.md): every public type and member.

## A request

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

With a schema per tenant, the connection string is shared and the tenant's schema becomes the model's default
schema. In a console or worker app there is no request, so you open the tenant's scope with
`ITenantScopeFactory<TKey>`; the rest is the same.

## What Tenantry.Pro does not do

- It does not register your `DbContext`: you do, with Tenantry core's `AddDbContextPerTenantDatabase` or EF Core's
  `AddDbContext`, and keep control of its options.
- It does not make up connection strings or schema names: you supply the delegates.
- It never provisions or migrates a tenant on its first request. You provision tenants explicitly, and run
  migrations as a deployment step or, if you opt in with `o.OnStartup`, at startup.
- It does not replace Tenantry core's resolution, stores or shared-database isolation.
- It has no admin dashboard or UI.
