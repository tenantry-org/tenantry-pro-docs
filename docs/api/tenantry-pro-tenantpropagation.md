# `TenantPropagation` class

Namespace: `Tenantry.Pro` · Package: `Tenantry.Pro` · [API reference](README.md)

How the Hangfire, MassTransit, Quartz.NET and Rebus integrations carry the tenant in a job or message.

```csharp
public static class TenantPropagation
```

## Fields

### `HeaderName`

The name of the Hangfire job parameter, MassTransit or Rebus message header, or Quartz.NET job data key that carries the tenant's id, formatted with the invariant culture: `tenantry-tenant-id`.

```csharp
public const string HeaderName = "tenantry-tenant-id"
```

Returns: `string`
