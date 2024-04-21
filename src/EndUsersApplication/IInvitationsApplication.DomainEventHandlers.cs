using Application.Interfaces;
using Common;
using Domain.Events.Shared.Organizations;

namespace EndUsersApplication;

partial interface IInvitationsApplication
{
    Task<Result<Error>> HandleOrganizationMemberInvitedAsync(ICallerContext caller, MemberInvited domainEvent,
        CancellationToken cancellationToken);

    Task<Result<Error>> HandleOrganizationMemberUnInvitedAsync(ICallerContext caller, MemberUnInvited domainEvent,
        CancellationToken cancellationToken);
}