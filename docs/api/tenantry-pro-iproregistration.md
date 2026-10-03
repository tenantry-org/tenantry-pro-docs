# `IProRegistration` interface

Namespace: `Tenantry.Pro` · Package: `Tenantry.Pro` · [API reference](README.md)

A registration that needs the tenant key type, added through [`IProBuilder.Add`](tenantry-pro-iprobuilder.md). Packages use it for builder methods that take a type parameter of their own, such as a `DbContext` type.

```csharp
public interface IProRegistration
```

## Methods

### `Apply<TKey>(IProBuilder<TKey>)`

Registers the feature's services for the tenant key type `TKey`.

```csharp
void Apply<TKey>(IProBuilder<TKey> pro) where TKey : IEquatable<TKey>, IParsable<TKey>
```

Type parameters:

- `TKey`: The tenant key type of the builder.

Parameters:

- `pro` [`IProBuilder<TKey>`](tenantry-pro-iprobuilder-1.md): The builder the registration was added to.
