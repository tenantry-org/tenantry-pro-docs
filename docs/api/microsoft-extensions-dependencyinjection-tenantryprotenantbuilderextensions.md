# `TenantryProTenantBuilderExtensions` class

Namespace: `Microsoft.Extensions.DependencyInjection` · Package: `Tenantry.Pro` · [API reference](README.md)

Extension methods for enabling Tenantry.Pro on `ITenantBuilder<TKey>`.

```csharp
public static class TenantryProTenantBuilderExtensions
```

## Methods

### `UsePro<TKey>(ITenantBuilder<TKey>, Action<IProBuilder<TKey>>?)`

Enables Tenantry.Pro: checks the licence key when the application starts, and configures Pro's features.

```csharp
public static ITenantBuilder<TKey> UsePro<TKey>(this ITenantBuilder<TKey> builder, Action<IProBuilder<TKey>>? configure = null) where TKey : IEquatable<TKey>, IParsable<TKey>
```

Type parameters:

- `TKey`: The tenant identifier type.

Parameters:

- `builder` `ITenantBuilder<TKey>`: The tenant builder `AddTenantry` passes to its callback.
- `configure` `Action<IProBuilder<TKey>>`: Adds and configures Pro's features.

Returns: `ITenantBuilder<TKey>`: The same `builder` for chaining.

The licence key is read from the `Tenantry:License` setting (the `Tenantry__License`     environment variable) when the application starts, or set with `pro.UseLicenseKey(key)`. A missing     or invalid key stops the application from starting with [`LicenseRequiredException`](tenantry-pro-licenserequiredexception.md).

It registers [`ITenantProvisioner<TKey>`](tenantry-pro-itenantprovisioner.md), [`ITenantDeprovisioner<TKey>`](tenantry-pro-itenantdeprovisioner.md) and     [`ITenantPropagator`](tenantry-pro-itenantpropagator.md), whatever features are configured.

Calling it again adds to the same Pro registration. In a chain, call it before methods that return the     builder without its key type, such as `AddDbContextPerTenantDatabase`.

```csharp
builder.Services.AddTenantry<string>(tenant => tenant
    .ResolveFromHeader("X-Tenant-Id")
    .UseStore<MyTenantStore>()
    .UsePro(pro => pro.AddSeeder<DefaultSettingsSeeder>()));
```
