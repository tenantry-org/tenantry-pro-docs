# Tenant lifecycle

Creating a new tenant usually means several steps in order: create its database or schema, apply
migrations, then seed initial data. `ITenantProvisioner<TKey>` runs those steps behind a single call
and reports what happened to each one.

## The steps

```
ProvisionAsync(tenant)
   │
   ├─ CreateDatabase / CreateSchema   ← AddDatabaseProvisioning<TContext> / AddSchemaProvisioning<TContext>
   ├─ Migrations                      ← AddMigrations<TContext>, in the tenant's database or schema
   └─ your steps and seeders          ← AddProvisioningStep<T> / AddSeeder<T>, in the order you add them
```

Tenantry's own steps always run first, in that order, whatever the order of your calls. The methods that take a
context type (`AddDatabaseProvisioning<TContext>()`, `AddSchemaProvisioning<TContext>()`, `AddMigrations<TContext>()`)
return the builder without its key type, so call them after the others in a chain. Provisioning runs
only the steps you register: register provisioning and migrations but no seeder, and it creates and
migrates the database, then stops. With no steps, it succeeds and does nothing. `UsePro` always registers
`ITenantProvisioner<TKey>`, as a singleton.

The provisioner does **not** add the tenant to your store — add the tenant first, then call
`ProvisionAsync` with its descriptor.

## Registration

```csharp
using Microsoft.EntityFrameworkCore;
using Tenantry;
using Tenantry.Pro;
builder.Services.AddTenantry<string>(tenant =>
{
    tenant.ResolveFromHeader("X-Tenant-Id");
    tenant.UseStore<MyTenantStore>();
    tenant.UseConnectionStrings(opts =>
        opts.GetConnectionString = t =>
            $"Server=.;Database=app_{t.TenantId};Integrated Security=true;TrustServerCertificate=True");

    tenant.UsePro(pro => pro
        .AddSeeder<MyTenantSeeder>()                  // your seeder, last
        .AddDatabaseProvisioning<AppDbContext>()      // CreateDatabase, which runs first
        .AddMigrations<AppDbContext>());              // Migrations, after it

    tenant.AddDbContextPerTenantDatabase<AppDbContext>((sp, options) => options.UseSqlServer());
});
```

`pro.ConfigureProvisioning(o => ...)` sets `TenantProvisioningOptions`:

| Option | Default | Meaning |
|--------|---------|---------|
| `StopOnFailure` | `true` | A failed step stops provisioning; the steps after it are reported as `NotRun`. Set `false` to run every step regardless; the result keeps each step's outcome. |

## Writing a seeder

Implement `ITenantSeeder<TKey>` and add it with `pro.AddSeeder<T>()`, which registers it as a scoped service. Each
seeder is resolved from a new scope for the tenant being provisioned, so the services it takes in its
constructor — your `DbContext` included — are already tenant-aware, and reads and writes target the new tenant.
Every seeder you add runs, with your steps, in the order you add them. A seeder registered only with
`AddScoped<ITenantSeeder<TKey>, T>()` is not run.

