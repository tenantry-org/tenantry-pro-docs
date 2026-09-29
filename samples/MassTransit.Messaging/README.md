# MassTransit.Messaging

Tenant-context propagation across **MassTransit** messages, using the in-memory transport so it runs
with no broker.

Demonstrates:

- `pro.AddMassTransitTenantFilters<string>()` — registers the publish/send/consume filters.
- `x.AddTenantryConsumeFilter<string>()` — restores the tenant scope on every receive endpoint.
- `cfg.UseTenantryPro<string>(ctx)` — stamps the tenant id header on outgoing messages.

Publishing inside an active tenant scope stamps the `tenantry-tenant-id` header; the consumer restores
the scope so `ITenantContext<string>` reflects the publishing tenant.

## Run

```bash
dotnet run --project samples/MassTransit.Messaging --Tenantry:Licence "<your-licence-key>"

curl -X POST localhost:5000/orders -H "X-Tenant-Id: acme"
# log: "Handling order ... for tenant 'acme'"
```

See the [MassTransit guide](../../docs/masstransit.md).
