# `TenantIncomingStep<TKey>` class

Namespace: `Tenantry.Pro.Rebus.Steps` · Package: `Tenantry.Pro.Rebus` · [API reference](README.md)

Rebus incoming pipeline step that restores the tenant scope from a message header.

The step reads the `tenantry-tenant-id` header set by     [`TenantOutgoingStep<TKey>`](tenantry-pro-rebus-steps-tenantoutgoingstep.md), looks the tenant up in     [`ITenantStore<TKey>`](https://tenantry.dev/docs/core/api/tenantry-core-itenantstore), and activates a tenant scope for the     duration of the handler's execution.

If no header is present, or the tenant cannot be found, the message is processed     without a tenant scope.

```csharp
public sealed class TenantIncomingStep<TKey> : IIncomingStep, IStep where TKey : IEquatable<TKey>, IParsable<TKey>
```

## Type parameters

- `TKey`: The tenant identifier type.

Implements `IIncomingStep`, `IStep`.

## Constructors

### `TenantIncomingStep(ITenantScope<TKey>, ITenantStoreAccessor<TKey>, MissingTenantBehavior, ILogger<TenantIncomingStep<TKey>>)`

Rebus incoming pipeline step that restores the tenant scope from a message header.

```csharp
public TenantIncomingStep(ITenantScope<TKey> tenantScope, ITenantStoreAccessor<TKey> storeAccessor, MissingTenantBehavior onMissingTenant, ILogger<TenantIncomingStep<TKey>> logger)
```

Parameters:

- `tenantScope` [`ITenantScope<TKey>`](https://tenantry.dev/docs/core/api/tenantry-core-itenantscope): Runs each handler under the message's tenant.
- `storeAccessor` [`ITenantStoreAccessor<TKey>`](https://tenantry.dev/docs/core/api/tenantry-core-itenantstoreaccessor): Looks up the message's tenant before it is handled.
- `onMissingTenant` [`MissingTenantBehavior`](https://tenantry.dev/docs/core/api/tenantry-core-missingtenantbehavior): What to do with a message that carries no tenant, or one the store does not know.
- `logger` `ILogger<TenantIncomingStep<TKey>>`: Logs messages handled without a tenant.

The step reads the `tenantry-tenant-id` header set by     [`TenantOutgoingStep<TKey>`](tenantry-pro-rebus-steps-tenantoutgoingstep.md), looks the tenant up in     [`ITenantStore<TKey>`](https://tenantry.dev/docs/core/api/tenantry-core-itenantstore), and activates a tenant scope for the     duration of the handler's execution.

If no header is present, or the tenant cannot be found, the message is processed     without a tenant scope.

## Methods

### `Process(IncomingStepContext, Func<Task>)`

Reads the tenant from the message's headers and runs the rest of the pipeline in that tenant's scope, or applies the missing-tenant behaviour when the message has no known tenant.

```csharp
public Task Process(IncomingStepContext context, Func<Task> next)
```

Parameters:

- `context` `IncomingStepContext`: The incoming message's step context.
- `next` `Func<Task>`: Runs the rest of the incoming pipeline.

Returns: `Task`: A task that completes when the rest of the pipeline has run.
