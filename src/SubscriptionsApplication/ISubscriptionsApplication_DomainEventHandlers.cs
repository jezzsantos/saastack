using Application.Interfaces;
using Common;
using Domain.Events.Shared.Organizations;
using Created = Domain.Events.Shared.UserProfiles.Created;

namespace SubscriptionsApplication;

partial interface ISubscriptionsApplication
{
    Task<Result<Error>> HandleOrganizationCreatedAsync(ICallerContext caller,
        Domain.Events.Shared.Organizations.Created domainEvent,
        CancellationToken cancellationToken);

    Task<Result<Error>> HandleOrganizationDeletedAsync(ICallerContext caller, Deleted domainEvent,
        CancellationToken cancellationToken);

    Task<Result<Error>> HandleUserProfileCreatedAsync(ICallerContext caller,
        Created domainEvent,
        CancellationToken cancellationToken);
}