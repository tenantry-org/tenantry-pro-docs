# API reference

Every public type in the packages, generated from their XML documentation comments. The guides explain how
the pieces fit together; this reference is for the details of each type and member.

## Tenantry.Pro

### `Microsoft.Extensions.DependencyInjection`

| Type | Kind | Summary |
|------|------|---------|
| [`TenantryProBuilderExtensions`](microsoft-extensions-dependencyinjection-tenantryprobuilderextensions.md) | class | Extension methods for configuring Tenantry.Pro's features on [`IProBuilder<TKey>`](tenantry-pro-iprobuilder-1.md). |
| [`TenantryProTenantBuilderExtensions`](microsoft-extensions-dependencyinjection-tenantryprotenantbuilderextensions.md) | class | Extension methods for enabling Tenantry.Pro on `ITenantBuilder<TKey>`. |

### `Tenantry.Pro`

| Type | Kind | Summary |
|------|------|---------|
| [`ConnectionStringCacheOptions`](tenantry-pro-connectionstringcacheoptions.md) | class | How tenants' connection strings are cached. Set with `pro.CacheConnectionStrings(o => ...)`. |
| [`IProBuilder`](tenantry-pro-iprobuilder.md) | interface | The builder `UsePro` passes to its configuration callback, without the tenant key type. |
| [`IProBuilder<TKey>`](tenantry-pro-iprobuilder-1.md) | interface | The builder `UsePro` passes to its configuration callback. |
| [`IProRegistration`](tenantry-pro-iproregistration.md) | interface | A registration that needs the tenant key type, added through [`IProBuilder.Add`](tenantry-pro-iprobuilder.md). Packages use it for builder methods that take a type parameter of their own, such as a `DbContext` type. |
| [`ITenantDeprovisioner<TKey>`](tenantry-pro-itenantdeprovisioner.md) | interface | Offboards a tenant, the reverse of [`ITenantProvisioner<TKey>`](tenantry-pro-itenantprovisioner.md): runs the deprovisioning steps and reports each one. |
| [`ITenantDeprovisioningStep<TKey>`](tenantry-pro-itenantdeprovisioningstep.md) | interface | A step of offboarding a tenant ([`ITenantDeprovisioner<TKey>`](tenantry-pro-itenantdeprovisioner.md)): export its data, archive it, tell another system. Add one with [`IProBuilder<TKey>.AddDeprovisioningStep<TStep>`](tenantry-pro-iprobuilder-1.md); the application's steps run before Tenantry drops anything. |
| [`ITenantProvisioner<TKey>`](tenantry-pro-itenantprovisioner.md) | interface | Provisions a new tenant: runs every [`ITenantProvisioningStep<TKey>`](tenantry-pro-itenantprovisioningstep.md) for it, in order, and reports each step's outcome. |
| [`ITenantProvisioningStep<TKey>`](tenantry-pro-itenantprovisioningstep.md) | interface | A step [`ITenantProvisioner<TKey>`](tenantry-pro-itenantprovisioner.md) runs to provision a tenant. Add one with [`IProBuilder<TKey>.AddProvisioningStep<TStep>`](tenantry-pro-iprobuilder-1.md). |
| [`ITenantSeeder<TKey>`](tenantry-pro-itenantseeder.md) | interface | Writes a new tenant's initial data, as a step of provisioning ([`ITenantProvisioner<TKey>`](tenantry-pro-itenantprovisioner.md)). |
| [`LicenseRequiredException`](tenantry-pro-licenserequiredexception.md) | class | Thrown when no Tenantry.Pro licence key is configured, or the configured key is invalid (malformed, a bad signature, or not a Tenantry.Pro licence): when the application starts, and by licence-guarded operations. |
| [`MixedModeOptions<TKey>`](tenantry-pro-mixedmodeoptions.md) | class | Mixed mode: tenants with different isolation in one application. Set with `pro.UseMixedMode(o => o.GetIsolation = ...)`. |
| [`PeriodicTenantBackgroundService<TKey>`](tenantry-pro-periodictenantbackgroundservice.md) | class | Base class for a hosted service that does work for every active tenant at startup, then again every [`PeriodicTenantBackgroundService<TKey>.Interval`](tenantry-pro-periodictenantbackgroundservice.md). |
| [`TenantBackgroundService<TKey>`](tenantry-pro-tenantbackgroundservice.md) | class | Base class for a hosted service that does work for every active tenant once, then completes. |
| [`TenantDeprovisioningContext<TKey>`](tenantry-pro-tenantdeprovisioningcontext.md) | class | What a deprovisioning step works with: the tenant being offboarded, a scope of its own, and its isolation. |
| [`TenantDeprovisioningResult<TKey>`](tenantry-pro-tenantdeprovisioningresult.md) | class | The outcome of offboarding a tenant: a result for each deprovisioning step, in the order they ran. |
| [`TenantIsolation`](tenantry-pro-tenantisolation.md) | enum | Where a tenant's data lives, in mixed mode ([`MixedModeOptions<TKey>.GetIsolation`](tenantry-pro-mixedmodeoptions.md)). |
| [`TenantLifecycleStepResult`](tenantry-pro-tenantlifecyclestepresult.md) | class | The outcome of one step, in [`TenantProvisioningResult<TKey>.Steps`](tenantry-pro-tenantprovisioningresult.md), or of an offboarding step, in [`TenantDeprovisioningResult<TKey>.Steps`](tenantry-pro-tenantdeprovisioningresult.md). |
| [`TenantLifecycleStepStatus`](tenantry-pro-tenantlifecyclestepstatus.md) | enum | What happened to a provisioning or deprovisioning step. |
| [`TenantPropagationBehavior`](tenantry-pro-tenantpropagationbehavior.md) | enum | What a tenant-propagation integration (Hangfire, MassTransit, Quartz.NET, Rebus) does with a job or message whose tenant it cannot make current. Set with [`TenantPropagationOptions.OnMissingTenant`](tenantry-pro-tenantpropagationoptions.md) and [`TenantPropagationOptions.OnUnresolvedTenant`](tenantry-pro-tenantpropagationoptions.md). |
| [`TenantPropagationOptions`](tenantry-pro-tenantpropagationoptions.md) | class | What a tenant-propagation integration (Hangfire, MassTransit, Quartz.NET, Rebus) does with a job or message that carries no tenant, or a tenant it cannot find. Set for each integration when it is added, as in `pro.AddRebusPropagation(o => o.OnMissingTenant = TenantPropagationBehavior.Reject)`. |
| [`TenantProvisioningContext<TKey>`](tenantry-pro-tenantprovisioningcontext.md) | class | The tenant an [`ITenantProvisioningStep<TKey>`](tenantry-pro-itenantprovisioningstep.md) runs for. |
| [`TenantProvisioningOptions`](tenantry-pro-tenantprovisioningoptions.md) | class | How [`ITenantProvisioner<TKey>`](tenantry-pro-itenantprovisioner.md) runs the steps. Set with `pro.ConfigureProvisioning(o => ...)`. |
| [`TenantProvisioningResult<TKey>`](tenantry-pro-tenantprovisioningresult.md) | class | The outcome of [`ITenantProvisioner<TKey>.ProvisionAsync`](tenantry-pro-itenantprovisioner.md). |

