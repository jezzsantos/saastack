using Common;
using Domain.Events.Shared.Organizations;
using Domain.Interfaces.Entities;
using Infrastructure.Eventing.Interfaces.Notifications;
using Infrastructure.Interfaces;
using SubscriptionsApplication;
using OrganizationCreated = Domain.Events.Shared.Organizations.Created;
using UserProfileCreated = Domain.Events.Shared.UserProfiles.Created;

namespace SubscriptionsInfrastructure.Notifications;

public class NotificationConsumer : IDomainEventNotificationConsumer
{
    private readonly ICallerContextFactory _callerContextFactory;
    private readonly ISubscriptionsApplication _subscriptionsApplication;

    public NotificationConsumer(ICallerContextFactory callerContextFactory,
        ISubscriptionsApplication subscriptionsApplication)
    {
        _callerContextFactory = callerContextFactory;
        _subscriptionsApplication = subscriptionsApplication;
    }

    public async Task<Result<Error>> NotifyAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        switch (domainEvent)
        {
            case UserProfileCreated created:
                return await _subscriptionsApplication.HandleUserProfileCreatedAsync(_callerContextFactory.Create(),
                    created, cancellationToken);

            case OrganizationCreated created:
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