# `ProBuilder<TKey>` class

Namespace: `Tenantry.Pro.Internal` · Package: `Tenantry.Pro` · [API reference](README.md)

Fluent builder for configuring Tenantry.Pro features. Obtained via `tenant.UsePro(pro => { ... })` inside `AddTenantry`.

```csharp
public sealed class ProBuilder<TKey> where TKey : IEquatable<TKey>, IParsable<TKey>
```

## Type parameters

- `TKey`: The tenant identifier type.

## Constructors

### `ProBuilder(IServiceCollection)`

Fluent builder for configuring Tenantry.Pro features. Obtained via `tenant.UsePro(pro => { ... })` inside `AddTenantry`.

```csharp
public ProBuilder(IServiceCollection services)
```

Parameters:

- `services` `IServiceCollection`: The application's service collection.

## Properties

### `Services`

Gets the underlying service collection.

```csharp
public IServiceCollection Services { get; }
```

Value: `IServiceCollection`

## Methods

### `UseDatabasePerTenant(Action<DatabasePerTenantOptions<TKey>>)`

Enables the database-per-tenant isolation strategy. Each tenant's data is stored in a dedicated SQL Server database.

```csharp
public ProBuilder<TKey> UseDatabasePerTenant(Action<DatabasePerTenantOptions<TKey>> configure)
```

Parameters:

- `configure` `Action<DatabasePerTenantOptions<TKey>>`: Sets how each tenant's connection string is resolved, cached and protected.

Returns: [`ProBuilder<TKey>`](tenantry-pro-internal-probuilder.md)

The connection-string delegates are registered with Core (`UseConnectionStrings`), so inject Core's [`ITenantConnectionStringResolver<TKey>`](https://tenantry.dev/docs/core/api/tenantry-core-itenantconnectionstringresolver) inside your `AddDbContext` factory to obtain the per-tenant connection string. Pro adds optional caching and at-rest encryption of cached values on top of it. To enable database provisioning, reference `Tenantry.Pro.EfCore.SqlServer` and call `pro.AddDatabaseProvisioning()`.

### `UseMixedMode(Action<MixedModeOptions<TKey>>)`

Enables the mixed-mode strategy, routing individual tenants to either database-per-tenant, schema-per-tenant, or shared isolation.

```csharp
public ProBuilder<TKey> UseMixedMode(Action<MixedModeOptions<TKey>> configure)
```

Parameters:

- `configure` `Action<MixedModeOptions<TKey>>`: Chooses each tenant's strategy and configures the strategies in use.

Returns: [`ProBuilder<TKey>`](tenantry-pro-internal-probuilder.md)

Call [`ProBuilder<TKey>.UseDatabasePerTenant`](tenantry-pro-internal-probuilder.md) and/or [`ProBuilder<TKey>.UseSchemaPerTenant`](tenantry-pro-internal-probuilder.md) before [`ProBuilder<TKey>.UseMixedMode`](tenantry-pro-internal-probuilder.md) for the strategies your delegate may return. If a strategy is returned at runtime but was never registered, an `InvalidOperationException` is thrown at resolution time with a clear message. Tenants returning [`TenantStrategy.Shared`](tenantry-pro-strategies-mixedmode-tenantstrategy.md) require no additional registration.

### `UseSchemaPerTenant(Action<SchemaPerTenantOptions<TKey>>)`

Enables the schema-per-tenant isolation strategy. Each tenant's data is stored in a dedicated schema within a shared SQL Server database.

```csharp
public ProBuilder<TKey> UseSchemaPerTenant(Action<SchemaPerTenantOptions<TKey>> configure)
```

Parameters:

- `configure` `Action<SchemaPerTenantOptions<TKey>>`: Sets how each tenant's schema is named.

Returns: [`ProBuilder<TKey>`](tenantry-pro-internal-probuilder.md)

After calling this, inject [`ISchemaNameResolver<TKey>`](tenantry-pro-strategies-schemapertenant-ischemanameresolver.md) in your `DbContext.OnModelCreating` to set `modelBuilder.HasDefaultSchema(...)`. Also call `options.AddSchemaPerTenantCaching<TKey>(sp)` in `AddDbContext` to ensure EF Core compiles a separate model per tenant schema. To enable schema provisioning, reference `Tenantry.Pro.EfCore.SqlServer` and call `pro.AddSchemaProvisioning()`.

### `WithLicence(string)`

Configures the licence key used to validate Pro features.

```csharp
public ProBuilder<TKey> WithLicence(string licenceKey)
```

Parameters:

- `licenceKey` `string`: The signed JWT licence key issued by Tenantry, from tenantry.dev/dashboard/pro. It does not expire. A missing or invalid key stops the application from starting ([`LicenseRequiredException`](tenantry-pro-exceptions-licenserequiredexception.md)).

Returns: [`ProBuilder<TKey>`](tenantry-pro-internal-probuilder.md)
