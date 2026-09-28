# Tenant lifecycle

Creating a new tenant usually means several steps in order: create its database or schema, apply
migrations, then seed initial data. `ITenantLifecycleManager<TKey>` runs that whole pipeline —
**provision → migrate → seed** — behind a single call, and reports exactly how far it got.

## The pipeline

```
ProvisionAsync(tenant)
   │
   ├─ 1. Provision infrastructure   (database or schema)   ← AddDatabaseProvisioning / AddSchemaProvisioning
   ├─ 2. Apply migrations           (EF Core)              ← WithMigrationOrchestration (database per tenant only)
   └─ 3. Seed data                  (your ITenantSeeder)   ← register an ITenantSeeder<TKey>
```

Each step runs **only if its service is registered**. Register provisioning but no seeder, and the
pipeline provisions and migrates, then stops cleanly. This lets you adopt the pipeline incrementally.

The manager does **not** persist the tenant to your store — persist the tenant first, then call
`ProvisionAsync` with its descriptor. The provisioning steps look the tenant up in the store by id, so a
tenant that is not there yet fails the first step.

## Registration

```csharp
using Microsoft.EntityFrameworkCore;
using Tenantry.Pro;
using Tenantry.Pro.Lifecycle;
using Tenantry.Pro.Lifecycle.Extensions;   // AddLifecycleManagement
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

        pro.AddDatabaseProvisioning();                       // step 1
        pro.WithMigrationOrchestration<string, AppDbContext>(// step 2
            cs => new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(cs).Options));

        pro.AddLifecycleManagement();                        // registers ITenantLifecycleManager
    });
});

// Step 3: register a seeder (optional).
builder.Services.AddScoped<ITenantSeeder<string>, MyTenantSeeder>();
```

`AddLifecycleManagement(opts => ...)` accepts a `TenantLifecycleOptions`:

| Option | Default | Meaning |
|--------|---------|---------|
| `StopOnFailure` | `true` | A failed step aborts the rest. Set `false` to attempt every step regardless; the result records the first error and the highest step reached. |

## Writing a seeder

Implement `ITenantSeeder<TKey>`. It is called inside a DI scope where all scoped services — your
`DbContext` included — are already tenant-aware, so reads and writes target the new tenant.

