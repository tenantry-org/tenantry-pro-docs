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

Any other entity would be read and written across tenants, so a `Shared` tenant's context with one throws
`TenantIsolationViolationException` (`Kind` is `ModelConfiguration`) on its first query or save, naming the entities.
`Schema` and `Database` tenants have tables of their own and need neither. Tenantry.Pro.EfCore makes the check, once
any of its registration methods is called (`UseSchemaPerTenant`, `AddMigrations` and the others): the migration of
the shared database for `Shared` tenants is refused too.

## What honours it

- **Connection strings** come from your `UseConnectionStrings` delegate, as above: it returns the tenant's own
  database for `Database` tenants and the shared database for the others, so `AddDbContextPerTenantDatabase`
  connects every tenant's context to the right database.
- **Schemas** come from your `GetSchemaName` delegate, which `UseSchemaPerTenant` calls only for `Schema`
  tenants: their contexts' models get their schema, and the other tenants' the database's default. EF Core
  keeps a model per schema: each schema tenant's model has its schema, and `Database` and `Shared` tenants
  share the model without one.
- **Provisioning** ([Tenant lifecycle](tenant-lifecycle.md)) gives each tenant only its own infrastructure:
  `CreateDatabase` applies only to `Database` tenants, `CreateSchema` only to `Schema` tenants, and `Migrations` to
  both, in the tenant's database or schema. The steps that do not apply are reported as `Skipped`; a `Shared`
  tenant's database is migrated with the others (below). Your own steps see the tenant's isolation in
  `context.Isolation`, and decide in `AppliesTo`.
- **Offboarding** ([Tenant lifecycle](tenant-lifecycle.md#offboarding-a-tenant)) removes only the tenant's own:
  `DropDatabase` applies only to `Database` tenants, `DropSchema` only to `Schema` tenants, and `DeleteSharedData` to
  `Shared` tenants, to `Schema` tenants' rows in a context schema per tenant leaves in the shared schema, and to
  `Database` tenants' rows in a context whose database `AddDatabaseDeprovisioning` does not drop.
- **Migrations** ([Tenant migrations](migration-orchestration.md)) and the migration
  [health check](health-checks.md) work per distinct database or schema: each `Database` tenant's database, each
  `Schema` tenant's schema, and the shared database's default schema once for all the `Shared` tenants. The
  connectivity check works per database: the shared database once, for the `Schema` and `Shared` tenants together.

## Limitations

- Mixed deployments are harder to operate, migrate and reason about than a single isolation: document which
  tenants get which, and why.

## See also

- [Database per tenant](database-per-tenant.md) · [Schema per tenant](schema-per-tenant.md)
- [Tenant lifecycle](tenant-lifecycle.md)
