# `ITenantInfrastructureProvisioner<TKey>` interface

Namespace: `Tenantry.Pro.Lifecycle` · Package: `Tenantry.Pro` · [API reference](README.md)

Provisions the infrastructure (database or schema) for a new tenant. Implemented by database-provider packages (`Tenantry.Pro.EfCore.SqlServer`, `Tenantry.Pro.EfCore.Npgsql`, etc.) and consumed by [`ITenantLifecycleManager<TKey>`](tenantry-pro-lifecycle-itenantlifecyclemanager.md) to run the provisioning step without depending on any specific provider or strategy.

```csharp
public interface ITenantInfrastructureProvisioner<TKey> where TKey : IEquatable<TKey>, IParsable<TKey>
```

## Type parameters

- `TKey`: The tenant identifier type.

Derived types: [`DatabaseProvisioningService<TKey>`](tenantry-pro-efcore-mysql-strategies-databasepertenant-databaseprovisioningservice.md), [`DatabaseProvisioningService<TKey>`](tenantry-pro-efcore-npgsql-strategies-databasepertenant-databaseprovisioningservice.md), [`SchemaProvisioningService<TKey>`](tenantry-pro-efcore-npgsql-strategies-schemapertenant-schemaprovisioningservice.md), [`DatabaseProvisioningService<TKey>`](tenantry-pro-efcore-sqlserver-strategies-databasepertenant-databaseprovisioningservice.md), [`SchemaProvisioningService<TKey>`](tenantry-pro-efcore-sqlserver-strategies-schemapertenant-schemaprovisioningservice.md), [`DatabaseProvisioningServiceBase<TKey>`](tenantry-pro-efcore-strategies-databasepertenant-databaseprovisioningservicebase.md), [`SchemaProvisioningServiceBase<TKey>`](tenantry-pro-efcore-strategies-schemapertenant-schemaprovisioningservicebase.md).

## Properties

### `Step`

Identifies which lifecycle step this provisioner represents. Used by [`TenantProvisioningResult<TKey>.CompletedUpTo`](tenantry-pro-lifecycle-tenantprovisioningresult.md) to record pipeline progress.

```csharp
TenantProvisioningStep Step { get; }
```

Value: [`TenantProvisioningStep`](tenantry-pro-lifecycle-tenantprovisioningstep.md)

## Methods

### `ProvisionAsync(TKey, CancellationToken)`

Creates the infrastructure (database or schema) for the given tenant if it does not already exist.

```csharp
Task ProvisionAsync(TKey tenantId, CancellationToken cancellationToken = default)
```

Parameters:

- `tenantId` `TKey`: The tenant to provision.
- `cancellationToken` `CancellationToken`: A cancellation token.

Returns: `Task`
