# `TenantPropagationDecision<TKey>` struct

Namespace: `Tenantry.Pro.BackgroundServices` · Package: `Tenantry.Pro` · [API reference](README.md)

The outcome of resolving the tenant for a single job or message.

```csharp
public readonly record struct TenantPropagationDecision<TKey> : IEquatable<TenantPropagationDecision<TKey>> where TKey : IEquatable<TKey>, IParsable<TKey>
```

## Type parameters

- `TKey`: The tenant identifier type.

Implements `IEquatable<TenantPropagationDecision<TKey>>`.

## Constructors

### `TenantPropagationDecision(TenantPropagationOutcome, ITenantDescriptor<TKey>?)`

The outcome of resolving the tenant for a single job or message.

```csharp
public TenantPropagationDecision(TenantPropagationOutcome Outcome, ITenantDescriptor<TKey>? Tenant)
```

Parameters:

- `Outcome` [`TenantPropagationOutcome`](tenantry-pro-backgroundservices-tenantpropagationoutcome.md): What the integration should do.
- `Tenant` [`ITenantDescriptor<TKey>`](https://tenantry.dev/docs/core/api/tenantry-core-itenantdescriptor): The tenant to scope into when `Outcome` is [`TenantPropagationOutcome.RunInScope`](tenantry-pro-backgroundservices-tenantpropagationoutcome.md); otherwise [null](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null).

## Properties

### `Outcome`

What the integration should do.

```csharp
public TenantPropagationOutcome Outcome { get; init; }
```

Value: [`TenantPropagationOutcome`](tenantry-pro-backgroundservices-tenantpropagationoutcome.md)

### `Tenant`

The tenant to scope into when `Outcome` is [`TenantPropagationOutcome.RunInScope`](tenantry-pro-backgroundservices-tenantpropagationoutcome.md); otherwise [null](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null).

```csharp
public ITenantDescriptor<TKey>? Tenant { get; init; }
```

Value: [`ITenantDescriptor<TKey>`](https://tenantry.dev/docs/core/api/tenantry-core-itenantdescriptor)
