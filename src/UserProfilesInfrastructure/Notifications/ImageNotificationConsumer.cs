using Common;
using Domain.Events.Shared.Images;
using Domain.Interfaces.Entities;
using Infrastructure.Eventing.Interfaces.Notifications;
using Infrastructure.Interfaces;
using UserProfilesApplication;

namespace UserProfilesInfrastructure.Notifications;

public class ImageNotificationConsumer : IDomainEventNotificationConsumer
{
    private readonly ICallerContextFactory _callerContextFactory;
    private readonly IUserProfilesApplication _userProfilesApplication;

    public ImageNotificationConsumer(ICallerContextFactory callerContextFactory,
        IUserProfilesApplication userProfilesApplication)
    {
        _callerContextFactory = callerContextFactory;
        _userProfilesApplication = userProfilesApplication;
    }

    public async Task<Result<Error>> NotifyAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        switch (domainEvent)
        {
            case Deleted deleted:
                return await _userProfilesApplication.HandleImageDeletedAsync(_callerContextFactory.Create(),
                    deleted, cancellationToken);

            default:
                return Result.Ok;
        }
    }
}