# `TenantContextPropagation` class

Namespace: `Tenantry.Pro.BackgroundServices` · Package: `Tenantry.Pro` · [API reference](README.md)

Shared tenant-resolution logic for the Tenantry.Pro background-job and messaging integrations. Parses a tenant identifier carried by a job/message, looks it up in the store, and applies the configured [`MissingTenantBehavior`](https://tenantry.dev/docs/core/api/tenantry-core-missingtenantbehavior) when no tenant can be resolved.

This consolidates the resolve → parse → lookup → policy logic that every integration (Hangfire, MassTransit, Quartz, Rebus) would otherwise duplicate, so the missing-tenant behaviour stays consistent across all of them.

```csharp
public static class TenantContextPropagation
```

## Methods

### `ResolveAsync<TKey>(string?, MissingTenantBehavior, ITenantStoreAccessor<TKey>, ILogger, string, CancellationToken)`

Resolves the tenant for an incoming job/message asynchronously.

```csharp
public static ValueTask<TenantPropagationDecision<TKey>> ResolveAsync<TKey>(string? rawTenantId, MissingTenantBehavior onMissingTenant, ITenantStoreAccessor<TKey> storeAccessor, ILogger logger, string carrier, CancellationToken cancellationToken = default) where TKey : IEquatable<TKey>, IParsable<TKey>
```

Type parameters:

- `TKey`: The tenant identifier type.

Parameters:

- `rawTenantId` `string`: The tenant identifier carried by the job/message, or [null](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null) if none.
- `onMissingTenant` [`MissingTenantBehavior`](https://tenantry.dev/docs/core/api/tenantry-core-missingtenantbehavior): The policy to apply when no tenant can be resolved.
- `storeAccessor` [`ITenantStoreAccessor<TKey>`](https://tenantry.dev/docs/core/api/tenantry-core-itenantstoreaccessor): Resolves tenants from the store.
- `logger` `ILogger`: Logger for warning/skip diagnostics.
- `carrier` `string`: A short description of the job/message for log messages (e.g. `"Hangfire job 42"`).
- `cancellationToken` `CancellationToken`: A cancellation token.

Returns: `ValueTask<TenantPropagationDecision<TKey>>`

Exceptions:

- [`TenantNotResolvedException`](https://tenantry.dev/docs/core/api/tenantry-core-exceptions-tenantnotresolvedexception): Thrown when no tenant can be resolved and `onMissingTenant` is `Reject`.

### `Resolve<TKey>(string?, MissingTenantBehavior, ITenantStoreAccessor<TKey>, ILogger, string)`

Resolves the tenant for an incoming job/message synchronously. Intended for callers (such as Hangfire server filters) whose pipeline is synchronous; the store lookup blocks the calling thread, which is acceptable on a background worker thread.

```csharp
public static TenantPropagationDecision<TKey> Resolve<TKey>(string? rawTenantId, MissingTenantBehavior onMissingTenant, ITenantStoreAccessor<TKey> storeAccessor, ILogger logger, string carrier) where TKey : IEquatable<TKey>, IParsable<TKey>
```

Type parameters:

- `TKey`: The tenant identifier type.

Parameters:

- `rawTenantId` `string`: The tenant identifier carried by the job/message, or [null](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null) if none.
- `onMissingTenant` [`MissingTenantBehavior`](https://tenantry.dev/docs/core/api/tenantry-core-missingtenantbehavior): The policy to apply when no tenant can be resolved.
- `storeAccessor` [`ITenantStoreAccessor<TKey>`](https://tenantry.dev/docs/core/api/tenantry-core-itenantstoreaccessor): Resolves tenants from the store.
- `logger` `ILogger`: Logger for warning/skip diagnostics.
- `carrier` `string`: A short description of the job/message for log messages (e.g. `"Hangfire job 42"`).

Returns: [`TenantPropagationDecision<TKey>`](tenantry-pro-backgroundservices-tenantpropagationdecision.md)

Exceptions:

- [`TenantNotResolvedException`](https://tenantry.dev/docs/core/api/tenantry-core-exceptions-tenantnotresolvedexception): Thrown when no tenant can be resolved and `onMissingTenant` is `Reject`.
