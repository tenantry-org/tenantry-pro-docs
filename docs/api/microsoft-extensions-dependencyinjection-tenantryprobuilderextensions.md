# `TenantryProBuilderExtensions` class

Namespace: `Microsoft.Extensions.DependencyInjection` · Package: `Tenantry.Pro` · [API reference](README.md)

Extension methods for configuring Tenantry.Pro's features on [`IProBuilder<TKey>`](tenantry-pro-iprobuilder-1.md).

```csharp
public static class TenantryProBuilderExtensions
```

## Methods

### `CacheConnectionStrings<TKey>(IProBuilder<TKey>, Action<ConnectionStringCacheOptions>?)`

Caches each tenant's connection string, so the connection-string delegates run once per tenant per [`ConnectionStringCacheOptions.Duration`](tenantry-pro-connectionstringcacheoptions.md) instead of for every context. Use it when the delegate is slow, such as one that reads a secrets store.

```csharp
public static IProBuilder<TKey> CacheConnectionStrings<TKey>(this IProBuilder<TKey> pro, Action<ConnectionStringCacheOptions>? configure = null) where TKey : IEquatable<TKey>, IParsable<TKey>
```

Type parameters:

- `TKey`: The tenant identifier type.

Parameters:

- `pro` [`IProBuilder<TKey>`](tenantry-pro-iprobuilder-1.md): The Pro builder.
- `configure` `Action<ConnectionStringCacheOptions>`: Sets how long a connection string is cached (30 minutes by default).

Returns: [`IProBuilder<TKey>`](tenantry-pro-iprobuilder-1.md): The same `pro` for chaining.

It wraps the tenants' `ITenantConnectionStringProvider<TKey>`, from `UseConnectionStrings`     in the same `AddTenantry`, called before or after `UsePro`, or a provider of your own registered     before `UsePro`. Everything that reads connection strings through it is then cached:     `AddDbContextPerTenantDatabase`, `CurrentTenantConnectionString<TKey>`, migrations and     health checks. Call `InvalidateAsync(TKey, CancellationToken)` when a tenant's connection     details change.

Once a tenant has been invalidated, the connection string the cache reads for it is computed from the     store's copy of the tenant, read through `ITenantLookup<TKey>` (from `CacheTenants`'     cache when there is one), so a request or job that still holds the descriptor it read before the     invalidation does not put the old connection string back in the cache. A tenant the store does not have,     made current with `MakeCurrent` or since removed, is read from the descriptor the caller passes. On a miss for an     invalidated tenant that `CacheTenants` does not have, the synchronous     `Get(ITenantDescriptor<TKey>)` blocks its thread while the store is read;     `GetAsync(ITenantDescriptor<TKey>, CancellationToken)` awaits the read.

The application fails to start if there are no connection strings to cache. A provider registered after     `UsePro` that does not wrap the cache takes its place, and a warning says so at startup. Calling it     again only applies `configure`.

```csharp
builder.Services.AddTenantry<string>(tenant => tenant
    .UseStore<MyTenantStore>()
    .UseConnectionStrings(o => o.GetConnectionStringAsync = (t, ct) => secrets.GetAsync($"db-{t.TenantId}", ct))
    .UsePro(pro => pro.CacheConnectionStrings(o => o.Duration = TimeSpan.FromMinutes(10))));
```

### `ConfigureProvisioning<TKey>(IProBuilder<TKey>, Action<TenantProvisioningOptions>)`

Sets how [`ITenantProvisioner<TKey>`](tenantry-pro-itenantprovisioner.md) runs the provisioning steps.

```csharp
public static IProBuilder<TKey> ConfigureProvisioning<TKey>(this IProBuilder<TKey> pro, Action<TenantProvisioningOptions> configure) where TKey : IEquatable<TKey>, IParsable<TKey>
```

Type parameters:

- `TKey`: The tenant identifier type.

Parameters:

- `pro` [`IProBuilder<TKey>`](tenantry-pro-iprobuilder-1.md): The Pro builder.
- `configure` `Action<TenantProvisioningOptions>`: Sets the options.

Returns: [`IProBuilder<TKey>`](tenantry-pro-iprobuilder-1.md): The same `pro` for chaining.

### `UseLicenseKey<TKey>(IProBuilder<TKey>, string)`

Sets the licence key, in place of the `Tenantry:License` setting.

```csharp
public static IProBuilder<TKey> UseLicenseKey<TKey>(this IProBuilder<TKey> pro, string key) where TKey : IEquatable<TKey>, IParsable<TKey>
```

Type parameters:

- `TKey`: The tenant identifier type.

Parameters:

- `pro` [`IProBuilder<TKey>`](tenantry-pro-iprobuilder-1.md): The Pro builder.
- `key` `string`: The licence key issued by Tenantry, from tenantry.dev/dashboard/pro. It does not expire. A missing or invalid key stops the application from starting ([`LicenseRequiredException`](tenantry-pro-licenserequiredexception.md)).

Returns: [`IProBuilder<TKey>`](tenantry-pro-iprobuilder-1.md): The same `pro` for chaining.

Exceptions:

- `ArgumentException`: `key` is empty or white space.

Without it, the key is read from configuration when the application starts, so it can come from any configuration source, including the `Tenantry__License` environment variable. Use this when the key comes from somewhere else.

### `UseMixedMode<TKey>(IProBuilder<TKey>, Action<MixedModeOptions<TKey>>)`

Turns on mixed mode: tenants with different isolation in one application, some on their own database, some on their own schema, and some sharing tables. `configure` says which tenant is which.

```csharp
public static IProBuilder<TKey> UseMixedMode<TKey>(this IProBuilder<TKey> pro, Action<MixedModeOptions<TKey>> configure) where TKey : IEquatable<TKey>, IParsable<TKey>
```

Type parameters:

- `TKey`: The tenant identifier type.

Parameters:

- `pro` [`IProBuilder<TKey>`](tenantry-pro-iprobuilder-1.md): The Pro builder.
- `configure` `Action<MixedModeOptions<TKey>>`: Sets [`MixedModeOptions<TKey>.GetIsolation`](tenantry-pro-mixedmodeoptions.md).

Returns: [`IProBuilder<TKey>`](tenantry-pro-iprobuilder-1.md): The same `pro` for chaining.

Exceptions:

- `InvalidOperationException`: `configure` does not set `GetIsolation`.

Provisioning steps see each tenant's isolation ([`TenantProvisioningContext<TKey>.Isolation`](tenantry-pro-tenantprovisioningcontext.md)):     creating a database applies only to [`TenantIsolation.Database`](tenantry-pro-tenantisolation.md) tenants, creating a schema     only to [`TenantIsolation.Schema`](tenantry-pro-tenantisolation.md) tenants, and migrations to both (the shared database is     migrated with the other tenants, by Tenantry.Pro.EfCore's migration runner).

Each tenant's connection string comes from your `UseConnectionStrings` delegate, which returns the     shared database's for tenants without their own. Tenantry.Pro.EfCore's `UseSchemaPerTenant` gives     only [`TenantIsolation.Schema`](tenantry-pro-tenantisolation.md) tenants a schema of their own; without it, a     [`TenantIsolation.Schema`](tenantry-pro-tenantisolation.md) tenant throws `InvalidOperationException` wherever its     isolation is read.

Every entity in a [`TenantIsolation.Shared`](tenantry-pro-tenantisolation.md) tenant's context must implement     `ITenantEntity<TKey>` or be marked as shared, with `[SharedAcrossTenants]` or     `IsSharedAcrossTenants()`.
