using Common;
using Domain.Events.Shared.Organizations;
using Domain.Interfaces.Entities;
using Infrastructure.Eventing.Interfaces.Notifications;
using Infrastructure.Interfaces;
using SubscriptionsApplication;

namespace SubscriptionsInfrastructure.Notifications;

public class OrganizationNotificationConsumer : IDomainEventNotificationConsumer
{
    private readonly ICallerContextFactory _callerContextFactory;
    private readonly ISubscriptionsApplication _subscriptionsApplication;

    public OrganizationNotificationConsumer(ICallerContextFactory callerContextFactory,
        ISubscriptionsApplication subscriptionsApplication)
    {
        _callerContextFactory = callerContextFactory;
        _subscriptionsApplication = subscriptionsApplication;
    }

    public async Task<Result<Error>> NotifyAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        switch (domainEvent)
        {
            case Created created:
                return await _subscriptionsApplication.HandleOrganizationCreatedAsync(_callerContextFactory.Create(),
                    created, cancellationToken);

            case Deleted removed:
                return await _subscriptionsApplication.HandleOrganizationDeletedAsync(
                    _callerContextFactory.Create(), removed, cancellationToken);

            default:
                return Result.Ok;
        }
    }
}