### Extension points

For code that extends the package, such as another package that builds on it. An application rarely needs them.

| Type | Namespace | Kind | Summary |
|------|-----------|------|---------|
| [`ITenantPropagationAdapter`](tenantry-pro-itenantpropagationadapter.md) | `Tenantry.Pro` | interface | A tenant-propagation adapter for a job or messaging library: what the startup check needs to know of it. |
| [`ITenantPropagator`](tenantry-pro-itenantpropagator.md) | `Tenantry.Pro` | interface | Carries the tenant into jobs and messages, with tenant ids as text, so a transport needs no tenant key type. |
| [`PropagatedTenant`](tenantry-pro-propagatedtenant.md) | `Tenantry.Pro` | struct | What [`ITenantPropagator`](tenantry-pro-itenantpropagator.md) resolved for a job or message: a tenant to run as ([`PropagatedTenant.Resolved`](tenantry-pro-propagatedtenant.md)), no tenant ([`PropagatedTenant.WithoutTenant`](tenantry-pro-propagatedtenant.md)), or that the work must not run ([`PropagatedTenant.Skipped`](tenantry-pro-propagatedtenant.md)). |
| [`TenantPropagationAdapter`](tenantry-pro-tenantpropagationadapter.md) | `Tenantry.Pro` | class | Registers a tenant-propagation adapter ([`ITenantPropagationAdapter`](tenantry-pro-itenantpropagationadapter.md)) for its `pro.Add…Propagation()` method, as Tenantry.Pro's Hangfire, MassTransit, Quartz.NET and Rebus integrations do, and formats the tenant ids its `WithTenant` methods take. |
| [`TenantPropagationIntegration<TAdapter>`](tenantry-pro-tenantpropagationintegration.md) | `Tenantry.Pro` | class | A tenant-propagation adapter as [`TenantPropagationAdapter.Add<TKey, TAdapter>`](tenantry-pro-tenantpropagationadapter.md) registered it: its options, the propagator, and how many times its host side has run. |

