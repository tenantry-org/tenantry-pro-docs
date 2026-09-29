# `DataProtectionEncryptionExtensions` class

Namespace: `Tenantry.Pro.AspNetCore` · Package: `Tenantry.Pro.AspNetCore` · [API reference](README.md)

Extension methods for enabling ASP.NET Core Data Protection encryption for cached connection strings.

```csharp
public static class DataProtectionEncryptionExtensions
```

## Methods

### `UseDataProtectionEncryption<TKey>(ProBuilder<TKey>)`

Registers [`IConnectionStringProtector`](tenantry-pro-strategies-databasepertenant-iconnectionstringprotector.md) backed by ASP.NET Core Data Protection. Keys are automatically managed, rotated, and persisted by the Data Protection stack.

```csharp
public static ProBuilder<TKey> UseDataProtectionEncryption<TKey>(this ProBuilder<TKey> builder) where TKey : IEquatable<TKey>, IParsable<TKey>
```

Type parameters:

- `TKey`: The tenant identifier type.

Parameters:

- `builder` [`ProBuilder<TKey>`](tenantry-pro-internal-probuilder.md): The Pro builder.

Returns: [`ProBuilder<TKey>`](tenantry-pro-internal-probuilder.md): The same builder for chaining.

Call `builder.Services.AddDataProtection()` before calling this method.     If `IDataProtectionProvider` is not registered, resolution will throw at runtime.

This provides the Data Protection-backed protector from `Tenantry.Pro.AspNetCore`     without requiring `Tenantry.Pro` to depend on ASP.NET Core Data Protection.

The protector is only consulted when connection-string caching is enabled     ([`DatabasePerTenantOptions<TKey>.CacheConnectionStrings`](tenantry-pro-strategies-databasepertenant-databasepertenantoptions.md) = [true](https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool)).     If caching is off this call has no effect; a warning is logged at startup.
