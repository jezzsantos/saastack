using Application.Interfaces;
using Common;
using Domain.Events.Shared.EndUsers;

namespace UserProfilesApplication;

partial interface IUserProfilesApplication
{
    Task<Result<Error>> HandleEndUserRegisteredAsync(ICallerContext caller, Registered domainEvent,
        CancellationToken cancellationToken);

    Task<Result<Error>> HandleEndUserDefaultMembershipChangedAsync(ICallerContext caller,
        DefaultMembershipChanged domainEvent,
        CancellationToken cancellationToken);
}