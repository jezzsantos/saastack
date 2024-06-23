using Common;
using Domain.Events.Shared.Subscriptions;
using Domain.Interfaces.Entities;
using Infrastructure.Eventing.Interfaces.Notifications;
using Infrastructure.Interfaces;
using OrganizationsApplication;

namespace OrganizationsInfrastructure.Notifications;

public class SubscriptionNotificationConsumer : IDomainEventNotificationConsumer
{
    private readonly ICallerContextFactory _callerContextFactory;
    private readonly IOrganizationsApplication _organizationsApplication;

    public SubscriptionNotificationConsumer(ICallerContextFactory callerContextFactory,
        IOrganizationsApplication organizationsApplication)
    {
        _callerContextFactory = callerContextFactory;
        _organizationsApplication = organizationsApplication;
    }

    public async Task<Result<Error>> NotifyAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        switch (domainEvent)
        {
            case Created created:
                return await _organizationsApplication.HandleSubscriptionCreatedAsync(
                    _callerContextFactory.Create(), created, cancellationToken);

            case SubscriptionTransferred transferred:
                return await _organizationsApplication.HandleSubscriptionTransferredAsync(
                    _callerContextFactory.Create(), transferred, cancellationToken);

            default:
                return Result.Ok;
        }
    }
}