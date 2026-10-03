# Tenantry.Pro.Samples.TenantLifecycle

Onboarding tenants from a **catalog database**: add the tenant to the catalog, then **create its database →
migrate → seed** behind a single `ITenantProvisioner.ProvisionAsync` call, then make it active. On SQL Server, with
a database per tenant.

Demonstrates:

- A catalog: `CatalogDbContext` lists every tenant in its own database (`catalog`), with a status the application
  keeps (`Provisioning`, `Active`, `Suspended`). `CatalogTenantStore` is the tenant store over it
  (`tenant.UseStore<CatalogTenantStore>()`), cached with `tenant.CacheTenants()`, and an access validator lets
  requests use active tenants only.
- Onboarding (`TenantOnboarding`): the tenant is added to the catalog first, as `Provisioning`, because the
  provisioner works on tenants in the store; once every step has succeeded it becomes `Active`, and is removed from
  the cache (`ITenantInvalidator<string>.InvalidateAsync`), where a request made during provisioning left it not active.
- `pro.AddDatabaseProvisioning<AppDbContext>()`: step 1 (`CREATE DATABASE`, through EF Core's database creator).
- `pro.AddMigrations<AppDbContext>()`: step 2 (apply the EF Core migration in `Migrations/`, generated with
  `dotnet ef migrations add --context AppDbContext` through `DesignTimeAppDbContextFactory`, through the tenant's own
  `AppDbContext`). The catalog's own migration is in `Catalog/Migrations/`, applied at startup.
- `pro.AddSeeder<DefaultSettingsSeeder>()`: step 3 (seed data), with the tenant's `AppDbContext` injected. It is
  idempotent, with a unique index on `Setting.Key`, so running it again never duplicates rows.
- Recovery: failed provisioning is fixed by running it again.
- Suspending a tenant: its status changes, and it is removed from the cache, so its requests are refused at once
  (by this instance: each instance of an application keeps its own cache, so others serve the tenant until its entry
  expires, after `CacheTenants`' duration).

## Run

Requires a SQL Server instance.

```bash
dotnet run --project Tenantry.Pro.Samples.TenantLifecycle \
  --ConnectionStrings:Server "Server=localhost;Integrated Security=true;TrustServerCertificate=true" \
  --Tenantry:License "<your-licence-key>"
```

Onboard `acme`, making seeding fail part-way through (after the first setting is saved):

```bash
curl -X POST "localhost:5000/tenants?simulateSeedFailure=true" \
  -H "Content-Type: application/json" -d '{"tenantId":"acme","name":"Acme"}'
# { "tenantId": "acme", "succeeded": false, "steps": [
#     { "name": "CreateDatabase", "status": "Succeeded", "error": null },
#     { "name": "Migrations", "status": "Succeeded", "error": null },
#     { "name": "DefaultSettingsSeeder", "status": "Failed",
#       "error": "Simulated failure while seeding tenant 'acme' (DisplayName was already saved)." } ], ... }

curl localhost:5000/settings -H "X-Tenant-Id: acme"
# 403: acme is in the catalog, but not active
```

Recover by provisioning again. The database exists and the migration is applied, so those steps do nothing; the
seeder adds only what is missing, and `acme` becomes active:

```bash
curl -X POST localhost:5000/tenants/acme/provision
# { "tenantId": "acme", "succeeded": true, "steps": [ ...every step "Succeeded"... ], ... }

curl localhost:5000/settings -H "X-Tenant-Id: acme"
# [ { "key": "DisplayName", "value": "Acme" }, { "key": "Plan", "value": "trial" } ]
```

Suspend it, and its requests are refused:

```bash
curl -X POST localhost:5000/tenants/acme/suspend
curl localhost:5000/settings -H "X-Tenant-Id: acme"
# 403
```

The `/tenants` endpoints are open to anyone here, to keep the sample short: protect them in an application.

See the [tenant lifecycle guide](../../docs/tenant-lifecycle.md), including
[When provisioning fails](../../docs/tenant-lifecycle.md#when-provisioning-fails).
