using Application.Interfaces;
using Common;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.Organizations;

namespace EndUsersApplication;

partial class InvitationsApplication
{
    public async Task<Result<Error>> HandleOrganizationMembershipAddedAsync(ICallerContext caller,
        MembershipAdded domainEvent,
        CancellationToken cancellationToken)
    {
        var membership = await InviteMemberToOrganizationAsync(caller, domainEvent.RootId.ToId(),
            domainEvent.InvitedById, domainEvent.UserId,
            domainEvent.EmailAddress, cancellationToken);
        if (!membership.IsSuccessful)
        {
            return membership.Error;
        }

        return Result.Ok;
    }
}