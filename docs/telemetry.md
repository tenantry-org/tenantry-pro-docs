# Telemetry

Tenantry.Pro names the tenant a job or message runs as in its logs and traces, and gives each of its log messages an
event id you can alert on.

Requests are Tenantry Core's: `app.UseTenantry()` names a request's tenant in its logs and traces, and
`tenant.TagRequestMetrics()` (Tenantry Core 0.7.0 and later) adds it to ASP.NET Core's request metric,
`http.server.request.duration`, for request rates, latency and errors per tenant. See
[Core's diagnostics](https://github.com/tenantry-org/tenantry-core/blob/master/docs/diagnostics.md#request-metrics-per-tenant).

## Logs and traces

The Hangfire, MassTransit, Quartz.NET and Rebus integrations name the tenant a job or message runs as, with no setup:

- While the job, consumer or handler runs, a log scope with one property, `TenantId`, is open. A logging
  provider that records scopes (Serilog's, or the console's and OpenTelemetry's with `IncludeScopes`) adds it to
  every entry written while it runs.
- The job's or message's trace span is tagged `tenant.id`: a Hangfire job's span (Tenantry's job filter
  tags it inside other filters, such as OpenTelemetry's Hangfire instrumentation), a Quartz.NET job's, a Rebus
  handler's, and a MassTransit routing-slip activity's. For a MassTransit consumer it is the message's receive span:
  the span MassTransit starts for the consumer is a child of it, started after Tenantry's filter. Without tracing,
  there is no span to tag.

The value is the tenant's id, formatted with the invariant culture, as the job or message carries it. A job or
message that runs without a tenant gets neither.

## Log event ids

Each of Tenantry.Pro's log messages has an event id that does not change between versions, so you can alert on it.
Tenantry Core's are 1xxx and 2xxx (see its diagnostics). Alert on these:

- 30xx: licence errors (Tenantry.Pro's features stop working)
- 3104, 3108: a provisioning or offboarding step failed
- 4103, 4113: a migration run had failures (each is also logged as 4105 or 4106)
- 4114: a migration run stopped early
- 4116: `migrate-tenants` could not start
- 4302: audit entries were lost

| Event id | Name | Level | When |
|----------|------|-------|------|
| 3001 | `LicenceValidated` | Information | The licence key is valid, at startup. |
| 3002 | `LicenceKeyMissing` | Error | No licence key is configured. |
| 3003 | `LicenceKeyMalformed` | Error | The licence key is not three base64url segments. |
| 3004 | `LicenceKeyUnreadable` | Error | The licence key's segments are not base64url-encoded JSON. |
| 3005 | `LicenceKeyNotEs256` | Error | The licence key is not signed with ES256. |
| 3006 | `LicenceKeyWithoutKeyId` | Error | The licence key names no signing key: a format from before 0.5. |
| 3007 | `LicenceKeyUnknownSigningKey` | Error | The licence key was signed by a key this version does not know. |
| 3008 | `LicenceKeyInvalidSignature` | Error | The licence key's signature is invalid. |
| 3009 | `LicenceKeyNotForTenantryPro` | Error | The licence key has the wrong issuer or audience. |
| 3010 | `LicenceKeyWithoutFormatVersion` | Error | The licence key has no format version: a format from before 0.5. |
| 3011 | `LicenceKeyUnsupportedFormat` | Error | The licence key is in a format this version does not read. |
| 3101 | `TenantProvisioned` | Information | A tenant was provisioned. |
| 3102 | `ProvisioningStepRunning` | Information | A provisioning step starts for a tenant. |
| 3103 | `ProvisioningStepSkipped` | Debug | A provisioning step does not apply to a tenant. |
| 3104 | `ProvisioningStepFailed` | Error | A provisioning step failed for a tenant. |
| 3105 | `TenantDeprovisioned` | Information | A tenant was deprovisioned (offboarded). |
| 3106 | `DeprovisioningStepRunning` | Information | A deprovisioning step starts for a tenant. |
| 3107 | `DeprovisioningStepSkipped` | Debug | A deprovisioning step does not apply to a tenant. |
| 3108 | `DeprovisioningStepFailed` | Error | A deprovisioning step failed for a tenant; the steps after it did not run. |
| 3201 | `TenantWorkFailed` | Error | A `TenantBackgroundService`'s work failed for a tenant; the other tenants go on. |
| 3202 | `SweepFailed` | Error | A `PeriodicTenantBackgroundService`'s sweep failed; the next one runs on schedule. |
| 3203 | `InactiveTenantSkipped` | Debug | Background work or a for-each-tenant schedule skips a tenant `ValidateTenantActivity` refuses. |
| 3204 | `SweepSkipped` | Debug | A `TenantBackgroundService`'s `ShouldRunAsync` returned false, so this instance skips the sweep. |
| 3301 | `ConnectionStringCacheBypassed` | Warning | `CacheConnectionStrings` was called, but a connection string provider registered after `UsePro` replaced the cache. |
| 3302 | `ConnectionStringTenantUnreadable` | Warning | The tenant store could not be read for an invalidated tenant's connection string, so `CacheConnectionStrings` read it from the caller's descriptor and did not cache it. It does the same without reading the store for that tenant for the next 5 seconds, so it is logged at most once per tenant in that time. |
| 3401 | `JobWithoutTenantRunning` | Warning | A job or message carries no tenant and runs without one (`OnMissingTenant` is Warn). |
| 3402 | `JobWithoutTenantSkipped` | Warning | A job or message carries no tenant and is skipped (`OnMissingTenant` is Skip). |
| 3403 | `JobWithInvalidTenantRunning` | Warning | A job or message carries an invalid tenant id and runs without a tenant (`OnUnresolvedTenant` is Warn). |
| 3404 | `JobWithInvalidTenantSkipped` | Warning | A job or message carries an invalid tenant id and is skipped (`OnUnresolvedTenant` is Skip). |
| 3405 | `JobWithUnknownTenantRunning` | Warning | A job or message carries a tenant the store does not have and runs without a tenant (`OnUnresolvedTenant` is Warn). |
| 3406 | `JobWithUnknownTenantSkipped` | Warning | A job or message carries a tenant the store does not have and is skipped (`OnUnresolvedTenant` is Skip). |
| 3407 | `JobWithInactiveTenantRunning` | Warning | A job or message carries a tenant that is not active and runs without a tenant (`OnUnresolvedTenant` is Warn). |
| 3408 | `JobWithInactiveTenantSkipped` | Warning | A job or message carries a tenant that is not active and is skipped (`OnUnresolvedTenant` is Skip). |
| 4001 | `TenantDatabaseExists` | Debug | A tenant's database already exists, so provisioning does not create it. |
| 4002 | `TenantDatabaseCreatedElsewhere` | Warning | Creating a tenant's database failed, but it exists: probably another provisioning run created it. |
| 4003 | `TenantDatabaseCreated` | Information | A tenant's database was created. |
| 4004 | `TenantSchemaInPlace` | Information | A tenant's schema exists, created or already there. |
| 4005 | `TenantDatabaseDropped` | Information | A tenant's database was dropped (`AddDatabaseDeprovisioning`). |
| 4006 | `TenantSchemaDropped` | Information | A tenant's schema was dropped (`AddSchemaDeprovisioning`). |
| 4007 | `TenantSharedDataDeleted` | Information | A tenant's rows were deleted from the shared database (`AddSharedDataDeletion`). |
| 4008 | `PoolNotCleared` | Debug | Provisioning found a tenant's database, and the connection's provider has no `ClearPool` to release the pooled connection it checked with. |
| 4101 | `MigrationRunStarting` | Information | A migration run starts (`MigrateAsync`, `MigrateAllAsync`, `MigrateTenantAsync`, `migrate-tenants`). |
| 4102 | `MigrationRunCompleted` | Information | A migration run completed with no failures. |
| 4103 | `MigrationRunCompletedWithFailures` | Warning | A migration run completed, and some databases or schemas failed (each failure is logged as 4105 or 4106). |
| 4104 | `MigrationProgressReporterFailed` | Warning | The progress reporter passed to a migration run threw; the run goes on. |
| 4105 | `MigrationFailed` | Error | Applying a context's migrations failed for a tenant. |
| 4106 | `MigrationContextNotCreated` | Error | A context could not be created for a tenant to apply its migrations. |
| 4107 | `MigrationsPending` | Information | A tenant's database or schema has migrations to apply. |
| 4108 | `TenantMigrated` | Information | A tenant's database or schema was migrated. |
| 4109 | `MigrationStatusUnreadable` | Warning | `GetStatusAsync`, `GetTenantStatusAsync` or the migration health check could not read a tenant's migrations. |
| 4110 | `MigrationStatusContextNotCreated` | Warning | `GetStatusAsync`, `GetTenantStatusAsync` or the migration health check could not create a context for a tenant. |
| 4111 | `StartupMigrationsStarting` | Information | Migrations at startup begin. |
| 4112 | `StartupMigrationsCompleted` | Information | Migrations at startup completed with no failures. |
| 4113 | `StartupMigrationsFailed` | Warning | Migrations at startup failed for some databases or schemas. |
| 4114 | `MigrationRunStopped` | Warning | A migration run stopped before attempting every database or schema: `MaxFailures` (`--max-failures`) was reached, or `migrate-tenants` was asked to stop. |
| 4115 | `MigrationRunStopping` | Warning | `migrate-tenants` got SIGTERM or Ctrl+C: the databases or schemas in progress finish, and no other starts. |
| 4116 | `MigrationRunNotStarted` | Error | `migrate-tenants` could not start: invalid arguments, no valid licence, a tenant the store does not have, or a tenant store that cannot be read. |
| 4201 | `TenantDatabaseUnreachable` | Warning | The database health check could not reach a tenant's database. |
| 4202 | `DatabaseHealthCheckContextNotCreated` | Warning | The database health check could not create a context for a tenant. |
| 4301 | `AuditEntry` | Information | The logging audit store writes an audit entry. |
| 4302 | `AuditEntriesNotWritten` | Error | Audit entries for committed changes were not written: the store could not be resolved, or it threw. |
| 4401 | `SchemaPerTenantContextNotChecked` | Warning | Schema per tenant could not create a registered context as the host started, so whether it calls `UseTenantry()` is checked only when Pro creates it for a tenant. Not logged for a context from `AddDbContextPerTenantDatabase` that needs a tenant to find its database. |
| 4402 | `SharedTenantsRefusedContext` | Warning | In mixed mode, a context has entity types that are neither tenant-owned nor marked as shared, so a `Shared` tenant is refused it. Logged once per context type, in the category `Tenantry.Pro.MixedMode`. |

## See also

- [Health checks](health-checks.md)
- [Troubleshooting](troubleshooting.md)
