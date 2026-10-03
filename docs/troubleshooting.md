# Troubleshooting

Common Tenantry.Pro pitfalls and how to diagnose them. For how tenants are resolved and stored (the
Tenantry core layer), see the [Tenantry core troubleshooting guide](https://github.com/tenantry-org/tenantry-core/blob/master/docs/troubleshooting.md).

## `LicenseRequiredException` is thrown

The application stopped at startup, or a licence-guarded operation (provisioning, applying migrations)
refused to run, because the licence key is missing or invalid. The message says
which; for an invalid key, the log says why it was rejected.

- **No key configured** — confirm the `Tenantry:License` setting is actually present in this environment
  (user secrets locally, the `Tenantry__License` environment variable or a secret in CI and production), or
  that `pro.UseLicenseKey(key)` receives it.
- **Malformed, tampered or wrong key** — the JWT failed validation: its signature, or its issuer, audience or
  format. The log says which. Copy the key again from your
  [Pro access page](https://tenantry.dev/dashboard/pro), in full: it is one three-segment token.

Keys do not expire, so a key that worked keeps working. Reading migration status
(`ITenantMigrationRunner.GetStatusAsync`) never throws this — use it for monitoring regardless of licence state. See
[Licensing](licensing.md).

## `TenantNotResolvedException` when reading a connection string

`CurrentTenantConnectionString<TKey>.Get()` was called with no tenant in scope.

- In ASP.NET Core, ensure `app.UseTenantry()` runs **before** anything that resolves a connection
  string, and that the request actually carries a resolvable tenant (e.g. the `X-Tenant-Id` header).
- In a worker/console host there is no request — open a scope first with
  `ITenantScopeFactory<TKey>`. See [Background jobs & non-HTTP hosts](background-jobs.md).

## `Get()` throws but `GetAsync()` works

You configured only `GetConnectionStringAsync`. The synchronous `Get()` cannot run an async
delegate — call `GetAsync(ct)`, or also set a synchronous `GetConnectionString`.

## All tenants see the same data under schema-per-tenant

The context's model has no tenant schema. `UseSchemaPerTenant` applies the schema only to contexts whose
options call `UseTenantry()`, and only while a tenant is current: check that the context is registered with
`UseTenantry()` (or with Tenantry Core's `AddDbContextPerTenantDatabase`), that `app.UseTenantry()` runs
before the code that uses it, and, in mixed mode, that `GetIsolation` returns `Schema` for the tenant. A
`HasDefaultSchema` call of your own in `OnModelCreating` is overridden for schema tenants. See
[Schema per tenant](schema-per-tenant.md).

## Schema per tenant: some requests are slow once many tenants are active

EF Core is compiling models or queries again after evicting them from its cache. Each context type keeps up
to `MaxCachedSchemas` schemas' models (500 by default), with room for `MaxCompiledQueriesPerSchema` compiled
queries each (100 by default); raise them to the schemas in use at once and the distinct queries you run,
with `pro.UseSchemaPerTenant(o => { o.MaxCachedSchemas = ...; o.MaxCompiledQueriesPerSchema = ...; })`. See
[How many schemas stay compiled](schema-per-tenant.md#how-many-schemas-stay-compiled).

## Schema or database provisioning fails

- `NotSupportedException` from `CreateSchema`: schemas are created on SQL Server and PostgreSQL only. MySQL
  has none apart from databases; use a database per tenant there.
- `NotSupportedException` from `CreateDatabase`: the context's EF Core provider is not relational.
- A permission error: the context's login may not create databases or schemas. Grant it, or give provisioning
  a context with a privileged login (`o.CreateContext`; see
  [Database per tenant](database-per-tenant.md#provisioning-a-new-tenant)).
- The application does not start, naming `GetSchemaName`: `AddSchemaProvisioning` needs
  `pro.UseSchemaPerTenant(...)`.

## `CacheConnectionStrings` stops the application starting, or warns at startup

- The application does not start, with "…so it needs them": it caches the tenants' connection strings, so call
  `UseConnectionStrings(...)` in the same `AddTenantry` (before or after `UsePro`), or register your own
  `ITenantConnectionStringProvider<TKey>` before `UsePro`.
- A warning says the provider "does not wrap the cache": a provider registered after `UsePro` took the cache's
  place, so connection strings are read without it. That is expected where a test replaces the provider; otherwise,
  register your provider before `UsePro`, and the cache wraps it.

## A schema tenant's migrations fail: "CreateContext returned … without schema per tenant"

With schema per tenant, the context `AddMigrations(o => o.CreateContext = …)` creates must get the tenant's schema,
or its migrations would go to the database's default schema. Build its options with
`UseApplicationServiceProvider(sp)` and `UseTenantry()`, as in [Tenant migrations](migration-orchestration.md#registration).

## A captive-dependency error mentioning `ITenantStore`

A singleton tried to inject the (scoped) tenant store directly. Inject
`ITenantLookup<TKey>` instead — it resolves the store from a fresh scope per call. This is the
correct dependency for hosted services and any custom singleton that enumerates tenants. See
[Background jobs & non-HTTP hosts](background-jobs.md).

## Trim/AOT analyzer warnings (IL2026, IL3050)

Expected where you call the EF Core features: `AddMigrations`, the migration runner,
`RunTenantMigrationsIfRequestedAsync`, `AddAuditLogging` and the migration health check. EF Core relies on reflection and runtime code
generation, so these APIs are annotated `[RequiresDynamicCode]`/`[RequiresUnreferencedCode]` and are
**not** Native AOT-compatible; schema-per-tenant model caching is EF Core configuration and has EF Core's
own limits. `Tenantry.Pro` and `Tenantry.Pro.AspNetCore` (licensing, connection-string caching, mixed mode,
tenant provisioning, telemetry) are trim- and AOT-compatible: an app over them publishes
with `PublishAot` without warnings. Provisioning runs migrations only when `AddMigrations` registered them, so its
warning appears at that call. If you publish with
`PublishTrimmed`/`PublishAot`, leave the EF Core features out, or suppress the warnings where you have
accepted the constraint.

## `TenantNotFoundException` when migrating

`MigrateTenantAsync` (and `GetTenantStatusAsync`) looks the tenant up in your store by
id. Save the tenant to your store before migrating it, and make sure the store returns
tenants whatever their status: a store that hides tenants that are provisioning or suspended fails here. Refuse those tenants with an
access validator instead (see [Tenant lifecycle](tenant-lifecycle.md#when-provisioning-fails)).

## A reactivated tenant fails with missing tables or columns

Its database missed migrations while it was suspended, usually because the store left it out of
`GetAllTenantsAsync` and every migration run skipped it. List every tenant in the store (see
[Suspended and inactive tenants](https://github.com/tenantry-org/tenantry-core/blob/master/docs/tenant-stores.md#suspended-and-inactive-tenants) in Tenantry
core), then bring the tenant up to date with `MigrateTenantAsync` before you reactivate it.

## Every replica is taken out of service when one tenant's database is down

A liveness or readiness probe includes the Tenantry health checks, and fails when they report their failure
status (Unhealthy, if you set it so, or Degraded on an endpoint that maps it to `503`) because one tenant's
database is unreachable. Keep probes to process-level checks and serve the tenant checks on a separate
monitoring endpoint (see [Health checks](health-checks.md#exposing-the-checks)).

## Requests have no `tenant.id` tag

- Make sure `app.UseTenantryMetrics()` runs **after** `app.UseTenantry()`, which makes the tenant current.
- A request without a tenant is not tagged: an endpoint that allows a missing tenant, or a request
  rejected before its tenant was resolved. Nor is a tenant for which your `GetTagValue` returns `null`.
- The tag is on ASP.NET Core's `http.server.request.duration`, not on `http.server.active_requests`. Make
  sure your collector listens to the `Microsoft.AspNetCore.Hosting` meter. See [Telemetry](telemetry.md).

## A background job / consumer runs without a tenant

Jobs enqueued or messages published while **no** tenant is current carry no tenant, so by default they run
without one (logging a warning). Enqueue, publish or send while a tenant is current, or name the tenant
(`jobs.ForTenant(id)` for Hangfire, `context.SetTenant(id)` for MassTransit, `headers.WithTenant(id)` for Rebus,
`WithTenant(id)` in a Quartz.NET job's data). For work every tenant needs on a schedule, use Hangfire's
`AddOrUpdateForEachTenant` or Quartz.NET's `ForEachTenant()`. Otherwise check `ITenantContext<TKey>.HasTenant`
inside the job and handle the case without one.

If the application fails to start with "`pro.Add…Propagation()` was called, but not its host side", add the call
the message names to the host library's configuration: `config.UseTenantry(sp)` for Hangfire,
`cfg.UseTenantry(context)` for MassTransit, `q.UseTenantry()` for Quartz.NET, `o.UseTenantry(sp)` for Rebus. If it
names more than one bus, make the call in each bus's configuration.

Two settings on the integration decide what happens to a job or message whose tenant cannot be made current:
`OnMissingTenant` when it carries none (default `Warn`), and `OnUnresolvedTenant` when it carries an id the store
does not return or that is not a valid id (default `Reject`). For example,
`pro.AddHangfirePropagation(o => o.OnMissingTenant = TenantPropagationBehavior.Reject)`. `Allow` runs it without
a tenant silently, `Warn` does the same and logs, `Reject` throws (the host's retry and error handling take over),
and `Skip` does not run it (Hangfire deletes the job, Quartz.NET counts the run as done, Rebus acknowledges the
message, MassTransit moves it to the endpoint's `_skipped` queue, and faults a routing slip). If your store hides suspended tenants, their jobs fail under the default
`Reject`: list them instead and [check the status in the job](background-jobs.md#suspended-tenants). See
[Hangfire](hangfire.md), [MassTransit](masstransit.md), [Quartz.NET](quartz.md), [Rebus](rebus.md),
and [Background jobs](background-jobs.md).
