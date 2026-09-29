# `TenantOutgoingStep<TKey>` class

Namespace: `Tenantry.Pro.Rebus.Steps` · Package: `Tenantry.Pro.Rebus` · [API reference](README.md)

Rebus outgoing pipeline step that stamps the current tenant ID as a message header.

If no tenant is active, the header is omitted and the message is sent normally.

Register via `pro.AddRebusTenantSteps()` and wire into Rebus using     `options.UseTenantryPro()`.

```csharp
public sealed class TenantOutgoingStep<TKey> : IOutgoingStep, IStep where TKey : IEquatable<TKey>, IParsable<TKey>
```

## Type parameters

- `TKey`: The tenant identifier type.

Implements `IOutgoingStep`, `IStep`.

## Constructors

### `TenantOutgoingStep(ITenantContext<TKey>)`

Rebus outgoing pipeline step that stamps the current tenant ID as a message header.

```csharp
public TenantOutgoingStep(ITenantContext<TKey> tenantContext)
```

Parameters:

- `tenantContext` [`ITenantContext<TKey>`](https://tenantry.dev/docs/core/api/tenantry-core-itenantcontext): Supplies the current tenant, written to each outgoing message's headers.

If no tenant is active, the header is omitted and the message is sent normally.

Register via `pro.AddRebusTenantSteps()` and wire into Rebus using     `options.UseTenantryPro()`.

## Fields

### `HeaderKey`

The message header key used to carry the tenant ID.

```csharp
public const string HeaderKey = "tenantry-tenant-id"
```

Returns: `string`

## Methods

### `Process(OutgoingStepContext, Func<Task>)`

Writes the current tenant to the outgoing message's headers, then runs the rest of the pipeline.

```csharp
public Task Process(OutgoingStepContext context, Func<Task> next)
```

Parameters:

- `context` `OutgoingStepContext`: The outgoing message's step context.
- `next` `Func<Task>`: Runs the rest of the outgoing pipeline.

Returns: `Task`: A task that completes when the rest of the pipeline has run.
