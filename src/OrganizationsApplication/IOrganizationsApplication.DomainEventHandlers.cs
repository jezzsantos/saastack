using Application.Interfaces;
using Common;
using Domain.Events.Shared.EndUsers;

namespace OrganizationsApplication;

partial interface IOrganizationsApplication
{
    Task<Result<Error>> HandleEndUserRegisteredAsync(ICallerContext caller, Registered domainEvent,
        CancellationToken cancellationToken);
}