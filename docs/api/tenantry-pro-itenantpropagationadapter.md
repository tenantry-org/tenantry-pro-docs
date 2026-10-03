# `ITenantPropagationAdapter` interface

Namespace: `Tenantry.Pro` · Package: `Tenantry.Pro` · [API reference](README.md)

An extension point: for code that extends the package, such as another package that builds on it. An application rarely needs it.

A tenant-propagation adapter for a job or messaging library: what the startup check needs to know of it. Tenantry.Pro's Hangfire, MassTransit, Quartz.NET and Rebus integrations implement it, and so can an adapter of your own for another library. Register it with [`TenantPropagationAdapter.Add<TKey, TAdapter>`](tenantry-pro-tenantpropagationadapter.md), which gives it its [`TenantPropagationOptions`](tenantry-pro-tenantpropagationoptions.md), the propagator, and the startup check.

An adapter has two sides. Its registration, a `pro.Add…Propagation()` method, calls     [`TenantPropagationAdapter.Add<TKey, TAdapter>`](tenantry-pro-tenantpropagationadapter.md). Its host side, a `UseTenantry` method on the     library's configuration, resolves [`TenantPropagationIntegration<TAdapter>`](tenantry-pro-tenantpropagationintegration.md), calls     [`TenantPropagationIntegration<TAdapter>.MarkWired`](tenantry-pro-tenantpropagationintegration.md), and adds the library's filters, which carry     the tenant with its [`TenantPropagationIntegration<TAdapter>.Propagator`](tenantry-pro-tenantpropagationintegration.md) and     [`TenantPropagationIntegration<TAdapter>.Options`](tenantry-pro-tenantpropagationintegration.md).

Once every hosted service has started, the startup check fails the host with     `InvalidOperationException` if the host side has not run, since the jobs or messages would then     carry no tenant. The message names [`ITenantPropagationAdapter.Registration`](tenantry-pro-itenantpropagationadapter.md) and asks for [`ITenantPropagationAdapter.HostSide`](tenantry-pro-itenantpropagationadapter.md), or says     what [`ITenantPropagationAdapter.FindUnwired`](tenantry-pro-itenantpropagationadapter.md) returns. The check waits up to five seconds for a host side that runs on the     thread pool after the host has started, as a library that configures itself in a `BackgroundService`     does.

It is registered as a singleton, and created as the host starts.

```csharp
internal sealed class KafkaPropagation : ITenantPropagationAdapter
{
    public string Registration => "pro.AddKafkaPropagation()";
    public string HostSide => "call consumer.UseTenantry(sp) on each Kafka consumer";
}
```

```csharp
[EditorBrowsable(EditorBrowsableState.Advanced)]
public interface ITenantPropagationAdapter
```

## Properties

### `HostSide`

The host-side call the startup error asks for, with an example, such as `call consumer.UseTenantry(sp) on each Kafka consumer`.

```csharp
string HostSide { get; }
```

Value: `string`

### `Registration`

The registration that added the adapter, for the startup error, such as `pro.AddKafkaPropagation()`.

```csharp
string Registration { get; }
```

Value: `string`

## Methods

### `FindUnwired(IServiceProvider, int)`

Returns what is not wired, for the startup error, when the host side has run, but not for everything that needs it: for a library that can have several buses, each configured on its own. Returns [null](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null) to leave it to the check's own rule, that the host side must have run at least once; that is the default.

```csharp
string? FindUnwired(IServiceProvider services, int wiredCount)
```

Parameters:

- `services` `IServiceProvider`: The application's services.
- `wiredCount` `int`: How many times the host side has run ([`TenantPropagationIntegration<TAdapter>.WiredCount`](tenantry-pro-tenantpropagationintegration.md)).

Returns: `string`: What is not wired, as a sentence that ends with a full stop, or [null](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null).

```csharp
public string? FindUnwired(IServiceProvider services, int wiredCount)
{
    var consumers = services.GetServices<IKafkaConsumer>().Count();
    return wiredCount >= consumers ? null : $"Only {wiredCount} of {consumers} Kafka consumers call consumer.UseTenantry(sp).";
}
```

### `RunDeferredHostConfiguration(IServiceProvider)`

Runs host configuration that the library defers until one of its services is first resolved, so the startup check sees a host side that runs there. The check calls it once, before it waits, when the host side has not run for everything that needs it. By default it does nothing.

```csharp
void RunDeferredHostConfiguration(IServiceProvider services)
```

Parameters:

- `services` `IServiceProvider`: The application's services.

Only resolve configuration that starts nothing: a job server or a bus must not start here.
