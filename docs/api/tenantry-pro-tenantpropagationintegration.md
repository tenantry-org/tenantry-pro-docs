# `TenantPropagationIntegration<TAdapter>` class

Namespace: `Tenantry.Pro` · Package: `Tenantry.Pro` · [API reference](README.md)

An extension point: for code that extends the package, such as another package that builds on it. An application rarely needs it.

A tenant-propagation adapter as [`TenantPropagationAdapter.Add<TKey, TAdapter>`](tenantry-pro-tenantpropagationadapter.md) registered it: its options, the propagator, and how many times its host side has run. Resolve it in the adapter's host side, mark it wired, and give it to the library's filters. Registered as a singleton.

```csharp
public static ConsumerBuilder<string, string> UseTenantry(this ConsumerBuilder<string, string> consumer, IServiceProvider services)
{
    var integration = services.GetService<TenantPropagationIntegration<KafkaPropagation>>()
        ?? throw new InvalidOperationException("Call pro.AddKafkaPropagation() in UsePro.");
    integration.MarkWired();
    // ...add the consumer's filter, which resolves each message's tenant with integration.Propagator and integration.Options
    return consumer;
}
```

```csharp
[EditorBrowsable(EditorBrowsableState.Advanced)]
public sealed class TenantPropagationIntegration<TAdapter> where TAdapter : class, ITenantPropagationAdapter
```

## Type parameters

- `TAdapter`: The adapter.

## Properties

### `IsWired`

Whether the host side has run at least once.

```csharp
public bool IsWired { get; }
```

Value: `bool`

### `Options`

The adapter's policy, which its registration configured: what happens to a job or message that carries no tenant, or one that cannot be found. Validated when the host starts.

```csharp
public TenantPropagationOptions Options { get; }
```

Value: [`TenantPropagationOptions`](tenantry-pro-tenantpropagationoptions.md)

### `Propagator`

Finds the tenant a job or message carries and makes it current: pass it [`TenantPropagationIntegration<TAdapter>.Options`](tenantry-pro-tenantpropagationintegration.md), the adapter's policy.

```csharp
public ITenantPropagator Propagator { get; }
```

Value: [`ITenantPropagator`](tenantry-pro-itenantpropagator.md)

### `WiredCount`

How many times the host side has run ([`TenantPropagationIntegration<TAdapter>.MarkWired`](tenantry-pro-tenantpropagationintegration.md)), once for each bus or configuration it wired.

```csharp
public int WiredCount { get; }
```

Value: `int`

## Methods

### `MarkWired()`

Records that the host side has wired one more bus or configuration. Call it from the host side, once for each, so the startup check knows it ran.

```csharp
public void MarkWired()
```
