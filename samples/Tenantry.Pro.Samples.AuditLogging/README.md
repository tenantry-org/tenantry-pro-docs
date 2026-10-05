# Tenantry.Pro.Samples.AuditLogging

Per-tenant EF Core audit logging with a **custom `IAuditStore`**. Runs on the EF Core in-memory
provider, so it needs no external database.

Demonstrates:

- `pro.AddAuditLogging()`: records the insert/update/delete changes of every context that uses
  `UseTenantry()`, with the current tenant id, once they are committed.
- `options.UseTenantry()`: isolates the context's tenant-owned entities, and with audit logging audits its saves.
- A custom `IAuditStore` (here `InMemoryAuditStore`) registered after `AddAuditLogging()` to replace
  the default logging store.

## Run

```bash
dotnet run --project Tenantry.Pro.Samples.AuditLogging --Tenantry:License "<your-licence-key>"

curl -X POST localhost:5000/products -H "X-Tenant-Id: acme" \
  -H "Content-Type: application/json" -d '{"name":"Widget","price":9.99}'

curl localhost:5000/audit -H "X-Tenant-Id: acme"
# shows the captured audit entry, tagged TenantId = "acme"
```

See the [audit logging guide](../../docs/audit-logging.md).