Make the seeder **idempotent**. Recovering from failed provisioning means running it again (see
[When provisioning fails](#when-provisioning-fails)), which runs the seeder again, possibly after an
earlier attempt wrote some of its rows. Add each row only if it is missing, and back that up with a unique
index so a re-run can never insert a duplicate:

```csharp
public sealed class MyTenantSeeder(AppDbContext db) : ITenantSeeder<string>
{
    public async Task SeedAsync(ITenantDescriptor<string> tenant, CancellationToken cancellationToken)
    {
        if (!await db.Roles.AnyAsync(r => r.Name == "Owner", cancellationToken))
            db.Roles.Add(new Role { Name = "Owner" });

        if (!await db.Settings.AnyAsync(s => s.Key == "DisplayName", cancellationToken))
            db.Settings.Add(new Setting { Key = "DisplayName", Value = tenant.Name });

        await db.SaveChangesAsync(cancellationToken);
    }
}

// In AppDbContext.OnModelCreating: one row per role name and per setting key.
modelBuilder.Entity<Role>().HasIndex(r => r.Name).IsUnique();
modelBuilder.Entity<Setting>().HasIndex(s => s.Key).IsUnique();
```

If the seed must be all-or-nothing, wrap it in a transaction
(`await using var tx = await db.Database.BeginTransactionAsync(ct)` … `await tx.CommitAsync(ct)`), so a
failure leaves nothing behind. Keep it idempotent anyway: a failure after the commit (a timeout reading
the result, say) still reruns it.

## Writing a step

For anything else a new tenant needs — granting a login access to its database, creating a storage bucket,
registering it with a billing system — implement `ITenantProvisioningStep<TKey>` and add it with
`pro.AddProvisioningStep<T>()`. Like a seeder, it is resolved from a scope for the tenant, and it must be
idempotent. `AppliesTo` decides whether it runs for a tenant; by default it runs for every tenant. In
[mixed mode](mixed-mode.md), `context.Isolation` says where the tenant's data lives. The step is created before
`AppliesTo` is called, for every tenant, so resolve a service that only some tenants can have (a context for a
database of their own, say) in `ExecuteAsync`, from `context.Scope`:

```csharp
// Run by a provisioning process that connects as an admin login: gives the application's login, app, access to
// each new tenant database.
public sealed class GrantDatabaseAccess : ITenantProvisioningStep<string>
{
    // Only tenants with a database of their own.
    public bool AppliesTo(TenantProvisioningContext<string> context) =>
        context.Isolation is null or TenantIsolation.Database;

    public async Task ExecuteAsync(TenantProvisioningContext<string> context, CancellationToken cancellationToken)
    {
        var db = context.Scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.ExecuteSqlRawAsync(
            "IF USER_ID('app') IS NULL CREATE USER app FOR LOGIN app; ALTER ROLE db_datawriter ADD MEMBER app;",
            cancellationToken);
    }
}
```

A step that does not apply is reported as `Skipped`.

## Provisioning a tenant

```csharp
public sealed class TenantOnboarding(
    ITenantProvisioner<string> provisioner,
    IMyTenantRepository tenants)
{
    public async Task<TenantProvisioningResult<string>> CreateAsync(string id, string name, CancellationToken ct)
    {
        // 1. Add the tenant to your store FIRST, not yet active (Tenant is your descriptor type; see below).
        var descriptor = new Tenant { TenantId = id, Name = name, IsActive = false };
        await tenants.AddAsync(descriptor, ct);

        // 2. Create the database, migrate it, seed it.
        var result = await provisioner.ProvisionAsync(descriptor, ct);

        // 3. Serve the tenant only once it is fully provisioned.
        if (result.Succeeded)
            await tenants.ActivateAsync(id, ct);

        return result;
    }
}
```

A step that fails is reported in the result rather than thrown. `ProvisionAsync` throws in these cases:

- `ArgumentException`, up front, for a tenant whose id Tenantry reserves for "no tenant": the key type's default
  (`Guid.Empty`, `0`) or an empty string. No tenant can have it.
- `LicenseRequiredException`, up front, if the licence key is missing or invalid, before any step runs.
- In mixed mode, up front: `InvalidOperationException` if `GetIsolation` returns a value that is not a
  `TenantIsolation`, and whatever `GetIsolation` itself throws.
- `OperationCanceledException` when the cancellation token is cancelled. Provisioning stops: no further step
  starts, and steps that already completed are not undone. A step that fails after you cancelled, however it
  reports it (a provider's "operation cancelled" error, say), is reported this way, with its exception as the
  inner exception. An `OperationCanceledException` that does *not* come from your token (for example an HTTP or
  command timeout inside a step) is an ordinary step failure and is reported in the result.

## Reading the result

`TenantProvisioningResult<TKey>`:

| Member | Meaning |
|--------|---------|
| `Succeeded` | True if no step failed |
| `Steps` | Every registered step's outcome, in the order the steps run |
| `Error` | The exception the first failed step threw, or `null` |
| `Duration` | Wall-clock time for the whole call |

Each `TenantProvisioningStepResult` has the step's `Name` (`CreateDatabase`, `CreateSchema`, `Migrations`, or
the type name of your step or seeder), its `Status`, its `Error` and its `Duration`. The statuses are
`Succeeded`, `Failed`, `Skipped` (the step does not apply to the tenant) and `NotRun` (an earlier step failed).

```csharp
var result = await provisioner.ProvisionAsync(descriptor, ct);
if (!result.Succeeded)
    logger.LogError(result.Error, "Provisioning tenant {Id} failed at {Step}",
        result.TenantId, result.Steps.First(step => step.Status == TenantProvisioningStepStatus.Failed).Name);
```

## When provisioning fails

**Recover by fixing the cause and calling `ProvisionAsync` again** with the same descriptor. Every
built-in step is safe to repeat: creating the database or schema skips one that already exists, and
migrations apply only what is pending.

**Two runs at once** for the same tenant (a double submit, a redelivered message) both get past creating the
database or schema: that is safe to run concurrently on every provider. The
migration step is not always: EF Core 9 and later make the second run wait for the first on SQL Server and
MySQL, but on PostgreSQL with EF Core 10 and later, and on every provider with EF Core 8, one run can fail
with "already exists" on a migration the other applied. Your seeders and steps must also tolerate a concurrent
run, for example by treating a unique-key violation as "already seeded". Better, let only one onboarding run
per tenant (deduplicate the request or the message); otherwise a failed run is repaired by running
`ProvisionAsync` again. Your seeders must be safe to repeat too (see
[Writing a seeder](#writing-a-seeder)). There is no separate "resume from step" call; running every step
again is the resume.

**Nothing is rolled back.** Tenantry never drops a database or schema, reverts a migration, or deletes
seeded rows after a failure or a cancellation. What is left depends on which step failed (with the
default `StopOnFailure = true`):

| Failed step | What is left behind | Before retrying |
|-------------|---------------------|-----------------|
| `CreateDatabase` / `CreateSchema` | Nothing Tenantry created. If `CREATE DATABASE`/`CREATE SCHEMA` itself failed, it either ran or it did not. | Fix the cause (permissions, server reachable). |
| `Migrations` | The database or schema, with any migrations applied before the failure (see below). | See *Failed migrations* below. |
| A seeder or your step | The schema, up to date, plus whatever your seeders and steps did before it failed, unless they work in a transaction. | Nothing, if they are idempotent. |

With `StopOnFailure = false`, later steps still run after a failure; each step's outcome is in `Steps`.

**Failed migrations.** On SQL Server and PostgreSQL, EF Core 8 and EF Core 10 or later apply each migration
in its own transaction, so a failed migration leaves the earlier ones applied and none of its own changes;
a retry applies it again. On those two, EF Core 9, which Tenantry.Pro's `net9.0` build uses, applies all
pending migrations in one transaction, so a failure usually leaves none of them applied (a migration
containing a `suppressTransaction: true` statement commits what ran before it). The provisioning result does not
say which migrations were committed: check the tenant with `ITenantMigrationRunner.GetTenantStatusAsync`. To get a
list per run, migrate with `MigrateTenantAsync`, whose result lists what that run applied (see
[Tenant migrations](migration-orchestration.md#failure-model)).
**MySQL does not roll back DDL**: each schema statement commits on its own, so the migrations before the
failing one stay applied on every EF Core version, and a migration that fails part way can leave some of
its tables or columns in place, and the retry then fails on them ("already
exists"). Inspect the database and finish or undo that migration's changes by hand before retrying.

**The tenant stays in your store.** Failed or cancelled provisioning does not remove the tenant, so requests
for it are still resolved and reach a database that may be missing tables or data. Keep a status on your
tenant (for example `Provisioning` → `Active` → `Suspended`), set it to active only after `ProvisionAsync`
succeeds, and reject requests for tenants that are not active with an access validator
(`tenant.ValidateTenantAccess(...)`; see
[Suspended and inactive tenants](https://github.com/tenantry-org/tenantry-core/blob/master/docs/tenant-stores.md#suspended-and-inactive-tenants) in Tenantry
core). Validators run only for HTTP requests, so background work must check the status itself (see
[Background jobs](background-jobs.md#suspended-tenants)).

Never hide a tenant from the store, whatever its status (provisioning, active or suspended):
`MigrateTenantAsync` looks it up there, and migration runs, migration status and the health checks cover
only the tenants the store lists. A tenant hidden while it is suspended misses every migration and fails
when it is reactivated.

The [TenantLifecycle sample](../samples/Tenantry.Pro.Samples.TenantLifecycle) onboards tenants from a catalog
database this way: it adds each tenant as provisioning, provisions it, and makes it active, then removes it from the
tenant cache. It walks through a failed seed and the retry that completes it without duplicating anything, and
suspends a tenant.

## See also

- [Database per tenant](database-per-tenant.md) · [Schema per tenant](schema-per-tenant.md) — creating the database or schema.
- [Tenant migrations](migration-orchestration.md) — the migration step, and migrating every tenant.
- [Mixed mode](mixed-mode.md) — which steps apply to which tenants.
- [Background jobs & non-HTTP hosts](background-jobs.md) — steps and seeders use a tenant scope created the same way.
