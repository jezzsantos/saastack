using Common;
using Domain.Events.Shared.Subscriptions;
using Domain.Interfaces.Entities;
using EndUsersApplication;
using Infrastructure.Eventing.Interfaces.Notifications;
using Infrastructure.Interfaces;

namespace EndUsersInfrastructure.Notifications;

public class SubscriptionNotificationConsumer : IDomainEventNotificationConsumer
{
    private readonly ICallerContextFactory _callerContextFactory;
    private readonly IEndUsersApplication _endUsersApplication;

    public SubscriptionNotificationConsumer(ICallerContextFactory callerContextFactory,
        IEndUsersApplication endUsersApplication)
    {
        _callerContextFactory = callerContextFactory;
        _endUsersApplication = endUsersApplication;
    }

    public async Task<Result<Error>> NotifyAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        switch (domainEvent)
        {
            case SubscriptionPlanChanged changed:
                return await _endUsersApplication.HandleSubscriptionPlanChangedAsync(
                    _callerContextFactory.Create(), changed, cancellationToken);

            case SubscriptionTransferred transferred:
                return await _endUsersApplication.HandleSubscriptionTransferredAsync(
                    _callerContextFactory.Create(), transferred, cancellationToken);

            default:
                return Result.Ok;
        }
    }
}