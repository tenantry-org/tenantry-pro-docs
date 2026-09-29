# Troubleshooting

Common Tenantry.Pro pitfalls and how to diagnose them. For how tenants are resolved and stored (the
Tenantry core layer), see the [Tenantry core troubleshooting guide](https://github.com/tenantry-org/tenantry-core/blob/main/docs/troubleshooting.md).

## `LicenseRequiredException` is thrown

The application stopped at startup, or a licence-guarded operation (provisioning, migration orchestration,
the lifecycle pipeline) refused to run, because the licence key is missing or invalid. The message says
which; for an invalid key, the log says why it was rejected.

- **No key configured** — call `pro.WithLicence(builder.Configuration["Tenantry:Licence"]!)` and
  confirm the configuration value is actually present in this environment (user secrets locally, an
  environment variable or secret in CI and production).
- **Malformed, tampered or wrong key** — the JWT failed signature or issuer validation. Copy the key again
  from your [Pro access page](https://tenantry.dev/dashboard/pro), in full: it is one three-segment token.

Keys do not expire, so a key that worked keeps working. Read-only `MigrationStatusTracker` never throws
this — use it for monitoring regardless of licence state. See [Licensing](licensing.md).

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

Expected where you call the EF Core features: `WithMigrationOrchestration`, `AddAuditLogging`, the
migration status tracker and the migration health check. EF Core relies on reflection and runtime code
generation, so these APIs are annotated `[RequiresDynamicCode]`/`[RequiresUnreferencedCode]` and are
**not** Native AOT-compatible; schema-per-tenant model caching is EF Core configuration and has EF Core's
own limits. `Tenantry.Pro` and `Tenantry.Pro.AspNetCore` (licensing, the strategies, caching and
encryption, the lifecycle pipeline, telemetry) are trim- and AOT-compatible: an app over them publishes
with `PublishAot` without warnings. The lifecycle pipeline runs migrations only when
`WithMigrationOrchestration` registered them, so its warning appears at that call. If you publish with
`PublishTrimmed`/`PublishAot`, leave the EF Core features out, or suppress the warnings where you have
accepted the constraint.

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
