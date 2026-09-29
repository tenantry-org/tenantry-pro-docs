# `DatabaseProvisioningService<TKey>` class

Namespace: `Tenantry.Pro.EfCore.SqlServer.Strategies.DatabasePerTenant` · Package: `Tenantry.Pro.EfCore.SqlServer` · [API reference](README.md)

Creates a dedicated SQL Server database for a tenant when called explicitly.

Inject this service in the code path that creates new tenants and call [`DatabaseProvisioningServiceBase<TKey>.ProvisionAsync`](tenantry-pro-efcore-strategies-databasepertenant-databaseprovisioningservicebase.md) explicitly. After provisioning, the database exists but contains no tables — apply EF Core migrations separately via `dbContext.Database.MigrateAsync()` or the migration orchestration service.

```csharp
public sealed class DatabaseProvisioningService<TKey> : DatabaseProvisioningServiceBase<TKey>, ITenantInfrastructureProvisioner<TKey> where TKey : IEquatable<TKey>, IParsable<TKey>
```

## Type parameters

- `TKey`: The tenant identifier type.

Inherits [`DatabaseProvisioningServiceBase<TKey>`](tenantry-pro-efcore-strategies-databasepertenant-databaseprovisioningservicebase.md).

Implements [`ITenantInfrastructureProvisioner<TKey>`](tenantry-pro-lifecycle-itenantinfrastructureprovisioner.md).

## Constructors

### `DatabaseProvisioningService(ITenantStoreAccessor<TKey>, ITenantConnectionStringResolver<TKey>, ILicenseGuard, ILogger<DatabaseProvisioningService<TKey>>)`

Creates the provisioning service. Resolved automatically by the DI container when you call `pro.AddDatabaseProvisioning()`; you do not normally construct it yourself.

```csharp
public DatabaseProvisioningService(ITenantStoreAccessor<TKey> storeAccessor, ITenantConnectionStringResolver<TKey> connectionStrings, ILicenseGuard licenseGuard, ILogger<DatabaseProvisioningService<TKey>> logger)
```

Parameters:

- `storeAccessor` [`ITenantStoreAccessor<TKey>`](https://tenantry.dev/docs/core/api/tenantry-core-itenantstoreaccessor): Resolves tenant descriptors.
- `connectionStrings` [`ITenantConnectionStringResolver<TKey>`](https://tenantry.dev/docs/core/api/tenantry-core-itenantconnectionstringresolver): Resolves each tenant's connection string (Core, configured by `UseDatabasePerTenant`).
- `licenseGuard` [`ILicenseGuard`](tenantry-pro-licensing-ilicenseguard.md): Guards provisioning behind a valid Tenantry.Pro licence.
- `logger` `ILogger<DatabaseProvisioningService<TKey>>`: The logger.

## Methods

### `BuildAdminConnectionString(string)`

Returns a connection string that targets a server/admin database (one that always exists), derived from the tenant connection string, so `CREATE DATABASE` can be issued.

```csharp
protected override string BuildAdminConnectionString(string tenantConnectionString)
```

Parameters:

- `tenantConnectionString` `string`: The tenant's connection string, naming a database that may not exist yet.

Returns: `string`

### `CreateDatabaseIfNotExistsAsync(string, string, CancellationToken)`

Issues the provider-specific, idempotent `CREATE DATABASE` against `adminConnectionString`. Implementations are responsible for identifier escaping.

```csharp
protected override Task CreateDatabaseIfNotExistsAsync(string adminConnectionString, string databaseName, CancellationToken cancellationToken)
```

Parameters:

- `adminConnectionString` `string`: A connection string for the server's administrative database, from [`DatabaseProvisioningServiceBase<TKey>.BuildAdminConnectionString`](tenantry-pro-efcore-strategies-databasepertenant-databaseprovisioningservicebase.md).
- `databaseName` `string`: The name of the tenant database to create, unescaped.
- `cancellationToken` `CancellationToken`: Cancels the operation.

Returns: `Task`

### `ExtractDatabaseName(string)`

Extracts the tenant database name from the tenant connection string.

```csharp
protected override string? ExtractDatabaseName(string tenantConnectionString)
```

Parameters:

- `tenantConnectionString` `string`: The tenant's connection string.

Returns: `string`
