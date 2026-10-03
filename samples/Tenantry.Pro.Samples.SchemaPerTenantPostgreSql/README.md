# Tenantry.Pro.Samples.SchemaPerTenantPostgreSql

Schema-per-tenant isolation on **PostgreSQL**: one shared database, one schema per tenant, provisioned on
demand.

Demonstrates:

- `pro.UseSchemaPerTenant(...)`, with the schema name derived from the tenant: `AppDbContext`, which uses
  `UseTenantry()`, gets the current tenant's schema and a compiled model per schema, with no schema code of
  its own.
- `pro.AddSchemaProvisioning<AppDbContext>()`, which creates the schema (`CREATE SCHEMA` unless it exists),
  and `pro.AddMigrations<AppDbContext>()`, which then applies the migration in `Migrations/` to it, with a migration
  history of its own. The migration was generated with `dotnet ef migrations add` through
  `DesignTimeAppDbContextFactory`, without a tenant, so it names no schema.
- `app.RunTenantMigrationsIfRequestedAsync(args)`: `dotnet run -- migrate-tenants` migrates every tenant's schema
  and exits.

## Run

Requires a PostgreSQL server.

```bash
dotnet run --project Tenantry.Pro.Samples.SchemaPerTenantPostgreSql \
  --ConnectionStrings:Default "Host=localhost;Username=postgres;Password=postgres;Database=app" \
  --Tenantry:License "<your-licence-key>"
```

Then:

```bash
curl -X POST localhost:5000/provision -H "X-Tenant-Id: acme"   # creates schema "acme" and migrates it
curl localhost:5000/products          -H "X-Tenant-Id: acme"   # reads from acme.Products
```

See the [schema-per-tenant guide](../../docs/schema-per-tenant.md) and
[database providers](../../docs/database-providers.md).
