# HealthChecks

ASP.NET Core health checks that probe **every** tenant database on SQL Server: one for connectivity,
one for pending EF Core migrations.

Demonstrates:

- `AddTenantryDatabaseCheck<string>(opts => opts.ConnectionFactory = cs => new SqlConnection(cs))` —
  opens each tenant's database and reports per-tenant connectivity.
- `AddTenantryMigrationCheck<string, AppDbContext>(...)` — reports per-tenant pending migrations.

Both enumerate tenants from the store, so make sure the store returns every tenant you want probed.

## Run

Requires a SQL Server instance.

```bash
dotnet run --project samples/HealthChecks \
  --ConnectionStrings:Server "Server=localhost;Integrated Security=true;TrustServerCertificate=true" \
  --Tenantry:Licence "<your-licence-key>"

curl localhost:5000/health
# Healthy when every tenant database is reachable and up to date; Unhealthy/Degraded otherwise.
```

See the [health checks guide](../../docs/health-checks.md).
