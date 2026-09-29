# `MySqlProBuilderExtensions` class

Namespace: `Tenantry.Pro.EfCore.MySql.Extensions` · Package: `Tenantry.Pro.EfCore.MySql` · [API reference](README.md)

Extension methods for registering MySQL/MariaDB-backed Tenantry.Pro services on [`ProBuilder<TKey>`](tenantry-pro-internal-probuilder.md).

MySQL treats schemas and databases as synonyms; therefore, only the database-per-tenant strategy is supported for MySQL/MariaDB. There is no `AddSchemaProvisioning` extension for this provider.

```csharp
public static class MySqlProBuilderExtensions
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
