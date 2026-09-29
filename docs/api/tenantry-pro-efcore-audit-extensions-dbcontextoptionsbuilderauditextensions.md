# `DbContextOptionsBuilderAuditExtensions` class

Namespace: `Tenantry.Pro.EfCore.Audit.Extensions` · Package: `Tenantry.Pro.EfCore` · [API reference](README.md)

Extension methods for wiring `Tenantry.Pro.EfCore.Audit` into an EF Core `DbContext`.

```csharp
public static class DbContextOptionsBuilderAuditExtensions
```

## Methods

### `UseAuditLogging(DbContextOptionsBuilder, IServiceProvider)`

Adds the `Tenantry.Pro` audit interceptor to the `DbContextOptionsBuilder`. Call this inside `AddDbContext` to enable audit logging for this context type.

```csharp
public static DbContextOptionsBuilder UseAuditLogging(this DbContextOptionsBuilder optionsBuilder, IServiceProvider serviceProvider)
```

Parameters:

- `optionsBuilder` `DbContextOptionsBuilder`: The EF Core options builder.
- `serviceProvider` `IServiceProvider`: The application's `IServiceProvider`; used to resolve the audit interceptor from DI.

Returns: `DbContextOptionsBuilder`: The same `optionsBuilder` for chaining.

Requires `pro.AddAuditLogging()` to have been called during service registration so the tenant type is known without specifying it here.

```csharp
builder.Services.AddDbContext<AppDbContext>((sp, options) =>
{
    options.UseSqlServer(connectionString);
    options.UseAuditLogging(sp);
});
```

### `UseAuditLogging<TKey>(DbContextOptionsBuilder, IServiceProvider)`

Adds the `Tenantry.Pro` audit interceptor to the `DbContextOptionsBuilder`. Call this inside `AddDbContext` to enable audit logging for this context type.

```csharp
public static DbContextOptionsBuilder UseAuditLogging<TKey>(this DbContextOptionsBuilder optionsBuilder, IServiceProvider serviceProvider) where TKey : IEquatable<TKey>, IParsable<TKey>
```

Type parameters:

- `TKey`: The tenant identifier type.

Parameters:

- `optionsBuilder` `DbContextOptionsBuilder`: The EF Core options builder.
- `serviceProvider` `IServiceProvider`: The application's `IServiceProvider`; used to resolve `TenantAuditInterceptor<TKey>` from DI.

Returns: `DbContextOptionsBuilder`: The same `optionsBuilder` for chaining.

```csharp
builder.Services.AddDbContext<AppDbContext>((sp, options) =>
{
    options.UseSqlServer(connectionString);
    options.UseAuditLogging<string>(sp);
});
```
