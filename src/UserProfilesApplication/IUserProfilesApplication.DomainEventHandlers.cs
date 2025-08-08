using Application.Interfaces;
using Common;
using Domain.Events.Shared.EndUsers;
using Deleted = Domain.Events.Shared.Images.Deleted;

namespace UserProfilesApplication;

partial interface IUserProfilesApplication
{
    Task<Result<Error>> HandleEndUserDefaultMembershipChangedAsync(ICallerContext caller,
        DefaultMembershipChanged domainEvent, CancellationToken cancellationToken);

    Task<Result<Error>> HandleEndUserRegisteredAsync(ICallerContext caller, Registered domainEvent,
        CancellationToken cancellationToken);

    Task<Result<Error>> HandleImageDeletedAsync(ICallerContext caller, Deleted domainEvent,
        CancellationToken cancellationToken);
}