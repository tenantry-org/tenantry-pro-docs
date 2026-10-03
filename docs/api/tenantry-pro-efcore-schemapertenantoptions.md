# `SchemaPerTenantOptions<TKey>` class

Namespace: `Tenantry.Pro.EfCore` · Package: `Tenantry.Pro.EfCore` · [API reference](README.md)

Schema per tenant: each tenant's tables in a schema of its own, in a shared database. Set with `pro.UseSchemaPerTenant(o => o.GetSchemaName = …)`.

EF Core compiles a model for each schema, and compiles each query again for each schema's model. Its own cache     holds the models of about 40 schemas (about 100 on EF Core 8), or about 500 compiled queries, in all; past that,     it evicts them and compiles them again as tenants take turns. So each context type gets a cache of its own,     sized for [`SchemaPerTenantOptions<TKey>.MaxCachedSchemas`](tenantry-pro-efcore-schemapertenantoptions.md) schemas with [`SchemaPerTenantOptions<TKey>.MaxCompiledQueriesPerSchema`](tenantry-pro-efcore-schemapertenantoptions.md) queries each,     on top of EF Core's default.

Each cached schema holds a compiled model and its compiled queries in memory, so with a large model and many     schemas, weigh the limits against your memory budget. Past them, the least recently used entries are compiled     again when next needed.

```csharp
public sealed class SchemaPerTenantOptions<TKey> where TKey : IEquatable<TKey>, IParsable<TKey>
```

## Type parameters

- `TKey`: The tenant identifier type.

## Properties

### `GetSchemaName`

Returns the name of a tenant's schema. Required. It is called often, so it must be fast, and it must give the same name for a tenant each time; tenants given the same name share a schema. In mixed mode it is called only for [`TenantIsolation.Schema`](tenantry-pro-tenantisolation.md) tenants.

```csharp
public Func<ITenantDescriptor<TKey>, string>? GetSchemaName { get; set; }
```

Value: `Func<ITenantDescriptor<TKey>, string>`

```csharp
o.GetSchemaName = tenant => $"tenant_{tenant.TenantId}";
```

### `MaxCachedSchemas`

How many schemas' models each `DbContext` type keeps compiled: at least the number of tenant schemas in use at once. Defaults to 500; must be greater than zero.

```csharp
public int MaxCachedSchemas { get; set; }
```

Value: `int`

### `MaxCompiledQueriesPerSchema`

How many compiled queries the cache has room for per schema: about the number of distinct queries your application runs against the context. Defaults to 100; must not be negative.

```csharp
public int MaxCompiledQueriesPerSchema { get; set; }
```

Value: `int`
