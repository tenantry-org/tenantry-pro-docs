# TenantLifecycle

The full tenant-creation pipeline — **provision → migrate → seed** — behind a single
`ITenantLifecycleManager.ProvisionAsync` call, on SQL Server with database-per-tenant.

Demonstrates:

- `pro.AddDatabaseProvisioning()` — step 1 (`CREATE DATABASE`).
- `pro.WithMigrationOrchestration<string, AppDbContext>(...)` — step 2 (apply the EF Core migration in
  `Migrations/`, generated with `dotnet ef migrations add` through `DesignTimeAppDbContextFactory`).
- A registered `ITenantSeeder<string>` (`DefaultSettingsSeeder`) — step 3 (seed data). It is idempotent,
  with a unique index on `Setting.Key`, so re-running it never duplicates rows.
- `pro.AddLifecycleManagement()` — registers `ITenantLifecycleManager<string>`, which runs all three
  and returns a `TenantProvisioningResult` describing how far it got.
- Recovery: a failed pipeline is fixed by running it again.

## Run

Requires a SQL Server instance.

```bash
dotnet run --project samples/TenantLifecycle \
  --ConnectionStrings:Server "Server=localhost;Integrated Security=true;TrustServerCertificate=true" \
  --Tenantry:Licence "<your-licence-key>"
```

Onboard `acme`, making seeding fail part-way through (after the first setting is saved):

```bash
curl -X POST "localhost:5000/tenants/acme/onboard?simulateSeedFailure=true"
# { "tenantId": "acme", "succeeded": false, "completedUpTo": "MigrationsApplied",
#   "error": "Simulated failure while seeding tenant 'acme' (DisplayName was already saved)." }

curl localhost:5000/tenants/acme/settings
# [ { "key": "DisplayName", "value": "Acme" } ]
```

Recover by running the pipeline again. The database exists and the migration is applied, so those steps
do nothing; the seeder adds only what is missing:

```bash
curl -X POST localhost:5000/tenants/acme/onboard
# { "tenantId": "acme", "succeeded": true, "completedUpTo": "Complete", ... }

curl localhost:5000/tenants/acme/settings
# [ { "key": "DisplayName", "value": "Acme" }, { "key": "Plan", "value": "trial" } ]
```

See the [tenant lifecycle guide](../../docs/tenant-lifecycle.md), including
[When provisioning fails](../../docs/tenant-lifecycle.md#when-provisioning-fails).
