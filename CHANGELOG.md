# Changelog

All notable changes to Tenantry.Pro will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.7.0] - 2026-10-05

### Upgrading from 0.6

- Pro 0.7 runs on Tenantry Core 0.7. Follow Core's "Upgrading from 0.6" list too.
- `ITenantPropagator.Use(tenant)` is now `MakeCurrent(tenant)`, as Tenantry Core's `ITenantContextSetter<TKey>.Use`
  is now `MakeCurrent`: rename the call in an adapter of your own.
- `IConnectionStringCache<TKey>` is removed. When a tenant's connection details change, call Tenantry Core's
  `ITenantInvalidator<TKey>.InvalidateAsync(tenantId)`, or `InvalidateAllAsync()` for every tenant. It clears the
  cached connection string and the tenant's cached descriptor, which the connection-string delegate may read; the
  removed interface's `Invalidate` cleared only the first, so with `CacheTenants` the next read could return the old
  connection string again.
- A class derived from `PeriodicTenantBackgroundService<TKey>` must be compiled again: its constructor takes an
  optional `TimeProvider` now, which code compiled against 0.6 does not pass. The source needs no change.
- On MySQL, offboarding now refuses to drop a tenant's database when another tenant has a database of the same name
  on a server with another `server_uuid`, which 0.6 dropped. If no MySQL server your tenants reach replicates
  another, set `o.IndependentMySqlServers = true` in `pro.AddDatabaseDeprovisioning<TContext>(o => …)`.

### Removed

