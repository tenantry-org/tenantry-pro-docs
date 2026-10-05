# Mixed mode

Use mixed mode when different tenants need different isolation in one application: some on their own
database, some on their own schema in a shared database, and some sharing tables. You say where each
tenant's data lives with a delegate, evaluated from the tenant's descriptor.

## Registration

```csharp
using Microsoft.EntityFrameworkCore;
using Tenantry;
using Tenantry.Pro;

// Some tenants get a database of their own; everyone else gets a schema in the shared database. Tenant is the
// application's own tenant type, which its store returns; DedicatedDatabase is set when the tenant is created and
// never changes, because its data stays where it was first put.
static TenantIsolation IsolationOf(ITenantDescriptor<string> t) =>
    t.As<Tenant>().DedicatedDatabase ? TenantIsolation.Database : TenantIsolation.Schema;

builder.Services.AddTenantry<string>(tenant =>
{
    tenant.ResolveFromHeader("X-Tenant-Id");
    tenant.UseStore<MyTenantStore>();

    // Each tenant's connection string: its own database, or the shared one.
    tenant.UseConnectionStrings(opts => opts.GetConnectionString = t => IsolationOf(t) == TenantIsolation.Database
        ? $"Server=.;Database=app_{t.TenantId};Integrated Security=true;TrustServerCertificate=True"
        : sharedConnectionString);

    tenant.UsePro(pro =>
    {
        pro.UseMixedMode(opts => opts.GetIsolation = IsolationOf);

        // The schema of each schema tenant; the others keep the database's default.
        pro.UseSchemaPerTenant(opts => opts.GetSchemaName = t => $"tenant_{t.TenantId}");

        // Provisioning: a database for database tenants, a schema for schema tenants, then the migrations.
        pro.AddDatabaseProvisioning<AppDbContext>()
            .AddSchemaProvisioning<AppDbContext>()
            .AddMigrations<AppDbContext>();
    });

    // Each tenant's context connects to its database, and schema tenants' models get their schema.
    tenant.AddDbContextPerTenantDatabase<AppDbContext>((_, options) => options.UseSqlServer());
});
```

`GetIsolation` receives the tenant as `ITenantDescriptor<TKey>`, so you can branch on its id or name, or read your
own tenant type's properties with Tenantry Core's `As<TTenant>()`
([your own tenant type](https://github.com/tenantry-org/tenantry-core/blob/master/docs/core-concepts.md#your-own-tenant-type)).
It is called often, so keep it fast, and it must give the same answer for a tenant every time: moving a tenant to
another isolation means moving its data, which Tenantry does not do. So branch on something fixed when the tenant
is created, not on something that changes, such as its plan.

`TenantIsolation` has three values:

| Value | Where the tenant's data lives |
|-------|-------------------------------|
| `Shared` | The shared database and schema, kept apart from other tenants' rows only by Tenantry's query filters ([Shared tenants](#shared-tenants)). |
| `Schema` | Its own schema of the shared database ([Schema per tenant](schema-per-tenant.md)). Needs `UseSchemaPerTenant`: without it, reading such a tenant's isolation throws `InvalidOperationException`. |
| `Database` | Its own database ([Database per tenant](database-per-tenant.md)). |

## Shared tenants

Tenantry's query filters apply only to entities that implement `ITenantEntity<TKey>`. So every entity of a context
a `Shared` tenant uses must implement it, or be marked as shared by every tenant, with `[SharedAcrossTenants]` or
`modelBuilder.Entity<Country>().IsSharedAcrossTenants()` (Tenantry Core):

```csharp
using Tenantry.EfCore;

public sealed class Order : TenantEntity<string>   // a tenant's own rows, filtered by TenantId
{
    public int Id { get; set; }
}

[SharedAcrossTenants]                              // the same rows for every tenant
public sealed class Country
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}
```

Tenantry Core takes any other entity type as shared by every tenant, and by default checks nothing
([`OnUnmarkedEntityType`](https://github.com/tenantry-org/tenantry-core/blob/master/docs/efcore-integration.md#entity-types-that-are-not-tenant-owned)).
In mixed mode that type means two things: a `Schema` or `Database` tenant has its rows to itself, in its own schema or
database, while the `Shared` tenants all read and write the same rows. So Tenantry.Pro.EfCore refuses it for a
`Shared` tenant: the context throws `TenantIsolationViolationException` (`Kind` is `ModelConfiguration`) on its first
query or save, naming the entity types, and the migration of the shared database for `Shared` tenants is refused too.
It does so whatever the context's `OnUnmarkedEntityType` is, as that option applies to every tenant of the context.
Pro makes the check once any of its registration methods is called (`UseSchemaPerTenant`, `AddMigrations` and the
others).

Which tenants are `Shared` is known only once a tenant is resolved, so the check cannot refuse anything as the host
starts. Instead, the host logs a warning for each registered context with such entity types, naming them
(`SharedTenantsRefusedContext`, [Telemetry](telemetry.md)), unless the warning is filtered or cannot be written. To find
them, it creates each registered context that uses `UseTenantry()` and builds its model as it starts, so code in the
context's options that connects to the database runs then too. A context it cannot create without a tenant, or one that
calls `UseTenantry()` only in its `OnConfiguring`, is logged the first time a tenant uses it. Implement
`ITenantEntity<TKey>` on each type that belongs to a tenant, and mark the others `[SharedAcrossTenants]`. An application
whose tenants all have a database or schema of their own can mark them too, or leave the warning out of its logs with
`builder.Logging.AddFilter("Tenantry.Pro.MixedMode", LogLevel.Error)`, which silences it for every context.

## What honours it

- Connection strings come from your `UseConnectionStrings` delegate, as above: it returns the tenant's own database
  for `Database` tenants and the shared database for the others, so `AddDbContextPerTenantDatabase` connects every
  tenant's context to the right database.
- Schemas come from your `GetSchemaName` delegate, which `UseSchemaPerTenant` calls only for `Schema` tenants.
  `Database` and `Shared` tenants use the database's default schema and share one compiled model.
- Provisioning ([Tenant lifecycle](tenant-lifecycle.md)) gives each tenant only its own infrastructure:
  `CreateDatabase` applies only to `Database` tenants, `CreateSchema` only to `Schema` tenants, and `Migrations` to
  both, in the tenant's database or schema. The steps that do not apply are reported as `Skipped`; a `Shared`
  tenant's database is migrated with the others (below). Your own steps see the tenant's isolation in
  `context.Isolation`, and decide in `AppliesTo`.
- Offboarding removes only the tenant's own: `DropDatabase` for `Database` tenants, `DropSchema` for `Schema`
  tenants, and `DeleteSharedData` for the tenant's rows in a context that is not dropped with it
  ([which tenants each step applies to](tenant-lifecycle.md#offboarding-a-tenant)).
- Migrations ([Tenant migrations](migration-orchestration.md)) and the migration
  [health check](health-checks.md) work per distinct database or schema: each `Database` tenant's database, each
  `Schema` tenant's schema, and the shared database's default schema once for all the `Shared` tenants. The
  connectivity check works per database: the shared database once, for the `Schema` and `Shared` tenants together.

## Limitations

- Mixed deployments are harder to operate, migrate and reason about than a single isolation: document which
  tenants get which, and why.

## See also

- [Database per tenant](database-per-tenant.md) · [Schema per tenant](schema-per-tenant.md)
- [Tenant lifecycle](tenant-lifecycle.md)
