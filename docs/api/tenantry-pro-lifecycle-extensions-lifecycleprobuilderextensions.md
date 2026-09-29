# `LifecycleProBuilderExtensions` class

Namespace: `Tenantry.Pro.Lifecycle.Extensions` · Package: `Tenantry.Pro` · [API reference](README.md)

Extension methods for registering tenant lifecycle management on [`ProBuilder<TKey>`](tenantry-pro-internal-probuilder.md).

```csharp
public static class LifecycleProBuilderExtensions
```

## Methods

### `AddLifecycleManagement<TKey>(ProBuilder<TKey>, Action<TenantLifecycleOptions>?)`

Registers [`ITenantLifecycleManager<TKey>`](tenantry-pro-lifecycle-itenantlifecyclemanager.md), which orchestrates the full tenant creation pipeline: provision → migrate → seed.

```csharp
public static ProBuilder<TKey> AddLifecycleManagement<TKey>(this ProBuilder<TKey> builder, Action<TenantLifecycleOptions>? configure = null) where TKey : IEquatable<TKey>, IParsable<TKey>
```

Type parameters:

- `TKey`: The tenant identifier type.

Parameters:

- `builder` [`ProBuilder<TKey>`](tenantry-pro-internal-probuilder.md): The Pro builder.
- `configure` `Action<TenantLifecycleOptions>`: Optional configuration for lifecycle behaviour.

Returns: [`ProBuilder<TKey>`](tenantry-pro-internal-probuilder.md): The same builder for chaining.

Each pipeline step only runs when its corresponding service is registered:

- &lt;span class="term"&gt;Provisioning&lt;/span&gt;Call `AddDatabaseProvisioning()` or `AddSchemaProvisioning()`.
- &lt;span class="term"&gt;Migrations&lt;/span&gt;Call `WithMigrationOrchestration()`.
- &lt;span class="term"&gt;Seeding&lt;/span&gt;Register your own `ITenantSeeder<TKey>` implementation.
