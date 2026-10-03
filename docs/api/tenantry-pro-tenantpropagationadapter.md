# `TenantPropagationAdapter` class

Namespace: `Tenantry.Pro` · Package: `Tenantry.Pro` · [API reference](README.md)

An extension point: for code that extends the package, such as another package that builds on it. An application rarely needs it.

Registers a tenant-propagation adapter ([`ITenantPropagationAdapter`](tenantry-pro-itenantpropagationadapter.md)) for its `pro.Add…Propagation()` method, as Tenantry.Pro's Hangfire, MassTransit, Quartz.NET and Rebus integrations do, and formats the tenant ids its `WithTenant` methods take.

```csharp
public static IProBuilder<TKey> AddKafkaPropagation<TKey>(this IProBuilder<TKey> pro, Action<TenantPropagationOptions>? configure = null)
    where TKey : IEquatable<TKey>, IParsable<TKey>
{
    TenantPropagationAdapter.Add<TKey, KafkaPropagation>(pro, "Kafka", configure);
    return pro;
}
```

```csharp
[EditorBrowsable(EditorBrowsableState.Advanced)]
public static class TenantPropagationAdapter
```

## Methods

### `Add<TKey, TAdapter>(IProBuilder<TKey>, string, Action<TenantPropagationOptions>?)`

Registers `TAdapter`, and [`TenantPropagationIntegration<TAdapter>`](tenantry-pro-tenantpropagationintegration.md) with its options, named `name` and configured by `configure`, as singletons; the propagator for the key type, which `UsePro` registers too; and the check that fails the host's start if the adapter's host side never ran ([`ITenantPropagationAdapter`](tenantry-pro-itenantpropagationadapter.md)).

```csharp
public static void Add<TKey, TAdapter>(IProBuilder<TKey> pro, string name, Action<TenantPropagationOptions>? configure = null) where TKey : IEquatable<TKey>, IParsable<TKey> where TAdapter : class, ITenantPropagationAdapter
```

Type parameters:

- `TKey`: The tenant identifier type.
- `TAdapter`: The adapter.

Parameters:

- `pro` [`IProBuilder<TKey>`](tenantry-pro-iprobuilder-1.md): The Pro builder.
- `name` `string`: The name of the adapter's [`TenantPropagationOptions`](tenantry-pro-tenantpropagationoptions.md), such as `Kafka`: also configurable with `services.Configure<TenantPropagationOptions>(name, …)`, and named in their validation error.
- `configure` `Action<TenantPropagationOptions>`: Configures the adapter's options, or [null](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null).

Exceptions:

- `ArgumentNullException`: `pro` or `name` is null.
- `ArgumentException`: `name` is empty or white space.

Calling it again for the same adapter adds `configure` to the same options, and registers nothing more. Options with a [`TenantPropagationBehavior`](tenantry-pro-tenantpropagationbehavior.md) that is not defined stop the host from starting with `OptionsValidationException`.

### `FormatTenantId<TKey>(TKey, string?)`

Formats a tenant id the application gives a job or message, for a `WithTenant` method, as [`ITenantPropagator.CurrentTenantId`](tenantry-pro-itenantpropagator.md) carries the current tenant's: with the invariant culture (`Format<TKey>(TKey)`). Refuses the ids Tenantry reserves for "no tenant".

```csharp
public static string FormatTenantId<TKey>(TKey tenantId, string? paramName = null) where TKey : IEquatable<TKey>, IParsable<TKey>
```

Type parameters:

- `TKey`: The tenant identifier type.

Parameters:

- `tenantId` `TKey`: The tenant's id.
- `paramName` `string`: The name of the caller's parameter, for the exception; by default the expression passed.

Returns: `string`: The id as text, for the job's or message's `HeaderName` header.

Exceptions:

- `ArgumentNullException`: `tenantId` is null.
- `ArgumentException`: `tenantId` is the key type's default value (`Guid.Empty`, `0`) or an empty string, which Tenantry reserves for "no tenant".

```csharp
headers[TenantPropagation.HeaderName] = TenantPropagationAdapter.FormatTenantId(tenantId);
```
