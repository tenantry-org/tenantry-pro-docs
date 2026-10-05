# Tenant lifecycle

Creating a new tenant usually means several steps in order: create its database or schema, apply
migrations, then seed initial data. `ITenantProvisioner<TKey>` runs those steps behind a single call
and reports what happened to each one. Removing a tenant is the same in reverse:
`ITenantDeprovisioner<TKey>` runs your export or archive steps, then drops the tenant's database or schema, or
deletes its rows from a shared database, and reports each step (see [Offboarding a tenant](#offboarding-a-tenant)).

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

The provisioner does not add the tenant to your store: add it first, then call `ProvisionAsync` with its
descriptor.

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
seeder is resolved from a new scope for the tenant being provisioned, so the services its constructor takes, your
`DbContext` included, already see the new tenant.
Every seeder you add runs, with your steps, in the order you add them. A seeder registered only with
`AddScoped<ITenantSeeder<TKey>, T>()` is not run.

Make the seeder idempotent. Recovering from failed provisioning means running it again (see
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

For anything else a new tenant needs, such as granting a login access to its database, creating a storage bucket or
registering it with a billing system, implement `ITenantProvisioningStep<TKey>` and add it with
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
  `TenantIsolation`, or `Schema` without `UseSchemaPerTenant`, and whatever `GetIsolation` itself throws.
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

Each `TenantLifecycleStepResult` has the step's `Name` (`CreateDatabase`, `CreateSchema`, `Migrations`, or
the type name of your step or seeder), its `Status`, its `Error` and its `Duration`. The statuses are
`Succeeded`, `Failed`, `Skipped` (the step does not apply to the tenant) and `NotRun` (an earlier step failed).

```csharp
var result = await provisioner.ProvisionAsync(descriptor, ct);
if (!result.Succeeded)
    logger.LogError(result.Error, "Provisioning tenant {Id} failed at {Step}",
        result.TenantId, result.Steps.First(step => step.Status == TenantLifecycleStepStatus.Failed).Name);
```

## When provisioning fails

Fix the cause and call `ProvisionAsync` again with the same descriptor. Every built-in step is safe to repeat:
creating the database or schema skips one that already exists, and migrations apply only what is pending. Your
seeders and steps run again too, which is why they must be idempotent. There is no separate "resume from step" call;
running every step again is the resume.

Two runs at once for one tenant (a double submit, a redelivered message) both get past creating the database or
schema. Depending on the EF Core version, the migration step may fail in one of them
([Multiple instances](migration-orchestration.md#multiple-instances)). Your seeders and steps must tolerate a
concurrent run, for example by treating a unique-key violation as already seeded. It is simpler to deduplicate the
request so that only one onboarding runs per tenant.

Nothing is rolled back: Tenantry never drops a database or schema, reverts a migration, or deletes seeded rows after a
failure or a cancellation. What is left depends on which step failed (with the default `StopOnFailure = true`):

| Failed step | What is left behind | Before retrying |
|-------------|---------------------|-----------------|
| `CreateDatabase` / `CreateSchema` | Nothing Tenantry created. If `CREATE DATABASE`/`CREATE SCHEMA` itself failed, it either ran or it did not. | Fix the cause (permissions, server reachable). |
| `Migrations` | The database or schema, with any migrations applied before the failure. | Check the tenant's status (below). |
| A seeder or your step | The schema, up to date, plus whatever your seeders and steps did before it failed, unless they work in a transaction. | Nothing, if they are idempotent. |

With `StopOnFailure = false`, later steps still run after a failure; each step's outcome is in `Steps`.

What a failed migration leaves applied depends on the EF Core version and database
([Tenant migrations](migration-orchestration.md#failure-model)). Check the tenant with `GetTenantStatusAsync` before
retrying; on MySQL, finish or undo a half-applied migration by hand first.

Failed or cancelled provisioning does not remove the tenant from your store, so requests for it are still resolved
and reach a database that may be missing tables or data. Keep a status on your tenant (for example `Provisioning`,
`Active`, `Suspended`), set it to active only after `ProvisionAsync` succeeds, and refuse the others with Tenantry
Core's `ValidateTenantActivity`, which stops their requests and their background work
([Suspended tenants](background-jobs.md#suspended-tenants)). Keep every tenant in the store, whatever its status
([why](migration-orchestration.md#which-tenants-are-migrated)).

The [TenantLifecycle sample](../samples/Tenantry.Pro.Samples.TenantLifecycle) onboards tenants from a catalog
database this way: it adds each tenant as provisioning, provisions it, and makes it active, then removes it from the
tenant cache. It walks through a failed seed and the retry that completes it without duplicating anything, and
suspends a tenant.

## Offboarding a tenant

`ITenantDeprovisioner<TKey>.DeprovisionAsync(tenant)` removes what a tenant has, in this order:

```
DeprovisionAsync(tenant)
   │
   ├─ your steps                  ← AddDeprovisioningStep<T>, in the order you add them: export, archive, notify
   ├─ DeleteSharedData            ← AddSharedDataDeletion<TContext>: one transaction, rolled back on failure
   ├─ DropDatabase / DropSchema   ← AddDatabaseDeprovisioning<TContext> / AddSchemaDeprovisioning<TContext>
   └─ ClearCaches                 ← always added, not run after a failed step: everything Tenantry keeps for the tenant
```

```csharp
tenant.UsePro(pro => pro
    .AddDeprovisioningStep<ExportTenantData>()                                 // yours, first
    .AddDatabaseDeprovisioning<AppDbContext>());                               // DropDatabase, after it
```

Your steps implement `ITenantDeprovisioningStep<TKey>`, which is shaped like a provisioning step: it is resolved
from a scope for the tenant, `AppliesTo` decides whether it runs, and `context.Isolation` gives its isolation in
mixed mode. They run while the tenant's data is still there. `UsePro` always registers
`ITenantDeprovisioner<TKey>`, as a singleton.

- Before any step, `DeprovisionAsync` clears this instance's cached copy of the tenant and reads the store again. If
  the store has the tenant and it is active, it throws `InvalidOperationException` and runs nothing. So first suspend
  the tenant, which needs a `ValidateTenantActivity` validator (without one every tenant is active), or remove it
  from the store.
- `DropDatabase` and `DropSchema` are steps only with `AddDatabaseDeprovisioning` or `AddSchemaDeprovisioning`;
  provisioning does not add them. They also drop a database or schema created outside Tenantry.
- The first failed step stops offboarding: the steps after it, `ClearCaches` included, are reported as `NotRun`, so a
  failed export never lets a drop run. There is no `StopOnFailure` setting for offboarding. Call
  `ITenantInvalidator<TKey>.InvalidateAsync` yourself if you need the caches cleared after a failure.
- A database or schema another tenant's context reaches is not dropped. Before dropping, the step creates every
  other tenant's context and reads the database it connects to, so `Database` and `Initial Catalog` are the same. When
  another tenant's database has the same name on a server written another way (`tcp:db,1433` and `db`, `localhost`
  and `127.0.0.1`), the step asks both databases which they are: SQL Server's database GUID, PostgreSQL's
  `system_identifier` (from `pg_control_system()`), MySQL's `server_uuid`. If they match, or either cannot answer, the
  drop is refused. SQL Server's and PostgreSQL's are the same on a physical replica (an availability group's
  secondary, a streaming standby) as on its primary. MySQL servers each have a `server_uuid` of their own, replicas and
  Group Replication or InnoDB Cluster members included, and `DROP DATABASE` on one replicates to the others, directly
  or through a chain (A to B to C), so a tenant that reaches the database through another of them would lose its data
  too. Tenantry cannot read every chain, so when the `server_uuid`s differ the drop is refused. If no MySQL server
  the tenants reach replicates another, say so with `o.IndependentMySqlServers = true` in
  `AddDatabaseDeprovisioning<TContext>(o => …)`, and such a drop goes ahead. Set it only when that holds: otherwise
  the other tenant's data goes with the leaving tenant's. MariaDB has no `@@server_uuid`, so asking which database it
  is fails, and on MariaDB such a drop is always refused. For a schema, another tenant uses it if its own schema has that name, or if its model maps anything
  (tables, views, sequences, migration history) into it, directly or through the connection's default schema. The
  database's default schema (`dbo`, `public`) is never dropped. If another tenant uses it, or its context cannot be
  created, or what it uses cannot be read, the step fails and nothing is dropped.
- Running it again is safe. A database or schema that no longer exists counts as dropped, and deleting rows
  again finds none. Recover from a failure by fixing the cause and calling `DeprovisionAsync` again. Your steps run
  again too. When every database or schema offboarding drops for the tenant is already gone, `context.DataDropped`
  is `true`: your steps ran before those drops, so a step that reads the data should return without doing anything.
  It is `false` while any of them exists, so with more than one drop a step that runs again may find some of them
  gone, and `false` when offboarding drops nothing for the tenant. Rows deleted from a shared database are not detected, so an export that runs again should not replace an
  earlier one.
- The login needs permission to drop what it removes. `o.CreateContext`, in `AddDatabaseDeprovisioning` or
  `AddSchemaDeprovisioning`, gives the step a context with another login.

  | Database | To drop a database | To drop a schema |
  |----------|--------------------|------------------|
  | SQL Server | `ALTER ANY DATABASE`, or `CONTROL` on the database | `CONTROL` on the schema, or `ALTER ANY SCHEMA` |
  | PostgreSQL | Ownership of the database (the login that created it), or superuser | Ownership of the schema, membership in its owner's role, or superuser |
  | MySQL | `DROP` | No schemas apart from databases |

`DropDatabase` drops the database through the EF Core provider's database creator, which first releases the
connections this process has pooled to it. On SQL Server, EF Core also ends the other sessions in the database. On
PostgreSQL, a connection another instance of the application holds makes the drop fail; offboard again once it
closes.

`DropSchema` drops the schema's foreign keys, tables (the migration history among them) and sequences, then the
schema, in one transaction. It reads what the schema holds from the database's own catalogue (`pg_catalog` on
PostgreSQL, the `sys` views on SQL Server): PostgreSQL's `INFORMATION_SCHEMA` does not show a login a schema it has
no privilege on, nor the tables of a schema it does not own, and SQL Server's does not show a login the tables it may
not use. When the login may not drop the schema, the step fails and says which right it needs. Views, functions,
procedures, types, collations, text search configurations and anything else that is not a table or a sequence are
not dropped: when the schema holds any, the step fails, names them, and the transaction puts the tables back. Drop
them in a step of your own, which runs first. The step also fails, before it drops anything, when a table in another
schema has a foreign key to one of the schema's tables, naming that table, and on PostgreSQL when the login does not
own a table with a foreign key: the schema's owner may drop a table, but only the table's owner may drop its foreign
keys. SQL Server shows a login only the tables it may use, so a referencing table the login cannot see is not named:
dropping the schema's tables then fails, with a message that says so and the server's own, and the transaction puts
back what was dropped.

`DeleteSharedData` (`pro.AddSharedDataDeletion<TContext>()`) is for a tenant in a shared database. In one
transaction, it deletes the tenant's rows from every table of `TContext` whose entity implements
`ITenantEntity<TKey>`, referencing rows first, and rows that cascade from them go too. It fails and deletes nothing
when a table that is not tenant-owned references a tenant's row, when tables reference each other in a cycle, or
when `TContext` has no tenant-owned table. Rows in tables without a tenant id stay: delete them in a step of your
own.

`ClearCaches` runs last, and invalidates the tenant (`ITenantInvalidator<TKey>.InvalidateAsync`): its cached descriptor
and connection string, its Tenantry.Caching entries, cached responses and options. Each instance of the application
has its own caches: this clears the instance that offboards, and the others too when Tenantry Core broadcasts
invalidations ([Several instances](https://github.com/tenantry-org/tenantry-core/blob/master/docs/tenant-stores.md#several-instances)).

In [mixed mode](mixed-mode.md):

- `DropDatabase` applies only to `Database` tenants, and `DropSchema` only to `Schema` tenants.
- `DeleteSharedData` applies to `Shared` tenants; to `Schema` tenants when schema per tenant does not give `TContext`
  the tenant's schema (a context over reference data that `SchemaPerTenantOptions.Contexts` does not list); and to
  `Database` tenants unless `AddDatabaseDeprovisioning<TContext>` drops that context's database. Those tenants' rows
  in such a context are beside the shared tenants'.

### Removing a tenant

Suspend the tenant, offboard it while it is still in your store, then remove it:

```csharp
public sealed class TenantRemoval(
    ITenantDeprovisioner<string> deprovisioner, IMyTenantRepository tenants, ITenantInvalidator<string> invalidator)
{
    public async Task<TenantDeprovisioningResult<string>> RemoveAsync(Tenant tenant, CancellationToken ct)
    {
        // 1. Stop its requests and background work: ValidateTenantActivity refuses a suspended tenant.
        await tenants.SuspendAsync(tenant.TenantId, ct);
        await invalidator.InvalidateAsync(tenant.TenantId, ct);

        // 2. Export, drop, clear the caches.
        var result = await deprovisioner.DeprovisionAsync(tenant, ct);

        // 3. Remove it from the store only once its data is gone.
        if (result.Succeeded)
            await tenants.DeleteAsync(tenant.TenantId, ct);

        return result;
    }
}
```

`InvalidateAsync` clears this instance's cache, and every other instance's when Tenantry Core broadcasts
invalidations with `tenant.BroadcastInvalidations(...)` ([Several instances](https://github.com/tenantry-org/tenantry-core/blob/master/docs/tenant-stores.md#several-instances)). Without that, with
`CacheTenants` and several instances, the others serve the tenant as active until their copy expires
(`TenantStoreCacheOptions.Duration`), so call `DeprovisionAsync` after that long, from a job or a queue. A job or message that started before
the suspension can still be running.

`DeprovisionAsync` returns a `TenantDeprovisioningResult<TKey>`, with the same members as the provisioning result
(`Succeeded`, `Steps`, `Error`, `Duration`). It throws in the same cases as `ProvisionAsync`: a reserved tenant id,
a missing or invalid licence, a mixed mode `GetIsolation` that fails, and your cancellation token, and also for an
active tenant (above).

## See also

- [Database per tenant](database-per-tenant.md) and [Schema per tenant](schema-per-tenant.md): creating the database
  or schema.
- [Tenant migrations](migration-orchestration.md): the migration step, and migrating every tenant.
- [Mixed mode](mixed-mode.md): which steps apply to which tenants.
- [Background jobs & non-HTTP hosts](background-jobs.md): steps and seeders use a tenant scope created the same way.
