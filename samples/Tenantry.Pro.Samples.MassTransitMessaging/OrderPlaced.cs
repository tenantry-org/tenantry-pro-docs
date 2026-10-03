using MassTransit;
using Tenantry;

namespace Tenantry.Pro.Samples.MassTransitMessaging;

/// <summary>A message published when an order is placed.</summary>
public sealed record OrderPlaced(string OrderId);

/// <summary>
/// Consumes <see cref="OrderPlaced"/>. Tenantry's consume filter restores the tenant scope from the
/// message header before this runs, so <see cref="ITenantContext{TKey}"/> reflects the tenant that
/// published the message — no manual plumbing required.
/// </summary>
public sealed class OrderPlacedConsumer(
    ITenantContext<string> tenantContext,
    ILogger<OrderPlacedConsumer> logger) : IConsumer<OrderPlaced>
{
    public Task Consume(ConsumeContext<OrderPlaced> context)
    {
        logger.LogInformation(
            "Handling order {OrderId} for tenant '{TenantId}'",
            context.Message.OrderId,
            tenantContext.CurrentTenantId ?? "(none)");

        return Task.CompletedTask;
    }
}
