# `TenantMetricsOptions` class

Namespace: `Tenantry.Pro.AspNetCore.Telemetry` · Package: `Tenantry.Pro.AspNetCore` · [API reference](README.md)

Options for tenant request metrics collection. Configure via `pro.AddTenantMetrics(opts => { ... })`.

```csharp
public sealed class TenantMetricsOptions
```

## Properties

### `AdditionalTags`

Additional tags added to every metric emitted by the middleware. Useful for environment or service-name tagging.

```csharp
public IDictionary<string, string> AdditionalTags { get; set; }
```

Value: `IDictionary<string, string>`

### `ExcludePaths`

Request paths that are excluded from metrics recording. Matched via prefix — e.g. `"/health"` excludes `/health`, `/health/live`, etc. Default: `["/health", "/healthz", "/ready"]`.

```csharp
public IList<string> ExcludePaths { get; set; }
```

Value: `IList<string>`
