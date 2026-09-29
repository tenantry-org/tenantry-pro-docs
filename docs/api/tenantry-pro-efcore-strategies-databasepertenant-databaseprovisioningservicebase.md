# `DatabaseProvisioningServiceBase<TKey>` class

Namespace: `Tenantry.Pro.EfCore.Strategies.DatabasePerTenant` · Package: `Tenantry.Pro.EfCore` · [API reference](README.md)

Provider-agnostic base for database-per-tenant provisioning. Implements the shared pipeline (licence guard, connection-string resolution, tenant lookup, database-name extraction, logging) as a template method and defers the ADO.NET-specific work to the provider subclass.

```csharp
public abstract class DatabaseProvisioningServiceBase<TKey> : ITenantInfrastructureProvisioner<TKey> where TKey : IEquatable<TKey>, IParsable<TKey>
```

## Type parameters

- `TKey`: The tenant identifier type.

Implements [`ITenantInfrastructureProvisioner<TKey>`](tenantry-pro-lifecycle-itenantinfrastructureprovisioner.md).

Derived types: [`DatabaseProvisioningService<TKey>`](tenantry-pro-efcore-mysql-strategies-databasepertenant-databaseprovisioningservice.md), [`DatabaseProvisioningService<TKey>`](tenantry-pro-efcore-npgsql-strategies-databasepertenant-databaseprovisioningservice.md), [`DatabaseProvisioningService<TKey>`](tenantry-pro-efcore-sqlserver-strategies-databasepertenant-databaseprovisioningservice.md).

## Properties

### `Step`

Identifies which lifecycle step this provisioner represents. Used by [`TenantProvisioningResult<TKey>.CompletedUpTo`](tenantry-pro-lifecycle-tenantprovisioningresult.md) to record pipeline progress.

```csharp
public TenantProvisioningStep Step { get; }
```

Value: [`TenantProvisioningStep`](tenantry-pro-lifecycle-tenantprovisioningstep.md)

## Methods

### `BuildAdminConnectionString(string)`

Returns a connection string that targets a server/admin database (one that always exists), derived from the tenant connection string, so `CREATE DATABASE` can be issued.

```csharp
protected abstract string BuildAdminConnectionString(string tenantConnectionString)
```

Parameters:

- `tenantConnectionString` `string`: The tenant's connection string, naming a database that may not exist yet.

Returns: `string`

### `CreateDatabaseIfNotExistsAsync(string, string, CancellationToken)`

Issues the provider-specific, idempotent `CREATE DATABASE` against `adminConnectionString`. Implementations are responsible for identifier escaping.

```csharp
protected abstract Task CreateDatabaseIfNotExistsAsync(string adminConnectionString, string databaseName, CancellationToken cancellationToken)
```

Parameters:

- `adminConnectionString` `string`: A connection string for the server's administrative database, from [`DatabaseProvisioningServiceBase<TKey>.BuildAdminConnectionString`](tenantry-pro-efcore-strategies-databasepertenant-databaseprovisioningservicebase.md).
- `databaseName` `string`: The name of the tenant database to create, unescaped.
- `cancellationToken` `CancellationToken`: Cancels the operation.

Returns: `Task`

### `ExtractDatabaseName(string)`

Extracts the tenant database name from the tenant connection string.

```csharp
protected abstract string? ExtractDatabaseName(string tenantConnectionString)
```

Parameters:

- `tenantConnectionString` `string`: The tenant's connection string.

Returns: `string`

### `ProvisionAsync(TKey, CancellationToken)`

Creates the database for `tenantId` if it does not already exist.

```csharp
public Task ProvisionAsync(TKey tenantId, CancellationToken cancellationToken = default)
```

Parameters:

- `tenantId` `TKey`: The tenant to provision.
- `cancellationToken` `CancellationToken`: A cancellation token.

Returns: `Task`

Exceptions:

- [`LicenseRequiredException`](tenantry-pro-exceptions-licenserequiredexception.md): The licence key is missing or invalid.
