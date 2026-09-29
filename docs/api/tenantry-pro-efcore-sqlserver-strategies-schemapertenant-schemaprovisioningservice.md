# `SchemaProvisioningService<TKey>` class

Namespace: `Tenantry.Pro.EfCore.SqlServer.Strategies.SchemaPerTenant` · Package: `Tenantry.Pro.EfCore.SqlServer` · [API reference](README.md)

Creates a dedicated SQL Server schema for a tenant when called explicitly.

Inject this service in the code path that creates new tenants and call [`SchemaProvisioningServiceBase<TKey>.ProvisionAsync`](tenantry-pro-efcore-strategies-schemapertenant-schemaprovisioningservicebase.md) explicitly.

```csharp
public sealed class SchemaProvisioningService<TKey> : SchemaProvisioningServiceBase<TKey>, ITenantInfrastructureProvisioner<TKey> where TKey : IEquatable<TKey>, IParsable<TKey>
```

## Type parameters

- `TKey`: The tenant identifier type.

Inherits [`SchemaProvisioningServiceBase<TKey>`](tenantry-pro-efcore-strategies-schemapertenant-schemaprovisioningservicebase.md).

Implements [`ITenantInfrastructureProvisioner<TKey>`](tenantry-pro-lifecycle-itenantinfrastructureprovisioner.md).

## Constructors

### `SchemaProvisioningService(ITenantStoreAccessor<TKey>, IOptions<SchemaPerTenantOptions<TKey>>, string, ILicenseGuard, ILogger<SchemaProvisioningService<TKey>>)`

Creates the provisioning service. Resolved automatically by the DI container when you call `pro.AddSchemaProvisioning(...)`; you do not normally construct it yourself.

```csharp
public SchemaProvisioningService(ITenantStoreAccessor<TKey> storeAccessor, IOptions<SchemaPerTenantOptions<TKey>> options, string connectionString, ILicenseGuard licenseGuard, ILogger<SchemaProvisioningService<TKey>> logger)
```

Parameters:

- `storeAccessor` [`ITenantStoreAccessor<TKey>`](https://tenantry.dev/docs/core/api/tenantry-core-itenantstoreaccessor): Resolves tenant descriptors.
- `options` `IOptions<SchemaPerTenantOptions<TKey>>`: Schema-per-tenant options (supplies the schema name).
- `connectionString` `string`: The shared-database connection string in which schemas are created.
- `licenseGuard` [`ILicenseGuard`](tenantry-pro-licensing-ilicenseguard.md): Guards provisioning behind a valid Tenantry.Pro licence.
- `logger` `ILogger<SchemaProvisioningService<TKey>>`: The logger.

## Methods

### `CreateSchemaIfNotExistsAsync(string, string, CancellationToken)`

Issues the provider-specific, idempotent `CREATE SCHEMA` against `connectionString`. Implementations are responsible for identifier escaping.

```csharp
protected override Task CreateSchemaIfNotExistsAsync(string connectionString, string schemaName, CancellationToken cancellationToken)
```

Parameters:

- `connectionString` `string`: A connection string for the shared database.
- `schemaName` `string`: The name of the tenant's schema, unescaped.
- `cancellationToken` `CancellationToken`: Cancels the operation.

Returns: `Task`
