# AuditLogging

Per-tenant EF Core audit logging with a **custom `IAuditStore`**. Runs on the EF Core in-memory
provider, so it needs no external database.

Demonstrates:

- `pro.AddAuditLogging()` — records insert/update/delete changes with the current tenant id.
- `options.UseAuditLogging<string>(sp)` — wires the audit interceptor into the `DbContext`.
- A custom `IAuditStore` (here `InMemoryAuditStore`) registered after `AddAuditLogging()` to replace
  the default logging store.

## Run

```bash
dotnet run --project samples/AuditLogging --Tenantry:Licence "<your-licence-key>"

curl -X POST localhost:5000/products -H "X-Tenant-Id: acme" \
  -H "Content-Type: application/json" -d '{"name":"Widget","price":9.99}'

curl localhost:5000/audit -H "X-Tenant-Id: acme"
# shows the captured audit entry, tagged TenantId = "acme"
```

See the [audit logging guide](../../docs/audit-logging.md).