- `IConnectionStringCache<TKey>` ([Upgrading](#upgrading-from-06)).
- `Tenantry.Pro.AspNetCore` and its `pro.AddTenantMetrics()`. Tenantry Core 0.7.0's `tenant.TagRequestMetrics()`, in
  `Tenantry.AspNetCore`, adds the same `tenant.id` tag to ASP.NET Core's request metric, and takes the function that
  was `TenantMetricsOptions.GetTagValue`. Remove the package reference and the call, and call
  `tenant.TagRequestMetrics()` on the `AddTenantry` builder instead.
- Log event 4117 (`AppliedMigrationsNotRecorded`): applied migrations no longer come from EF Core's diagnostic events,
  which a context could make unobservable.

### Added

- Log event 4008 (`PoolNotCleared`, debug): provisioning found a tenant's database and could not release the pooled
  connection it checked with, because the connection's provider has no `ClearPool`.
- In mixed mode, the host logs a warning as it starts for each registered context with entity types that are neither
  tenant-owned nor marked as shared, naming them, as a `Shared` tenant is refused that context (event 4402,
  `SharedTenantsRefusedContext`). A context that cannot be created without a tenant is logged the first time a tenant
  uses it.
- `TenantBackgroundService.ShouldRunAsync`, called before each sweep, to run a sweep on one instance of several: check
  that the instance is the leader, or take a lease. A sweep it skips is logged at debug level (event 3204,
  `SweepSkipped`), and a periodic service asks again at the next interval.
- `TenantBackgroundService.MaxConcurrency`, how many tenants a sweep works on at once. The default, 1, keeps them one
  after another.
- `PeriodicTenantBackgroundService` takes an optional `TimeProvider`, which times its sweeps
  ([Upgrading](#upgrading-from-06)).

### Changed

- `MigrationResult.AppliedMigrations` lists the migrations in the database's history after the run that were not in
  it before. It was read from EF Core's diagnostic events, which a context could make unobservable. The history does
  not say who applied a migration, so when two runners migrate one database at once a
  migration may be listed by either, or by both.
- The docs no longer list MariaDB as supported, because no test suite runs against it.
- Pack compares each package's API with 0.6.0, which CI downloads from the feed, so a patch cannot break code built
  against 0.6.0.

### Fixed

- Pro's own awaits no longer return to the caller's synchronization context, so a desktop app that waits on its UI
  thread for provisioning, offboarding, a migration run, a cached connection string or a sweep no longer deadlocks.
  Provisioning and offboarding steps, and a sweep's per-tenant work, may run on a thread-pool thread when a UI thread
  started them. A `Progress<T>` passed to `MigrateAsync` still reports on the context it was created on.
- After `ITenantInvalidator<TKey>.InvalidateAsync(tenantId)` or `InvalidateAllAsync()`, a request or job that read the
  tenant before the invalidation no longer puts the old connection string back in the `CacheConnectionStrings()`
  cache, where every later read got it until `Duration` ended. The connection string of a tenant invalidated since
  the application started is computed from the store's copy of the tenant, read through `ITenantLookup<TKey>`, and
  from the caller's descriptor when the store does not have the tenant. When the store cannot be read, the caller's
  descriptor is used, its connection string is not cached, and a warning is logged (event 3302).
- The audit entries of a transaction disposed without a commit or a rollback no longer attach to a later transaction
  in the same `DbTransaction` object. Npgsql reuses its transaction objects, so a transaction begun through ADO.NET
  on that connection and handed to a context with `UseTransaction` took them: committed through EF Core, it wrote the
  entries of the changes that were rolled back, and its own entries waited for that commit instead of being written
  at each save. A transaction that an audited context began and still has keeps its entries when another context
  joins it with `UseTransaction`.
- Schema per tenant refuses a registered context it applies to whose options do not call `UseTenantry()`: one listed
  in `SchemaPerTenantOptions.Contexts`, or, with none listed, every registered context when none calls it. It reads
  the options a created context has, so a call in `OnConfiguring` counts, and two contexts that make it there are found
  as the host starts. A context it cannot create without a tenant is checked when Pro creates it for a tenant, and the
  host logs a warning that names it (event 4401, `SchemaPerTenantContextNotChecked`), unless it comes from
  `AddDbContextPerTenantDatabase` and needs a tenant only to find its database. The host does not start, `migrate-tenants` returns 3, `DeprovisionAsync` throws before any step, and a migration of a context
  whose options need a tenant fails for that tenant, each with an `InvalidOperationException` that names the context
  or contexts. Such a context never got a tenant's
  schema: the first tenant's migration created its tables in the database's default schema, and every tenant read and
  wrote them there, with no error.
- `migrate-tenants` leaves configuration overrides whose key begins like one of its options to the application:
  `--Tenantry:License=...`, `--Tenants:0:Id=...`, `--TenantStore=...`. It refused them with "Unknown option" and
  exit code 3. It still refuses a misspelt option, such as `--tenants`, `--Tenant` or `--max-failure`, and now
  refuses `-t acme`, `--tenant:acme` and an option written with `/`, a single `-` or a dash autocorrect put in, such
  as `/tenant acme`, `-tenant acme`, or the long dash autocorrect writes for `--` (the guide shows it), which it read as the application's and migrated every tenant.
  The argument after the application's own option is that option's value, so `--urls /tenant` is not refused, unless
  it is an option of `migrate-tenants` or a misspelling of one that begins with `--`, so `--verbose --tenant acme`
  migrates acme.
- `TenantDeprovisioningContext.DataDropped` is `true` only when every database or schema offboarding drops for the
  tenant is gone. With two drops registered it was `true` as soon as one found its target gone, so a step that
  exports the tenant's data could skip a database that still held it, which the drop then removed.
- Offboarding tells a tenant's database apart from another tenant's database of the same name when their connection
  strings name the server differently: it drops the leaving tenant's when they are two databases, and refuses, naming
  the other tenant, when they are one. `DropDatabase` and `DropSchema` failed in both cases with "whether it is the
  same database could not be read", because the leaving tenant's database was asked which it is while the other
  tenant was current, which Tenantry Core refuses.
- On MySQL, offboarding refuses to drop a tenant's database when another tenant has a database of the same name on a
  server with another `server_uuid`, unless `DatabaseDeprovisioningOptions.IndependentMySqlServers` says no server
  replicates another ([Upgrading](#upgrading-from-06)). A replica or group member has a `server_uuid` of its own and
  a drop on one server reaches those that replicate it, so offboarding dropped the leaving tenant's database when
  another tenant reached it through a replica under another host name.
- With schema per tenant on PostgreSQL, offboarding no longer reports a tenant's schema dropped while it is still
  there. The login's `INFORMATION_SCHEMA` does not show it a schema it has no privilege on, so `DropSchema` took the
  schema for gone and succeeded, and the migration runner reported the schema missing. Both now read the database's
  own catalogue, and `DropSchema` fails, naming the right it needs, when the login may not drop the schema (on SQL
  Server too, where the drop used to fail with the server's own message). A schema that holds something
  `DropSchema` does not drop (a view, a function, a type, a collation, a text search configuration) fails the step
  with their names; on PostgreSQL, everything `pg_depend` ties to the schema, an extension named once. A table in
  another schema that references the schema's tables, and on PostgreSQL a table with a foreign key that the login
  does not own, fail the step before anything is dropped, naming the table. On SQL Server, which shows a login only
  the tables it may use, a referencing table the login cannot see fails the drop instead, with a message saying so
  and the server's; the transaction puts back what was dropped.
- Offboarding asks which database it is when another tenant has a database of the same name on the same host but
  another port. MySQL's drivers leave the port out of the server name, so two MySQL servers on one host were taken
  for one, and the drop was refused with "uses it too".

## [0.6.0] - 2026-10-03

### Upgrading from 0.5

- Pro 0.6 runs on Tenantry Core 0.6.
- `TenantPropagation.HeaderName` moved to Tenantry Core: it is `Tenantry.TenantPropagation`, so code that named
  `Tenantry.Pro.TenantPropagation` names `Tenantry.TenantPropagation`, or adds `using Tenantry;`.
- A migration run (`migrate-tenants`, `OnStartup`, `ITenantMigrationRunner`) no longer creates a tenant's database that
  does not exist, nor a tenant schema that does not exist: it reports that tenant as failed. Provision
  tenants first, as provisioning's `Migrations` step still creates; for development, set
  `o.CreateMissingDatabases = builder.Environment.IsDevelopment()` in `AddMigrations`.
- `migrate-tenants` returns 3, logged, rather than throwing, when it cannot start (no valid licence, a tenant store that
  cannot be read), and 2 when it stops early.
- Schema per tenant fails closed: a context's first command or save throws `TenantNotResolvedException` without a
  current tenant, where it used the database's default schema, and `TenantIsolationViolationException`
  (`TenantSchemaMismatch`) under a tenant other than the one whose schema its model has. `dotnet ef database update`
  on such a context now throws: migrate tenants with `migrate-tenants`.
- Schema per tenant applies to the one context type that uses `UseTenantry()`. An application with more than one,
  each of which 0.5 put in the tenant's schema, lists those that get it in `SchemaPerTenantOptions.Contexts`
  (`o.Contexts.Add(typeof(AppDbContext))`). Until it does, the host does not start and `migrate-tenants` returns 3,
  with an `InvalidOperationException` that names the context types; a context built outside the host fails when its
  options are built. A context type that derives from another, such as a test's, counts as that one.
- In mixed mode, a `Shared` tenant's context throws `TenantIsolationViolationException` (`ModelConfiguration`) when
  an entity in its model neither implements `ITenantEntity<TKey>` nor is marked with `[SharedAcrossTenants]` or
  `IsSharedAcrossTenants()`. Such an entity's rows were read and written across tenants.
- In mixed mode, `GetIsolation` returning `TenantIsolation.Schema` without `UseSchemaPerTenant` throws
  `InvalidOperationException` wherever the tenant's isolation is read (provisioning, offboarding, its contexts). The
  tenant used to get the shared schema, and provisioning reported success.
- `app.UseTenantryMetrics()` is gone: remove the call. `pro.AddTenantMetrics()` adds the `tenant.id` tag when
  `app.UseTenantry()` resolves the tenant, through Tenantry Core's `OnResolved`, after which an `OnResolved` handler
  of your own still runs. `Tenantry.Pro.AspNetCore` now depends on `Tenantry.AspNetCore`.
- Hangfire's `jobs.ForTenant(id)` and MassTransit's `context.SetTenant(id)` are `WithTenant`, as in the Quartz.NET
  and Rebus integrations. Each `WithTenant` also takes the tenant itself, which keeps an id of the wrong type from
  compiling.
- `ITenantMigrationRunner<TKey>` has two members, `MigrateAsync(options?, progress?, ct)` and
  `GetStatusAsync(options?, ct)`. `MigrateAllAsync`, `MigrateTenantAsync` and `GetTenantStatusAsync` are extension
  methods in `TenantMigrationRunnerExtensions`. Pass a token alone to `GetStatusAsync` by name
  (`GetStatusAsync(cancellationToken: ct)`).
- `TenantProvisioningStepResult` and `TenantProvisioningStepStatus` are `TenantLifecycleStepResult` and
  `TenantLifecycleStepStatus`, as offboarding's results use them too: rename them where your code names them.

### Added

- Offboarding: `ITenantDeprovisioner<TKey>.DeprovisionAsync(tenant)` runs the application's steps
  (`pro.AddDeprovisioningStep<T>()`), then drops the tenant's database or schema
  (`pro.AddDatabaseDeprovisioning<TContext>()` or `pro.AddSchemaDeprovisioning<TContext>()`) or deletes its rows from a shared database
  (`pro.AddSharedDataDeletion<TContext>()`), then clears what Tenantry caches for it. A failed step stops it, and a
  database or schema another tenant's context uses is never dropped. It refuses a tenant the store still has and that
  is active, read past this instance's tenant cache. Shared rows are deleted before any drop, and on a retry the
  application's steps see `TenantDeprovisioningContext.DataDropped` when a drop finds the data already gone. Log
  events 3105 to 3108 and 4005 to 4007.
- `ITenantMigrationRunner.MigrateAsync(MigrationRunOptions<TKey>)`: migrate the tenants it names (`Tenants`), leave
  some out (`ExcludedTenants`), and stop starting databases after a number of failures (`MaxFailures`). Those it did not
  start are in the report as not attempted (`MigrationResult.Attempted`, `MigrationReport.NotAttempted` and `Stopped`).
- `migrate-tenants --tenant <id> --exclude <id> --max-failures <n>`, with documented exit codes: 0 migrated, 1 a
  failure, 2 stopped early, 3 not started. A first SIGTERM or Ctrl+C lets the databases in progress finish and starts no
  other. An id the store does not have, excluded or selected, and a near miss of an option (`--tenants`) do not start
  the run.
- `TenantMigrationOptions.CreateMissingDatabases`, for development.
- `TenantBackgroundService` opens a log scope with `TenantId` for each tenant's work, as jobs and messages do.
- Background work honours Tenantry Core's `ValidateTenantActivity`: `TenantBackgroundService`,
  `PeriodicTenantBackgroundService`, Hangfire's `AddOrUpdateForEachTenant` and Quartz.NET's `ForEachTenant` skip a
  tenant it refuses (log event 3203), and a job or message for one is handled as unresolved (`OnUnresolvedTenant`,
  which by default throws `TenantInactiveException`; log events 3407 and 3408).
- `SchemaPerTenantOptions.Contexts`: list the context types that get the tenant's schema, and their subclasses, so
  another context that uses `UseTenantry()` can stay in the shared schema. With only one such context there is nothing
  to list; with more than one and none listed, schema per tenant fails closed rather than guess (see Upgrading).
- `ITenantPropagator` and `PropagatedTenant` are public, and `UsePro` registers the propagator, so the tenant can be
  carried over a bus or job library Tenantry.Pro has no integration for. `PropagatedTenant` is one of three outcomes:
  `Resolved(tenant)`, `WithoutTenant` or `Skipped`. `Use` runs work carried without a tenant as no tenant, even inside
  another tenant's flow, and refuses skipped work.
- A public API for propagation adapters, which the four integrations now use. Register an
  `ITenantPropagationAdapter` with `TenantPropagationAdapter.Add`: it gets its own `TenantPropagationOptions`, the
  propagator, and the startup check that fails the host when the adapter's host side never ran. The host side calls
  `TenantPropagationIntegration<TAdapter>.MarkWired()` and carries the tenant with its `Propagator` and `Options`.
  `TenantPropagationAdapter.FormatTenantId` formats an id for a `WithTenant` method. These types and the propagator
  are `[EditorBrowsable(EditorBrowsableState.Advanced)]`, and the API reference lists them apart, as extension points;
  the background jobs guide has an example.
- Log events 4114 to 4117: a run that stopped early, a stop signal, a deployment step that could not start, and EF
  Core's diagnostic events being unobservable for a context.

### Changed

- `Tenantry.Pro.Hangfire`, `Tenantry.Pro.MassTransit`, `Tenantry.Pro.Quartz` and `Tenantry.Pro.Rebus` use only
  `Tenantry.Pro`'s public API, so each depends on `Tenantry.Pro` from its own release up to the next minor, rather
  than on exactly its own release. `Tenantry.Pro.EfCore` still depends on exactly its own release.
- Invalidating a tenant with Tenantry Core's `ITenantInvalidator` clears its cached connection string too
  (`CacheConnectionStrings`).
- A run for some tenants, `MigrateTenantAsync` included, names every tenant of a database or schema it migrated in the
  result: migrating one tenant of a shared database migrates it for all of them.
- Tenants whose connection strings reach the same server, port and database share their migrations and their migration
  health, though the strings differ in a setting such as a timeout: such a database is migrated once, and excluding
  one of its tenants leaves it out. When the model sets no schema, tenants whose login, or PostgreSQL search path,
  differs are kept apart, since their tables are in different schemas.
- Every public Tenantry.Pro.EfCore method that configures or creates an EF Core context or model carries
  `[RequiresUnreferencedCode]` and `[RequiresDynamicCode]`, as Tenantry.EfCore's do: `UseSchemaPerTenant`,
  `AddDatabaseProvisioning`, `AddSchemaProvisioning`, `AddSharedDataDeletion` and `AddTenantDatabaseCheck` now warn
  under the trim and AOT analyzers too.
- `CacheConnectionStrings` wraps the connection-string provider through Tenantry Core's `DecorateConnectionStrings`,
  and passes on its `CanGetSynchronously`. The tenant key type comes from Core's `ITenantKeyType`.
- The tenant ids, telemetry names and propagation header come from Tenantry Core's `TenantIds`, `TenantTelemetry` and
  `TenantPropagation`, which Tenantry.Http uses too.
- The migration guide and the database-per-tenant samples no longer list an init container as a way to migrate once
  per deployment: it runs in every replica. The migration guide also says to run one migration step at a time, and to
  invalidate Tenantry's caches when you restore a tenant. The licensing guide says that when a subscription ends
  restores from the feed fail for every version, so keep copies of the packages you build with, as the installation
  guide says. The readme says Tenantry.Pro is in beta until 1.0.

### Fixed

- An audit entry for an insert records in `NewValues` what the database generated (an identity key, a default, a
  computed column), not EF Core's temporary value from before the insert.
- The audit entries of a transaction that rolls back, or whose commit or rollback fails, are dropped at once. Npgsql
  reuses its transaction objects, so a later transaction handed to a context with `UseTransaction` could write them.

## [0.5.0] - 2026-10-03

### Upgrading from 0.4

0.5 reshapes the public API once, before 1.0, and runs on Tenantry Core 0.5: update the Core calls first, with
Core's changelog ("Upgrading from 0.4"). Registration code needs no Tenantry.Pro `using` directive any more; code
that uses Pro's types needs `using Tenantry.Pro;` (and `using Tenantry.Pro.EfCore;` or
`using Tenantry.Pro.AspNetCore;` for theirs). The **Breaking** entries below give the details.

| 0.4 | 0.5 |
|-----|-----|
| packages `Tenantry.Pro.EfCore.SqlServer`, `.Npgsql` and `.MySql` | `Tenantry.Pro.EfCore`, which works through your context's own EF Core provider |
| package `Tenantry.Pro.HealthChecks` | `Tenantry.Pro.EfCore` |
| namespaces `Tenantry.Pro.BackgroundServices`, `.Exceptions`, `.Licensing`, `.Lifecycle` and `.Strategies.*` | `Tenantry.Pro` |
| namespaces `Tenantry.Pro.EfCore.Audit`, `.Migrations` and `.Strategies.*` | `Tenantry.Pro.EfCore` |
| namespaces `Tenantry.Pro.AspNetCore.Telemetry` and `.Telemetry.Extensions` | `Tenantry.Pro.AspNetCore` |
| `pro.WithLicence(key)`, the `Tenantry:Licence` setting | `pro.UseLicenseKey(key)`, the `Tenantry:License` setting (`Tenantry__License`) |
| `pro.UseDatabasePerTenant(…)` and `DatabasePerTenantOptions` | Core's `tenant.UseConnectionStrings(…)` and `tenant.AddDbContextPerTenantDatabase<TContext>(…)` |
| `DatabasePerTenantOptions.CacheConnectionStrings` and `CacheDuration` | `pro.CacheConnectionStrings(o => o.Duration = …)` |
| connection-string encryption (`UseDataProtectionEncryption()`, `ConnectionStringEncryptionOptions`) | removed |
| `ITenantLifecycleManager<TKey>`, `pro.AddLifecycleManagement(…)`, `TenantLifecycleOptions` | `ITenantProvisioner<TKey>` (always registered), `pro.ConfigureProvisioning(o => …)`, `TenantProvisioningOptions` |
| a seeder registered in the service collection, with `SeedAsync(tenant, scopedProvider, ct)` | `pro.AddSeeder<T>()`, with `SeedAsync(tenant, ct)` and its services in the constructor; **a seeder that is only in the service collection is no longer run** |
| `pro.UseMixedMode(o => o.GetStrategyForTenant = …)`, `TenantStrategy` | `pro.UseMixedMode(o => o.GetIsolation = …)`, `TenantIsolation` |
| `pro.AddDatabaseProvisioning()`, `pro.AddSchemaProvisioning(o => o.ConnectionString = …)`, `DatabaseProvisioningService<TKey>`, `SchemaProvisioningService<TKey>` | `pro.AddDatabaseProvisioning<TContext>()`, `pro.AddSchemaProvisioning<TContext>()`; provision with `ITenantProvisioner<TKey>` |
| `pro.AddSchemaPerTenantCaching()`, `options.AddSchemaPerTenantCaching<TKey>(sp)`, `ISchemaNameResolver<TKey>`, `HasDefaultSchema(…)` in `OnModelCreating` | `pro.UseSchemaPerTenant(o => o.GetSchemaName = …)` and `options.UseTenantry()` |
| `pro.WithMigrationOrchestration<TKey, TContext>(factory, runAtStartup, failStartupOnMigrationError)`, `MigrationOrchestratorService<TKey, TContext>`, `MigrationStatusTracker<TKey, TContext>` | `pro.AddMigrations<TContext>(o => o.OnStartup = …)`, `ITenantMigrationRunner<TKey>` |
| `AddTenantryDatabaseCheck<TKey>(…)`, `AddTenantryMigrationCheck<TKey, TContext>(factory)`, `TenantHealthCheckOptions.ConnectionFactory` | `AddTenantDatabaseCheck<TContext>()`, `AddTenantMigrationCheck<TContext>()`, through the application's context |
| `options.UseAuditLogging(sp)`, `AuditOptions.ExcludeTypes` | `pro.AddAuditLogging()` for every context with `options.UseTenantry()`; `Exclude<T>()`, `ExcludeProperty<T>(…)`, `ShouldAudit` |
| `TenantBackgroundService<TKey>.ExecuteForTenantAsync(tenant, scopedProvider, ct)`, the constructor's `storeAccessor` | `ExecuteForTenantAsync(scope, ct)`, the constructor's `ITenantLookup<TKey> tenantLookup` |
| `pro.AddTenantMetrics()` and the `tenantry.requests.*` instruments | the `tenant.id` tag on ASP.NET Core's `http.server.request.duration` (see Changed for dashboards) |
| `AddHangfireTenantFilter()`, `app.UseTenantryHangfire()` | `pro.AddHangfirePropagation()`, `config.UseTenantry(sp)` in `AddHangfire` |
| `AddMassTransitTenantFilters()`, `x.AddTenantryConsumeFilter()`, `cfg.UseTenantryPro(ctx)` | `pro.AddMassTransitPropagation()`, `cfg.UseTenantry(context)` |
| `AddQuartzTenantScope()` | `pro.AddQuartzPropagation()`, `q.UseTenantry()` in `AddQuartz` |
| `AddRebusTenantSteps()`, `o.UseTenantryPro(sp)` | `pro.AddRebusPropagation()`, `o.UseTenantry(sp)` |
| `MissingTenantBehavior` in the integrations' propagation options | `TenantPropagationBehavior` (`Allow`, `Warn`, `Skip`, `Reject`) |

### Added

- Audit entries say who made the change and what made it: `AuditEntry.Actor`, `CorrelationId` and `Data`, from an
  `IAuditContextProvider` (by default no actor, and the current trace's id; register your own to name the user, as
  the audit logging guide shows for ASP.NET Core), and EF Core's name for the entity type, `EntityType`. With
  `AuditOptions.OnStoreFailure = AuditStoreFailureBehavior.Throw`, a store that fails to write the entries of changes
  already saved makes `SaveChanges`, or the commit, throw an `AuditStoreException` holding the entries (by default the
  failure is logged, as before).
- Migrating several databases or schemas at once: `pro.AddMigrations<TContext>(o => o.MaxConcurrency = 8)` (1 by
  default), which also bounds `GetStatusAsync`.
  `ITenantMigrationRunner<TKey>.MigrateAllAsync(IProgress<MigrationResult<TKey>>, …)` reports each one's result as it
  completes, with every tenant whose data is there. To know which tenants share a database or schema before migrating
  it, a run now creates each tenant's context first, so the first tenant's context of each is created twice.
- Migrations for schema per tenant: `pro.AddMigrations<TContext>()` applies migrations generated without a schema
  (as `dotnet ef migrations add` generates them, with no tenant) to each tenant's schema, with a migration history
  table in that schema, and gives the snapshot the tenant's schema for EF Core 9+'s pending-changes check. Tested on
  SQL Server and PostgreSQL; SQL in `migrationBuilder.Sql(…)` is applied as written. See Changed for `AddMigrations`.
- `app.RunTenantMigrationsIfRequestedAsync(args)` (an `IHost` extension): with the argument `migrate-tenants`, migrates every
  tenant and returns the exit code (1 if any database or schema failed), so a deployment step is one line.
- Provisioning steps of your own: `ITenantProvisioningStep<TKey>`, added with `pro.AddProvisioningStep<T>()`,
  runs for each new tenant in a scope for it, after Tenantry.Pro's own steps; its `AppliesTo` can skip a tenant,
  for example by its isolation in mixed mode. See Changed for the provisioning API.
- `IConnectionStringCache<TKey>.InvalidateAll()`, to drop every tenant's cached connection string after
  rotating credentials.
- Jobs and messages for a tenant by name, whichever tenant is current: `jobs.ForTenant(tenantId).Enqueue(…)` for
  Hangfire (any `Enqueue`, `Schedule` or `ContinueJobWith`), `Publish(message, context => context.SetTenant(tenantId))`
  or `Send(…)` for MassTransit, and `bus.Send(message, new Dictionary<string, string>().WithTenant(tenantId))` (or
  `Publish`, `Defer`) for Rebus. A tenant given this way wins over the current one: Hangfire's filter and Rebus's step
  add the current tenant only to a job or message that carries none, and MassTransit runs the callback after its
  filters.
- Recurring work for each tenant: `recurringJobs.AddOrUpdateForEachTenant<T>(id, job => …, cron)` for Hangfire, whose
  runs enqueue the job once for each tenant in the store, and `new JobDataMap().ForEachTenant()` for a Quartz.NET job,
  which then, whenever it fires without a tenant, schedules a run for each tenant with the firing trigger's data and
  priority. Each tenant's job runs, and fails, on its own; Hangfire's recurring jobs otherwise run without a tenant.
- MassTransit routing slips (Courier): `cfg.UseTenantry(context)` adds Tenantry's execute and compensate activity
  filters, so each activity executes and compensates as the routing slip's tenant, which it passes on. They ran
  without it.
- Jobs and messages name their tenant in logs and traces: while a Hangfire job, Quartz.NET job, MassTransit consumer
  or activity, or Rebus handler runs as its tenant, a log scope with `TenantId` is open, and its trace span is tagged
  `tenant.id` (for a MassTransit consumer, the message's receive span, of which the consumer's is a child).
- CI builds every ```csharp block in the README and docs against the packed packages
  (`scripts/check-doc-snippets.sh`), so a guide can no longer show code that does not compile; a block that is
  not meant to compile is marked ```csharp no-compile.
- An `.editorconfig` shared with Tenantry core, checked in CI with `dotnet format --verify-no-changes`.
- `SchemaPerTenantOptions<TKey>.MaxCachedSchemas`, how many schemas' compiled models each context type keeps
  (500 by default), and `MaxCompiledQueriesPerSchema`, room for each schema's compiled queries (100 by
  default), set in `pro.UseSchemaPerTenant(o => …)`. See Fixed.
- CI starts every sample in Development, where the host validates its registrations, against SQL Server,
  MySQL and PostgreSQL in containers and with a licence signed for the run, and checks each does what its
  README says (`scripts/smoke-samples.cs`). Each package has a conformance test: its features registered
  through `UsePro`, scopes and every registration validated, every Tenantry service resolved in a tenant's
  scope, the host started (licence check included). A weekly lane builds and tests Pro against Tenantry
  Core's `master` (`scripts/build-against-local-core.sh --test`).

### Fixed

- `AuditEntry.PrimaryKey` showed a byte array key as `System.Byte[]`, and a date key without its fractional seconds,
  so entries of different rows could not be told apart, and wrote other values in the server's culture, so a decimal
  key's comma read as a separator. A byte array is now written in hexadecimal (`0x0102`), a date or time to the tick
  (`2026-10-02T13:04:05.1230000`, a `DateTime` without its kind, so an inserted row's key matches its later entries'),
  a strongly typed id (a value-converted key that cannot be formatted in the invariant culture) as the value it
  is stored as, and every other value in the invariant culture.
- Audit entries left out the values of complex properties (`ComplexProperty`, and EF Core 10's `ComplexCollection`):
  an update of `Address.City` was saved and recorded with no old or new values. They are now recorded by path
  (`Address.City`), and a complex collection whole, as a list of its elements' values, when it changes.
  `ExcludeProperty` leaves out a complex property, or, named on the complex type, one of its properties (it no longer
  requires a class, so a struct complex type can be named).
- Audit entries held the entity's own instances of mutable values, such as a byte array, so changing one after
  `SaveChanges`, before the transaction committed, changed the entry but not the saved row. Each value is now copied
  when the save starts, as EF Core copies it to detect changes, and an array is copied too.
- The API reference repeated a type parameter's variance in its constraints (`where TKey : IEquatable<in TKey>`),
  which is not C#.
- With a `Guid` or `int` tenant key, a Hangfire job, MassTransit or Rebus message or audit entry created without a
  tenant carried the key's default (`00000000-…`, `0`) as its tenant: without a tenant,
  `ITenantContext.CurrentTenantId` is the key's default, not null. They now carry no tenant.
- `Tenantry.Pro.MassTransit` failed on MassTransit 8.1 and later: `AddTenantryConsumeFilter` threw
  `MissingMethodException` at startup, because MassTransit 8.1 changed the endpoint-callback delegate it uses
  and the package was built against 8.0. It now requires MassTransit 8.1 or later 8.x. (Found by the sample
  smoke check; the tests ran on 8.0.)
- Quartz jobs whose constructor takes a scoped service that reads the tenant (a `DbContext` choosing its
  connection, say) got it without a tenant: Quartz's DI job factory creates the job, and its scoped
  dependencies, before the tenant scope opened. The job is now created inside the scope, and returned to
  Quartz's factory before the scope closes; with `Skip` it is not created at all. A constructor that throws
  now fails that run of the job, which Quartz retries on the next firing, instead of putting its triggers in
  the `Error` state.
- Rebus: a message whose tenant could not be resolved under `Reject`, or whose tenant lookup threw, was never
  retried or moved to the error queue, but redelivered for ever: the incoming step ran in front of Rebus's
  retry step. It now runs just before Rebus deserializes the message, and a pipeline without that step fails
  to start rather than skip the tenant step. Data bus hydration and decryption, which Rebus runs before
  deserializing, now run outside the tenant scope. The tenant lookup is cancelled when the bus stops, and log
  messages name the message id.
- MassTransit: an endpoint configured with `cfg.ReceiveEndpoint(…)` consumed messages without their tenant, because
  `AddTenantryConsumeFilter` reached only the endpoints `ConfigureEndpoints` configured. `cfg.UseTenantry(context)`
  covers every endpoint, consumers, sagas and handlers alike, and the consume filter runs inside MassTransit's
  message retry, so a rejected message is retried like a consumer that throws.
- MassTransit: a batch consumer (`IConsumer<Batch<T>>`) could consume a batch whose messages carried different
  tenants as one of those tenants. A batch is consumed as its messages' tenant only when they all carry it; one that
  mixes tenants fails, and the guide shows how to group batches by tenant.
- A tenant id carried by a job or message was formatted and parsed with the current culture, so an `int` or `long`
  id could fail to round trip between cultures that write a minus sign differently. Both use the invariant culture.
- The Hangfire guide said a job skipped by the `Skip` policy is marked succeeded; Hangfire deletes it ("Canceled by
  filter").
- `PeriodicTenantBackgroundService` stopped for good (and, by default, stopped the host) when a sweep failed
  as a whole, for example because the tenant store could not be read. The failure is now logged, and the next
  sweep runs on schedule.
- Schema per tenant: EF Core's cache holds the models of about 40 schemas (about 100 on EF Core 8), and about
  500 compiled queries, which EF Core compiles again for each schema's model; so with more tenants in use it
  evicted and compiled them again as tenants took turns. Each context type now has its own cache with room for
  `MaxCachedSchemas` schemas and `MaxCompiledQueriesPerSchema` queries each, and the models are keyed on the
  schema name rather than the tenant id, so tenants that share a schema share a model. The documentation said
  EF Core's model cache never evicts.
- Schema per tenant on a pooled context (`AddDbContextPool`, `AddPooledDbContextFactory`, Core's pooled
  database-per-tenant registration) queried the first tenant's schema for every tenant: a pooled context keeps the
  model it was first built with. Schema per tenant now refuses a pooled context: creating one throws
  `InvalidOperationException`.
- Audit logging resolved `IAuditStore` once, into the singleton interceptor: a scoped store, such as one that
  saves through a `DbContext`, failed scope validation, or was kept for the application's lifetime with its
  context. The store is now resolved for each save from the saving context's scope, or from a scope created
  for the save when the context has none (pooled, from an `IDbContextFactory`, or created by hand), so a
  scoped store works everywhere and a transient one is disposed.

- Every package's README said "Licensed under the Apache License 2.0". The packages now carry their own
  README for customers (`eng/package-readme.md`): the commercial licence, the feed, links to tenantry.dev and
  the support address. `Tenantry.Pro.AspNetCore`'s description and the docs no longer claim per-request
  licence enforcement, which does not exist (the key is checked once, at startup). Security reports go to
  support@tenantry.dev rather than the private repository.
- The getting-started guide, the README and most guides left out the using directives their code needs, and
  showed APIs from packages they did not name (a Hangfire memory storage, RabbitMQ).
- The `HangfireJobs` sample could not start: it registered no tenant store and never resolved a
  tenant, so no job carried one. It now runs with in-memory tenants and job storage, and its job depends on a
  scoped service that reads the tenant.
- The MassTransit guide and the `Skip` documentation said it acknowledges and drops a message; MassTransit
  moves it to the endpoint's `_skipped` queue, and what `Skip` does is up to each host. The Hangfire guide now says recurring jobs run without a tenant; see Added for one
  that runs for each tenant.

- `MigrationResult.AppliedMigrations` lists what the run applied. It listed the migrations pending when the
  run started, so of two runners migrating the same tenant at once, the one that waited for EF Core's lock
  and applied nothing reported them too; and it was always empty when a run failed, even after committing
  some. It now lists the migrations whose row in the migration history this run wrote and committed (from EF
  Core's diagnostic events): each once when an execution strategy retries, never one another runner applied,
  including one it waited for, failed on or skipped on a retry, and on failure those committed before it.
- Reading migration status (`GetStatusAsync`, was `MigrationStatusTracker`'s) no longer fails for every tenant when
  one tenant's database cannot be read. That entry has the new `Error` set, and `IsUpToDate` is false;
  `GetTenantStatusAsync` returns such an entry instead of throwing. Cancellation still throws.
- Provisioning the same tenant twice at once (a double submit, a redelivered message) no longer fails with
  "already exists" in the `CreateDatabase` or `CreateSchema` step: a run whose `CREATE DATABASE` fails waits
  for the database another run created to come online, and a run whose `CREATE SCHEMA` fails checks for the
  schema again.
- The database health check reports the registration's failure status instead of always `Unhealthy`, and checks
  up to `TenantHealthCheckOptions.MaxConcurrency` databases at a time (default 8) instead of one after another, so
  unreachable tenants no longer each add a full timeout in turn.
- A failed migration was logged twice, once as it failed and again in the run's summary. It is logged once, and the
  summary of a run with failures is a warning.
- Creating several tenants' contexts at once in a new application, as the tenant health checks do, could fail on
  Oracle's `MySql.EntityFrameworkCore`, which builds its type mappings, shared by every context, the first time
  without a lock (seen in CI). Tenantry.Pro now works through tenants one at a time until one tenant's context has
  been created and used, then goes on several at once. Two such runs at once in one new process can still meet there.
- `AuditEntry.TenantId` and the tenant health checks' data keys (`tenant:{id}`) format the tenant id with the
  invariant culture, as job and message headers and traces do. They used the machine's culture, so for a key type
  whose text depends on it they could differ from the id elsewhere.
- `ProvisionAsync`, `MigrateTenantAsync` and `GetTenantStatusAsync` throw `ArgumentException` for an id Tenantry
  reserves for "no tenant" (the key type's default, `Guid.Empty` or `0`, or an empty string), before anything runs,
  as Tenantry Core's `RunInScopeAsync` does. Provisioning such a tenant reported success when no steps were
  registered and a failed step otherwise, and the migration runner asked the store for it and threw
  `TenantNotFoundException`. Every Pro package now applies the same rules for formatting tenant ids and refusing the
  reserved ones.

### Changed

- `Tenantry.Pro.Hangfire`, `Tenantry.Pro.MassTransit`, `Tenantry.Pro.Quartz` and `Tenantry.Pro.Rebus` use only
  `Tenantry.Pro`'s public API, so each depends on `Tenantry.Pro` from its own release up to the next minor, rather
  than on exactly its own release. `Tenantry.Pro.EfCore` still depends on exactly its own release.
- **Breaking:** Tenantry.Pro follows Tenantry Core 0.5's API (see Core's changelog, "Upgrading from 0.4"):
  `AddTenantry` is the one entry point for every host (`AddTenantryCore` is gone), Core's types are in the
  `Tenantry` namespace and its registration methods need no `using`, `ITenantContextSetter<TKey>.Use` makes a
  tenant current (was `ITenantScope.BeginScope`), `ITenantScopeFactory` creates an `ITenantScope<TKey>` (was
  `ITenantServiceScope`), singletons read tenants through `ITenantLookup<TKey>` (was `ITenantStoreAccessor`), and
  a tenant's connection string comes from `ITenantConnectionStringProvider<TKey>.Get` or, for the current tenant,
  `CurrentTenantConnectionString<TKey>.Get` (was `ITenantConnectionStringResolver.Resolve`).
  Core's `tenant.AddDbContextPerTenantDatabase<TContext>(…)`, pooled or not, after `tenant.UseConnectionStrings(…)`,
  replaces Core's `AddTenantDbContextPool` for a database per tenant.
- **Breaking:** the propagation options of the Hangfire, MassTransit, Quartz and Rebus integrations take
  `Tenantry.Pro.TenantPropagationBehavior` (`Allow`, `Warn`, `Skip`, `Reject`), in place of Core's
  `MissingTenantBehavior`, which is now EF Core's and has no `Skip`.
- **Breaking:** `UsePro` passes an `IProBuilder<TKey>`, a public interface in `Tenantry.Pro`; the builder
  class it replaces (`Tenantry.Pro.Internal.ProBuilder<TKey>`) is internal and only `UsePro`, which registers the
  licence check, creates it. `UseSchemaPerTenant` and `UseMixedMode` are extension methods like the other
  features. A feature that takes a type parameter of its own can register through
  `IProBuilder.Add(IProRegistration)`, which applies it with the builder's key type, so its callers need not
  repeat the key type.
- **Breaking:** Tenantry.Pro's types are in the `Tenantry.Pro` namespace (they were in
  `Tenantry.Pro.BackgroundServices`, `.Exceptions`, `.Licensing`, `.Lifecycle` and `.Strategies.*`), and
  Tenantry.Pro.AspNetCore's in `Tenantry.Pro.AspNetCore` (were `.Telemetry` and `.Telemetry.Extensions`).
  Registration methods (`UsePro`, the builder's and `AddTenantMetrics`) are in
  `Microsoft.Extensions.DependencyInjection`, and `UseTenantryMetrics` in `Microsoft.AspNetCore.Builder`, so they
  need no `using`.
- **Breaking:** `UsePro` reads the licence key from the `Tenantry:License` setting (the `Tenantry__License`
  environment variable) when the application starts, so configuration added after the services, such as a test
  host's, reaches it; `pro.UseLicenseKey(key)` sets it in code instead. `pro.WithLicence(key)` and the
  `Tenantry:Licence` setting are gone, and `LicenseOptions` is internal. Code identifiers use American spelling.
- **Breaking:** a licence key must name the key it was signed with (`kid`, the key's RFC 7638 thumbprint), and
  carry the audience `tenantry-pro` (`aud`) and the licence format `1` (`ver`), as keys issued by tenantry.dev
  now do. A key in the earlier format names none of them and no longer validates; no customer was issued one,
  and support@tenantry.dev replaces any that turns up. Pro finds the public key by its `kid`, so a future signing key can be added without
  invalidating keys already issued; a key in a newer format than the package reads asks for an update.
- **Breaking:** a database per tenant is Tenantry Core's: `pro.UseDatabasePerTenant(…)` and
  `DatabasePerTenantOptions` are gone. Set the connection strings with `tenant.UseConnectionStrings(…)` and
  register the context with `tenant.AddDbContextPerTenantDatabase<TContext>(…)`. `pro.CacheConnectionStrings(o =>
  o.Duration = …)` caches them (was `CacheConnectionStrings` and `CacheDuration`): it wraps the provider
  `UseConnectionStrings` registers, whether called before or after `UsePro`, or one of your own registered before
  `UsePro`. The application fails to start if there is none, or if the duration is not positive, and warns at
  startup if a provider registered later took the cache's place. `IConnectionStringCache<TKey>`
  is always registered and has `InvalidateAll()`; a connection string read while it is being invalidated is no
  longer cached afterwards.
- **Breaking:** tenant provisioning. `ITenantLifecycleManager<TKey>` is `ITenantProvisioner<TKey>`, which `UsePro`
  always registers, as a singleton (`pro.AddLifecycleManagement(…)` is gone; `pro.ConfigureProvisioning(o => …)`
  sets `TenantProvisioningOptions`, was `TenantLifecycleOptions`). It runs `ITenantProvisioningStep<TKey>`s:
  Pro's own first (creating the database or schema, then migrations), then the steps and seeders added with
  `pro.AddProvisioningStep<T>()` and `pro.AddSeeder<T>()`, in the order they were added. Each step is resolved
  from a new scope for the tenant and gets the tenant, so it no longer reads it from the store again, and, in
  mixed mode, its isolation; its `AppliesTo` can skip a tenant. `TenantProvisioningResult` lists every step's
  outcome (`Steps`: `Succeeded`, `Failed`, `Skipped` or `NotRun`) in place of `CompletedUpTo` and the
  `TenantProvisioningStep` enum. `ITenantSeeder<TKey>.SeedAsync(tenant, ct)` no longer takes a service provider:
  a seeder takes the services it needs, such as the `DbContext`, in its constructor; and every seeder added runs
  (only the first registered one ran). **Add seeders with `pro.AddSeeder<T>()`: a seeder registered only in the
  service collection (`AddScoped<ITenantSeeder<TKey>, T>()`, as 0.4 documented) is no longer run.**
  `ITenantInfrastructureProvisioner` and `ITenantMigrator` are gone: `AddDatabaseProvisioning`,
  `AddSchemaProvisioning` and `AddMigrations` (see below) add steps. Cancelling stops provisioning before the next
  step, and a step that fails after the cancellation is reported as `OperationCanceledException`.
- `MigrateTenantAsync` and `GetTenantStatusAsync` throw Core's `TenantNotFoundException`
  for a tenant the store does not return. It derives
  from `InvalidOperationException`, which they threw before, so existing handlers still catch it.
- **Breaking:** mixed mode is honoured by provisioning. `pro.UseMixedMode(o => o.GetIsolation = …)` returns each
  tenant's `TenantIsolation` (`Shared`, `Schema` or `Database`; was `GetStrategyForTenant` and `TenantStrategy`).
  Creating a database applies only to `Database` tenants, creating a schema only to `Schema` tenants, and
  migrations, which go to each distinct database or schema, to both (see below). `MixedStrategyResolver` is gone: each tenant's connection string comes from the
  `UseConnectionStrings` delegate.
- **Breaking:** per-tenant request metrics are ASP.NET Core's. `pro.AddTenantMetrics()` and
  `app.UseTenantryMetrics()` add a `tenant.id` tag to ASP.NET Core's `http.server.request.duration` (a histogram
  in seconds, on the `Microsoft.AspNetCore.Hosting` meter, already tagged with the route, method, status code and
  `error.type`) instead of recording Tenantry.Pro's own instruments; see Removed. For dashboards:
  `tenantry.requests.count` is the histogram's count, `tenantry.requests.duration` (milliseconds) the histogram,
  and `tenantry.requests.errors` the requests with `error.type` or a `5xx` `http.response.status_code`.
  `tenantry.requests.active` has no per-tenant replacement: ASP.NET Core records `http.server.active_requests`
  before the tenant is known. A request without a tenant has no `tenant.id` tag (it was `unknown`).
  `TenantMetricsOptions<TKey>.GetTagValue` sets the tag for each tenant, or leaves it off, to bound the number of
  series; `ExcludePaths` and `AdditionalTags` are gone (on ASP.NET Core 9 and later, `DisableHttpMetrics()` leaves
  an endpoint out of the metric). `UseTenantryMetrics` without `AddTenantMetrics` says so.
- **Breaking:** `TenantBackgroundService<TKey>.ExecuteForTenantAsync` takes the tenant's `ITenantScope<TKey>`
  (`scope.Tenant`, `scope.ServiceProvider`) in place of the tenant and a service provider, and the base class has
  a protected `Logger`, the logger passed to its constructor. Its constructor, and
  `PeriodicTenantBackgroundService<TKey>`'s, take an `ITenantLookup<TKey>` named `tenantLookup` (was `storeAccessor`).
- **Breaking:** provisioning works through your context's own EF Core provider, so the provider packages are
  gone (see Removed). `pro.AddDatabaseProvisioning<TContext>()` and `pro.AddSchemaProvisioning<TContext>()`, in
  `Tenantry.Pro.EfCore`, replace their `AddDatabaseProvisioning()` and `AddSchemaProvisioning(o =>
  o.ConnectionString = …)`: the step resolves `TContext` in the tenant's scope, so it connects as the
  application does, and creates the database through EF Core's database creator, or the schema with EF Core's
  migrations SQL (SQL Server and PostgreSQL; another provider fails the step with `NotSupportedException`).
  `o.CreateContext` gives the step a context of its own, with credentials allowed to create databases or
  schemas. A run whose create fails but finds the database once it waits (up to 30 seconds) succeeds and logs
  the failure as a warning. `DatabaseProvisioningService<TKey>`, `SchemaProvisioningService<TKey>`, their base classes,
  `SchemaProvisioningOptions.ConnectionString` and `ProvisionAsync(tenantId)` are gone: provision with
  `ITenantProvisioner<TKey>`. The methods take the context type, so they return the builder without its key type:
  call them after the others in a chain.
- **Breaking:** schema per tenant needs nothing in the context. `pro.UseSchemaPerTenant(o => o.GetSchemaName = …)`
  is in `Tenantry.Pro.EfCore`, and every context that uses `UseTenantry()` gets the current tenant's schema as
  its default schema, after `OnModelCreating`, with a compiled model per schema. `pro.AddSchemaPerTenantCaching()`,
  `options.AddSchemaPerTenantCaching(sp)`, `ISchemaNameResolver<TKey>` and the public
  `TenantModelCacheKeyFactory<TKey>` are gone: replace `options.AddSchemaPerTenantCaching<TKey>(sp)` with
  `options.UseTenantry()` (a context without it gets no tenant schema, and every tenant would use the
  database's default), and remove `pro.AddSchemaPerTenantCaching()`, `ISchemaNameResolver<TKey>` and the
  `HasDefaultSchema(…)` call in `OnModelCreating`. `SchemaPerTenantOptions<TKey>` is in `Tenantry.Pro.EfCore` and has the cache limits (was
  `SchemaPerTenantCachingOptions`); options that are not valid, `GetSchemaName` missing included, stop the
  application from starting. In mixed mode `GetSchemaName` is called only for `Schema` tenants, whose contexts
  get their schema; the others keep the database's default. Without a current tenant, such as in `dotnet ef`, the
  model has no default schema, so migrations are generated without one. A schema name that is empty, has
  control characters, or is longer than the database allows (128 characters on SQL Server, 63 bytes on
  PostgreSQL, which would otherwise shorten it) fails the tenant's queries with `InvalidOperationException`.
  Each context type's model cache belongs to an EF Core internal service provider of the context type's own,
  which every application in a process shares, so a test suite that starts many hosts does not reach EF Core's
  limit of 20 internal service providers; an application with more than about 20 such context types would.
  `UseMemoryCache`, or `ReplaceService` of `IModelCacheKeyFactory` or `IMemoryCache`, on such a context's
  options throws `InvalidOperationException`, since either would undo the cache or let tenants share a model.
- **Breaking:** Tenantry.Pro.EfCore's provisioning, schema-per-tenant, migration, health-check and audit types are in
  `Tenantry.Pro.EfCore` (they were in `.Strategies.DatabasePerTenant`, `.Strategies.SchemaPerTenant`, `.Migrations`,
  `.Audit` and `.Extensions`), its builder and health-check methods (`UseSchemaPerTenant`, `AddDatabaseProvisioning`,
  `AddSchemaProvisioning`, `AddMigrations`, `AddAuditLogging`, `AddTenantDatabaseCheck`, `AddTenantMigrationCheck`) in
  `Microsoft.Extensions.DependencyInjection`, and `RunTenantMigrationsIfRequestedAsync` in
  `Microsoft.Extensions.Hosting`, so they need no `using`.
- **Breaking:** migrations. `pro.AddMigrations<TContext>(o => …)` replaces
  `pro.WithMigrationOrchestration<TKey, TContext>(factory, runAtStartup, failStartupOnMigrationError)`, and
  `ITenantMigrationRunner<TKey>` (`MigrateAllAsync`, `MigrateTenantAsync`, `GetStatusAsync`, `GetTenantStatusAsync`)
  replaces `MigrationOrchestratorService<TKey, TContext>` and `MigrationStatusTracker<TKey, TContext>`. The context
  comes from the application's registration in each tenant's scope (its `IDbContextFactory<TContext>` when it cannot
  be created there, as with only an asynchronous connection string), so there is no factory from a connection string;
  `o.CreateContext` creates one of its own, for other credentials. One runner migrates every context added, in the
  order added, and tenants whose context has the same connection string, default schema and migration history
  table are migrated once: a result or status entry is per context and database or schema, with its `TenantIds`,
  `ContextType`, `Database` and `Schema` (it had one `TenantId`), `MigrateTenantAsync` returns a report, and
  `GetTenantStatusAsync` a list of entries, one per context (it returned one entry). `o.OnStartup = StartupMigrations.LogFailures` or `FailOnError` replaces `runAtStartup` and
  `failStartupOnMigrationError`. Every context added is a `Migrations` provisioning step; in mixed mode it applies
  to `Database` and `Schema` tenants (only `Database` before), and the shared database is migrated with the other
  tenants. `UseConnectionStrings` is no longer required.
- **Breaking:** the health checks are in `Tenantry.Pro.EfCore` (see Removed).
  `AddTenantDatabaseCheck<TContext>()` and `AddTenantMigrationCheck<TContext>()` replace
  `AddTenantryDatabaseCheck<TKey>(…)` and `AddTenantryMigrationCheck<TKey, TContext>(factory)`: they go through the
  application's context in each tenant's scope, so `TenantHealthCheckOptions.ConnectionFactory` is gone, and read
  each distinct database (or, for migrations, database and schema) once. They take the standard `name`,
  `failureStatus` (Degraded by default), `tags`, `timeout` (30 seconds by default) and `configure` arguments, which replace
  `TenantHealthCheckOptions.FailureStatus` and `Tags`; the default names are `tenant-databases` and
  `tenant-migrations` (were `tenantry-databases` and `tenantry-migrations`). `DatabaseTimeout` (was
  `ConnectionTimeout`) and `MaxConcurrency` apply to both checks, and a check reports its last result for
  `CacheDuration` (30 seconds by default), so frequent polls do not each reach every tenant database. A tenant's
  entry reads `reachable` or `unreachable: …` (was `healthy` or `unhealthy: …`). They need `UsePro`: without it they
  report their failure status, saying so.
- **Breaking:** audit logging. `pro.AddAuditLogging()` audits every context that uses `UseTenantry()`, which adds the
  audit interceptor after Tenantry's own, so `options.UseAuditLogging(sp)` and `options.UseAuditLogging<TKey>(sp)` are
  gone, and a new tenant-owned entity is always recorded with its `TenantId`. A context that does not use
  `UseTenantry()` is not audited, nor are the saves the store makes. Entries reach the store once their changes are
  committed (`AuditOptions.Timing`, `AuditTiming.AfterCommit` by default): those of changes saved in a transaction
  (`Database.BeginTransaction`, shared with other contexts through `UseTransaction` or not, a `TransactionScope`, or
  an enlisted transaction) when it commits, through whichever context, and none if it rolls back, or for changes
  rolled back to a savepoint. They were passed at the end of each `SaveChanges`, so a transaction rolled back later
  still had its entries. A store failure is logged, as before, and the store gets a token that is never cancelled, so
  a save cancelled once its changes are committed keeps their entries (cancellation was caught and logged as a store
  failure). With `AuditTiming.InTransaction` the store is called at the end of each `SaveChanges`, inside its
  transaction, to write through the same connection and transaction, and its failure or cancellation is thrown from
  `SaveChanges`. `IAuditStore.SaveAsync` takes the context that saved the changes first; the store comes from a scope
  of its own when the saving context's scope is gone by the time the transaction commits. `AuditOptions.ExcludeTypes`
  is replaced by `Exclude<T>()`, which also leaves out the types derived from `T` (it matched the mapped type exactly)
  and the entities an excluded type owns, `ExcludeProperty<T>(x => x.Property)` and a `ShouldAudit` predicate, each
  applied before an entity's values are read (they were copied first). `AuditEntry` has a required `EntityType`, so
  code that creates entries (a store's tests, say) sets it, and the default store's log message names the actor. See
  Added for who made the change.
- **Breaking:** `ILicenseGuard` is internal: the licensed operations check the licence themselves.
- **Breaking:** the Hangfire, MassTransit, Quartz.NET and Rebus integrations are each added with one builder method
  and wired with one call on the host library's configuration: `pro.AddHangfirePropagation()` and
  `config.UseTenantry(sp)` in `AddHangfire((sp, config) => …)` (were `AddHangfireTenantFilter()` and
  `app.UseTenantryHangfire()`); `pro.AddMassTransitPropagation()` and `cfg.UseTenantry(context)` (were
  `AddMassTransitTenantFilters()`, `x.AddTenantryConsumeFilter()` and `cfg.UseTenantryPro(ctx)`);
  `pro.AddQuartzPropagation()` and `q.UseTenantry()` in `AddQuartz` (was `AddQuartzTenantScope()`, which had to come
  after `AddQuartz`; the two now come in either order); `pro.AddRebusPropagation()` and `o.UseTenantry(sp)` (were
  `AddRebusTenantSteps()` and `o.UseTenantryPro(sp)`). The application fails to start if an integration was added
  but its host side never ran, which left its jobs or messages without their tenant, or, with several MassTransit
  buses (MultiBus) or Rebus buses (Rebus.ServiceProvider's `AddRebus(…, key: …)`), if it did not run for each. The filters, steps and job
  factory are internal and need no key type, so the generic overloads are gone, and so are
  `TenantContextPropagation`, `TenantPropagationOutcome` and `TenantPropagationDecision`. The job parameter, header
  and job data key is `TenantPropagation.HeaderName`, with the same value, `tenantry-tenant-id`
  (`TenantPublishFilter<TKey>.HeaderKey`, `TenantOutgoingStep<TKey>.HeaderKey` and `TenantJobData.TenantIdKey` are
  gone). The host-side methods are in the host libraries' namespaces (`Hangfire`, `MassTransit`, `Quartz`,
  `Rebus.Config`, with `WithTenant` in `Quartz`) and the builder methods in `Microsoft.Extensions.DependencyInjection`,
  so they need no `using`. `Tenantry.Pro.Hangfire` no longer needs the ASP.NET Core shared framework, so a worker
  service can use it. Quartz.NET's `WithTenant` refuses the key type's default value (`Guid.Empty`, `0`) and an
  empty string, which Tenantry reserves for no tenant.
- **Breaking:** a job or message that carries a tenant the store does not have, or an id that is not a valid tenant
  id, follows a setting of its own, `TenantPropagationOptions.OnUnresolvedTenant`, which is `Reject` by default: it
  fails, so the host's retry and error handling take over (with `TenantNotFoundException` for a tenant the store does
  not have). It ran without a tenant, with a warning, as a job or message that carries no tenant still does
  (`OnMissingTenant`, `Warn` by default).
- Every log message has an event id that does not change between versions, listed in `docs/telemetry.md`: 30xx
  the licence, 31xx provisioning, 32xx background services, 33xx connection strings, 34xx jobs and messages (the
  integrations' messages, which also name the job or message), and Tenantry.Pro.EfCore's 4xxx (provisioning
  databases and schemas, migrations, health checks, audit). Alert on the licence errors, on 4103 and 4113 (migration
  runs with failures) and on 4302 (lost audit entries).
- The provisioning steps that create databases and schemas use the application's `IDbContextFactory<TContext>` when
  the context cannot be created in the tenant's scope, as migrations do, so Tenantry core's asynchronous connection
  strings work there too.
- The licence check is one internal service, which the startup check and the guarded operations share. A key
  that is not valid is logged once, with the reason.
- `Tenantry.Pro` depends on `Microsoft.Extensions.DependencyInjection.Abstractions`, not the DI container.
- Only the packages that use `Tenantry.Pro`'s internals (`Tenantry.Pro.EfCore` and the Hangfire, MassTransit,
  Quartz.NET and Rebus packages) depend on exactly their own release of it; `Tenantry.Pro.AspNetCore` takes it up
  to the next minor. `Microsoft.Extensions.Diagnostics.HealthChecks` takes 10.0.0 or later on .NET 10, as the other
  `Microsoft.Extensions` packages do.
- **Breaking:** the Hangfire and Rebus packages no longer depend on `Newtonsoft.Json`, which they referenced only to
  raise a vulnerable version Hangfire and Rebus allow. `Tenantry.Pro.Rebus` needs Rebus 8.4 or later, whose .NET
  build requires a fixed version itself (earlier 8.x releases allow a vulnerable `Newtonsoft.Json` or
  `System.Text.Json`). Hangfire allows the vulnerable 11.0.1: an application using Hangfire references
  `Newtonsoft.Json` 13.0.1 or later itself (the Hangfire guide says so).
- The database health check reports `Degraded` by default when a tenant database is unreachable
  (the `failureStatus` argument; it was `Unhealthy`): it is a monitoring check, and one tenant's
  outage is not the application's. ASP.NET Core answers `Degraded` with `200`, so a monitor that reads only the
  status code should map it to `503` on the monitoring endpoint, as the guide shows.

- The health checks are for monitoring, not liveness or readiness probes: one unreachable tenant database
  fails a probe on every replica at once. The guide recommended the `tenantry`-tagged checks for readiness;
  it now shows separate liveness, readiness and monitoring endpoints, and how to protect the monitoring one
  (the per-tenant data lists tenant ids and provider error messages). The `TenantHealthChecks` sample maps
  `/health/live` and `/health/tenants`.
- The docs say the tenant store must list every tenant, suspended ones included: `MigrateTenantAsync` looks
  tenants up there, and migration runs, migration status and the health checks
  cover only the tenants it lists. Refuse suspended tenants with an access validator, and check the status
  in background jobs, which Tenantry does not do. Keep suspended tenants' databases online.
- The docs now cover: EF Core 9 applying all pending migrations in one transaction; concurrent runners on
  PostgreSQL with EF Core 10+, where EF Core releases its lock between migrations, so two runners can each
  apply some and one fails; the permission to create databases that provisioning needs, and provisioning
  from a separate process (which must grant the application access); EF Core's `Migrate` creating a missing
  database; and reading the logs or the migration status after a cancelled run.
- The samples are named `Tenantry.Pro.Samples.<Name>`, for their folder, project and namespace, as Tenantry Core's
  are `Tenantry.Samples.<Name>`. The Hangfire, MassTransit, Quartz.NET and Rebus samples declared their types in
  those libraries' own namespaces: `Hangfire.BackgroundJobs` is now `HangfireJobs`, `MassTransit.Messaging`
  `MassTransitMessaging`, `Quartz.Scheduling` `QuartzScheduling` and `Rebus.Messaging` `RebusMessaging`.
  `HealthChecks`, the root namespace of the AspNetCore.HealthChecks packages, is `TenantHealthChecks`, and
  `SchemaPerTenant.Npgsql` is `SchemaPerTenantPostgreSql`; the others drop the dot (`DatabasePerTenant.MySql` is
  `DatabasePerTenantMySql`).
- The assemblies carry their PDBs, so stack traces from Tenantry.Pro code show file names and line numbers. There
  are no symbol packages any more.
- The packages have an icon, the project URL (tenantry.dev) and a copyright notice, and their descriptions match
  what each package holds. Packing checks that each target framework's assembly keeps the API of the lower ones
  (package validation).
- A GitHub release's notes are its section of this changelog, and the release publishes this changelog with its
  docs, for the Changelog page of the site's docs.

### Removed

- **Breaking:** `Tenantry.Pro.HealthChecks`: its checks are in `Tenantry.Pro.EfCore` (see Changed). Reference
  `Tenantry.Pro.EfCore` instead.
- **Breaking:** `WithMigrationOrchestration`, `MigrationOrchestratorService<TKey, TContext>`,
  `MigrationStatusTracker<TKey, TContext>` and the `Func<string, TContext>` they registered; see Changed.
- **Breaking:** `Tenantry.Pro.EfCore.SqlServer`, `Tenantry.Pro.EfCore.Npgsql` and `Tenantry.Pro.EfCore.MySql`:
  `Tenantry.Pro.EfCore` provisions with whichever EF Core provider your context uses (see Changed). Reference
  `Tenantry.Pro.EfCore` instead. Tenantry.Pro no longer depends on `Microsoft.Data.SqlClient`, `Npgsql`,
  `MySqlConnector` or the `Azure.Identity` and `Microsoft.Identity.Client` versions it pinned for SqlClient.
- **Breaking:** the `Tenantry.Pro` meter (`TenantryMeter`) and its `tenantry.requests.*` instruments, which
  repeated ASP.NET Core's request metrics under other names and units; see Changed for the `tenant.id` tag that
  replaces them. Instruments of your own belong on a meter of your own.
- **Breaking:** connection-string encryption: `ConnectionStringEncryptionOptions`, `ConnectionStringEncryptionMode`,
  `IConnectionStringProtector`, the AES protector and `Tenantry.Pro.AspNetCore`'s `UseDataProtectionEncryption()`.
  It encrypted only the in-process cache, with a key held by the same process, and every read handed the plain
  connection string to EF Core and the ADO.NET pool, so it protected nothing.

## [0.4.0] - 2026-09-29

The first release, and the start of the beta. Tenantry.Pro stays on 0.x releases until 1.0; a minor release
can change the API, and this changelog says how to update. It depends on Tenantry Core 0.4.x
(`[0.4.0, 0.5.0)`), since a Core minor release may break it too. These entries describe changes against the
previous development builds, so anyone who used one knows what to update; there are no compatibility shims.

### Security

- The licence verification public key embedded in `Tenantry.Pro` was replaced. Licences signed with the
  previous key no longer validate: request a new licence key.
- Licence keys are standard ES256 JWTs: the signature is the r‖s concatenation (IEEE P1363) that RFC 7518
  requires, instead of a DER sequence, so any JWT library can verify them. Licences in the old encoding no
  longer validate (the key rotation above already invalidates them).

### Added

- An API reference, `docs/api`: a page for every public type and its members, generated from the XML
  documentation comments by `scripts/generate-api-docs.sh` (docfx metadata, then `scripts/api-docs.cs`), and
  published with the docs. CI fails when the pages do not match the source, or when a public parameter or
  type parameter has no description; every one now has one.
- `WithMigrationOrchestration(..., runAtStartup: true, failStartupOnMigrationError: true)` stops the
  application from starting when a tenant's startup migration fails. Without it, failures are logged and
  the application starts.

### Changed

- `Tenantry.Pro.Hangfire`, `Tenantry.Pro.MassTransit`, `Tenantry.Pro.Quartz` and `Tenantry.Pro.Rebus` use only
  `Tenantry.Pro`'s public API, so each depends on `Tenantry.Pro` from its own release up to the next minor, rather
  than on exactly its own release. `Tenantry.Pro.EfCore` still depends on exactly its own release.
- MySQL on .NET 10 is supported with Oracle's `MySql.EntityFrameworkCore` (Pomelo has no EF Core 10
  release); on EF Core 8 and 9, use Pomelo. The MySQL sample moved from .NET 9 and Pomelo to
  .NET 10 and Oracle's provider, and the providers guide says which provider to use for each EF Core
  version.
- The licence format is pinned by a contract fixture shared with the licence issuer: the validator must
  accept a token the issuer signed and reject the same signature re-encoded as P1363.
- Migration orchestration is tested against real PostgreSQL 16 and MySQL 8.4 databases as well as SQL
  Server, on .NET 8, 9 and 10, including a failing tenant that is retried and two concurrent runners. The
  results are in the database providers guide.
- **Worker scopes and connection strings come from Tenantry Core.** Pro's own copies are gone:
  `ITenantScopeFactory<TKey>`, `ITenantServiceScope<TKey>` and `ITenantStoreAccessor<TKey>` are now in the
  `Tenantry.Core` namespace (registered by `AddTenantry`/`AddTenantryCore`), and
  `IConnectionStringResolver<TKey>` is replaced by Core's `ITenantConnectionStringResolver<TKey>`, a
  singleton. Update `using Tenantry.Pro.BackgroundServices` to `using Tenantry.Core` for those types.
  `UseDatabasePerTenant` registers its delegates with Core; caching and at-rest encryption wrap Core's
  resolver. The migration orchestrator, migration status tracker, database provisioning services and
  health checks now resolve through it, so they honour `GetConnectionStringAsync` and the cache instead of
  requiring the synchronous delegate. DbContext pooling with a database per tenant is Core's
  `AddTenantDbContextPool`.
- **Licence keys do not expire, and a missing or invalid key stops the application at startup.** The
  key is checked once when the host starts: no key, or a key that is malformed, wrongly signed or from
  another issuer, throws `LicenseRequiredException` and the application does not start. A valid key is
  accepted whatever its `exp` claim, so it never needs replacing and a running application can never be
  stopped by it; the subscription controls access to the package feed and new versions. Provisioning,
  migration orchestration and the tenant lifecycle still check the key (for use outside a host) and
  throw for a missing or invalid one. Removed: the 30-day grace period, `LicenseEnforcement` and
  `LicenseOptions.Enforcement`, and `app.UseLicenseCheck()` with its middleware. Changed signatures:
  `ProBuilder.WithLicence(key)` has no enforcement parameter, `ILicenseGuard.EnsureLicensed(capability)`
  reads the configured key itself, and the provisioning services take an `ILicenseGuard` instead of
  `IOptions<LicenseOptions>` and a guard.
- The database-per-tenant samples no longer migrate at startup; they show a `migrate` command to run
  once per deployment. The migration guide has a "Multiple instances" section: Tenantry takes no lock,
  EF Core 9+ locks on providers that support it, and EF Core 8 does not.
- The tenant lifecycle guide documents recovery: retry by running `ProvisionAsync` again, what each
  `CompletedUpTo` value leaves behind, that nothing is rolled back (and that MySQL cannot roll back DDL),
  and that the tenant must be in the store and stays resolvable after a failure. The seeder examples are
  idempotent, and the TenantLifecycle sample has a real migration and demonstrates a failed seed and the
  retry that completes it.
- Documented schema-per-tenant limits: migration orchestration (and the lifecycle's migration step)
  covers a database per tenant only, with guidance for creating and changing schema tenants' tables; and
  there is no MySQL schema strategy. The provider matrix and package descriptions say so.
- Dependency ranges replace floating and open-ended versions. EF Core and Npgsql match the target
  framework's major (for example `[10.0.12, 11.0.0)` on .NET 10), because each framework's build is compiled
  against that major; Hangfire, MassTransit, Quartz.NET, Rebus and Newtonsoft.Json stay within one major.
  Microsoft.Extensions, `Microsoft.Data.SqlClient`, MySqlConnector, Azure.Identity and
  Microsoft.Identity.Client take a minimum only (Microsoft.Extensions from the target framework's own major),
  so a .NET 8 app can use current Azure SDKs, which need Microsoft.Extensions 10.x. `Tenantry.Core` runs from
  the Core version Pro is tested against up to, but not including, 1.0.0. Every minimum is one the tests run
  against. The Tenantry.Pro packages depend on each other at exactly the same version, because they share
  internals: update them together.
- `Tenantry.Pro` and `Tenantry.Pro.AspNetCore` are marked `IsAotCompatible`, and CI publishes a Native AOT
  app over them without warnings. The lifecycle pipeline no longer carries hidden trimming/AOT warnings
  (they were suppressed only for the compiler, so trimming still reported them in your app):
  `WithMigrationOrchestration` and `AddAuditLogging` now carry `[RequiresUnreferencedCode]` and
  `[RequiresDynamicCode]`, so the warning appears where you opt into EF Core migrations or auditing, and
  `ITenantMigrator.MigrateAsync` no longer does.
- `JobDataMap.WithTenant` sets the value through the indexer, so it builds against current Quartz.NET 3.x,
  which marks `Put` obsolete.

### Removed

- `ITenantScopeFactory.CreateScopeAsync(tenantId)`. It could not work: the tenant scope it opened inside an
  `async` method was never active for the caller, and `await using` did not restore the previous tenant.
  Use `RunInScopeAsync(tenantId, work, ct)`, or look the tenant up and call `CreateScope(tenant)`.

### Fixed

- The SQL Server and MySQL database-per-tenant samples never called `app.UseTenantry()`, so every request
  failed to resolve a connection string, and had no migrations, so tenant databases had no tables. Both
  now resolve the tenant, return 400 without one, and ship an initial migration with a design-time
  factory.
- Disposing a worker scope with `await using` now restores the previous tenant. Previously the tenant
  stayed active after the scope, so a sweep over every tenant left the last one current.
- Cancelling the token passed to `MigrateAllAsync`, `MigrateTenantAsync` or `ProvisionAsync` now stops the
  work and throws `OperationCanceledException`. Previously each tenant (or pipeline step) caught the
  cancellation as a failure and carried on, so a cancelled run reported every remaining tenant as failed.
  An `OperationCanceledException` that does not come from the caller's token, such as a command timeout,
  is still recorded as that tenant's or step's failure. The tenant health checks also propagate
  cancellation.
- The schema-per-tenant guide, README, mixed-mode guide and both schema samples omitted
  `pro.AddSchemaPerTenantCaching()`, so `options.AddSchemaPerTenantCaching<TKey>(sp)` failed on the first
  request; it now also explains what is missing. The SQL Server schema sample also called
  `WithMigrationOrchestration` (which needs a database per tenant, so the app could not start), had no
  tenant store and no `UseTenantry()`; both schema samples now provision a tenant's schema and tables.
- `NuGet.local.config` was not valid XML (its comment contained `--`), so building against a local Core
  failed. `scripts/build-against-local-core.sh` now packs Core and builds Pro against it.
