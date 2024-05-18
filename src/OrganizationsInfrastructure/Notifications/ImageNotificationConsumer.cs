using Common;
using Domain.Events.Shared.Images;
using Domain.Interfaces.Entities;
using Infrastructure.Eventing.Interfaces.Notifications;
using Infrastructure.Interfaces;
using OrganizationsApplication;

namespace OrganizationsInfrastructure.Notifications;

public class ImageNotificationConsumer : IDomainEventNotificationConsumer
{
    private readonly ICallerContextFactory _callerContextFactory;
    private readonly IOrganizationsApplication _organizationsApplication;

    public ImageNotificationConsumer(ICallerContextFactory callerContextFactory,
        IOrganizationsApplication organizationsApplication)
    {
        _callerContextFactory = callerContextFactory;
        _organizationsApplication = organizationsApplication;
    }

    public async Task<Result<Error>> NotifyAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        switch (domainEvent)
        {
            case Deleted deleted:
                return await _organizationsApplication.HandleImageDeletedAsync(_callerContextFactory.Create(),
                    deleted, cancellationToken);

            default:
                return Result.Ok;
        }
    }
}