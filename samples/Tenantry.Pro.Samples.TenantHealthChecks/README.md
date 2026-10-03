# Tenantry.Pro.Samples.TenantHealthChecks

ASP.NET Core health checks that probe **every** tenant database on SQL Server: one for connectivity,
one for pending EF Core migrations.

Demonstrates:

- `AddTenantDatabaseCheck<AppDbContext>()`: opens a connection through `AppDbContext`, created in each
  tenant's scope, once per distinct database, and reports per-tenant connectivity.
- `AddTenantMigrationCheck<AppDbContext>()`: reports per-tenant pending migrations.

Both enumerate tenants from the store, which lists every tenant, suspended ones included, and keep their
result for 30 seconds, so frequent polls do not each reach every database.

They are for monitoring, not for liveness or readiness probes: one unreachable tenant database would fail
the probe on every replica at once. The sample maps `/health/live` for the orchestrator (no checks) and
`/health/tenants` for monitoring. In production, protect `/health/tenants`: its detail lists tenant ids and
provider error messages.

## Run

Requires a SQL Server instance.

```bash
dotnet run --project Tenantry.Pro.Samples.TenantHealthChecks \
  --ConnectionStrings:Server "Server=localhost;Integrated Security=true;TrustServerCertificate=true" \
  --Tenantry:License "<your-licence-key>"

curl localhost:5000/health/tenants
# Healthy when every tenant database is reachable and up to date; Degraded otherwise.
```

See the [health checks guide](../../docs/health-checks.md).
