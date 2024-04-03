using Application.Interfaces;
using Application.Resources.Shared;
using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.Organizations;

namespace EndUsersApplication;

partial class EndUsersApplication
{
    public async Task<Result<Error>> HandleOrganizationCreatedAsync(ICallerContext caller, Created domainEvent,
        CancellationToken cancellationToken)
    {
        var ownership = domainEvent.Ownership.ToEnumOrDefault(OrganizationOwnership.Shared);
        var membership = await CreateMembershipAsync(caller, domainEvent.CreatedById.ToId(), domainEvent.RootId.ToId(),
            ownership, cancellationToken);
        if (!membership.IsSuccessful)
        {
            return membership.Error;
        }

        return Result.Ok;
    }
}