# `TenantStrategy` enum

Namespace: `Tenantry.Pro.Strategies.MixedMode` · Package: `Tenantry.Pro` · [API reference](README.md)

Specifies which isolation strategy a tenant uses in mixed-mode deployments.

```csharp
public enum TenantStrategy
```

## Values

| Value | Description |
|-------|-------------|
| `Shared = 0` | The tenant shares the default database with row-level isolation. This is the default Core behaviour — no dedicated database or schema is provisioned. [`MixedStrategyResolver<TKey>.ResolveConnectionString`](tenantry-pro-strategies-mixedmode-mixedstrategyresolver.md) and [`MixedStrategyResolver<TKey>.ResolveSchemaName`](tenantry-pro-strategies-mixedmode-mixedstrategyresolver.md) both return [null](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null). |
| `Database = 1` | The tenant has its own dedicated database. |
| `Schema = 2` | The tenant uses a dedicated schema in a shared database. |
