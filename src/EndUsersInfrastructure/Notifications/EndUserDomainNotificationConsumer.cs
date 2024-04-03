using Common;
using Domain.Events.Shared.Organizations;
using Domain.Interfaces.Entities;
using EndUsersApplication;
using Infrastructure.Eventing.Interfaces.Notifications;
using Infrastructure.Interfaces;

namespace EndUsersInfrastructure.Notifications;

public class EndUserDomainNotificationConsumer : IDomainEventNotificationConsumer
{
    private readonly ICallerContextFactory _callerContextFactory;
    private readonly IEndUsersApplication _endUsersApplication;

    public EndUserDomainNotificationConsumer(ICallerContextFactory callerContextFactory,
        IEndUsersApplication endUsersApplication)
    {
        _callerContextFactory = callerContextFactory;
        _endUsersApplication = endUsersApplication;
    }

    public async Task<Result<Error>> NotifyAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        switch (domainEvent)
        {
            case Created created:
                return await _endUsersApplication.HandleOrganizationCreatedAsync(_callerContextFactory.Create(),
                    created, cancellationToken);

            default:
                return Result.Ok;
        }
    }
}