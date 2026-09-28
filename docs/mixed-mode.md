# Mixed mode

Use mixed mode when different tenants need different isolation: some on a dedicated database, some on
a dedicated schema, and some on the shared store. You assign a strategy **per tenant**, evaluated at
runtime from the tenant descriptor.

## What Tenantry.Pro provides

- `MixedStrategyResolver<TKey>` — resolves the active tenant's `TenantStrategy` and, for it, either a
  connection string (database mode), a schema name (schema mode), or `null` (shared mode).
- The tenant-to-strategy mapping you configure via `UseMixedMode`.

## Important boundary

Mixed mode gives you the **routing decision** — which strategy a tenant uses, and the corresponding
connection string or schema name. It does **not** automatically rewire your `DbContext` registration
between database-per-tenant and schema-per-tenant at runtime. You compose the final behaviour in your
`AddDbContext` factory using `MixedStrategyResolver<TKey>`. This keeps the mechanism explicit, because
mixing strategies meaningfully increases operational complexity.

## Registration

Register the strategies your mapping can return **before** `UseMixedMode`. A tenant routed to
`TenantStrategy.Database` requires `UseDatabasePerTenant`; `TenantStrategy.Schema` requires
`UseSchemaPerTenant`; `TenantStrategy.Shared` requires neither. Returning a strategy that was never
registered throws a clear `InvalidOperationException` at resolution time.

```csharp
using Tenantry.Pro;
using Tenantry.Pro.Strategies.MixedMode;

builder.Services.AddTenantry<string>(tenant =>
{
    tenant.ResolveFromHeader("X-Tenant-Id");
    tenant.UseStore<MyTenantStore>();

    tenant.UsePro(pro =>
    {
        pro.WithLicence(builder.Configuration["Tenantry:Licence"]!);

        pro.UseDatabasePerTenant(opts =>
            opts.GetConnectionString = t =>
                $"Server=.;Database=app_{t.TenantId};Integrated Security=true;TrustServerCertificate=True");

        pro.UseSchemaPerTenant(opts =>
            opts.GetSchemaName = t => $"tenant_{t.TenantId}");
        pro.AddSchemaPerTenantCaching();   // with AddSchemaPerTenantCaching(sp) on the DbContext below

        pro.UseMixedMode(opts =>
            opts.GetStrategyForTenant = t =>
                t.TenantId.StartsWith("enterprise_", StringComparison.OrdinalIgnoreCase)
                    ? TenantStrategy.Database     // big tenants get a dedicated database
                    : TenantStrategy.Schema);     // everyone else shares a database, schema per tenant
    });
});
```

`GetStrategyForTenant` receives the resolved `ITenantDescriptor<TKey>`, so you can branch on tenant
ID, name, a tier flag, or any custom property you carry.

## Composing runtime behaviour

Inject `MixedStrategyResolver<TKey>` where you build the context or branch behaviour:

```csharp
builder.Services.AddDbContext<AppDbContext>((sp, options) =>
{
    var resolver = sp.GetRequiredService<MixedStrategyResolver<string>>();

    switch (resolver.GetCurrentStrategy())
    {
        case TenantStrategy.Database:
            options.UseSqlServer(resolver.ResolveConnectionString()!);
            break;

        case TenantStrategy.Schema:
        case TenantStrategy.Shared:
            options.UseSqlServer(sharedConnectionString)
                   .AddSchemaPerTenantCaching<string>(sp);   // schema applied in OnModelCreating
            break;
    }
});
```

`MixedStrategyResolver<TKey>` members:

| Member | Returns |
|--------|---------|
| `GetCurrentStrategy()` | The active tenant's `TenantStrategy`. Throws `TenantNotResolvedException` if no tenant is in scope. |
| `ResolveConnectionString()` | The connection string for `Database` tenants; `null` for `Schema`/`Shared`. |
| `ResolveSchemaName()` | The schema name for `Schema` tenants; `null` for `Database`/`Shared`. |

## Limitations

- Runtime composition (switching the EF Core registration) is your responsibility.
- There is no built-in `DbContext` auto-switching layer.
- Document your branching rules clearly — mixed deployments are harder to operate, migrate, and
  reason about than a single strategy.

## See also

- [Database per tenant](database-per-tenant.md) · [Schema per tenant](schema-per-tenant.md)
