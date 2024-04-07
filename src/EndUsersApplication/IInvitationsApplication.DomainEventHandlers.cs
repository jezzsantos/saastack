using Application.Interfaces;
using Common;
using Domain.Events.Shared.Organizations;

namespace EndUsersApplication;

partial interface IInvitationsApplication
{
    Task<Result<Error>> HandleOrganizationMembershipAddedAsync(ICallerContext caller, MembershipAdded domainEvent,
        CancellationToken cancellationToken);
}