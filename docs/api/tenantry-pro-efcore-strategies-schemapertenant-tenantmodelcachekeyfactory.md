# `TenantModelCacheKeyFactory<TKey>` class

Namespace: `Tenantry.Pro.EfCore.Strategies.SchemaPerTenant` · Package: `Tenantry.Pro.EfCore` · [API reference](README.md)

EF Core `IModelCacheKeyFactory` that includes the current tenant ID in the cache key.

By default EF Core caches one compiled model per `DbContext` type. For schema-per-tenant,     each tenant needs a separate model so that `modelBuilder.HasDefaultSchema()` in     `OnModelCreating` resolves to the correct schema at query time.

Register this factory by calling `options.AddSchemaPerTenantCaching<TKey>(sp)`     inside your `AddDbContext` callback. Your `DbContext.OnModelCreating` must call     `modelBuilder.HasDefaultSchema(_schemaNameResolver.Resolve())` to apply the tenant schema.

**Memory note:** EF Core's model cache does not evict entries. With many tenants     each with its own schema, a compiled model is held per tenant. Restart the application if     schema configuration changes.

```csharp
public sealed class TenantModelCacheKeyFactory<TKey> : IModelCacheKeyFactory where TKey : IEquatable<TKey>, IParsable<TKey>
```

## Type parameters

- `TKey`: The tenant identifier type.

Implements `IModelCacheKeyFactory`.

## Constructors

### `TenantModelCacheKeyFactory(ITenantContext<TKey>)`

EF Core `IModelCacheKeyFactory` that includes the current tenant ID in the cache key.

```csharp
public TenantModelCacheKeyFactory(ITenantContext<TKey> tenantContext)
```

Parameters:

- `tenantContext` [`ITenantContext<TKey>`](https://tenantry.dev/docs/core/api/tenantry-core-itenantcontext): Supplies the current tenant, whose schema is part of the cache key.

By default EF Core caches one compiled model per `DbContext` type. For schema-per-tenant,     each tenant needs a separate model so that `modelBuilder.HasDefaultSchema()` in     `OnModelCreating` resolves to the correct schema at query time.

Register this factory by calling `options.AddSchemaPerTenantCaching<TKey>(sp)`     inside your `AddDbContext` callback. Your `DbContext.OnModelCreating` must call     `modelBuilder.HasDefaultSchema(_schemaNameResolver.Resolve())` to apply the tenant schema.

**Memory note:** EF Core's model cache does not evict entries. With many tenants     each with its own schema, a compiled model is held per tenant. Restart the application if     schema configuration changes.

## Methods

### `Create(DbContext, bool)`

Gets the model cache key for a given context.

```csharp
public object Create(DbContext context, bool designTime)
```

Parameters:

- `context` `DbContext`: The context to get the model cache key for.
- `designTime` `bool`: Whether the model should contain design-time configuration.

Returns: `object`: The created key.
