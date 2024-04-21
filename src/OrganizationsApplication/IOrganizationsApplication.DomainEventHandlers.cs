using Application.Interfaces;
using Common;
using Domain.Events.Shared.EndUsers;

namespace OrganizationsApplication;

partial interface IOrganizationsApplication
{
    Task<Result<Error>> HandleEndUserMembershipAddedAsync(ICallerContext caller, MembershipAdded domainEvent,
        CancellationToken cancellationToken);

    Task<Result<Error>> HandleEndUserMembershipRemovedAsync(ICallerContext caller, MembershipRemoved domainEvent,
        CancellationToken cancellationToken);

    Task<Result<Error>> HandleEndUserRegisteredAsync(ICallerContext caller, Registered domainEvent,
        CancellationToken cancellationToken);
}