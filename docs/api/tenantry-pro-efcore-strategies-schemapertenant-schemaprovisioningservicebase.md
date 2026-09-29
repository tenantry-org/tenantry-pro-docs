# `SchemaProvisioningServiceBase<TKey>` class

Namespace: `Tenantry.Pro.EfCore.Strategies.SchemaPerTenant` · Package: `Tenantry.Pro.EfCore` · [API reference](README.md)

Provider-agnostic base for schema-per-tenant provisioning. Implements the shared pipeline (licence guard, schema-name resolution, tenant lookup, connection-string guard, logging) as a template method and defers the ADO.NET-specific `CREATE SCHEMA` to the provider subclass.

```csharp
public abstract class SchemaProvisioningServiceBase<TKey> : ITenantInfrastructureProvisioner<TKey> where TKey : IEquatable<TKey>, IParsable<TKey>
```

## Type parameters

- `TKey`: The tenant identifier type.

Implements [`ITenantInfrastructureProvisioner<TKey>`](tenantry-pro-lifecycle-itenantinfrastructureprovisioner.md).

Derived types: [`SchemaProvisioningService<TKey>`](tenantry-pro-efcore-npgsql-strategies-schemapertenant-schemaprovisioningservice.md), [`SchemaProvisioningService<TKey>`](tenantry-pro-efcore-sqlserver-strategies-schemapertenant-schemaprovisioningservice.md).

## Properties

### `Step`

Identifies which lifecycle step this provisioner represents. Used by [`TenantProvisioningResult<TKey>.CompletedUpTo`](tenantry-pro-lifecycle-tenantprovisioningresult.md) to record pipeline progress.

```csharp
public TenantProvisioningStep Step { get; }
```

Value: [`TenantProvisioningStep`](tenantry-pro-lifecycle-tenantprovisioningstep.md)

## Methods

### `CreateSchemaIfNotExistsAsync(string, string, CancellationToken)`

Issues the provider-specific, idempotent `CREATE SCHEMA` against `connectionString`. Implementations are responsible for identifier escaping.

```csharp
protected abstract Task CreateSchemaIfNotExistsAsync(string connectionString, string schemaName, CancellationToken cancellationToken)
```

Parameters:

- `connectionString` `string`: A connection string for the shared database.
- `schemaName` `string`: The name of the tenant's schema, unescaped.
- `cancellationToken` `CancellationToken`: Cancels the operation.

Returns: `Task`

### `ProvisionAsync(TKey, CancellationToken)`

Creates the schema for `tenantId` if it does not already exist.

```csharp
public Task ProvisionAsync(TKey tenantId, CancellationToken cancellationToken = default)
```

Parameters:

- `tenantId` `TKey`: The tenant to provision.
- `cancellationToken` `CancellationToken`: A cancellation token.

Returns: `Task`

Exceptions:

- [`LicenseRequiredException`](tenantry-pro-exceptions-licenserequiredexception.md): The licence key is missing or invalid.
