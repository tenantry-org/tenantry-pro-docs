# `AuditProBuilderExtensions` class

Namespace: `Tenantry.Pro.EfCore.Audit.Extensions` · Package: `Tenantry.Pro.EfCore` · [API reference](README.md)

Extension methods for registering audit-logging services on [`ProBuilder<TKey>`](tenantry-pro-internal-probuilder.md).

```csharp
public static class AuditProBuilderExtensions
```

## Methods

### `AddAuditLogging<TKey>(ProBuilder<TKey>, Action<AuditOptions>?)`

Registers EF Core audit logging for the current tenant. After each successful `SaveChanges` call, entity changes are forwarded to [`IAuditStore`](tenantry-pro-efcore-audit-iauditstore.md). The default store writes structured log entries.

```csharp
[RequiresUnreferencedCode("Audit logging reads EF Core's change tracker, which uses reflection and is not trim-safe.")]
[RequiresDynamicCode("Audit logging uses EF Core, which generates code at run time and is not Native AOT-compatible.")]
public static ProBuilder<TKey> AddAuditLogging<TKey>(this ProBuilder<TKey> builder, Action<AuditOptions>? configure = null) where TKey : IEquatable<TKey>, IParsable<TKey>
```

Type parameters:

- `TKey`: The tenant identifier type.

Parameters:

- `builder` [`ProBuilder<TKey>`](tenantry-pro-internal-probuilder.md): The Pro builder.
- `configure` `Action<AuditOptions>`: Optional callback to configure [`AuditOptions`](tenantry-pro-efcore-audit-auditoptions.md).

Returns: [`ProBuilder<TKey>`](tenantry-pro-internal-probuilder.md): The same `builder` for chaining.

Wire the interceptor into each `DbContext` by calling `options.UseAuditLogging<TKey>(sp)` inside `AddDbContext`. To replace the default logging store, register your own [`IAuditStore`](tenantry-pro-efcore-audit-iauditstore.md) implementation after this call.
