# `IProBuilder<TKey>` interface

Namespace: `Tenantry.Pro` · Package: `Tenantry.Pro` · [API reference](README.md)

The builder `UsePro` passes to its configuration callback. Tenantry.Pro's features are extension methods on it that return it, so calls chain.

An extension method that registers through [`IProBuilder.Add`](tenantry-pro-iprobuilder.md) returns the builder without its key type, as [`IProBuilder`](tenantry-pro-iprobuilder.md): in a chain, call it after the others.

```csharp
public interface IProBuilder<TKey> : IProBuilder where TKey : IEquatable<TKey>, IParsable<TKey>
```

## Type parameters

- `TKey`: The tenant identifier type, as passed to `AddTenantry`.

## Methods

### `AddDeprovisioningStep<TStep>()`

Adds `TStep` to offboarding ([`ITenantDeprovisioner<TKey>`](tenantry-pro-itenantdeprovisioner.md)), after the steps already added and before anything Tenantry drops or deletes.

```csharp
IProBuilder<TKey> AddDeprovisioningStep<TStep>() where TStep : class, ITenantDeprovisioningStep<TKey>
```

Type parameters:

- `TStep`: The step: export, archive or notify.

Returns: [`IProBuilder<TKey>`](tenantry-pro-iprobuilder-1.md): The same builder for chaining.

Registers the step as a scoped service, unless it is already registered. Adding the same step again has no effect.

### `AddProvisioningStep<TStep>()`

Adds `TStep` to tenant provisioning ([`ITenantProvisioner<TKey>`](tenantry-pro-itenantprovisioner.md)), after the steps and seeders already added.

```csharp
IProBuilder<TKey> AddProvisioningStep<TStep>() where TStep : class, ITenantProvisioningStep<TKey>
```

Type parameters:

- `TStep`: The step.

Returns: [`IProBuilder<TKey>`](tenantry-pro-iprobuilder-1.md): The same builder for chaining.

Registers the step as a scoped service, unless it is already registered. Adding the same step again has no effect.

### `AddSeeder<TSeeder>()`

Adds `TSeeder` to tenant provisioning ([`ITenantProvisioner<TKey>`](tenantry-pro-itenantprovisioner.md)), after the steps and seeders already added.

```csharp
IProBuilder<TKey> AddSeeder<TSeeder>() where TSeeder : class, ITenantSeeder<TKey>
```

Type parameters:

- `TSeeder`: The seeder.

Returns: [`IProBuilder<TKey>`](tenantry-pro-iprobuilder-1.md): The same builder for chaining.

Registers the seeder as a scoped service, unless it is already registered. Adding the same seeder again has no effect.
