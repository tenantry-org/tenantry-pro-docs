# Tenant migrations

Every tenant's database, or schema, needs your EF Core migrations applied. `pro.AddMigrations<TContext>()` applies
them for **all** tenants (or one), through your own context, once for each distinct database or schema, with each
database's failure kept to itself: as a deployment step, at startup, and when a new tenant is provisioned.

## What it provides

- `pro.AddMigrations<TContext>(o => …)`: registers the runner, adds the `Migrations` step to
  [tenant provisioning](tenant-lifecycle.md), optionally migrates at startup (`o.OnStartup`), and sets how many
  databases or schemas are migrated at once (`o.MaxConcurrency`).
- `ITenantMigrationRunner<TKey>`: applies migrations for every tenant (`MigrateAllAsync`, optionally reporting each
  result as it completes), for the tenants `MigrationRunOptions<TKey>` selects, stopping after a number of failures
  (`MigrateAsync`), or for one (`MigrateTenantAsync`), and reads what is applied and pending (`GetStatusAsync`,
  `GetTenantStatusAsync`; read-only, **no licence required**).
- `app.RunTenantMigrationsIfRequestedAsync(args)`: the deployment step, with `--tenant`, `--exclude` and
  `--max-failures`, and an exit code for each outcome.
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

`MigrateAsync` takes `MigrationRunOptions<TKey>`: the tenants to migrate (`Tenants`, every tenant when null), those to
leave out (`ExcludedTenants`), how many databases or schemas may fail before the run stops starting others
(`MaxFailures`), and a token that stops it starting others when cancelled while those in progress finish
(`StopStarting`, which `migrate-tenants` cancels on SIGTERM). Those it did not start are in the report as not
attempted. Ids are matched exactly, and an id in either list that the store does not have throws
`TenantNotFoundException` before anything runs. A canary run, then the rest:

```csharp
var migrations = app.Services.GetRequiredService<ITenantMigrationRunner<string>>();
string[] canaries = ["acme", "globex", "initech"];

var canary = await migrations.MigrateAsync(new MigrationRunOptions<string> { Tenants = canaries, MaxFailures = 1 }, cancellationToken: ct);
if (!canary.HasFailures && !canary.Stopped)
    await migrations.MigrateAsync(new MigrationRunOptions<string> { ExcludedTenants = canaries }, cancellationToken: ct);
```

A database or schema is migrated once for every tenant whose data is in it, so a selection picks databases and
schemas: one is in the run when one of its tenants is selected and none is excluded, and its result names all its
tenants. A run for some tenants still creates every tenant's context first, to find which tenants share one. Once a
run has stopped, the contexts it has not reached are not created: their tenants are reported one by one as not
attempted, with no database.

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

**Once per database or schema.** Tenants whose contexts reach the same database (server, port and database, as
the provider reads the connection string) with the same default schema and history table are migrated once,
together, and share one result: the shared database in [mixed mode](mixed-mode.md), say. When the model sets no
schema, the connection's default schema decides, so the login and PostgreSQL's `Search Path` and `Options` keep
tenants apart; a password or a timeout does not. Do not combine
`AddMigrations` with a connection interceptor that switches the database or schema for each tenant, which hides
this.

### Which tenants are migrated

The runner migrates the tenants your store lists, and `MigrateTenantAsync` throws `TenantNotFoundException` for one
it does not. So list suspended tenants too, and keep their databases online: a tenant hidden while suspended misses
every run, and an unreachable database fails every run. To retire a tenant, remove it from the store. To bring it
back, restore its database, add it as inactive, run `MigrateTenantAsync`, invalidate it
(`ITenantInvalidator<TKey>.InvalidateAsync`, which clears cached connection strings too), then reactivate it.

**A migration run does not create a tenant's database.** EF Core's `Migrate` creates a database that does not exist,
so a stale or mistyped connection string, or a tenant whose database was dropped but who is still in the store, would
get a new, empty, fully migrated database that its requests then reach. A run reports such a tenant as failed instead,
and migrates the others; with schema per tenant, the same goes for a tenant schema that does not exist. Create
tenants' databases and schemas with [provisioning](database-per-tenant.md#provisioning-a-new-tenant), whose
`Migrations` step creates as it always has. For development, where a new developer's databases do not exist yet, let
runs create them:

```csharp
tenant.UsePro(pro => pro.AddMigrations<AppDbContext>(o => o.CreateMissingDatabases = builder.Environment.IsDevelopment()));
```

### Run as a deployment step (recommended)

Migrate once per deployment, from one process, before the new version starts serving traffic: a CI/CD
stage or a Kubernetes `Job` (not an init container, which runs in every replica). Run the application with the
argument `migrate-tenants`; it migrates every tenant and exits, without starting the host:

```csharp
await using var app = builder.Build();   // disposing it at exit writes out the last log messages

// dotnet run -- migrate-tenants
if (await app.RunTenantMigrationsIfRequestedAsync(args) is { } exitCode)
    return exitCode;   // not 0 unless every database and schema migrated, which fails the deployment step

await app.RunAsync();
return 0;
```

Each failure is logged, once, as an error, and the run ends with a summary: a warning if any failed.

These options after `migrate-tenants` shape the run, as `--name value` or `--name=value`; other arguments are left to
the application's configuration:

| Option | Effect |
|--------|--------|
| `--tenant <id>` | Migrate only this tenant's database or schema. Repeat it for more. |
| `--exclude <id>` | Leave out this tenant's database or schema, for every tenant in it. Repeat it for more. |
| `--max-failures <n>` | Stop starting databases and schemas once `n` have failed. Those in progress finish. |

The process's exit code says what happened:

| Exit code | Meaning |
|-----------|---------|
| 0 | Every selected database and schema is migrated (or was up to date). |
| 1 | At least one failed; the run went through all the others. |
| 2 | The run stopped before attempting them all: `--max-failures` was reached, or it got a stop signal. |
| 3 | The run could not start: an option it cannot use (`--tenants` or `--Tenant` too), no valid licence, a `--tenant` or `--exclude` the store does not have, a tenant store that cannot be read, or schema per tenant with more than one context that uses `UseTenantry()` and none listed in `SchemaPerTenantOptions.Contexts` ([Which contexts get the schema](schema-per-tenant.md#which-contexts-get-the-schema)). |

A first SIGTERM or Ctrl+C lets the migrations in progress finish and starts no other (exit code 2); a second ends the
process at once. A pipeline can migrate canary tenants first, check them, then the rest:

```bash
dotnet MyApp.dll migrate-tenants --tenant acme --tenant globex --max-failures 1
# … check acme and globex …
dotnet MyApp.dll migrate-tenants --exclude acme --exclude globex
```

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
for single-instance deployments and development. As any run, it does not create a missing database, which is a failure;
in development, add `o.CreateMissingDatabases = builder.Environment.IsDevelopment()`.

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
- A run refuses a tenant schema that does not exist. Create schemas with
  [schema provisioning](schema-per-tenant.md#provisioning-a-new-tenant), which runs before the `Migrations` step, or
  set `CreateMissingDatabases` for development (see [Running migrations](#running-migrations)).
- Tested on SQL Server and PostgreSQL; MySQL has no schemas apart from databases.

## Failure model

- **One at a time, by default.** Databases and schemas are migrated one at a time, so a large tenant count does not
  overwhelm the server. See [Several at once](#several-at-once).
- **Each database on its own.** A failure migrating one never stops the others, unless `MaxFailures` says to stop.
  `MigrateAllAsync` does not throw on a migration error: every outcome is in the report, and each failure is logged
  once, as an error. A tenant whose context cannot be created (its connection string cannot be read, say), or whose
  database does not exist, is a failure of its own.
- **Cancellation stops the run.** When the cancellation token is cancelled, `MigrateAllAsync` and
  `MigrateTenantAsync` throw `OperationCanceledException`: the databases in progress are abandoned, the rest are not
  attempted (they are not reported as failures), and those already migrated stay migrated. The report is not
  returned, so after a cancelled run (a CI timeout, say) read the logs, where each outcome is written as it finishes,
  or check every tenant with `GetStatusAsync`. A timeout inside one migration that does not come from your token is
  that database's failure, and the run continues.
- **What a failed migration leaves.** On SQL Server and PostgreSQL, EF Core 8 and 10 or later commit each migration
  on its own, so the earlier ones stay applied; EF Core 9 (Pro's `net9.0` build) commits them together, so usually
  none do. MySQL commits each DDL statement itself, so the earlier migrations stay, and a migration that fails part way
  can leave some of its changes: finish or undo them by hand before retrying. Check a tenant with
  `GetTenantStatusAsync`.
- **Licence check.** The licence is checked once, up front: a missing or invalid key throws
  `LicenseRequiredException` before any tenant is touched.

`MigrationReport<TKey>` aggregates the run, in the order of the contexts and of the tenants in the store, however
many were migrated at once:

| Member | Meaning |
|--------|---------|
| `Results` | One `MigrationResult<TKey>` per context and database or schema |
| `Total` / `Succeeded` / `Failed` / `NotAttempted` | Counts across the run |
| `HasFailures` | True if any failed |
| `Stopped` | True if the run stopped before attempting them all (`MaxFailures`, or a stop signal) |

Each `MigrationResult<TKey>` carries `ContextType`, `TenantIds` (the tenants whose data is there), `Database` and
`Schema`, `Succeeded`, `Attempted` (false for one the run stopped before), `AppliedMigrations`, `Duration`, and `Error`
(the exception on failure). `AppliedMigrations` lists the migrations whose history row this run committed, in
order: never one another instance applied, and after a failure only those committed before it (above).

## Several at once

With many tenant databases, a run migrating one at a time can take longer than your deployment allows. Set
`o.MaxConcurrency` to migrate several at once:

```csharp
tenant.UsePro(pro => pro.AddMigrations<AppDbContext>(o => o.MaxConcurrency = 8));
```

- Each tenant's context is still created in the tenant's own scope, but now several at once: your connection-string
  provider and `CreateContext` must allow that (Tenantry's are).
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
        migrations.GetStatusAsync(cancellationToken: ct);

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
- Migrations are not trim- or Native AOT-compatible
  ([Troubleshooting](troubleshooting.md#trimaot-analyzer-warnings-il2026-il3050)).

## See also

- [Tenant lifecycle](tenant-lifecycle.md): provision, migrate, and seed a new tenant in one call.
- [Health checks](health-checks.md): report pending migrations to your monitoring.
- [Mixed mode](mixed-mode.md): a database, a schema or the shared database, per tenant.
