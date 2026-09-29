# API reference

Every public type in the packages, generated from their XML documentation comments. The guides explain how
the pieces fit together; this reference is for the details of each type and member.

## Tenantry.Pro

### `Tenantry.Pro`

| Type | Kind | Summary |
|------|------|---------|
| [`ProServiceCollectionExtensions`](tenantry-pro-proservicecollectionextensions.md) | class | Extension methods for enabling Tenantry.Pro features on [`ITenantBuilder<TKey>`](https://tenantry.dev/docs/core/api/tenantry-core-itenantbuilder). |

### `Tenantry.Pro.BackgroundServices`

| Type | Kind | Summary |
|------|------|---------|
| [`PeriodicTenantBackgroundService<TKey>`](tenantry-pro-backgroundservices-periodictenantbackgroundservice.md) | class | Base class for a hosted service that performs work for every tenant on a recurring interval. The first sweep runs at startup, then again every [`PeriodicTenantBackgroundService<TKey>.Interval`](tenantry-pro-backgroundservices-periodictenantbackgroundservice.md). Each tenant is processed inside its own scope with per-tenant failure isolation (see [`TenantBackgroundService<TKey>`](tenantry-pro-backgroundservices-tenantbackgroundservice.md)); a failure in one sweep does not stop later sweeps. |
| [`TenantBackgroundService<TKey>`](tenantry-pro-backgroundservices-tenantbackgroundservice.md) | class | Base class for a hosted service that performs work for every tenant once, then completes. |
| [`TenantContextPropagation`](tenantry-pro-backgroundservices-tenantcontextpropagation.md) | class | Shared tenant-resolution logic for the Tenantry.Pro background-job and messaging integrations. Parses a tenant identifier carried by a job/message, looks it up in the store, and applies the configured [`MissingTenantBehavior`](https://tenantry.dev/docs/core/api/tenantry-core-missingtenantbehavior) when no tenant can be resolved. |
| [`TenantPropagationDecision<TKey>`](tenantry-pro-backgroundservices-tenantpropagationdecision.md) | struct | The outcome of resolving the tenant for a single job or message. |
| [`TenantPropagationOptions`](tenantry-pro-backgroundservices-tenantpropagationoptions.md) | class | Configures how a tenant-context propagation integration (Hangfire, MassTransit, Quartz, Rebus) behaves when a job or message has no resolvable tenant — because none was attached, the attached value could not be parsed as `TKey`, or the tenant was not found in the store. |
| [`TenantPropagationOutcome`](tenantry-pro-backgroundservices-tenantpropagationoutcome.md) | enum | The action a tenant-propagation integration should take for a single job or message, as decided by [`TenantContextPropagation`](tenantry-pro-backgroundservices-tenantcontextpropagation.md). |

### `Tenantry.Pro.Exceptions`

| Type | Kind | Summary |
|------|------|---------|
| [`LicenseRequiredException`](tenantry-pro-exceptions-licenserequiredexception.md) | class | Thrown when no Tenantry.Pro licence key is configured, or the configured key is invalid (malformed, a bad signature or the wrong issuer): when the application starts, and by licence-guarded operations. |

### `Tenantry.Pro.Internal`

| Type | Kind | Summary |
|------|------|---------|
| [`ProBuilder<TKey>`](tenantry-pro-internal-probuilder.md) | class | Fluent builder for configuring Tenantry.Pro features. Obtained via `tenant.UsePro(pro => { ... })` inside `AddTenantry`. |

### `Tenantry.Pro.Licensing`

| Type | Kind | Summary |
|------|------|---------|
| [`ILicenseGuard`](tenantry-pro-licensing-ilicenseguard.md) | interface | Checks that a valid Tenantry.Pro licence is configured. A public seam so that public, provider-agnostic services (such as the EF Core provisioning services) can take the licence check in a `public` constructor, while the validator itself stays internal. |
| [`LicenseOptions`](tenantry-pro-licensing-licenseoptions.md) | class | Options for configuring the Tenantry.Pro licence. |

### `Tenantry.Pro.Lifecycle`

| Type | Kind | Summary |
|------|------|---------|
| [`ITenantInfrastructureProvisioner<TKey>`](tenantry-pro-lifecycle-itenantinfrastructureprovisioner.md) | interface | Provisions the infrastructure (database or schema) for a new tenant. Implemented by database-provider packages (`Tenantry.Pro.EfCore.SqlServer`, `Tenantry.Pro.EfCore.Npgsql`, etc.) and consumed by [`ITenantLifecycleManager<TKey>`](tenantry-pro-lifecycle-itenantlifecyclemanager.md) to run the provisioning step without depending on any specific provider or strategy. |
| [`ITenantLifecycleManager<TKey>`](tenantry-pro-lifecycle-itenantlifecyclemanager.md) | interface | Orchestrates the full tenant creation lifecycle: provision → migrate → seed. |
| [`ITenantMigrator<TKey>`](tenantry-pro-lifecycle-itenantmigrator.md) | interface | Runs EF Core migrations for a single tenant's database. Implemented by `Tenantry.Pro.Migrations` and consumed by [`ITenantLifecycleManager<TKey>`](tenantry-pro-lifecycle-itenantlifecyclemanager.md) without exposing the `TContext` type parameter. The EF Core implementation is registered by `WithMigrationOrchestration`, which carries the trimming and Native AOT warnings, so this interface and the lifecycle pipeline do not. |
| [`ITenantSeeder<TKey>`](tenantry-pro-lifecycle-itenantseeder.md) | interface | Consumer-implemented interface for seeding initial data into a new tenant's database. Register an implementation in DI before calling [`ITenantLifecycleManager<TKey>.ProvisionAsync`](tenantry-pro-lifecycle-itenantlifecyclemanager.md):  ```csharp services.AddScoped<ITenantSeeder<Guid>, MyTenantSeeder>(); ``` |
| [`TenantLifecycleOptions`](tenantry-pro-lifecycle-tenantlifecycleoptions.md) | class | Options for [`ITenantLifecycleManager<TKey>`](tenantry-pro-lifecycle-itenantlifecyclemanager.md) behaviour. Configure via `pro.AddLifecycleManagement(opts => { ... })`. |
| [`TenantProvisioningResult<TKey>`](tenantry-pro-lifecycle-tenantprovisioningresult.md) | class | Describes the outcome of a [`ITenantLifecycleManager<TKey>.ProvisionAsync`](tenantry-pro-lifecycle-itenantlifecyclemanager.md) call. |
| [`TenantProvisioningStep`](tenantry-pro-lifecycle-tenantprovisioningstep.md) | enum | Represents the pipeline stages in the tenant provisioning lifecycle. Used by [`TenantProvisioningResult<TKey>.CompletedUpTo`](tenantry-pro-lifecycle-tenantprovisioningresult.md) to indicate which stage completed successfully before a failure occurred (or before the pipeline finished). |

### `Tenantry.Pro.Lifecycle.Extensions`

| Type | Kind | Summary |
|------|------|---------|
| [`LifecycleProBuilderExtensions`](tenantry-pro-lifecycle-extensions-lifecycleprobuilderextensions.md) | class | Extension methods for registering tenant lifecycle management on [`ProBuilder<TKey>`](tenantry-pro-internal-probuilder.md). |

### `Tenantry.Pro.Strategies.DatabasePerTenant`

| Type | Kind | Summary |
|------|------|---------|
| [`ConnectionStringEncryptionMode`](tenantry-pro-strategies-databasepertenant-connectionstringencryptionmode.md) | enum | Determines how connection strings are encrypted before being stored in the cache. |
| [`ConnectionStringEncryptionOptions`](tenantry-pro-strategies-databasepertenant-connectionstringencryptionoptions.md) | class | Configures at-rest encryption for cached connection strings. Set via `opts.Encryption.Mode = ...` inside `pro.UseDatabasePerTenant(...)`. |
| [`DatabasePerTenantOptions<TKey>`](tenantry-pro-strategies-databasepertenant-databasepertenantoptions.md) | class | Options for the database-per-tenant strategy. Configure via `pro.UseDatabasePerTenant(opts => { ... })`. |
| [`IConnectionStringCache<TKey>`](tenantry-pro-strategies-databasepertenant-iconnectionstringcache.md) | interface | Allows invalidating cached connection strings for a specific tenant. |
| [`IConnectionStringProtector`](tenantry-pro-strategies-databasepertenant-iconnectionstringprotector.md) | interface | Encrypts and decrypts connection strings for at-rest protection in the connection string cache. Implement this interface and register it in DI when using [`ConnectionStringEncryptionMode.Custom`](tenantry-pro-strategies-databasepertenant-connectionstringencryptionmode.md). |

### `Tenantry.Pro.Strategies.MixedMode`

| Type | Kind | Summary |
|------|------|---------|
| [`MixedModeOptions<TKey>`](tenantry-pro-strategies-mixedmode-mixedmodeoptions.md) | class | Options for the mixed-mode strategy, which routes individual tenants to either database-per-tenant or schema-per-tenant isolation. |
| [`MixedStrategyResolver<TKey>`](tenantry-pro-strategies-mixedmode-mixedstrategyresolver.md) | class | Resolves the connection string or schema name for the tenant currently in scope, delegating to the appropriate underlying resolver based on the tenant's assigned strategy. |
| [`TenantStrategy`](tenantry-pro-strategies-mixedmode-tenantstrategy.md) | enum | Specifies which isolation strategy a tenant uses in mixed-mode deployments. |

### `Tenantry.Pro.Strategies.SchemaPerTenant`

| Type | Kind | Summary |
|------|------|---------|
| [`ISchemaNameResolver<TKey>`](tenantry-pro-strategies-schemapertenant-ischemanameresolver.md) | interface | Resolves the SQL schema name for the tenant currently in scope. Inject this into your `DbContext.OnModelCreating` override to set the default schema for the current tenant's model. |
| [`SchemaPerTenantOptions<TKey>`](tenantry-pro-strategies-schemapertenant-schemapertenantoptions.md) | class | Options for the schema-per-tenant strategy. Configure via `pro.UseSchemaPerTenant(opts => { ... })`. |

### `Tenantry.Pro.Telemetry`

| Type | Kind | Summary |
|------|------|---------|
| [`TenantryMeter`](tenantry-pro-telemetry-tenantrymeter.md) | class | Central meter for all Tenantry.Pro tenant-scoped metrics. |

## Tenantry.Pro.AspNetCore

### `Tenantry.Pro.AspNetCore`

| Type | Kind | Summary |
|------|------|---------|
| [`DataProtectionEncryptionExtensions`](tenantry-pro-aspnetcore-dataprotectionencryptionextensions.md) | class | Extension methods for enabling ASP.NET Core Data Protection encryption for cached connection strings. |

### `Tenantry.Pro.AspNetCore.Telemetry`

| Type | Kind | Summary |
|------|------|---------|
| [`TenantMetricsOptions`](tenantry-pro-aspnetcore-telemetry-tenantmetricsoptions.md) | class | Options for tenant request metrics collection. Configure via `pro.AddTenantMetrics(opts => { ... })`. |

### `Tenantry.Pro.AspNetCore.Telemetry.Extensions`

| Type | Kind | Summary |
|------|------|---------|
| [`ApplicationBuilderTelemetryExtensions`](tenantry-pro-aspnetcore-telemetry-extensions-applicationbuildertelemetryextensions.md) | class | Extension methods for wiring tenant metrics middleware into the ASP.NET Core pipeline. |
| [`TelemetryProBuilderExtensions`](tenantry-pro-aspnetcore-telemetry-extensions-telemetryprobuilderextensions.md) | class | Extension methods for registering tenant metrics on [`ProBuilder<TKey>`](tenantry-pro-internal-probuilder.md). |

## Tenantry.Pro.EfCore

### `Tenantry.Pro.EfCore`

| Type | Kind | Summary |
|------|------|---------|
| [`ProBuilderEfCoreExtensions`](tenantry-pro-efcore-probuilderefcoreextensions.md) | class | Extension methods for [`ProBuilder<TKey>`](tenantry-pro-internal-probuilder.md) that wire up EF Core services. |

### `Tenantry.Pro.EfCore.Audit`

| Type | Kind | Summary |
|------|------|---------|
| [`AuditAction`](tenantry-pro-efcore-audit-auditaction.md) | enum | The type of data-modification event captured by an audit entry. |
| [`AuditEntry`](tenantry-pro-efcore-audit-auditentry.md) | class | Represents a single audited entity change captured before an EF Core `SaveChanges` call. |
| [`AuditOptions`](tenantry-pro-efcore-audit-auditoptions.md) | class | Options for the Tenantry.Pro audit-logging feature. Configure via `pro.AddAuditLogging(opts => { ... })`. |
| [`IAuditStore`](tenantry-pro-efcore-audit-iauditstore.md) | interface | Receives audit entries produced by `Tenantry.Pro.EfCore.Audit` after each successful EF Core `SaveChanges` call. |

### `Tenantry.Pro.EfCore.Audit.Extensions`

| Type | Kind | Summary |
|------|------|---------|
| [`AuditProBuilderExtensions`](tenantry-pro-efcore-audit-extensions-auditprobuilderextensions.md) | class | Extension methods for registering audit-logging services on [`ProBuilder<TKey>`](tenantry-pro-internal-probuilder.md). |
| [`DbContextOptionsBuilderAuditExtensions`](tenantry-pro-efcore-audit-extensions-dbcontextoptionsbuilderauditextensions.md) | class | Extension methods for wiring `Tenantry.Pro.EfCore.Audit` into an EF Core `DbContext`. |

### `Tenantry.Pro.EfCore.Extensions`

| Type | Kind | Summary |
|------|------|---------|
| [`DbContextOptionsBuilderExtensions`](tenantry-pro-efcore-extensions-dbcontextoptionsbuilderextensions.md) | class | Extension methods for wiring Tenantry.Pro schema-per-tenant services into `DbContextOptionsBuilder`. |

### `Tenantry.Pro.EfCore.Migrations`

| Type | Kind | Summary |
|------|------|---------|
| [`MigrationOrchestratorService<TKey, TContext>`](tenantry-pro-efcore-migrations-migrationorchestratorservice.md) | class | Runs pending EF Core migrations across all tenant databases discovered via [`ITenantStore<TKey>`](https://tenantry.dev/docs/core/api/tenantry-core-itenantstore). |
| [`MigrationReport<TKey>`](tenantry-pro-efcore-migrations-migrationreport.md) | class | The aggregate outcome of running EF Core migrations across all tenant databases. |
| [`MigrationResult<TKey>`](tenantry-pro-efcore-migrations-migrationresult.md) | class | The outcome of running EF Core migrations against a single tenant's database. |
| [`MigrationStatusEntry<TKey>`](tenantry-pro-efcore-migrations-migrationstatusentry.md) | class | A point-in-time snapshot of which EF Core migrations have been applied to a specific tenant's database. |
| [`MigrationStatusTracker<TKey, TContext>`](tenantry-pro-efcore-migrations-migrationstatustracker.md) | class | Queries the applied and pending EF Core migrations for each tenant's database. |

### `Tenantry.Pro.EfCore.Strategies.DatabasePerTenant`

| Type | Kind | Summary |
|------|------|---------|
| [`DatabaseProvisioningServiceBase<TKey>`](tenantry-pro-efcore-strategies-databasepertenant-databaseprovisioningservicebase.md) | class | Provider-agnostic base for database-per-tenant provisioning. |

### `Tenantry.Pro.EfCore.Strategies.SchemaPerTenant`

| Type | Kind | Summary |
|------|------|---------|
| [`SchemaProvisioningOptions`](tenantry-pro-efcore-strategies-schemapertenant-schemaprovisioningoptions.md) | class | Options for schema-per-tenant provisioning. Configure via `pro.AddSchemaProvisioning(opts => opts.ConnectionString = ...)`. |
| [`SchemaProvisioningServiceBase<TKey>`](tenantry-pro-efcore-strategies-schemapertenant-schemaprovisioningservicebase.md) | class | Provider-agnostic base for schema-per-tenant provisioning. Implements the shared pipeline (licence guard, schema-name resolution, tenant lookup, connection-string guard, logging) as a template method and defers the ADO.NET-specific `CREATE SCHEMA` to the provider subclass. |
| [`TenantModelCacheKeyFactory<TKey>`](tenantry-pro-efcore-strategies-schemapertenant-tenantmodelcachekeyfactory.md) | class | EF Core `IModelCacheKeyFactory` that includes the current tenant ID in the cache key. |

## Tenantry.Pro.EfCore.MySql

### `Tenantry.Pro.EfCore.MySql.Extensions`

| Type | Kind | Summary |
|------|------|---------|
| [`MySqlProBuilderExtensions`](tenantry-pro-efcore-mysql-extensions-mysqlprobuilderextensions.md) | class | Extension methods for registering MySQL/MariaDB-backed Tenantry.Pro services on [`ProBuilder<TKey>`](tenantry-pro-internal-probuilder.md). |

### `Tenantry.Pro.EfCore.MySql.Strategies.DatabasePerTenant`

| Type | Kind | Summary |
|------|------|---------|
| [`DatabaseProvisioningService<TKey>`](tenantry-pro-efcore-mysql-strategies-databasepertenant-databaseprovisioningservice.md) | class | Creates a dedicated MySQL or MariaDB database for a tenant when called explicitly. |

## Tenantry.Pro.EfCore.Npgsql

### `Tenantry.Pro.EfCore.Npgsql.Extensions`

| Type | Kind | Summary |
|------|------|---------|
| [`PostgreSqlProBuilderExtensions`](tenantry-pro-efcore-npgsql-extensions-postgresqlprobuilderextensions.md) | class | Extension methods for registering PostgreSQL-backed Tenantry.Pro services on [`ProBuilder<TKey>`](tenantry-pro-internal-probuilder.md). |

### `Tenantry.Pro.EfCore.Npgsql.Strategies.DatabasePerTenant`

| Type | Kind | Summary |
|------|------|---------|
| [`DatabaseProvisioningService<TKey>`](tenantry-pro-efcore-npgsql-strategies-databasepertenant-databaseprovisioningservice.md) | class | Creates a dedicated PostgreSQL database for a tenant when called explicitly. |

### `Tenantry.Pro.EfCore.Npgsql.Strategies.SchemaPerTenant`

| Type | Kind | Summary |
|------|------|---------|
| [`SchemaProvisioningService<TKey>`](tenantry-pro-efcore-npgsql-strategies-schemapertenant-schemaprovisioningservice.md) | class | Creates a dedicated PostgreSQL schema for a tenant when called explicitly. |

## Tenantry.Pro.EfCore.SqlServer

### `Tenantry.Pro.EfCore.SqlServer.Extensions`

| Type | Kind | Summary |
|------|------|---------|
| [`SqlServerProBuilderExtensions`](tenantry-pro-efcore-sqlserver-extensions-sqlserverprobuilderextensions.md) | class | Extension methods for registering SQL Server-backed Tenantry.Pro services on [`ProBuilder<TKey>`](tenantry-pro-internal-probuilder.md). |

### `Tenantry.Pro.EfCore.SqlServer.Strategies.DatabasePerTenant`

| Type | Kind | Summary |
|------|------|---------|
| [`DatabaseProvisioningService<TKey>`](tenantry-pro-efcore-sqlserver-strategies-databasepertenant-databaseprovisioningservice.md) | class | Creates a dedicated SQL Server database for a tenant when called explicitly. |

### `Tenantry.Pro.EfCore.SqlServer.Strategies.SchemaPerTenant`

| Type | Kind | Summary |
|------|------|---------|
| [`SchemaProvisioningService<TKey>`](tenantry-pro-efcore-sqlserver-strategies-schemapertenant-schemaprovisioningservice.md) | class | Creates a dedicated SQL Server schema for a tenant when called explicitly. |

## Tenantry.Pro.Hangfire

### `Tenantry.Pro.Hangfire.Extensions`

| Type | Kind | Summary |
|------|------|---------|
| [`ApplicationBuilderHangfireExtensions`](tenantry-pro-hangfire-extensions-applicationbuilderhangfireextensions.md) | class | Extension methods for wiring Tenantry.Pro's Hangfire integration into the ASP.NET Core application pipeline. |
| [`HangfireProBuilderExtensions`](tenantry-pro-hangfire-extensions-hangfireprobuilderextensions.md) | class | Extension methods for registering Hangfire tenant-propagation services on [`ProBuilder<TKey>`](tenantry-pro-internal-probuilder.md). |

### `Tenantry.Pro.Hangfire.Filters`

| Type | Kind | Summary |
|------|------|---------|
| [`TenantJobFilter<TKey>`](tenantry-pro-hangfire-filters-tenantjobfilter.md) | class | Hangfire filter that propagates the current tenant context into background jobs. |

## Tenantry.Pro.HealthChecks

### `Tenantry.Pro.HealthChecks`

| Type | Kind | Summary |
|------|------|---------|
| [`TenantHealthCheckOptions`](tenantry-pro-healthchecks-tenanthealthcheckoptions.md) | class | Options for tenant database health checks. Configure via `builder.Services.AddHealthChecks().AddTenantryDatabaseCheck<TKey>(opts => { ... })`. |

### `Tenantry.Pro.HealthChecks.Extensions`

| Type | Kind | Summary |
|------|------|---------|
| [`HealthCheckBuilderExtensions`](tenantry-pro-healthchecks-extensions-healthcheckbuilderextensions.md) | class | Extension methods for adding Tenantry.Pro health checks to the ASP.NET Core health check pipeline. |

## Tenantry.Pro.MassTransit

### `Tenantry.Pro.MassTransit.Extensions`

| Type | Kind | Summary |
|------|------|---------|
| [`BusFactoryConfiguratorExtensions`](tenantry-pro-masstransit-extensions-busfactoryconfiguratorextensions.md) | class | Extension methods for wiring Tenantry.Pro's publish and send filters into the MassTransit bus pipeline. |
| [`BusRegistrationConfiguratorExtensions`](tenantry-pro-masstransit-extensions-busregistrationconfiguratorextensions.md) | class | Extension methods for adding Tenantry.Pro's consume-side tenant filter to all MassTransit receive endpoints. |
| [`MassTransitProBuilderExtensions`](tenantry-pro-masstransit-extensions-masstransitprobuilderextensions.md) | class | Extension methods for registering MassTransit tenant-propagation services on [`ProBuilder<TKey>`](tenantry-pro-internal-probuilder.md). |

### `Tenantry.Pro.MassTransit.Filters`

| Type | Kind | Summary |
|------|------|---------|
| [`TenantConsumeFilter<TKey>`](tenantry-pro-masstransit-filters-tenantconsumefilter.md) | class | MassTransit pipeline filter that restores the tenant scope when a message is consumed. |
| [`TenantPublishFilter<TKey>`](tenantry-pro-masstransit-filters-tenantpublishfilter.md) | class | MassTransit pipeline filter that adds the current tenant ID as a message header when a message is published. |
| [`TenantSendFilter<TKey>`](tenantry-pro-masstransit-filters-tenantsendfilter.md) | class | MassTransit pipeline filter that adds the current tenant ID as a message header when a message is sent point-to-point. |

## Tenantry.Pro.Quartz

### `Tenantry.Pro.Quartz`

| Type | Kind | Summary |
|------|------|---------|
| [`TenantJobData`](tenantry-pro-quartz-tenantjobdata.md) | class | Well-known keys used to carry tenant context through Quartz job data maps. |

### `Tenantry.Pro.Quartz.Extensions`

| Type | Kind | Summary |
|------|------|---------|
| [`JobDataMapExtensions`](tenantry-pro-quartz-extensions-jobdatamapextensions.md) | class | Extension methods for `JobDataMap` to support tenant propagation. |
| [`QuartzProBuilderExtensions`](tenantry-pro-quartz-extensions-quartzprobuilderextensions.md) | class | Extension methods for registering Quartz tenant-propagation services on [`ProBuilder<TKey>`](tenantry-pro-internal-probuilder.md). |

## Tenantry.Pro.Rebus

### `Tenantry.Pro.Rebus.Extensions`

| Type | Kind | Summary |
|------|------|---------|
| [`RebusConfigurationExtensions`](tenantry-pro-rebus-extensions-rebusconfigurationextensions.md) | class | Extension methods for wiring Tenantry tenant steps into the Rebus pipeline. |
| [`RebusProBuilderExtensions`](tenantry-pro-rebus-extensions-rebusprobuilderextensions.md) | class | Extension methods for registering Rebus tenant-propagation services on [`ProBuilder<TKey>`](tenantry-pro-internal-probuilder.md). |

### `Tenantry.Pro.Rebus.Steps`

| Type | Kind | Summary |
|------|------|---------|
| [`TenantIncomingStep<TKey>`](tenantry-pro-rebus-steps-tenantincomingstep.md) | class | Rebus incoming pipeline step that restores the tenant scope from a message header. |
| [`TenantOutgoingStep<TKey>`](tenantry-pro-rebus-steps-tenantoutgoingstep.md) | class | Rebus outgoing pipeline step that stamps the current tenant ID as a message header. |
