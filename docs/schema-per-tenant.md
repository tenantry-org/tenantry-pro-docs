# Schema per tenant

All tenants share one database, but each gets a dedicated schema (e.g. `tenant_acme.Orders`,
`tenant_globex.Orders`). This isolates data at the schema level while keeping a single database to
back up, connect to, and pay for — a good middle ground between a shared table and a database per
tenant.

## What Tenantry.Pro provides

- `ISchemaNameResolver<TKey>` — resolves the current tenant's schema name.
- `pro.AddSchemaPerTenantCaching()` with `options.AddSchemaPerTenantCaching<TKey>(sp)` — makes EF Core
  compile and cache a **separate model per tenant schema**, so the right schema is baked into each
  tenant's queries. Both calls are needed.
- `SchemaProvisioningService<TKey>` — creates a tenant's schema on demand (per provider package).

## What you provide

- Tenant resolution and a tenant store via `AddTenantry<TKey>(...)` (Tenantry core).
- The shared database connection string.
- The schema-name convention (`GetSchemaName` delegate).
- Your own `DbContext` registration, applying the schema in `OnModelCreating`.
- An explicit tenant-creation flow that calls schema provisioning when you want schemas created.

## Registration

```csharp
using Microsoft.EntityFrameworkCore;
using Tenantry.Pro;
using Tenantry.Pro.EfCore;                       // pro.AddSchemaPerTenantCaching()
using Tenantry.Pro.EfCore.Extensions;            // options.AddSchemaPerTenantCaching(sp)
using Tenantry.Pro.EfCore.SqlServer.Extensions;  // AddSchemaProvisioning

var connectionString = builder.Configuration.GetConnectionString("AppDb")!;

builder.Services.AddTenantry<string>(tenant =>
{
    tenant.ResolveFromHeader("X-Tenant-Id");
    tenant.UseStore<MyTenantStore>();

    tenant.UsePro(pro =>
    {
        pro.WithLicence(builder.Configuration["Tenantry:Licence"]!);

        pro.UseSchemaPerTenant(opts =>
            opts.GetSchemaName = t => $"tenant_{t.TenantId}");

        // Registers the per-schema model cache used by AddSchemaPerTenantCaching(sp) below.
        pro.AddSchemaPerTenantCaching();

        // Optional: provision tenant schemas on demand. The connection string for the shared
        // database is configured here, on the provisioning options — not on UseSchemaPerTenant.
        pro.AddSchemaProvisioning(opts => opts.ConnectionString = connectionString);
    });
});

// Register your DbContext against the shared database, with per-tenant model caching.
builder.Services.AddDbContext<AppDbContext>((sp, options) =>
    options.UseSqlServer(connectionString)
           .AddSchemaPerTenantCaching<string>(sp));
```

> **API note:** `GetSchemaName` lives on `SchemaPerTenantOptions`; the shared database connection
> string for provisioning lives on `SchemaProvisioningOptions`, set inside `AddSchemaProvisioning(...)`.
> `AddSchemaProvisioning` now **requires** a configure delegate. Omit the call entirely if you create
> schemas outside Tenantry.

## DbContext integration

Your `DbContext` must apply the resolved schema in `OnModelCreating`. You can inject either
`ISchemaNameResolver<TKey>` or, more simply, `ITenantContext<TKey>`:

```csharp
public sealed class AppDbContext(
    DbContextOptions<AppDbContext> options,
    ISchemaNameResolver<string> schemaNameResolver) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(schemaNameResolver.Resolve());
    }
}
```

`AddSchemaPerTenantCaching<TKey>(sp)` is what makes this correct under load. By default EF Core caches
**one** compiled model per context type, which would freeze whichever tenant's schema was seen first.
The caching extension installs a model-cache key that varies by tenant, so EF Core holds a distinct
compiled model per schema and every tenant sees its own.

## Provisioning a new tenant

`AddSchemaProvisioning(...)` registers `SchemaProvisioningService<TKey>`, which does **not** run
automatically. Call it during tenant creation:

```csharp
public sealed class TenantAdminService(SchemaProvisioningService<string> provisioning)
{
    public Task CreateSchemaAsync(string tenantId, CancellationToken ct) =>
        provisioning.ProvisionAsync(tenantId, ct);   // idempotent CREATE SCHEMA
}
```

`ProvisionAsync` checks the licence, resolves the schema name for the tenant, and issues a
provider-specific, idempotent `CREATE SCHEMA` against the configured connection string. Schema
provisioning is available for **SQL Server** and **PostgreSQL**. MySQL/MariaDB treat schemas and
databases as the same thing, so use database-per-tenant there — see
[Database providers](database-providers.md).

## Tables and migrations

**Migration orchestration does not cover schema per tenant.** `WithMigrationOrchestration`,
`MigrationOrchestratorService`, the tenant lifecycle's migration step and the migration health check all
work on a database per tenant, and `WithMigrationOrchestration` refuses to register without
`UseDatabasePerTenant`. EF Core migrations name the schema when they are generated, so replaying one
migration into many schemas needs schema-aware migrations, which Tenantry does not provide.

- **Creating a new tenant's tables.** After provisioning the schema, create the model's tables in it from
  a context whose default schema is the tenant's (the context is tenant-scoped, so its model already
  targets that schema):

  ```csharp
  await provisioning.ProvisionAsync(tenantId, ct);                          // CREATE SCHEMA
  await db.GetService<IRelationalDatabaseCreator>().CreateTablesAsync(ct);  // only for a new, empty schema
  ```

  `CreateTablesAsync` is not idempotent, so check the schema has no tables first; the samples query
  `information_schema.tables`. It records no migration history.
- **Changing the schema later.** Apply your changes to every tenant schema with your own per-schema
  scripts (for example `dotnet ef migrations script` output with the schema substituted), run as a
  deployment step.

If you need Tenantry to migrate every tenant for you, use a database per tenant.

## Limitations

- **No migration orchestration** (above).
- **No MySQL/MariaDB strategy.** MySQL has no schemas separate from databases (a schema *is* a
  database), so there is no schema provisioning for it; use a database per tenant there.
- EF Core holds one compiled model per tenant schema in memory. With a very large number of tenants,
  watch model-cache memory.
- Schema provisioning is explicit; Tenantry.Pro does not create schemas on first request.

## See also

- [Database providers](database-providers.md) · [Mixed mode](mixed-mode.md)
- [Tenant lifecycle](tenant-lifecycle.md) — provision a schema and seed it in one call (its migration
  step is database-per-tenant only).
