# `DatabasePerTenantOptions<TKey>` class

Namespace: `Tenantry.Pro.Strategies.DatabasePerTenant` · Package: `Tenantry.Pro` · [API reference](README.md)

Options for the database-per-tenant strategy. Configure via `pro.UseDatabasePerTenant(opts => { ... })`.

```csharp
public sealed class DatabasePerTenantOptions<TKey> where TKey : IEquatable<TKey>, IParsable<TKey>
```

## Type parameters

- `TKey`: The tenant identifier type.

## Properties

### `CacheConnectionStrings`

When [true](https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool), resolved connection strings are cached in memory for [`DatabasePerTenantOptions<TKey>.CacheDuration`](tenantry-pro-strategies-databasepertenant-databasepertenantoptions.md) before being re-resolved. Enable this when [`DatabasePerTenantOptions<TKey>.GetConnectionStringAsync`](tenantry-pro-strategies-databasepertenant-databasepertenantoptions.md) performs expensive lookups.

```csharp
public bool CacheConnectionStrings { get; set; }
```

Value: `bool`

At-rest [`DatabasePerTenantOptions<TKey>.Encryption`](tenantry-pro-strategies-databasepertenant-databasepertenantoptions.md) (including `UseDataProtectionEncryption()`) only applies to cached values, so it has no effect unless caching is enabled.

### `CacheDuration`

How long a cached connection string is considered valid. Defaults to 30 minutes. Only used when [`DatabasePerTenantOptions<TKey>.CacheConnectionStrings`](tenantry-pro-strategies-databasepertenant-databasepertenantoptions.md) is [true](https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool).

```csharp
public TimeSpan CacheDuration { get; set; }
```

Value: `TimeSpan`

### `Encryption`

Configures at-rest encryption for cached connection strings. When set to any mode other than [`ConnectionStringEncryptionMode.None`](tenantry-pro-strategies-databasepertenant-connectionstringencryptionmode.md), the cache stores ciphertext and decrypts on every cache hit. Only effective when [`DatabasePerTenantOptions<TKey>.CacheConnectionStrings`](tenantry-pro-strategies-databasepertenant-databasepertenantoptions.md) is [true](https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool).

```csharp
public ConnectionStringEncryptionOptions Encryption { get; set; }
```

Value: [`ConnectionStringEncryptionOptions`](tenantry-pro-strategies-databasepertenant-connectionstringencryptionoptions.md)

### `GetConnectionString`

A delegate that returns the connection string for the given tenant descriptor. Registered with Core's [`TenantConnectionStringOptions<TKey>`](https://tenantry.dev/docs/core/api/tenantry-core-tenantconnectionstringoptions); read connection strings through [`ITenantConnectionStringResolver<TKey>`](https://tenantry.dev/docs/core/api/tenantry-core-itenantconnectionstringresolver), which also applies caching when enabled. Without caching it runs every time a `DbContext` is created, so it must be fast — compute the string from tenant properties, do not call external services.

```csharp
public Func<ITenantDescriptor<TKey>, string>? GetConnectionString { get; set; }
```

Value: `Func<ITenantDescriptor<TKey>, string>`

```csharp
opts.GetConnectionString = tenant => $"Server=.;Database=myapp_{tenant.TenantId};Integrated Security=true";
```

### `GetConnectionStringAsync`

An asynchronous delegate that returns the connection string for the given tenant descriptor. Use this when the connection string must be retrieved from an external source (e.g. a secrets vault or a configuration database). If both [`DatabasePerTenantOptions<TKey>.GetConnectionString`](tenantry-pro-strategies-databasepertenant-databasepertenantoptions.md) and [`DatabasePerTenantOptions<TKey>.GetConnectionStringAsync`](tenantry-pro-strategies-databasepertenant-databasepertenantoptions.md) are set, the async delegate is preferred by `ITenantConnectionStringResolver.ResolveAsync`.

```csharp
public Func<ITenantDescriptor<TKey>, CancellationToken, ValueTask<string>>? GetConnectionStringAsync { get; set; }
```

Value: `Func<ITenantDescriptor<TKey>, CancellationToken, ValueTask<string>>`

```csharp
opts.GetConnectionStringAsync = async (tenant, ct) =>
    await secretsClient.GetSecretAsync($"connstr-{tenant.TenantId}", ct);
```
