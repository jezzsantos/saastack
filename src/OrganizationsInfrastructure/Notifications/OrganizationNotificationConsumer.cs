using Common;
using Domain.Events.Shared.EndUsers;
using Domain.Interfaces.Entities;
using Infrastructure.Eventing.Interfaces.Notifications;
using Infrastructure.Interfaces;
using OrganizationsApplication;

namespace OrganizationsInfrastructure.Notifications;

public class OrganizationNotificationConsumer : IDomainEventNotificationConsumer
{
    private readonly ICallerContextFactory _callerContextFactory;
    private readonly IOrganizationsApplication _organizationsApplication;

    public OrganizationNotificationConsumer(ICallerContextFactory callerContextFactory,
        IOrganizationsApplication organizationsApplication)
    {
        _callerContextFactory = callerContextFactory;
        _organizationsApplication = organizationsApplication;
    }

    public async Task<Result<Error>> NotifyAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        switch (domainEvent)
        {
            case Registered registered:
                return await _organizationsApplication.HandleEndUserRegisteredAsync(_callerContextFactory.Create(),
                    registered, cancellationToken);

            default:
                return Result.Ok;
        }
    }
}