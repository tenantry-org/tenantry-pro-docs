# SchemaPerTenant.Npgsql

Schema-per-tenant isolation on **PostgreSQL**: one shared database, one schema per tenant, with EF
Core per-schema model caching and on-demand `CREATE SCHEMA` provisioning.

Demonstrates:

- `pro.UseSchemaPerTenant(...)` with the schema name derived from the tenant.
- `pro.AddSchemaProvisioning(opts => opts.ConnectionString = ...)` — the Npgsql provisioning service
  (`CREATE SCHEMA IF NOT EXISTS`).
- `options.AddSchemaPerTenantCaching<string>(sp)` — a separate compiled model per tenant schema.

## Run

Requires a PostgreSQL server.

```bash
dotnet run --project samples/SchemaPerTenant.Npgsql \
  --ConnectionStrings:Default "Host=localhost;Username=postgres;Password=postgres;Database=app" \
  --Tenantry:Licence "<your-licence-key>"
```

Then:

```bash
curl -X POST localhost:5000/provision -H "X-Tenant-Id: acme"   # creates schema "acme"
curl localhost:5000/products          -H "X-Tenant-Id: acme"   # reads from acme.Products
```

See the [schema-per-tenant guide](../../docs/schema-per-tenant.md) and
[database providers](../../docs/database-providers.md).
