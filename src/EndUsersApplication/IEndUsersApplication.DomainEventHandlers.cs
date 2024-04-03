using Application.Interfaces;
using Common;
using Domain.Events.Shared.Organizations;

namespace EndUsersApplication;

partial interface IEndUsersApplication
{
    Task<Result<Error>> HandleOrganizationCreatedAsync(ICallerContext caller, Created domainEvent,
        CancellationToken cancellationToken);
}