using Application.Interfaces;
using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.EndUsers;
using Domain.Shared.Organizations;

namespace OrganizationsApplication;

partial class OrganizationsApplication
{
    public async Task<Result<Error>> HandleEndUserRegisteredAsync(ICallerContext caller, Registered domainEvent,
        CancellationToken cancellationToken)
    {
        var name =
            $"{domainEvent.UserProfile.FirstName}{(domainEvent.UserProfile.LastName.HasValue() ? " " + domainEvent.UserProfile.LastName : string.Empty)}";
        var organization = await CreateOrganizationInternalAsync(caller, domainEvent.RootId.ToId(),
            domainEvent.Classification, name,
            OrganizationOwnership.Personal, cancellationToken);
        if (!organization.IsSuccessful)
        {
            return organization.Error;
        }

        return Result.Ok;
    }
}