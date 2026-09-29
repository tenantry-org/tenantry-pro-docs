# Migration orchestration

When you run database-per-tenant, every tenant has its own database — and every one of them needs
your EF Core migrations applied. Migration orchestration applies pending migrations across **all**
tenant databases (or a single one), on demand or automatically at startup, with per-tenant failure
isolation.

## What it provides

- `MigrationOrchestratorService<TKey, TContext>` — applies migrations to all tenants
  (`MigrateAllAsync`) or one tenant (`MigrateTenantAsync`).
- `MigrationStatusTracker<TKey, TContext>` — reports applied and pending migrations per tenant
  (read-only, **no licence required**).
- Optional startup execution via `runAtStartup: true`, with `failStartupOnMigrationError: true` to stop
  the application from starting when a tenant fails.
- `MigrationReport<TKey>` / `MigrationResult<TKey>` / `MigrationStatusEntry<TKey>` result types.

## Requirements

- `pro.UseDatabasePerTenant(...)` must be configured first (orchestration throws at registration
  otherwise). Orchestration is for a **database per tenant only**; schema per tenant is not supported
  (see [Schema per tenant](schema-per-tenant.md#tables-and-migrations)).
- A reference to the provider package (`Tenantry.Pro.EfCore.SqlServer`, `.Npgsql`, or `.MySql`).
- A valid licence key, checked before any migration runs.
- A factory that builds your `DbContext` from a connection string.

## Registration

```csharp
using Microsoft.EntityFrameworkCore;
using Tenantry.Pro;
using Tenantry.Pro.EfCore.SqlServer.Extensions;

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

        pro.WithMigrationOrchestration<string, AppDbContext>(
            cs => new AppDbContext(
                new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(cs).Options),
            runAtStartup: false);   // true migrates every tenant database before the app accepts traffic
    });
});
```

The factory receives a per-tenant connection string and returns a configured `DbContext`. The
returned context is disposed after each tenant's migration run, so build a fresh one each call.

## Running migrations

Inject `MigrationOrchestratorService<TKey, TContext>` from an admin endpoint or a background job:

```csharp
public sealed class MigrationAdminService(MigrationOrchestratorService<string, AppDbContext> orchestrator)
{
    // Migrate every tenant database. Per-tenant failures are captured in the report; cancelling ct stops the run.
    public Task<MigrationReport<string>> MigrateAllAsync(CancellationToken ct) =>
        orchestrator.MigrateAllAsync(ct);

    // Migrate a single named tenant. Returns a failed result rather than throwing on migration error.
    public Task<MigrationResult<string>> MigrateOneAsync(string tenantId, CancellationToken ct) =>
        orchestrator.MigrateTenantAsync(tenantId, ct);
}
```

### Run as a deployment step (recommended)

Migrate once per deployment, from one process, before the new version starts serving traffic: a CI/CD
stage, a Kubernetes `Job` or init container, or a `migrate` command in your application. The
database-per-tenant samples show the command form:

```csharp
var app = builder.Build();

// dotnet run -- migrate
if (args.Contains("migrate"))
{
    var report = await app.Services.GetRequiredService<MigrationOrchestratorService<string, AppDbContext>>()
        .MigrateAllAsync();
    return report.HasFailures ? 1 : 0;   // a non-zero exit code fails the deployment step
}

await app.RunAsync();
return 0;
```

### Run at startup

Passing `runAtStartup: true` registers a hosted service that calls `MigrateAllAsync` during startup,
before the app begins accepting requests. It lengthens boot time in proportion to the number of
tenants, runs on **every instance** (see below), and by default only logs failed tenants and starts
anyway. Add `failStartupOnMigrationError: true` to stop the application from starting instead, so an
instance never serves tenants whose schema it could not bring up to date:

```csharp
pro.WithMigrationOrchestration<string, AppDbContext>(
    cs => new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(cs).Options),
    runAtStartup: true,
    failStartupOnMigrationError: true);
```

Keep `runAtStartup` for single-instance deployments and development.

### Multiple instances

Tenantry takes **no lock** when it migrates. If several instances run `MigrateAllAsync` at the same time
(for example `runAtStartup: true` on every replica of a scaled-out app), each one migrates every tenant:

- **EF Core 9 and later** take a database lock while applying migrations on providers that support it,
  so concurrent runs against the same tenant database wait for each other and the second finds nothing
  pending. In our tests this held on SQL Server, PostgreSQL and MySQL (see
  [Tested combinations](database-providers.md#tested-combinations)).
- **EF Core 8**, which Tenantry.Pro's `net8.0` assets use, takes no lock. In our tests two runners
  raced on every provider: one applied the migration and the other reported that tenant as failed
  ("already exists"). The end state was correct and a rerun applied nothing, but on MySQL, whose DDL is
  not transactional, a race inside a larger migration can leave it half applied.

Either way, every instance repeats the whole sweep, so startup time grows with the number of instances
and tenants. With more than one instance, run migrations as a deployment step (above) and leave
`runAtStartup` off.

## Failure model

- **Sequential, not parallel.** Tenants are migrated one at a time to avoid overwhelming the database
  server under CI or production load.
- **Per-tenant isolation.** A failure migrating one tenant never aborts the others. `MigrateAllAsync`
  does not throw on a migration error — every per-tenant outcome is captured in the report.
- **Cancellation stops the run.** When the cancellation token is cancelled, `MigrateAllAsync` and
  `MigrateTenantAsync` throw `OperationCanceledException`: the tenant in progress is abandoned, the
  remaining tenants are not attempted (they are not reported as failures), and tenants already
  migrated stay migrated. A timeout inside one tenant's migration that does not come from your token is
  that tenant's failure and the run continues.
- **Licence check.** The licence is checked once, up front: a missing or invalid key throws
  `LicenseRequiredException` before any tenant is touched.

`MigrationReport<TKey>` aggregates the run:

| Member | Meaning |
|--------|---------|
| `Results` | One `MigrationResult<TKey>` per tenant |
| `Total` / `Succeeded` / `Failed` | Counts across the run |
| `HasFailures` | True if any tenant failed |

Each `MigrationResult<TKey>` carries `TenantId`, `Succeeded`, `AppliedMigrations` (names applied this
run), `Duration`, and `Error` (the exception on failure).

## Checking status without migrating

`MigrationStatusTracker<TKey, TContext>` is read-only and **requires no licence**, so it is safe for
dashboards and monitoring even when a licence has lapsed:

```csharp
public sealed class MigrationStatusService(MigrationStatusTracker<string, AppDbContext> tracker)
{
    public Task<IReadOnlyList<MigrationStatusEntry<string>>> AllAsync(CancellationToken ct) =>
        tracker.GetStatusAsync(ct);

    public Task<MigrationStatusEntry<string>> OneAsync(string tenantId, CancellationToken ct) =>
        tracker.GetTenantStatusAsync(tenantId, ct);
}
```

Each `MigrationStatusEntry<TKey>` exposes `AppliedMigrations`, `PendingMigrations`, and `IsUpToDate`.
The same information powers the [migration health check](health-checks.md).

## Limitations

- Targets the **database-per-tenant** path (one connection string per tenant).
- Tenantry.Pro executes your existing EF Core migrations; it does not generate them.
- Migrations use reflection and runtime code generation, so these APIs are annotated
  `[RequiresDynamicCode]`/`[RequiresUnreferencedCode]` and are not Native-AOT compatible.

## See also

- [Tenant lifecycle](tenant-lifecycle.md) — provision, migrate, and seed a new tenant in one call.
- [Health checks](health-checks.md) — surface pending migrations as a health probe.