## Tenantry.Pro.EfCore

### `Microsoft.Extensions.DependencyInjection`

| Type | Kind | Summary |
|------|------|---------|
| [`TenantryProEfCoreBuilderExtensions`](microsoft-extensions-dependencyinjection-tenantryproefcorebuilderextensions.md) | class | Registers Tenantry.Pro's EF Core features on [`IProBuilder<TKey>`](tenantry-pro-iprobuilder-1.md). |
| [`TenantryProHealthChecksBuilderExtensions`](microsoft-extensions-dependencyinjection-tenantryprohealthchecksbuilderextensions.md) | class | Adds Tenantry.Pro's tenant health checks to ASP.NET Core's (or any host's) health checks. |

### `Microsoft.Extensions.Hosting`

| Type | Kind | Summary |
|------|------|---------|
| [`TenantryProEfCoreHostExtensions`](microsoft-extensions-hosting-tenantryproefcorehostextensions.md) | class | Runs Tenantry.Pro's tenant migrations as a deployment step. |

### `Tenantry.Pro.EfCore`

| Type | Kind | Summary |
|------|------|---------|
| [`AuditAction`](tenantry-pro-efcore-auditaction.md) | enum | The type of data-modification event captured by an audit entry. |
| [`AuditContext`](tenantry-pro-efcore-auditcontext.md) | class | Who made a save's changes, and what ties them to the request or job that made them, as an [`IAuditContextProvider`](tenantry-pro-efcore-iauditcontextprovider.md) gives it. |
| [`AuditEntry`](tenantry-pro-efcore-auditentry.md) | class | One entity change an EF Core `SaveChanges` call saved, recorded by audit logging (`pro.AddAuditLogging()`). |
| [`AuditOptions`](tenantry-pro-efcore-auditoptions.md) | class | Options for the Tenantry.Pro audit-logging feature. Configure via `pro.AddAuditLogging(opts => { ... })`. |
| [`AuditStoreException`](tenantry-pro-efcore-auditstoreexception.md) | class | Thrown from `SaveChanges`, or from a transaction's commit, when [`IAuditStore`](tenantry-pro-efcore-iauditstore.md) failed to write the entries of changes that were saved, with [`AuditOptions.OnStoreFailure`](tenantry-pro-efcore-auditoptions.md) set to [`AuditStoreFailureBehavior.Throw`](tenantry-pro-efcore-auditstorefailurebehavior.md). |
| [`AuditStoreFailureBehavior`](tenantry-pro-efcore-auditstorefailurebehavior.md) | enum | What happens when [`IAuditStore`](tenantry-pro-efcore-iauditstore.md) fails to write the entries of changes that are already saved, with [`AuditTiming.AfterCommit`](tenantry-pro-efcore-audittiming.md). Set with [`AuditOptions.OnStoreFailure`](tenantry-pro-efcore-auditoptions.md). |
| [`AuditTiming`](tenantry-pro-efcore-audittiming.md) | enum | When audit logging passes a save's entries to [`IAuditStore`](tenantry-pro-efcore-iauditstore.md). |
| [`DatabaseDeprovisioningOptions<TContext>`](tenantry-pro-efcore-databasedeprovisioningoptions.md) | class | How offboarding drops each tenant's database. Set with `pro.AddDatabaseDeprovisioning<TContext>(o => …)`. |
| [`DatabaseProvisioningOptions<TContext>`](tenantry-pro-efcore-databaseprovisioningoptions.md) | class | How tenant provisioning creates each tenant's database. Set with `pro.AddDatabaseProvisioning<TContext>(o => …)`. |
| [`IAuditContextProvider`](tenantry-pro-efcore-iauditcontextprovider.md) | interface | Says who made the changes a save records, and what ties them to the request or job that made them: the [`AuditEntry.Actor`](tenantry-pro-efcore-auditentry.md), [`AuditEntry.CorrelationId`](tenantry-pro-efcore-auditentry.md) and [`AuditEntry.Data`](tenantry-pro-efcore-auditentry.md) of each entry. |
| [`IAuditStore`](tenantry-pro-efcore-iauditstore.md) | interface | Receives the entries audit logging (`pro.AddAuditLogging()`) records for the changes a context saves. |
| [`ITenantMigrationRunner<TKey>`](tenantry-pro-efcore-itenantmigrationrunner.md) | interface | Applies the migrations of every context added with `pro.AddMigrations<TContext>()`, for every tenant, and reads which are applied. Registered by `AddMigrations`, as a singleton. |
| [`MigrationReport<TKey>`](tenantry-pro-efcore-migrationreport.md) | class | The outcome of a migration run: a result for each database or schema it covered. |
| [`MigrationResult<TKey>`](tenantry-pro-efcore-migrationresult.md) | class | The outcome of applying a context's migrations to one database or schema: the one the context connects to for the tenants in [`MigrationResult<TKey>.TenantIds`](tenantry-pro-efcore-migrationresult.md). |
| [`MigrationRunOptions<TKey>`](tenantry-pro-efcore-migrationrunoptions.md) | class | What a migration run covers and when it stops early: the tenants to migrate or leave out, and how many failures to allow. Pass it to [`ITenantMigrationRunner<TKey>.MigrateAsync`](tenantry-pro-efcore-itenantmigrationrunner.md); `migrate-tenants` builds it from `--tenant`, `--exclude` and `--max-failures`. |
| [`MigrationStatusEntry<TKey>`](tenantry-pro-efcore-migrationstatusentry.md) | class | Which of a context's migrations a database or schema has applied, and which are pending: the database or schema the context connects to for the tenants in [`MigrationStatusEntry<TKey>.TenantIds`](tenantry-pro-efcore-migrationstatusentry.md). |
| [`SchemaDeprovisioningOptions<TContext>`](tenantry-pro-efcore-schemadeprovisioningoptions.md) | class | How offboarding drops each tenant's schema. Set with `pro.AddSchemaDeprovisioning<TContext>(o => …)`. |
| [`SchemaPerTenantOptions<TKey>`](tenantry-pro-efcore-schemapertenantoptions.md) | class | Schema per tenant: each tenant's tables in a schema of its own, in a shared database. Set with `pro.UseSchemaPerTenant(o => o.GetSchemaName = …)`. |
| [`SchemaProvisioningOptions<TContext>`](tenantry-pro-efcore-schemaprovisioningoptions.md) | class | How tenant provisioning creates each tenant's schema. Set with `pro.AddSchemaProvisioning<TContext>(o => …)`. |
| [`StartupMigrations`](tenantry-pro-efcore-startupmigrations.md) | enum | Whether, and how, an application applies a context's migrations when it starts. |
| [`TenantHealthCheckOptions`](tenantry-pro-efcore-tenanthealthcheckoptions.md) | class | How a tenant health check (`AddTenantDatabaseCheck`, `AddTenantMigrationCheck`) checks the tenants' databases. Set with its `configure` argument. |
| [`TenantMigrationOptions<TContext>`](tenantry-pro-efcore-tenantmigrationoptions.md) | class | How a context's migrations are applied. Set with `pro.AddMigrations<TContext>(o => …)`. |
| [`TenantMigrationRunnerExtensions`](tenantry-pro-efcore-tenantmigrationrunnerextensions.md) | class | Shorter forms of [`ITenantMigrationRunner<TKey>`](tenantry-pro-efcore-itenantmigrationrunner.md)'s runs and reads: every tenant, or one. |

## Tenantry.Pro.Hangfire

### `Hangfire`

| Type | Kind | Summary |
|------|------|---------|
| [`TenantryBackgroundJobClientExtensions`](hangfire-tenantrybackgroundjobclientextensions.md) | class | Extension methods for enqueueing a Hangfire job for a tenant other than the current one. |
| [`TenantryHangfireConfigurationExtensions`](hangfire-tenantryhangfireconfigurationextensions.md) | class | Extension methods for wiring Tenantry.Pro's Hangfire integration into Hangfire's configuration. |
| [`TenantryRecurringJobManagerExtensions`](hangfire-tenantryrecurringjobmanagerextensions.md) | class | Extension methods for a recurring Hangfire job that runs for each tenant. |

### `Microsoft.Extensions.DependencyInjection`

| Type | Kind | Summary |
|------|------|---------|
| [`TenantryProHangfireBuilderExtensions`](microsoft-extensions-dependencyinjection-tenantryprohangfirebuilderextensions.md) | class | Extension methods for adding the Hangfire integration on [`IProBuilder<TKey>`](tenantry-pro-iprobuilder-1.md). |

## Tenantry.Pro.MassTransit

### `MassTransit`

| Type | Kind | Summary |
|------|------|---------|
| [`TenantryMassTransitBusFactoryConfiguratorExtensions`](masstransit-tenantrymasstransitbusfactoryconfiguratorextensions.md) | class | Extension methods for wiring Tenantry.Pro's MassTransit integration into a bus. |
| [`TenantryMassTransitSendContextExtensions`](masstransit-tenantrymasstransitsendcontextextensions.md) | class | Extension methods for publishing or sending a message for a tenant other than the current one. |

### `Microsoft.Extensions.DependencyInjection`

| Type | Kind | Summary |
|------|------|---------|
| [`TenantryProMassTransitBuilderExtensions`](microsoft-extensions-dependencyinjection-tenantrypromasstransitbuilderextensions.md) | class | Extension methods for adding the MassTransit integration on [`IProBuilder<TKey>`](tenantry-pro-iprobuilder-1.md). |

## Tenantry.Pro.Quartz

### `Microsoft.Extensions.DependencyInjection`

| Type | Kind | Summary |
|------|------|---------|
| [`TenantryProQuartzBuilderExtensions`](microsoft-extensions-dependencyinjection-tenantryproquartzbuilderextensions.md) | class | Extension methods for adding the Quartz.NET integration on [`IProBuilder<TKey>`](tenantry-pro-iprobuilder-1.md). |

### `Quartz`

| Type | Kind | Summary |
|------|------|---------|
| [`TenantryQuartzExtensions`](quartz-tenantryquartzextensions.md) | class | Extension methods for wiring Tenantry.Pro's Quartz.NET integration into Quartz, and for scheduling a job for a tenant. |

## Tenantry.Pro.Rebus

### `Microsoft.Extensions.DependencyInjection`

| Type | Kind | Summary |
|------|------|---------|
| [`TenantryProRebusBuilderExtensions`](microsoft-extensions-dependencyinjection-tenantryprorebusbuilderextensions.md) | class | Extension methods for adding the Rebus integration on [`IProBuilder<TKey>`](tenantry-pro-iprobuilder-1.md). |

### `Rebus.Bus`

| Type | Kind | Summary |
|------|------|---------|
| [`TenantryRebusHeadersExtensions`](rebus-bus-tenantryrebusheadersextensions.md) | class | Extension methods for sending or publishing a message for a tenant other than the current one. |

### `Rebus.Config`

| Type | Kind | Summary |
|------|------|---------|
| [`TenantryRebusOptionsConfigurerExtensions`](rebus-config-tenantryrebusoptionsconfigurerextensions.md) | class | Extension methods for wiring Tenantry.Pro's Rebus integration into a bus. |
