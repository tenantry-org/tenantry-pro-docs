# `MixedStrategyResolver<TKey>` class

Namespace: `Tenantry.Pro.Strategies.MixedMode` · Package: `Tenantry.Pro` · [API reference](README.md)

Resolves the connection string or schema name for the tenant currently in scope, delegating to the appropriate underlying resolver based on the tenant's assigned strategy.

```csharp
public sealed class MixedStrategyResolver<TKey> where TKey : IEquatable<TKey>, IParsable<TKey>
```

## Type parameters

- `TKey`: The tenant identifier type.

## Constructors

### `MixedStrategyResolver(ITenantContext<TKey>, IOptions<MixedModeOptions<TKey>>, IServiceProvider)`

Resolves the connection string or schema name for the tenant currently in scope, delegating to the appropriate underlying resolver based on the tenant's assigned strategy.

```csharp
public MixedStrategyResolver(ITenantContext<TKey> tenantContext, IOptions<MixedModeOptions<TKey>> options, IServiceProvider serviceProvider)
```

Parameters:

- `tenantContext` [`ITenantContext<TKey>`](https://tenantry.dev/docs/core/api/tenantry-core-itenantcontext): Supplies the current tenant.
- `options` `IOptions<MixedModeOptions<TKey>>`: The mixed-mode options, which choose each tenant's strategy.
- `serviceProvider` `IServiceProvider`: Resolves the chosen strategy's services.

## Methods

### `GetCurrentStrategy()`

Returns the [`TenantStrategy`](tenantry-pro-strategies-mixedmode-tenantstrategy.md) for the tenant currently in scope.

```csharp
public TenantStrategy GetCurrentStrategy()
```

Returns: [`TenantStrategy`](tenantry-pro-strategies-mixedmode-tenantstrategy.md)

Exceptions:

- [`TenantNotResolvedException`](https://tenantry.dev/docs/core/api/tenantry-core-exceptions-tenantnotresolvedexception): No tenant in scope.

### `ResolveConnectionString()`

Returns the connection string for the current tenant, or [null](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null) when the strategy is [`TenantStrategy.Shared`](tenantry-pro-strategies-mixedmode-tenantstrategy.md) or [`TenantStrategy.Schema`](tenantry-pro-strategies-mixedmode-tenantstrategy.md).

```csharp
public string? ResolveConnectionString()
```

Returns: `string`

Exceptions:

- `InvalidOperationException`: Strategy is [`TenantStrategy.Database`](tenantry-pro-strategies-mixedmode-tenantstrategy.md) but `UseDatabasePerTenant()` was not called.

### `ResolveSchemaName()`

Returns the schema name for the current tenant, or [null](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null) when the strategy is [`TenantStrategy.Shared`](tenantry-pro-strategies-mixedmode-tenantstrategy.md) or [`TenantStrategy.Database`](tenantry-pro-strategies-mixedmode-tenantstrategy.md).

```csharp
public string? ResolveSchemaName()
```

Returns: `string`

Exceptions:

- `InvalidOperationException`: Strategy is [`TenantStrategy.Schema`](tenantry-pro-strategies-mixedmode-tenantstrategy.md) but `UseSchemaPerTenant()` was not called.
