# `IConnectionStringProtector` interface

Namespace: `Tenantry.Pro.Strategies.DatabasePerTenant` · Package: `Tenantry.Pro` · [API reference](README.md)

Encrypts and decrypts connection strings for at-rest protection in the connection string cache. Implement this interface and register it in DI when using [`ConnectionStringEncryptionMode.Custom`](tenantry-pro-strategies-databasepertenant-connectionstringencryptionmode.md).

The protector is only applied to values stored in the cache. [`ITenantConnectionStringResolver<TKey>`](https://tenantry.dev/docs/core/api/tenantry-core-itenantconnectionstringresolver) always returns plaintext to callers — encryption is transparent.

```csharp
public interface IConnectionStringProtector
```

## Methods

### `Protect(string)`

Encrypts `connectionString` for cache storage.

```csharp
string Protect(string connectionString)
```

Parameters:

- `connectionString` `string`: The connection string to protect.

Returns: `string`

### `Unprotect(string)`

Decrypts a previously [`IConnectionStringProtector.Protect`](tenantry-pro-strategies-databasepertenant-iconnectionstringprotector.md)-ed value.

```csharp
string Unprotect(string protectedConnectionString)
```

Parameters:

- `protectedConnectionString` `string`: A connection string returned by [`IConnectionStringProtector.Protect`](tenantry-pro-strategies-databasepertenant-iconnectionstringprotector.md).

Returns: `string`
