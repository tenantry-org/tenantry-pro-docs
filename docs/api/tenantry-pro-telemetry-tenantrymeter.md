# `TenantryMeter` class

Namespace: `Tenantry.Pro.Telemetry` · Package: `Tenantry.Pro` · [API reference](README.md)

Central meter for all Tenantry.Pro tenant-scoped metrics. Instruments are compatible with any OpenTelemetry-compliant collector (Prometheus, Grafana, Azure Monitor, etc.) and do not require the OpenTelemetry SDK.

Register the meter in your OpenTelemetry pipeline by adding the meter name `"Tenantry.Pro"`:

```csharp
builder.Services.AddOpenTelemetry()
    .WithMetrics(m => m.AddMeter("Tenantry.Pro"));
```

Use `dotnet-counters monitor --counters Tenantry.Pro` for development-time inspection.

```csharp
public static class TenantryMeter
```

## Fields

### `Instance`

The underlying `Meter` instance. Name: `"Tenantry.Pro"`, version: `"1.0.0"`.

```csharp
public static readonly Meter Instance
```

Returns: `Meter`
