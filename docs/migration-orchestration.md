# Tenant migrations

Every tenant's database, or schema, needs your EF Core migrations applied. `pro.AddMigrations<TContext>()` applies
them for **all** tenants (or one), through your own context, once for each distinct database or schema, with each
database's failure kept to itself: as a deployment step, at startup, and when a new tenant is provisioned.

## What it provides

- `pro.AddMigrations<TContext>(o => …)`: registers the runner, adds the `Migrations` step to
  [tenant provisioning](tenant-lifecycle.md), optionally migrates at startup (`o.OnStartup`), and sets how many
  databases or schemas are migrated at once (`o.MaxConcurrency`).
- `ITenantMigrationRunner<TKey>`: applies migrations for every tenant (`MigrateAllAsync`, optionally reporting each
  result as it completes) or one (`MigrateTenantAsync`), and reads what is applied and pending (`GetStatusAsync`,
  `GetTenantStatusAsync`; read-only, **no licence required**).
- `app.RunTenantMigrationsIfRequestedAsync(args)`: the deployment step.
- `MigrationReport<TKey>`, `MigrationResult<TKey>` and `MigrationStatusEntry<TKey>` result types.

## Requirements

- A reference to `Tenantry.Pro.EfCore`, and to the EF Core provider your context uses.
- The context registered so that a tenant's scope gives the tenant's own database or schema: with Tenantry core's
  `AddDbContextPerTenantDatabase` for a database per tenant, or with `UseTenantry()` and
  [schema per tenant](#schema-per-tenant).
- A valid licence key, checked before any migration runs. Reading the status needs none.

## Registration

```csharp
using Microsoft.EntityFrameworkCore;
using Tenantry;
using Tenantry.Pro.EfCore;

builder.Services.AddTenantry<string>(tenant => tenant
    .ResolveFromHeader("X-Tenant-Id")
    .UseStore<MyTenantStore>()
    .UseConnectionStrings(opts =>
        opts.GetConnectionString = t =>
            $"Server=.;Database=app_{t.TenantId};Integrated Security=true;TrustServerCertificate=True")
    .UsePro(pro => pro.AddMigrations<AppDbContext>())
    .AddDbContextPerTenantDatabase<AppDbContext>((_, options) => options.UseSqlServer()));
```

The runner gets the context from each tenant's scope, as a request does. When it cannot be created there, as with
`AddDbContextPerTenantDatabase` and only an asynchronous connection string (`GetConnectionStringAsync`), it comes from
the application's `IDbContextFactory<TContext>`, which creates it with the application's root services, so its
constructor must not need scoped ones. To apply migrations with other credentials than the application's, such as a
login allowed to change the schema, create the context yourself; it is disposed after use:

```csharp
tenant.UsePro(pro => pro.AddMigrations<AppDbContext>(o => o.CreateContext = sp =>
{
    var current = sp.GetRequiredService<ITenantContext<string>>().CurrentTenant!;
    return new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
        .UseSqlServer($"Server=.;Database=app_{current.TenantId};User Id=migrator;Password={adminPassword}")
        .Options);
}));
```

With schema per tenant, build that context with `UseApplicationServiceProvider(sp)` and `UseTenantry()` too, so it
gets the tenant's schema; a context without them is refused for a tenant that has a schema, rather than migrating the
database's default schema.

Call `AddMigrations` once for each context to migrate. Each runs in the order added, and each is a provisioning step.

## Running migrations

Inject `ITenantMigrationRunner<TKey>` in an admin endpoint or a background job:

```csharp
public sealed class MigrationAdminService(ITenantMigrationRunner<string> migrations)
{
    // Every tenant. Each database's failure is in the report; cancelling ct stops the run.
    public Task<MigrationReport<string>> MigrateAllAsync(CancellationToken ct) =>
        migrations.MigrateAllAsync(ct);

    // One tenant, for each context. A failed migration is in the report rather than thrown.
    public Task<MigrationReport<string>> MigrateOneAsync(string tenantId, CancellationToken ct) =>
        migrations.MigrateTenantAsync(tenantId, ct);
}
```

To follow a long run, pass an `IProgress<MigrationResult<TKey>>`: its `Report` is called with each database's or
schema's result as it completes, with every tenant whose data is there, once, and one call at a time:

```csharp
var migrations = app.Services.GetRequiredService<ITenantMigrationRunner<string>>();
var report = await migrations.MigrateAllAsync(
    new Progress<MigrationResult<string>>(result => Console.WriteLine(
        $"{result.Database}/{result.Schema}: {(result.Succeeded ? "migrated" : "failed")}")),
    ct);
```

`Progress<T>` runs the callback on the synchronization context it was created on or, without one (a console
application, the deployment step, ASP.NET Core), on the thread pool, where callbacks can overlap, run out of order and
come after `MigrateAllAsync` returns: keep it thread-safe, as `Console.WriteLine` is. An `IProgress<T>` of your own is
called on the thread that completed the result, one call at a time. An exception from `Report` is logged, and the run
goes on.

**Once per database or schema.** Tenants whose context connects to the same database and schema are migrated once,
together, and share one result: the shared database in [mixed mode](mixed-mode.md), say, or tenants that
`GetSchemaName` gives one schema. Tenants share a database and schema when their contexts have the same connection
string, the same default schema and the same migration history table. A connection interceptor that switches the
database or schema for each tenant hides that, so do not combine one with `AddMigrations`. To know which tenants share
one before migrating, a run first creates each tenant's context, then creates it again for the first tenant of each
database or schema to migrate it.

`MigrateAllAsync` migrates every tenant your store's `GetAllTenantsAsync` returns, and `MigrateTenantAsync`
throws `TenantNotFoundException` for a tenant the store does not return. So the store must list
suspended tenants too (see
[Tenant stores](https://github.com/tenantry-org/tenantry-core/blob/master/docs/tenant-stores.md#suspended-and-inactive-tenants)
in Tenantry core): a tenant hidden while it is suspended misses every run and fails when it is
reactivated.

Keep a suspended tenant's database online, so it keeps receiving migrations: suspension decides who may use
the tenant, not whether its database runs. A tenant whose database cannot be reached is reported as a
failure on every run, which fails the deployment step below and, with `StartupMigrations.FailOnError`, stops
startup. If you take a tenant's database offline (archived or dropped), remove the tenant from the store. To bring
it back, restore the database, add the tenant back as inactive, run `MigrateTenantAsync`, then reactivate it. With
Tenantry core's `CacheTenants`, call `ITenantStoreCache<TKey>.Invalidate` as you reactivate it, or it is served as
inactive until its entry expires; with `CacheConnectionStrings`, call `IConnectionStringCache<TKey>.Invalidate` if the
restored database's connection details differ.

`MigrateAsync` in EF Core creates a tenant's database if it does not exist, so a mistyped or stale
connection string gets a new, empty, fully migrated database rather than an error. Create databases with
[provisioning](database-per-tenant.md#provisioning-a-new-tenant) and check the connection strings your
store produces.

### Run as a deployment step (recommended)

Migrate once per deployment, from one process, before the new version starts serving traffic: a CI/CD
stage or a Kubernetes `Job` (not an init container, which runs in every replica). Run the application with the
argument `migrate-tenants`; it migrates every tenant and exits, without starting the host:

```csharp
await using var app = builder.Build();   // disposing it at exit writes out the last log messages

// dotnet run -- migrate-tenants
if (await app.RunTenantMigrationsIfRequestedAsync(args) is { } exitCode)
    return exitCode;   // 1 if any database or schema failed, which fails the deployment step

await app.RunAsync();
return 0;
```

Each failure is logged, once, as an error, and the run ends with a summary: a warning if any failed.

Run one migration step at a time: two runs at once, from overlapping deployments, race as several instances do (see
[Multiple instances](#multiple-instances)). In CI, put the step in a concurrency group that queues a second run
rather than cancelling the first: cancelling stops the process mid-migration, which on MySQL can leave a migration
half applied. A Kubernetes `Job` that fails is retried up to its `backoffLimit` (6 by default); each retry runs the
whole sweep again, which applies only what is still pending, so a tenant that failed for a lasting reason fails each
time.

### Run at startup

`o.OnStartup` applies the context's migrations as the application starts, before it serves requests:

```csharp
tenant.UsePro(pro => pro.AddMigrations<AppDbContext>(o => o.OnStartup = StartupMigrations.FailOnError));
```

| `OnStartup` | At startup |
|-------------|------------|
| `None` (the default) | Nothing: run them as a deployment step |
| `LogFailures` | Migrates every tenant; failures are logged and the application starts |
| `FailOnError` | Migrates every tenant; any failure stops the application from starting |

It lengthens boot time in proportion to the number of tenants and runs on **every instance** (see below), so keep it
for single-instance deployments and development.

### Multiple instances

Tenantry takes **no lock** when it migrates. If several instances migrate at the same time (for example
`OnStartup` on every replica of a scaled-out app), each one migrates every tenant:

- **EF Core 9 and later** take a database lock while applying migrations on providers that support it,
  so concurrent runs against the same tenant database wait for each other and the second finds nothing
  pending, and reports nothing applied for that tenant. In our tests this held on SQL Server and MySQL,
  and on PostgreSQL with EF Core 9 (see [Tested combinations](database-providers.md#tested-combinations)).
- **PostgreSQL with EF Core 10 and later** holds that lock only until each migration commits, so with
  several migrations pending two runs can take turns: in our tests one applied the first migration, the
  other applied the rest, and the first then failed with "already exists". Each run reports only the
  migrations it applied, the end state was correct, and a rerun applied nothing.
- **EF Core 8**, which Tenantry.Pro's `net8.0` assets use, takes no lock. In our tests two runners
  raced on every provider: one applied the migration and the other reported that tenant as failed
  ("already exists"). The end state was correct and a rerun applied nothing, but on MySQL, whose DDL is
  not transactional, a race inside a larger migration can leave it half applied.

Either way, every instance repeats the whole sweep, so startup time grows with the number of instances
and tenants. With more than one instance, run migrations as a deployment step (above) and leave `OnStartup` at
`None`.

## Schema per tenant

With [schema per tenant](schema-per-tenant.md), each tenant's schema gets your migrations, with a migration history
table of its own in that schema. Nothing in the migrations names the schema:

- **Generate migrations as usual.** `dotnet ef migrations add` builds the model without a tenant, so without a schema,
  and the migrations and snapshot it writes name none. Keep `HasDefaultSchema` and explicit schemas out of the model's
  tenant tables: a schema a migration names is kept, so that table would not move to each tenant's schema.
- **At run time**, every table, index, key and sequence a migration names without a schema gets the tenant's, and so
  does the snapshot that EF Core 9 and later compare with the model before migrating. The history table keeps its
  name (`MigrationsHistoryTable`, if you set one) and moves to the tenant's schema.
- **SQL you add with `migrationBuilder.Sql(...)` is applied as written**: names in it are not put in the tenant's
  schema, so unqualified ones resolve to the database's default. Keep tenant migrations to EF Core's operations.
- A tenant's schema that does not exist yet is created with its history table, but create schemas with
  [schema provisioning](schema-per-tenant.md#provisioning-a-new-tenant), which runs before the `Migrations` step.
- Tested on SQL Server and PostgreSQL; MySQL has no schemas apart from databases.

## Failure model

- **One at a time, by default.** Databases and schemas are migrated one at a time, so a large tenant count does not
  overwhelm the server. See [Several at once](#several-at-once).
- **Each database on its own.** A failure migrating one never stops the others. `MigrateAllAsync` does not throw on a
  migration error: every outcome is in the report, and each failure is logged once, as an error. A tenant whose
  context cannot be created (its connection string cannot be read, say) is a failure of its own.
- **Cancellation stops the run.** When the cancellation token is cancelled, `MigrateAllAsync` and
  `MigrateTenantAsync` throw `OperationCanceledException`: the databases in progress are abandoned, the rest are not
  attempted (they are not reported as failures), and those already migrated stay migrated. The report is not
  returned, so after a cancelled run (a CI timeout, say) read the logs, where each outcome is written as it finishes,
  or check every tenant with `GetStatusAsync`. A timeout inside one migration that does not come from your token is
  that database's failure, and the run continues.
- **Licence check.** The licence is checked once, up front: a missing or invalid key throws
  `LicenseRequiredException` before any tenant is touched.

`MigrationReport<TKey>` aggregates the run, in the order of the contexts and of the tenants in the store, however
many were migrated at once:

| Member | Meaning |
|--------|---------|
| `Results` | One `MigrationResult<TKey>` per context and database or schema |
| `Total` / `Succeeded` / `Failed` | Counts across the run |
| `HasFailures` | True if any failed |

Each `MigrationResult<TKey>` carries `ContextType`, `TenantIds` (the tenants whose data is there), `Database` and
`Schema`, `Succeeded`, `AppliedMigrations`, `Duration`, and `Error` (the exception on failure). `AppliedMigrations`
lists the migrations this run applied, in order: those whose row in the migration history this run wrote and
committed. So it never lists a migration another instance applied, whether this run waited for it, failed on it, or
skipped it on a retry (see [Multiple instances](#multiple-instances)). When a run fails, it lists the migrations
committed before the failure, which depends on the EF Core version (see
[Failed migrations](tenant-lifecycle.md#when-provisioning-fails)); the failing migration is never listed. On MySQL
with EF Core 9 a failed run lists none, although MySQL keeps the earlier migrations applied. After a failure, check
the tenant with `GetTenantStatusAsync` (below).

## Several at once

With many tenant databases, a run migrating one at a time can take longer than your deployment allows. Set
`o.MaxConcurrency` to migrate several at once:

```csharp
tenant.UsePro(pro => pro.AddMigrations<AppDbContext>(o => o.MaxConcurrency = 8));
```

- Each tenant's context is still created in the tenant's own scope, but now several at once: your connection-string
  provider and `CreateContext` must allow that (Tenantry's are).
- Tenants are still worked on one at a time until one tenant's context has been created: some EF Core providers set
  up shared state the first time a context is used, without a lock (Oracle's `MySql.EntityFrameworkCore` builds its
  type mappings so), and contexts created at once in a new application could corrupt it. Two runs at once in one new
  process (startup migrations and a migration health check, say) can still meet there.
- It helps most with a database per tenant, spread over servers. Schemas in one database may take turns: EF Core 9
  and later lock the database while applying migrations, on providers that support it.
- `MaxConcurrency` is set for each context, and also bounds `GetStatusAsync`. Contexts are migrated one after
  another. A value below 1 stops the application from starting.

## Checking status without migrating

`GetStatusAsync` and `GetTenantStatusAsync` are read-only and **never check the licence key** (they change nothing),
so they never throw `LicenseRequiredException`:

```csharp
public sealed class MigrationStatusService(ITenantMigrationRunner<string> migrations)
{
    public Task<IReadOnlyList<MigrationStatusEntry<string>>> AllAsync(CancellationToken ct) =>
        migrations.GetStatusAsync(ct);

    public Task<IReadOnlyList<MigrationStatusEntry<string>>> OneAsync(string tenantId, CancellationToken ct) =>
        migrations.GetTenantStatusAsync(tenantId, ct);
}
```

Each `MigrationStatusEntry<TKey>` names its context, tenants, database and schema, and exposes `AppliedMigrations`,
`PendingMigrations`, `IsUpToDate`, and `Error`. A database whose status cannot be read, for example because it is
unreachable, gets an entry with `Error` set (and `IsUpToDate` false) instead of failing the whole call, so the others
are still read. Cancelling the token still throws. The [migration health check](health-checks.md#migration-check)
reads the same information.

## Limitations

- Tenantry.Pro applies your existing EF Core migrations; it does not generate them.
- Migrations use reflection and runtime code generation, so these APIs are annotated
  `[RequiresDynamicCode]`/`[RequiresUnreferencedCode]` and are not Native-AOT compatible.

## See also

- [Tenant lifecycle](tenant-lifecycle.md): provision, migrate, and seed a new tenant in one call.
- [Health checks](health-checks.md): report pending migrations to your monitoring.
- [Mixed mode](mixed-mode.md): a database, a schema or the shared database, per tenant.
