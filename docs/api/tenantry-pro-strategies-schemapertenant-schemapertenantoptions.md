# `SchemaPerTenantOptions<TKey>` class

Namespace: `Tenantry.Pro.Strategies.SchemaPerTenant` · Package: `Tenantry.Pro` · [API reference](README.md)

Options for the schema-per-tenant strategy. Configure via `pro.UseSchemaPerTenant(opts => { ... })`.

```csharp
public sealed class SchemaPerTenantOptions<TKey> where TKey : IEquatable<TKey>, IParsable<TKey>
```

## Type parameters

- `TKey`: The tenant identifier type.

## Properties

### `GetSchemaName`

A delegate that returns the SQL schema name for the given tenant descriptor.

```csharp
public Func<ITenantDescriptor<TKey>, string>? GetSchemaName { get; set; }
```

Value: `Func<ITenantDescriptor<TKey>, string>`

```csharp
opts.GetSchemaName = tenant => $"tenant_{tenant.TenantId}";
```
