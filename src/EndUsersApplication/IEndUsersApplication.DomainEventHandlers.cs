using Application.Interfaces;
using Common;
using Domain.Events.Shared.Organizations;

namespace EndUsersApplication;

partial interface IEndUsersApplication
{
    Task<Result<Error>> HandleOrganizationCreatedAsync(ICallerContext caller, Created domainEvent,
        CancellationToken cancellationToken);

    Task<Result<Error>> HandleOrganizationDeletedAsync(ICallerContext caller, Deleted domainEvent,
        CancellationToken cancellationToken);

    Task<Result<Error>> HandleOrganizationRoleAssignedAsync(ICallerContext caller, RoleAssigned domainEvent,
        CancellationToken cancellationToken);

    Task<Result<Error>> HandleOrganizationRoleUnassignedAsync(ICallerContext caller, RoleUnassigned domainEvent,
        CancellationToken cancellationToken);
}