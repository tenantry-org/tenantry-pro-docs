# Troubleshooting

Common Tenantry.Pro pitfalls and how to diagnose them. For how tenants are resolved and stored (the
Tenantry core layer), see the [Tenantry core troubleshooting guide](https://github.com/tenantry-org/tenantry-core/blob/main/docs/troubleshooting.md).

## `LicenseRequiredException` is thrown

A licence-guarded operation (provisioning, migration orchestration, the lifecycle pipeline) ran
without a usable licence **while you have opted into `LicenseEnforcement.Throw`**. Under the default
`Warn` mode this is logged instead of thrown — drop the `Throw` argument from `WithLicence` if you do
not want fail-fast behaviour.

- **No key configured** — call `pro.WithLicence(builder.Configuration["Tenantry:Licence"]!)` and
  confirm the configuration value is actually present (check user secrets / environment binding).
- **Wrong issuer or tampered key** — the JWT failed signature or issuer validation. Re-copy the key
  exactly; it must be the full three-segment token.
- **Grace period over** — the licence expired more than 30 days ago. Renew it. Within 30 days of
  expiry, guarded operations still run (with a warning).

Read-only `MigrationStatusTracker` never throws this — use it for monitoring regardless of licence
state. See [Licensing](licensing.md).

## Every request returns HTTP 503

`app.UseLicenseCheck()` is enforcing the licence **and you have opted into `LicenseEnforcement.Throw`**,
and the licence is missing/invalid or expired beyond grace. (Under the default `Warn` mode the
middleware never returns 503 — it logs and passes the request through.) The 503 body and the logged
error name the reason. Fix the key, switch to `Warn`, or remove `UseLicenseCheck()` if you only want
the non-blocking startup log rather than hard per-request enforcement.

## `TenantNotResolvedException` when resolving a connection string

`ITenantConnectionStringResolver<TKey>.Resolve()` was called with no tenant in scope.

- In ASP.NET Core, ensure `app.UseTenantry()` runs **before** anything that resolves a connection
  string, and that the request actually carries a resolvable tenant (e.g. the `X-Tenant-Id` header).
- In a worker/console host there is no request — open a scope first with
  `ITenantScopeFactory<TKey>`. See [Background jobs & non-HTTP hosts](background-jobs.md).

## `Resolve()` throws but `ResolveAsync()` works

You configured only `GetConnectionStringAsync`. The synchronous `Resolve()` cannot run an async
delegate — call `ResolveAsync(ct)`, or also set a synchronous `GetConnectionString`.

## All tenants see the same data under schema-per-tenant

EF Core cached one compiled model (the first tenant's schema) for the whole context type. Add
`options.AddSchemaPerTenantCaching<TKey>(sp)` in your `AddDbContext` factory so EF Core keeps a
separate model per schema, and make sure `OnModelCreating` calls `HasDefaultSchema(...)` with the
resolved schema. See [Schema per tenant](schema-per-tenant.md).

## `AddSchemaProvisioning` does not compile / connection string is ignored

`AddSchemaProvisioning` now **requires** a configure delegate, and the shared-database connection
string lives on its options — not on `UseSchemaPerTenant`:

```csharp
pro.UseSchemaPerTenant(opts => opts.GetSchemaName = t => $"tenant_{t.TenantId}");
pro.AddSchemaProvisioning(opts => opts.ConnectionString = connectionString);
```

`SchemaPerTenantOptions` has **no** `ConnectionString` property.

## `WithMigrationOrchestration` throws at startup

It throws immediately if `UseDatabasePerTenant(...)` was not called first on the same builder. Order
the calls so `UseDatabasePerTenant` precedes `WithMigrationOrchestration`. Pass both type arguments:
`WithMigrationOrchestration<TKey, TContext>(...)`.

## A captive-dependency error mentioning `ITenantStore`

A singleton tried to inject the (scoped) tenant store directly. Inject
`ITenantStoreAccessor<TKey>` instead — it resolves the store from a fresh scope per call. This is the
correct dependency for hosted services and any custom singleton that enumerates tenants. See
[Background jobs & non-HTTP hosts](background-jobs.md).

## Trim/AOT analyzer warnings (IL2026, IL3050)

Expected on the EF Core features — audit logging, migration orchestration, the migration health
check, and schema-per-tenant model building. EF Core relies on reflection and runtime code
generation, so these APIs are annotated `[RequiresDynamicCode]`/`[RequiresUnreferencedCode]` and are
**not** Native-AOT compatible. The base `Tenantry.Pro` and `Tenantry.Pro.AspNetCore` packages
(licensing, strategy resolution, caching/encryption, telemetry) are trim-friendly. If you publish with
`PublishTrimmed`/`PublishAot`, scope those features out or suppress the warnings deliberately where you
have accepted the constraint.

## Per-tenant metrics are missing or all show `tenant.id = unknown`

- Ensure `app.UseTenantryMetrics()` runs **after** `app.UseTenantry()`, so the tenant context is
  populated before a metric is recorded.
- `unknown` means no tenant was resolved for that request — expected for unauthenticated or
  pre-resolution endpoints. Excluded paths (default `/health`, `/healthz`, `/ready`) emit nothing.
- Confirm your collector subscribes to the meter named `Tenantry.Pro`. See [Telemetry](telemetry.md).

## A background job / consumer runs without a tenant

Jobs enqueued or messages published **outside** a tenant scope carry no tenant, so by default they run
without one (logging a warning). Stamp/enqueue from within an active scope, or check
`ITenantContext<TKey>.HasTenant` inside the job and handle the scopeless case.

To change what happens when a job/message has no resolvable tenant, set the missing-tenant policy on
the integration's registration — e.g. `pro.AddHangfireTenantFilter(o => o.OnMissingTenant =
MissingTenantBehavior.Reject)`. `Allow` runs scopeless silently, `Warn` (default) runs scopeless and
logs, `Reject` throws (the host's retry/error handling takes over), and `Skip` drops the job/message
without running it. This mirrors core's `EfCoreIsolationOptions.OnMissingTenant`. See
[Hangfire](hangfire.md), [MassTransit](masstransit.md), [Quartz.NET](quartz.md), [Rebus](rebus.md),
and [Background jobs](background-jobs.md).
