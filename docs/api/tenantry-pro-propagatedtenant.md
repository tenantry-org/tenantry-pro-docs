# `PropagatedTenant` struct

Namespace: `Tenantry.Pro` · Package: `Tenantry.Pro` · [API reference](README.md)

An extension point: for code that extends the package, such as another package that builds on it. An application rarely needs it.

What [`ITenantPropagator`](tenantry-pro-itenantpropagator.md) resolved for a job or message: a tenant to run as ([`PropagatedTenant.Resolved`](tenantry-pro-propagatedtenant.md)), no tenant ([`PropagatedTenant.WithoutTenant`](tenantry-pro-propagatedtenant.md)), or that the work must not run ([`PropagatedTenant.Skipped`](tenantry-pro-propagatedtenant.md)).

```csharp
[EditorBrowsable(EditorBrowsableState.Advanced)]
public readonly record struct PropagatedTenant : IEquatable<PropagatedTenant>
```

Implements `IEquatable<PropagatedTenant>`.

## Properties

### `Skip`

The work must not run: the policy that applied is [`TenantPropagationBehavior.Skip`](tenantry-pro-tenantpropagationbehavior.md).

```csharp
public bool Skip { get; }
```

Value: `bool`

### `Skipped`

Do not run: the policy that applied is [`TenantPropagationBehavior.Skip`](tenantry-pro-tenantpropagationbehavior.md).

```csharp
public static PropagatedTenant Skipped { get; }
```

Value: [`PropagatedTenant`](tenantry-pro-propagatedtenant.md)

### `Tenant`

The tenant to run as, an `ITenantDescriptor<TKey>`, or [null](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null) when the work runs without one or is skipped.

```csharp
public ITenantDescriptor? Tenant { get; }
```

Value: `ITenantDescriptor`

### `WithoutTenant`

Run without a tenant.

```csharp
public static PropagatedTenant WithoutTenant { get; }
```

Value: [`PropagatedTenant`](tenantry-pro-propagatedtenant.md)

## Methods

### `Resolved(ITenantDescriptor)`

Run as `tenant`.

```csharp
public static PropagatedTenant Resolved(ITenantDescriptor tenant)
```

Parameters:

- `tenant` `ITenantDescriptor`: The tenant, an `ITenantDescriptor<TKey>` of the application's tenant key type.

Returns: [`PropagatedTenant`](tenantry-pro-propagatedtenant.md): The result to pass to [`ITenantPropagator.Use`](tenantry-pro-itenantpropagator.md).
