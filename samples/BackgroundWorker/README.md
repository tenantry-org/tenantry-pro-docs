# BackgroundWorker

Tenant scoping in a **non-HTTP host** (a `Microsoft.NET.Sdk.Worker` app). There is no request to open
a tenant scope, so the worker opens one per tenant with `ITenantScopeFactory<string>`.

Demonstrates:

- `AddTenantryCore<string>(...)` instead of `AddTenantry` — no HTTP pipeline, no resolvers.
- Core's `ITenantScopeFactory<string>` and `ITenantStoreAccessor<string>`, registered by
  `AddTenantryCore`.
- `scopeFactory.CreateScope(tenant)` — a combined DI + tenant scope whose scoped services
  (`GreetingService` here) see exactly that tenant.
- `scopeFactory.RunInScopeAsync(id, …)` — the same, when you only have the tenant id.

This sample runs end to end with no external dependencies: it sweeps every tenant, logs a per-tenant
greeting, and exits.

## Run

```bash
dotnet run --project samples/BackgroundWorker
# info: Hello from tenant 'acme' (Acme)
# info: Hello from tenant 'globex' (Globex)
# info: By id: Hello from tenant 'globex' (Globex)
# info: Sweep complete
```

See the [background jobs & non-HTTP hosts guide](../../docs/background-jobs.md).
