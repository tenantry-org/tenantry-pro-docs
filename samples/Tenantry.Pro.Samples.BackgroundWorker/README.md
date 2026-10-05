# Tenantry.Pro.Samples.BackgroundWorker

Recurring work for every tenant in a **non-HTTP host** (a `Microsoft.NET.Sdk.Worker` app), with
`PeriodicTenantBackgroundService<string>`.

Demonstrates:

- `AddTenantry<string>(...)` from Tenantry.Core: no HTTP pipeline, no resolvers.
- `GreetingSweep : PeriodicTenantBackgroundService<string>`: the sweep runs at startup and then every `Interval`,
  calling `ExecuteForTenantAsync` for each tenant inside that tenant's scope, so scoped services (`GreetingService`
  here) see the tenant. A tenant whose work throws is logged without stopping the others, and a sweep that cannot
  read the store is logged and tried again at the next interval.
- `tenant.ValidateTenantActivity(t => t.As<Tenant>().IsActive)`: the sweep skips the tenants the application has
  marked inactive (`initech`), and logs each skip at `Debug`.

It runs with no external dependencies, and sweeps every 30 seconds until you stop it.

## Run

```bash
dotnet run --project Tenantry.Pro.Samples.BackgroundWorker --Tenantry:License "<your-licence-key>" --Sweep:Interval 00:00:05
# info: Hello from tenant 'acme' (Acme)
# info: Hello from tenant 'globex' (Globex)
# ...and again every 5 seconds
```

Add `--Logging:LogLevel:Tenantry.Pro.Samples.BackgroundWorker Debug` to see the skip:

```text
dbug: Tenantry.Pro: tenant 'initech' is not active, so GreetingSweep skips it
```

For work that runs once at startup, derive from `TenantBackgroundService<string>` instead. For work you schedule
yourself (a queue message for one tenant, say), open the tenant's scope with Core's `ITenantScopeFactory<string>`.
See the [background jobs & non-HTTP hosts guide](../../docs/background-jobs.md).
