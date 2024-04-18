using Common;
using Domain.Events.Shared.EndUsers;
using Domain.Interfaces.Entities;
using Infrastructure.Eventing.Interfaces.Notifications;
using Infrastructure.Interfaces;
using UserProfilesApplication;

namespace UserProfilesInfrastructure.Notifications;

public class UserProfileNotificationConsumer : IDomainEventNotificationConsumer
{
    private readonly ICallerContextFactory _callerContextFactory;
    private readonly IUserProfilesApplication _userProfilesApplication;

    public UserProfileNotificationConsumer(ICallerContextFactory callerContextFactory,
        IUserProfilesApplication userProfilesApplication)
    {
        _callerContextFactory = callerContextFactory;
        _userProfilesApplication = userProfilesApplication;
    }

    public async Task<Result<Error>> NotifyAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        switch (domainEvent)
        {
            case Registered registered:
                return await _userProfilesApplication.HandleEndUserRegisteredAsync(_callerContextFactory.Create(),
                    registered, cancellationToken);

            case MembershipDefaultChanged changed:
                return await _userProfilesApplication.HandleEndUserDefaultOrganizationChangedAsync(
                    _callerContextFactory.Create(),
                    changed, cancellationToken);

            default:
                return Result.Ok;
        }
    }
}