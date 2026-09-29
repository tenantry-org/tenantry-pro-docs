# `ProBuilderEfCoreExtensions` class

Namespace: `Tenantry.Pro.EfCore` · Package: `Tenantry.Pro.EfCore` · [API reference](README.md)

Extension methods for [`ProBuilder<TKey>`](tenantry-pro-internal-probuilder.md) that wire up EF Core services.

```csharp
public static class ProBuilderEfCoreExtensions
```

## Methods

### `AddDatabaseProvisioning<TKey, TService>(ProBuilder<TKey>)`

Registers `TService` as the database-per-tenant provisioner singleton. Call this after `pro.UseDatabasePerTenant(...)` when you need on-demand database provisioning for new tenants.

```csharp
public static ProBuilder<TKey> AddDatabaseProvisioning<TKey, TService>(this ProBuilder<TKey> builder) where TKey : IEquatable<TKey>, IParsable<TKey> where TService : class, ITenantInfrastructureProvisioner<TKey>
```

Type parameters:

- `TKey`: The tenant identifier type.
- `TService`: The provider-specific `DatabaseProvisioningService<TKey>` to register.

Parameters:

- `builder` [`ProBuilder<TKey>`](tenantry-pro-internal-probuilder.md): The Pro builder.

Returns: [`ProBuilder<TKey>`](tenantry-pro-internal-probuilder.md)

### `AddSchemaPerTenantCaching<TKey>(ProBuilder<TKey>)`

Registers EF Core model caching services for schema-per-tenant isolation.

```csharp
public static ProBuilder<TKey> AddSchemaPerTenantCaching<TKey>(this ProBuilder<TKey> builder) where TKey : IEquatable<TKey>, IParsable<TKey>
```

Type parameters:

- `TKey`: The tenant identifier type. Must match the key type used in `AddTenantry`.

Parameters:

- `builder` [`ProBuilder<TKey>`](tenantry-pro-internal-probuilder.md): The Pro builder obtained from `tenant.UsePro(...)`.

Returns: [`ProBuilder<TKey>`](tenantry-pro-internal-probuilder.md): The same `builder` for chaining.

Call this after `pro.UseSchemaPerTenant(...)`. Then, in your `AddDbContext` factory, call `options.AddSchemaPerTenantCaching(sp)` on the `DbContextOptionsBuilder`.

### `AddSchemaProvisioning<TKey, TService>(ProBuilder<TKey>, Action<SchemaProvisioningOptions>)`

Registers `TService` as the schema-per-tenant provisioner singleton. Call this after `pro.UseSchemaPerTenant(...)` when you need on-demand schema provisioning for new tenants.

```csharp
public static ProBuilder<TKey> AddSchemaProvisioning<TKey, TService>(this ProBuilder<TKey> builder, Action<SchemaProvisioningOptions> configure) where TKey : IEquatable<TKey>, IParsable<TKey> where TService : class, ITenantInfrastructureProvisioner<TKey>
```

Type parameters:

- `TKey`: The tenant identifier type.
- `TService`: The provider-specific `SchemaProvisioningService<TKey>` to register.

Parameters:

- `builder` [`ProBuilder<TKey>`](tenantry-pro-internal-probuilder.md): The Pro builder.
- `configure` `Action<SchemaProvisioningOptions>`: Configures provisioning options. At minimum set [`SchemaProvisioningOptions.ConnectionString`](tenantry-pro-efcore-strategies-schemapertenant-schemaprovisioningoptions.md) to the shared database connection string.

Returns: [`ProBuilder<TKey>`](tenantry-pro-internal-probuilder.md)

### `WithMigrationOrchestration<TKey, TContext>(ProBuilder<TKey>, Func<string, TContext>, bool, bool)`

Registers EF Core migration orchestration for database-per-tenant databases. Requires `pro.UseDatabasePerTenant(...)` to be called first.

```csharp
[RequiresUnreferencedCode("EF Core migrations use reflection and are not trim-safe.")]
[RequiresDynamicCode("EF Core migrations generate code at run time and are not Native AOT-compatible.")]
public static ProBuilder<TKey> WithMigrationOrchestration<TKey, TContext>(this ProBuilder<TKey> builder, Func<string, TContext> contextFactory, bool runAtStartup = false, bool failStartupOnMigrationError = false) where TKey : IEquatable<TKey>, IParsable<TKey> where TContext : DbContext
```

Type parameters:

- `TKey`: The tenant identifier type.
- `TContext`: The consumer's EF Core `DbContext` type.

Parameters:

- `builder` [`ProBuilder<TKey>`](tenantry-pro-internal-probuilder.md): The Pro builder.
- `contextFactory` `Func<string, TContext>`: A factory that accepts a per-tenant connection string and returns a configured `TContext`. The returned context is disposed after each migration run.
- `runAtStartup` `bool`: When [true](https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool), registers a hosted service that migrates all tenant databases automatically before the application begins accepting requests. Every instance of the application does this, so with more than one instance prefer running migrations once per deployment instead (see the "Multiple instances" section of the migration orchestration guide).
- `failStartupOnMigrationError` `bool`: With `runAtStartup`, stop the application from starting when any tenant's migration fails, instead of logging the failures and serving traffic against the tenants whose schema is out of date. Requires `runAtStartup`.

Returns: [`ProBuilder<TKey>`](tenantry-pro-internal-probuilder.md)

Exceptions:

- `InvalidOperationException`: Thrown immediately if `UseDatabasePerTenant` has not been called first.
- `ArgumentException`: `failStartupOnMigrationError` is set without `runAtStartup`.
