# Tenantry.Pro.Samples.RebusMessaging

Tenant-context propagation across **Rebus** messages, using the in-memory transport so it runs with no broker.

Demonstrates:

- `pro.AddRebusPropagation()` and `o.UseTenantry(sp)` in the bus's options: a message carries the tenant it was sent
  or published for, and its handlers run as that tenant.
- `POST /orders`: sending while a tenant is current puts the `tenantry-tenant-id` header on the message, and the
  handler runs as that tenant.
- `POST /admin/tenants/{id}/payment-reminder`: code that runs without a tenant sends for a tenant by name, with
  `new Dictionary<string, string>().WithTenant(id)` as the message's headers.

## Run

```bash
dotnet run --project Tenantry.Pro.Samples.RebusMessaging --Tenantry:License "<your-licence-key>"

curl -X POST localhost:5000/orders -H "X-Tenant-Id: acme"
# log: "Handling order ... for tenant 'acme'"

curl -X POST localhost:5000/admin/tenants/globex/payment-reminder
# log: "Sending a payment reminder for tenant 'globex'"
```

The admin endpoint is open to anyone here, to keep the sample short: protect such endpoints in an application.

See the [Rebus guide](../../docs/rebus.md).
