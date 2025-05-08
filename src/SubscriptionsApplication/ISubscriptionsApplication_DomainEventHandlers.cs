using Application.Interfaces;
using Common;
using Domain.Events.Shared.Organizations;
using UserProfileCreated = Domain.Events.Shared.UserProfiles.Created;
using OrganizationCreated = Domain.Events.Shared.Organizations.Created;

namespace SubscriptionsApplication;

partial interface ISubscriptionsApplication
{
    Task<Result<Error>> HandleOrganizationCreatedAsync(ICallerContext caller,
        OrganizationCreated domainEvent, CancellationToken cancellationToken);

    Task<Result<Error>> HandleOrganizationDeletedAsync(ICallerContext caller, Deleted domainEvent,
        CancellationToken cancellationToken);

    Task<Result<Error>> HandleUserProfileCreatedAsync(ICallerContext caller,
        UserProfileCreated domainEvent, CancellationToken cancellationToken);
}