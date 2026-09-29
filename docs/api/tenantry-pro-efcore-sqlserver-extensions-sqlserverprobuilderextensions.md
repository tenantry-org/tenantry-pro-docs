# `SqlServerProBuilderExtensions` class

Namespace: `Tenantry.Pro.EfCore.SqlServer.Extensions` · Package: `Tenantry.Pro.EfCore.SqlServer` · [API reference](README.md)

Extension methods for registering SQL Server-backed Tenantry.Pro services on [`ProBuilder<TKey>`](tenantry-pro-internal-probuilder.md).

```csharp
public static class SqlServerProBuilderExtensions
```

## Methods

### `AddDatabaseProvisioning<TKey>(ProBuilder<TKey>)`

Registers `TService` as the database-per-tenant provisioner singleton. Call this after `pro.UseDatabasePerTenant(...)` when you need on-demand database provisioning for new tenants.

```csharp
public static ProBuilder<TKey> AddDatabaseProvisioning<TKey>(this ProBuilder<TKey> builder) where TKey : IEquatable<TKey>, IParsable<TKey>
```

Type parameters:

- `TKey`: The tenant identifier type.

Parameters:

- `builder` [`ProBuilder<TKey>`](tenantry-pro-internal-probuilder.md): The Pro builder.

Returns: [`ProBuilder<TKey>`](tenantry-pro-internal-probuilder.md)

### `AddSchemaProvisioning<TKey>(ProBuilder<TKey>, Action<SchemaProvisioningOptions>)`

Registers `TService` as the schema-per-tenant provisioner singleton. Call this after `pro.UseSchemaPerTenant(...)` when you need on-demand schema provisioning for new tenants.

```csharp
public static ProBuilder<TKey> AddSchemaProvisioning<TKey>(this ProBuilder<TKey> builder, Action<SchemaProvisioningOptions> configure) where TKey : IEquatable<TKey>, IParsable<TKey>
```

Type parameters:

- `TKey`: The tenant identifier type.

Parameters:

- `builder` [`ProBuilder<TKey>`](tenantry-pro-internal-probuilder.md): The Pro builder.
- `configure` `Action<SchemaProvisioningOptions>`: Configures provisioning options. At minimum set [`SchemaProvisioningOptions.ConnectionString`](tenantry-pro-efcore-strategies-schemapertenant-schemaprovisioningoptions.md) to the shared database connection string.

Returns: [`ProBuilder<TKey>`](tenantry-pro-internal-probuilder.md)
