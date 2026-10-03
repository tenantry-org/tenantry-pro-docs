# Tenantry.Pro.Samples.MassTransitMessaging

Tenant-context propagation across **MassTransit** messages, using the in-memory transport so it runs
with no broker.

Demonstrates:

- `pro.AddMassTransitPropagation()`: a message carries the tenant it was published or sent for, and is consumed
  as that tenant.
- `cfg.UseTenantry(ctx)`: adds the publish, send and consume filters for every receive endpoint.

Publishing while a tenant is current puts the `tenantry-tenant-id` header on the message; the consumer runs as
that tenant, so `ITenantContext<string>` and the scoped services it takes see it.

## Run

```bash
dotnet run --project Tenantry.Pro.Samples.MassTransitMessaging --Tenantry:License "<your-licence-key>"

curl -X POST localhost:5000/orders -H "X-Tenant-Id: acme"
# log: "Handling order ... for tenant 'acme'"
```

See the [MassTransit guide](../../docs/masstransit.md).
