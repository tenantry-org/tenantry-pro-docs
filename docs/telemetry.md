# Telemetry

Tenantry.Pro emits per-tenant HTTP request metrics through `System.Diagnostics.Metrics`, tagged by
`tenant.id`. The instruments are standard .NET meters, so any OpenTelemetry-compatible collector
(Prometheus, Grafana, Azure Monitor, …) can scrape them — and `dotnet-counters` can read them with no
extra setup.

## The meter

All instruments live on a single meter named **`Tenantry.Pro`** (`TenantryMeter.Instance`), each
tagged with `tenant.id`:

| Instrument | Type | Meaning |
|------------|------|---------|
| `tenantry.requests.count` | Counter | Total HTTP requests per tenant |
| `tenantry.requests.duration` | Histogram (ms) | Request duration per tenant |
| `tenantry.requests.errors` | Counter | Requests that returned a 5xx per tenant |
| `tenantry.requests.active` | UpDownCounter | In-flight requests per tenant |

## Enabling per-request metrics (ASP.NET Core)

Metrics are recorded by middleware in `Tenantry.Pro.AspNetCore`. Register it on the Pro builder, then
add the middleware **after** `UseTenantry()` so the tenant context is populated before a metric is
recorded:

```csharp
using Tenantry.Pro.AspNetCore.Telemetry.Extensions;

builder.Services.AddTenantry<string>(tenant =>
{
    tenant.ResolveFromHeader("X-Tenant-Id");
    tenant.UseStore<MyTenantStore>();

    tenant.UsePro(pro =>
    {
        pro.WithLicence(builder.Configuration["Tenantry:Licence"]!);
        pro.AddTenantMetrics(opts =>
        {
            opts.ExcludePaths = ["/health", "/healthz", "/ready"];   // prefix match; these are the defaults
            opts.AdditionalTags["service"] = "orders-api";           // added to every metric
        });
    });
});

var app = builder.Build();

app.UseTenantry();
app.UseTenantryMetrics();   // must come AFTER UseTenantry()
```

Options (`TenantMetricsOptions`):

| Option | Default | Meaning |
|--------|---------|---------|
| `ExcludePaths` | `["/health", "/healthz", "/ready"]` | Paths excluded from metrics (prefix match) — keep probe noise out |
| `AdditionalTags` | empty | Extra tags added to every emitted metric (e.g. environment, service name) |

Requests with no resolved tenant are still recorded, tagged `tenant.id = "unknown"`.

## Collecting the metrics

### OpenTelemetry

Add the meter name to your OpenTelemetry metrics pipeline:

```csharp
builder.Services.AddOpenTelemetry()
    .WithMetrics(m => m
        .AddMeter("Tenantry.Pro")
        .AddPrometheusExporter());      // or OTLP, Azure Monitor, etc.
```

### dotnet-counters (development)

```bash
dotnet-counters monitor --counters Tenantry.Pro --process-id <pid>
```

## Custom metrics

`TenantryMeter.Instance` is public, so you can attach your own instruments to the same meter and have
them collected wherever you scrape `Tenantry.Pro`:

```csharp
var myCounter = TenantryMeter.Instance.CreateCounter<long>("myapp.orders.created");
```

## A note on cardinality

Every metric is tagged with `tenant.id`. With a large tenant population this is high-cardinality —
size your metrics backend accordingly, or aggregate/drop the `tenant.id` tag in your collector for
tenants you do not need to track individually.

## See also

- [Health checks](health-checks.md) — `/health` is excluded from metrics by default.
