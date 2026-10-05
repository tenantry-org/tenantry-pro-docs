# `IProBuilder` interface

Namespace: `Tenantry.Pro` · Package: `Tenantry.Pro` · [API reference](README.md)

The builder `UsePro` passes to its configuration callback, without the tenant key type.

A feature that takes a type parameter of its own, such as a `DbContext` type, registers through [`IProBuilder.Add`](tenantry-pro-iprobuilder.md), so its callers never repeat the key type.

```csharp
public interface IProBuilder
```

## Properties

### `Services`

Gets the application's service collection.

```csharp
IServiceCollection Services { get; }
```

Value: `IServiceCollection`

## Methods

### `Add(IProRegistration)`

Applies `registration` with the tenant key type this builder was created for.

```csharp
void Add(IProRegistration registration)
```

Parameters:

- `registration` [`IProRegistration`](tenantry-pro-iproregistration.md): The registration to apply.
