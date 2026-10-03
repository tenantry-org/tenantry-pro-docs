# `ConnectionStringCacheOptions` class

Namespace: `Tenantry.Pro` · Package: `Tenantry.Pro` · [API reference](README.md)

How tenants' connection strings are cached. Set with `pro.CacheConnectionStrings(o => ...)`.

```csharp
public sealed class ConnectionStringCacheOptions
```

## Properties

### `Duration`

How long a tenant's connection string is reused before the connection-string delegates are called again. Defaults to 30 minutes. It must be positive, or the application does not start.

```csharp
public TimeSpan Duration { get; set; }
```

Value: `TimeSpan`
