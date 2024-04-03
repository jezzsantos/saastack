using Application.Interfaces;
using Application.Resources.Shared;
using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.EndUsers;

namespace OrganizationsApplication;

partial class OrganizationsApplication
{
    public async Task<Result<Error>> HandleEndUserRegisteredAsync(ICallerContext caller, Registered domainEvent,
        CancellationToken cancellationToken)
    {
        var name =
            $"{domainEvent.UserProfile.FirstName}{(domainEvent.UserProfile.LastName.HasValue() ? " " + domainEvent.UserProfile.LastName : string.Empty)}";
        var organization = await CreateOrganizationAsync(caller, domainEvent.RootId.ToId(), name,
            OrganizationOwnership.Personal, cancellationToken);
        if (!organization.IsSuccessful)
        {
            return organization.Error;
        }

        return Result.Ok;
    }
}