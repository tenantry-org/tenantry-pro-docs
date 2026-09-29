# `TenantLifecycleOptions` class

Namespace: `Tenantry.Pro.Lifecycle` · Package: `Tenantry.Pro` · [API reference](README.md)

Options for [`ITenantLifecycleManager<TKey>`](tenantry-pro-lifecycle-itenantlifecyclemanager.md) behaviour. Configure via `pro.AddLifecycleManagement(opts => { ... })`.

```csharp
public sealed class TenantLifecycleOptions
```

## Properties

### `StopOnFailure`

When [true](https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool) (the default), a failed pipeline step aborts the remaining steps. When [false](https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool), all configured steps are attempted regardless of prior failures; the result records the first error and reflects the highest step that actually completed. Default: [true](https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool).

```csharp
public bool StopOnFailure { get; set; }
```

Value: `bool`
