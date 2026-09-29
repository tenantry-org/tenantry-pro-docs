# `ISchemaNameResolver<TKey>` interface

Namespace: `Tenantry.Pro.Strategies.SchemaPerTenant` · Package: `Tenantry.Pro` · [API reference](README.md)

Resolves the SQL schema name for the tenant currently in scope. Inject this into your `DbContext.OnModelCreating` override to set the default schema for the current tenant's model.

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    var schema = _schemaNameResolver.Resolve();
    modelBuilder.HasDefaultSchema(schema);
    base.OnModelCreating(modelBuilder);
}
```

```csharp
public interface ISchemaNameResolver<TKey> where TKey : IEquatable<TKey>, IParsable<TKey>
```

## Type parameters

- `TKey`: The tenant identifier type.

## Methods

### `Resolve()`

Returns the schema name for the tenant currently in scope.

```csharp
string Resolve()
```

Returns: `string`

Exceptions:

- [`TenantNotResolvedException`](https://tenantry.dev/docs/core/api/tenantry-core-exceptions-tenantnotresolvedexception): Thrown when no tenant has been resolved for the current scope.
