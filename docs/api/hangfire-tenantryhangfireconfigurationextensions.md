# `TenantryHangfireConfigurationExtensions` class

Namespace: `Hangfire` · Package: `Tenantry.Pro.Hangfire` · [API reference](README.md)

Extension methods for wiring Tenantry.Pro's Hangfire integration into Hangfire's configuration.

```csharp
public static class TenantryHangfireConfigurationExtensions
```

## Methods

### `UseTenantry(IGlobalConfiguration, IServiceProvider)`

Adds Tenantry's job filter to Hangfire's global filters: a job enqueued while a tenant is current carries it, and runs as that tenant. Requires `pro.AddHangfirePropagation()`.

```csharp
public static IGlobalConfiguration UseTenantry(this IGlobalConfiguration configuration, IServiceProvider services)
```

Parameters:

- `configuration` `IGlobalConfiguration`: Hangfire's configuration.
- `services` `IServiceProvider`: The application's services, where `pro.AddHangfirePropagation()` registered the integration.

Returns: `IGlobalConfiguration`: The same `configuration` for chaining.

Exceptions:

- `InvalidOperationException`: `pro.AddHangfirePropagation()` was not called.

Call it in the `AddHangfire((sp, config) => …)` callback (Hangfire.NetCore, which Hangfire.AspNetCore includes), or on `GlobalConfiguration.Configuration` with the built application's services before the application starts. Hangfire's filters are global, so calling it again replaces the filter, for the services given last.
