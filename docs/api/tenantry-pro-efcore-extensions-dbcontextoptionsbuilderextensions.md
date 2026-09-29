# `DbContextOptionsBuilderExtensions` class

Namespace: `Tenantry.Pro.EfCore.Extensions` · Package: `Tenantry.Pro.EfCore` · [API reference](README.md)

Extension methods for wiring Tenantry.Pro schema-per-tenant services into `DbContextOptionsBuilder`.

```csharp
public static class DbContextOptionsBuilderExtensions
```

## Methods

### `AddSchemaPerTenantCaching(DbContextOptionsBuilder, IServiceProvider)`

Configures EF Core model caching for schema-per-tenant isolation.

```csharp
public static DbContextOptionsBuilder AddSchemaPerTenantCaching(this DbContextOptionsBuilder optionsBuilder, IServiceProvider serviceProvider)
```

Parameters:

- `optionsBuilder` `DbContextOptionsBuilder`: The options builder for the application's `DbContext`.
- `serviceProvider` `IServiceProvider`: The application service provider from the `AddDbContext` factory callback.

Returns: `DbContextOptionsBuilder`: The same `optionsBuilder` for chaining.

Requires `pro.AddSchemaPerTenantCaching<TKey>()` to have been called during service registration so the tenant type is known without specifying it here.

### `AddSchemaPerTenantCaching<TKey>(DbContextOptionsBuilder, IServiceProvider)`

Configures EF Core model caching for schema-per-tenant isolation.

```csharp
public static DbContextOptionsBuilder AddSchemaPerTenantCaching<TKey>(this DbContextOptionsBuilder optionsBuilder, IServiceProvider serviceProvider) where TKey : IEquatable<TKey>, IParsable<TKey>
```

Type parameters:

- `TKey`: The tenant identifier type. Must match the key type used in the Tenantry DI registration.

Parameters:

- `optionsBuilder` `DbContextOptionsBuilder`: The options builder for the application's `DbContext`.
- `serviceProvider` `IServiceProvider`: The application service provider from the `AddDbContext` factory callback. This is used so the custom model cache key factory can resolve the current `ITenantContext<TKey>` from application DI.

Returns: `DbContextOptionsBuilder`: The same `optionsBuilder` for chaining.

Use this together with `pro.UseSchemaPerTenant(...)` and `pro.AddSchemaPerTenantCaching()`, which registers the model cache key factory this uses. Your `DbContext` must also apply the tenant schema in `OnModelCreating`, typically via `modelBuilder.HasDefaultSchema(_schemaNameResolver.Resolve())`.
