using Rebus.Handlers;

namespace Tenantry.Pro.Samples.RebusMessaging;

public sealed record OrderPlaced(Guid OrderId);

public sealed record PaymentReminder;

/// <summary>
/// Rebus creates the handler, with its scoped dependencies, once Tenantry has made the message's tenant current.
/// </summary>
public sealed class OrderPlacedHandler(ITenantContext<string> tenantContext, ILogger<OrderPlacedHandler> logger)
    : IHandleMessages<OrderPlaced>
{
    public Task Handle(OrderPlaced message)
    {
        logger.LogInformation("Handling order {OrderId} for tenant '{TenantId}'", message.OrderId, tenantContext.CurrentTenantId);
        return Task.CompletedTask;
    }
}

/// <summary>Handles a message sent for a tenant by name, with no tenant current when it was sent.</summary>
public sealed class PaymentReminderHandler(ITenantContext<string> tenantContext, ILogger<PaymentReminderHandler> logger)
    : IHandleMessages<PaymentReminder>
{
    public Task Handle(PaymentReminder message)
    {
        logger.LogInformation("Sending a payment reminder for tenant '{TenantId}'", tenantContext.CurrentTenantId);
        return Task.CompletedTask;
    }
}
