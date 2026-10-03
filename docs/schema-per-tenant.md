# Schema per tenant

All tenants share one database, but each gets a dedicated schema (e.g. `tenant_acme.Orders`,
`tenant_globex.Orders`). This isolates data at the schema level while keeping a single database to
back up, connect to, and pay for — a good middle ground between a shared table and a database per
tenant. It works on **SQL Server** and **PostgreSQL**; MySQL has no schemas apart from databases, so
use a database per tenant there.

## What Tenantry.Pro provides

- `pro.UseSchemaPerTenant(o => o.GetSchemaName = …)` — the context that uses `UseTenantry()` gets the
  current tenant's schema as its default schema, with a **separate compiled model per schema**, so the
  right schema is in each tenant's queries. Your `DbContext` names no schema. With more than one such context, you
  list those that get it ([Which contexts get the schema](#which-contexts-get-the-schema)).
- `pro.AddSchemaProvisioning<TContext>()` — creates a tenant's schema when you provision it (see
  [Tenant lifecycle](tenant-lifecycle.md)).
- `pro.AddMigrations<TContext>()` — applies your EF Core migrations, generated without a schema, to each tenant's
  schema, with a migration history of its own there (see [Tables and migrations](#tables-and-migrations)).

## What you provide

- Tenant resolution and a tenant store via `AddTenantry<TKey>(...)` (Tenantry core).
- The schema-name convention (`GetSchemaName`).
- Your `DbContext`, registered against the shared database with `UseTenantry()`.
- An explicit tenant-creation flow that provisions each new tenant.

## Registration

```csharp
using Microsoft.EntityFrameworkCore;
using Tenantry;
using Tenantry.Pro;

var connectionString = builder.Configuration.GetConnectionString("AppDb")!;

builder.Services.AddTenantry<string>(tenant =>
{
    tenant.ResolveFromHeader("X-Tenant-Id");
    tenant.UseStore<MyTenantStore>();

    tenant.UsePro(pro =>
    {
        pro.UseSchemaPerTenant(opts => opts.GetSchemaName = t => $"tenant_{t.TenantId}");

        // Optional: provisioning a new tenant creates its schema, in the database AppDbContext connects to.
        pro.AddSchemaProvisioning<AppDbContext>();
    });
});

// Your DbContext, against the shared database. UseTenantry() is what gives it the tenant's schema.
builder.Services.AddDbContext<AppDbContext>(options => options
    .UseSqlServer(connectionString)
    .UseTenantry());
```

The context itself needs nothing for the schema:

```csharp
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
}
```

The tenant's schema becomes the model's default schema after `OnModelCreating`, so it applies to every
table that does not name a schema of its own. Tenants whose `GetSchemaName` gives the same name share a
schema and a compiled model. Without a current tenant, the model has no default schema: that is the model
design-time tools such as `dotnet ef migrations add` see, so migrations are generated without a schema.

**It fails closed.** A context used without a current tenant would use the database's default schema, so its
first query, command or save throws `TenantNotResolvedException`. EF Core fixes the schema when it first builds a
context's model, so a context keeps the schema of the tenant that was current then. Used under another tenant, it
throws `TenantIsolationViolationException` (`Kind` is `TenantSchemaMismatch`). Create a context for each tenant, in
its scope, as requests and `ITenantScopeFactory` do.

`GetSchemaName` is called often, so keep it fast, and it must give a tenant the same name every time. A
name that is empty, has control characters or is longer than the database allows (128 characters on SQL
Server, 63 bytes on PostgreSQL, which would otherwise cut it short without an error) fails the tenant's
queries with `InvalidOperationException`.

### Which contexts get the schema

With one context type that uses `UseTenantry()`, as above, that context gets the tenant's schema, and there is nothing
to list. With more than one (a second over reference data with `ITenantEntity` rows, say, which stays in the shared
schema), list those that get it:

```csharp
pro.UseSchemaPerTenant(o =>
{
    o.GetSchemaName = t => $"tenant_{t.TenantId}";
    o.Contexts.Add(typeof(AppDbContext));   // and its subclasses; other contexts keep Tenantry's filters only
});
```

Leave the list empty with more than one, and schema per tenant does not guess which of them get the schema: it fails
closed, with an `InvalidOperationException` that names the context types and asks you to list them in
`SchemaPerTenantOptions.Contexts`.

- **When the host starts**, for the contexts you register with `AddDbContext`, `AddDbContextFactory`, their pooled
  forms or Tenantry Core's `AddDbContextPerTenantDatabase`. The host builds each one's options, without a tenant,
  before any hosted service starts, so before migrations at startup and before any request.
- **Before `migrate-tenants` or `ITenantMigrationRunner` migrates a tenant**, the same way, since a deployment step
  does not start the host.
- **Otherwise, when the second context's options are built**: a context you build by hand, one whose options cannot
  be built without a tenant, or one used by a tool that builds your host without starting it, such as `dotnet ef`.
  From then on every schema-per-tenant context of the application throws too.

A design-time factory (`IDesignTimeDbContextFactory`) that builds the options without your application's services,
as the samples' does, gets no schema per tenant and is not counted.

A context type that derives from another counts as that one, as it does in the list: a test's
`TestAppDbContext : AppDbContext` beside `AppDbContext` needs no list. Two contexts that derive from a common base
class, and not from one another, are two.

### How many schemas stay compiled

EF Core compiles a model for each schema, and compiles each query again for each schema's model. Its own
cache holds the models of about 40 schemas (about 100 on EF Core 8), or about 500 compiled queries, in
all: beyond that, it evicts them and compiles them again as tenants take turns, which makes the first
request after each eviction slow. So each context type gets a cache of its own, with room for
`MaxCachedSchemas` schemas (500 by default) and `MaxCompiledQueriesPerSchema` compiled queries for each
(100 by default), on top of EF Core's default:

```csharp
pro.UseSchemaPerTenant(o =>
{
    o.GetSchemaName = t => $"tenant_{t.TenantId}";
    o.MaxCachedSchemas = 2_000;            // at least the schemas in use at once
    o.MaxCompiledQueriesPerSchema = 200;   // about the distinct queries your application runs
});
```

Set them within your memory budget: each cached schema holds a compiled model and its compiled queries. Past
the limits, the least recently used entries are compiled again when next needed. The cache belongs to EF
Core's internal service provider for the context type, which every application in the process shares, so
`UseMemoryCache`, or replacing EF Core's `IModelCacheKeyFactory` or `IMemoryCache` with `ReplaceService`, on
these contexts' options is refused with `InvalidOperationException`: either would undo the cache, or let
tenants share a model. Each context type that uses `UseTenantry()` gets an internal service provider of its
own (and one more for each different pair of limits); EF Core throws once a process has built more than 20.

**Not with DbContext pooling.** A pooled context keeps the model it was first built with, so every tenant
that leased it would query the first tenant's schema. A context that uses `UseTenantry()` and is registered
with `AddDbContextPool`, `AddPooledDbContextFactory` or Tenantry Core's `AddDbContextPerTenantDatabase` with
`pooled: true` throws `InvalidOperationException`; use `AddDbContext` or `AddDbContextFactory`.

## Provisioning a new tenant

`AddSchemaProvisioning<TContext>()` adds creating the tenant's schema to tenant provisioning, as its first
step (`CreateSchema`). It does **not** run automatically: add the tenant to your store, then provision it
with `ITenantProvisioner<TKey>`:

```csharp
public sealed class TenantAdminService(ITenantProvisioner<string> provisioner)
{
    public async Task<bool> CreateAsync(ITenantDescriptor<string> tenant, CancellationToken ct) =>
        (await provisioner.ProvisionAsync(tenant, ct)).Succeeded;   // CREATE SCHEMA unless it exists, then your steps
}
```

The step creates the schema with EF Core's own migrations SQL for the provider, against the database
`TContext` connects to in the tenant's scope. It checks for the schema first, so provisioning a tenant
again does nothing, and two runs for one tenant at once both succeed. The context's credentials must be
allowed to create schemas; if your application's are not, give provisioning a context of its own with
`o.CreateContext`:

```csharp
pro.AddSchemaProvisioning<AppDbContext>(o => o.CreateContext = sp =>
    new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(adminConnectionString).Options));
```

On a provider other than SQL Server or PostgreSQL the step fails with `NotSupportedException`.
`AddSchemaProvisioning` without `UseSchemaPerTenant` stops the application from starting.

To drop the tenant's schema, with its tables, when it leaves, add `pro.AddSchemaDeprovisioning<TContext>()` and call
`ITenantDeprovisioner<TKey>`: it refuses a schema another tenant has. See
[Offboarding a tenant](tenant-lifecycle.md#offboarding-a-tenant).

## Tables and migrations

`pro.AddMigrations<TContext>()` applies your migrations to each tenant's schema: as the `Migrations` provisioning
step, right after `CreateSchema`, and for every tenant from the [migration runner](migration-orchestration.md):

```csharp
tenant.UsePro(pro => pro
    .UseSchemaPerTenant(o => o.GetSchemaName = t => $"tenant_{t.TenantId}")
    .AddSchemaProvisioning<AppDbContext>()     // CREATE SCHEMA, unless it exists
    .AddMigrations<AppDbContext>());           // then the migrations, in that schema
```

Generate the migrations as usual, with `dotnet ef migrations add`. The tools create the context without a tenant, so
its model has no schema and neither do the migrations or the snapshot. When they are applied for a tenant, every
table, index and key they name without a schema gets the tenant's, and the migration history table
(`__EFMigrationsHistory`) is the one in the tenant's schema, so each schema records its own migrations. EF Core 9 and
later compare the snapshot with the model before migrating; Tenantry gives the snapshot the tenant's schema too, so
they match.

- Keep the schema out of the model: no `HasDefaultSchema`, and no schema in `ToTable` for the tenant tables. A schema
  a migration names is kept, so that table would stay where it names.
- SQL you add with `migrationBuilder.Sql(...)` is applied as written: names in it are not put in the tenant's schema.
- `dotnet ef database update` has no tenant, so it throws `TenantNotResolvedException`. Migrate tenants with the
  runner, as a deployment step (`app.RunTenantMigrationsIfRequestedAsync(args)`, see
  [Tenant migrations](migration-orchestration.md#run-as-a-deployment-step-recommended)).

The [SchemaPerTenant samples](../samples/Tenantry.Pro.Samples.SchemaPerTenantPostgreSql) do this end to end.

## Limitations

- **No MySQL strategy.** MySQL has no schemas separate from databases (a schema *is* a
  database), so there is no schema provisioning for it; use a database per tenant there.
- EF Core holds one compiled model per schema in memory, up to `MaxCachedSchemas` per context type
  ([above](#how-many-schemas-stay-compiled)).
- Schema provisioning is explicit; Tenantry.Pro does not create schemas on first request.
- The contexts that get the tenant's schema cannot be pooled.

## See also

- [Database providers](database-providers.md) · [Mixed mode](mixed-mode.md)
- [Tenant lifecycle](tenant-lifecycle.md) — provision a schema, migrate it and seed it in one call.
- [Tenant migrations](migration-orchestration.md) — migrate every tenant's schema.
