# `ProServiceCollectionExtensions` class

Namespace: `Tenantry.Pro` · Package: `Tenantry.Pro` · [API reference](README.md)

Extension methods for enabling Tenantry.Pro features on [`ITenantBuilder<TKey>`](https://tenantry.dev/docs/core/api/tenantry-core-itenantbuilder).

```csharp
public static class ProServiceCollectionExtensions
```

## Methods

### `UsePro<TKey>(ITenantBuilder<TKey>, Action<ProBuilder<TKey>>)`

Enables Tenantry.Pro features: database-per-tenant, schema-per-tenant, and offline licence validation.

```csharp
public static ITenantBuilder<TKey> UsePro<TKey>(this ITenantBuilder<TKey> builder, Action<ProBuilder<TKey>> configure) where TKey : IEquatable<TKey>, IParsable<TKey>
```

Type parameters:

- `TKey`: The tenant identifier type. Must match the key type used in `AddTenantry`.

Parameters:

- `builder` [`ITenantBuilder<TKey>`](https://tenantry.dev/docs/core/api/tenantry-core-itenantbuilder): The tenant builder obtained from `AddTenantry`.
- `configure` `Action<ProBuilder<TKey>>`: A delegate for configuring Pro features via [`ProBuilder<TKey>`](tenantry-pro-internal-probuilder.md).

Returns: [`ITenantBuilder<TKey>`](https://tenantry.dev/docs/core/api/tenantry-core-itenantbuilder): The same `builder` for chaining.

```csharp
builder.Services.AddTenantry<string>(tenant =>
{
    tenant.ResolveFromHeader("X-Tenant-Id");
    tenant.UseStore<MyTenantStore>();
    tenant.UsePro(pro =>
    {
        pro.WithLicence(config["Tenantry:Licence"]!);
        pro.UseDatabasePerTenant(opts =>
            opts.GetConnectionString = tenant => $"Server=.;Database=myapp_{tenant.TenantId};Integrated Security=true");
    });
});
```
