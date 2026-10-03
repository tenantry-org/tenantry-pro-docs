# `StartupMigrations` enum

Namespace: `Tenantry.Pro.EfCore` · Package: `Tenantry.Pro.EfCore` · [API reference](README.md)

Whether, and how, an application applies a context's migrations when it starts.

```csharp
public enum StartupMigrations
```

## Values

| Value | Description |
|-------|-------------|
| `None = 0` | Not at startup: run them once per deployment instead (`RunTenantMigrationsIfRequestedAsync`). The default. |
| `LogFailures = 1` | Apply them before the application serves requests; failures are logged and the application starts. |
| `FailOnError = 2` | Apply them before the application serves requests; any failure stops the application from starting. |