Make the seeder **idempotent**. Recovering from a failed pipeline means running it again (see
[When provisioning fails](#when-provisioning-fails)), which runs the seeder again, possibly after an
earlier attempt wrote some of its rows. Add each row only if it is missing, and back that up with a unique
index so a re-run can never insert a duplicate:

```csharp
public sealed class MyTenantSeeder : ITenantSeeder<string>
{
    public async Task SeedAsync(
        ITenantDescriptor<string> tenant,
        IServiceProvider scopedProvider,
        CancellationToken cancellationToken)
    {
        var db = scopedProvider.GetRequiredService<AppDbContext>();

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

Resolve services from the supplied `scopedProvider` — not from a captured root provider — so they see
the correct tenant.

## Running the pipeline

```csharp
public sealed class TenantOnboarding(
    ITenantLifecycleManager<string> lifecycle,
    IMyTenantRepository tenants)
{
    public async Task<TenantProvisioningResult<string>> CreateAsync(string id, string name, CancellationToken ct)
    {
        // 1. Persist the tenant to your store FIRST.
        var descriptor = new TenantDescriptor<string> { TenantId = id, Name = name };
        await tenants.AddAsync(descriptor, ct);

        // 2. Provision → migrate → seed.
        return await lifecycle.ProvisionAsync(descriptor, ct);
    }
}
```

A step that fails is captured in the result rather than thrown. `ProvisionAsync` throws in two cases:

- `LicenseRequiredException`, up front, if the licence is missing or out of grace (only under
  `LicenseEnforcement.Throw`), before any step runs.
- `OperationCanceledException` when the cancellation token is cancelled. The pipeline stops at once:
  steps that already completed are not undone, and later steps do not run. An
  `OperationCanceledException` that does *not* come from your token (for example an HTTP or command
  timeout inside a step) is an ordinary step failure and is captured in the result.

## Reading the result

`TenantProvisioningResult<TKey>`:

| Member | Meaning |
|--------|---------|
| `Succeeded` | True only if every configured step completed |
| `CompletedUpTo` | The last `TenantProvisioningStep` reached — `Complete` on success, or where it stopped/failed |
| `Error` | The first exception, or `null` |
| `Duration` | Wall-clock time for the whole pipeline |

`TenantProvisioningStep` values: `None`, `DatabaseProvisioned`, `SchemaProvisioned`,
`MigrationsApplied`, `DataSeeded`, `Complete`.

```csharp
var result = await lifecycle.ProvisionAsync(descriptor, ct);
if (!result.Succeeded)
    logger.LogError(result.Error,
        "Tenant {Id} provisioning stopped after {Step}", result.TenantId, result.CompletedUpTo);
```

## When provisioning fails

**Recover by fixing the cause and calling `ProvisionAsync` again** with the same descriptor. Every
built-in step is safe to repeat: creating the database or schema skips one that already exists, and
migrations apply only what is pending. Your seeder must be safe to repeat too (see
[Writing a seeder](#writing-a-seeder)). There is no separate "resume from step" call; running the whole
pipeline again is the resume.

**Nothing is rolled back.** Tenantry never drops a database or schema, reverts a migration, or deletes
seeded rows after a failure or a cancellation. What is left depends on how far the pipeline got (with the
default `StopOnFailure = true`):

| `CompletedUpTo` | Failed step | What is left behind | Before retrying |
|-----------------|-------------|---------------------|-----------------|
| `None` | Infrastructure (or no step was registered before the failing one) | Nothing Tenantry created. If `CREATE DATABASE`/`CREATE SCHEMA` itself failed, it either ran or it did not. | Fix the cause (permissions, server reachable, tenant in the store). |
| `DatabaseProvisioned` / `SchemaProvisioned` | Migrations | The database or schema, with the migrations that succeeded before the failing one. | See *Failed migrations* below. |
| `MigrationsApplied` | Seeding | The schema, up to date, plus whatever your seeder saved before it failed, unless it seeds in a transaction. | Nothing, if your seeder is idempotent. |
| `Complete` | — | A fully provisioned tenant. | — |

With no infrastructure provisioner or migrator registered, their steps are skipped and do not move
`CompletedUpTo`, so a seeding failure can report `None`. With `StopOnFailure = false`, later steps still
run after a failure; the result keeps the first error and the furthest step that succeeded.

**Failed migrations.** On SQL Server and PostgreSQL, EF Core applies each migration in a transaction, so a
failed migration leaves the earlier ones applied and none of its own changes; a retry applies it again.
**MySQL does not roll back DDL**: each schema statement commits on its own, so a migration that fails part
way can leave some of its tables or columns in place, and the retry then fails on them ("already
exists"). Inspect the database and finish or undo that migration's changes by hand before retrying.

**The tenant stays in your store.** A failed or cancelled pipeline does not remove the tenant, so requests
for it are still resolved and reach a database that may be missing tables or data. Keep a status on your
tenant (for example `Provisioning` → `Active`), set it to active only after `ProvisionAsync` succeeds, and
reject requests for tenants that are not active, for example with an access validator
(`tenant.ValidateTenantAccess(...)` in Tenantry core). Do not hide the tenant from the store while it is
provisioning: the steps look it up there.

The [TenantLifecycle sample](../samples/TenantLifecycle) walks through a failed seed and the retry that
completes it without duplicating anything.

## See also

- [Database per tenant](database-per-tenant.md) · [Schema per tenant](schema-per-tenant.md) — the provisioning step.
- [Migration orchestration](migration-orchestration.md) — the migration step.
- [Background jobs & non-HTTP hosts](background-jobs.md) — the seeding step uses a tenant scope created the same way.
