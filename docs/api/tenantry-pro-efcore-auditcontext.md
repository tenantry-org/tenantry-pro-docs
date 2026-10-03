# `AuditContext` class

Namespace: `Tenantry.Pro.EfCore` · Package: `Tenantry.Pro.EfCore` · [API reference](README.md)

Who made a save's changes, and what ties them to the request or job that made them, as an [`IAuditContextProvider`](tenantry-pro-efcore-iauditcontextprovider.md) gives it.

```csharp
public sealed record AuditContext : IEquatable<AuditContext>
```

Implements `IEquatable<AuditContext>`.

## Properties

### `Actor`

The user or service that made the changes, or [null](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null) when not known.

```csharp
public string? Actor { get; init; }
```

Value: `string`

### `CorrelationId`

An id that ties the changes to the request, message or job that made them (a trace id, say), or [null](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null).

```csharp
public string? CorrelationId { get; init; }
```

Value: `string`

### `Data`

Values of your own to record with each entry: a client address, a reason. Empty by default.

```csharp
public IReadOnlyDictionary<string, object?> Data { get; init; }
```

Value: `IReadOnlyDictionary<string, object>`
