using Application.Interfaces;
using Common;
using Domain.Events.Shared.Organizations;
using Domain.Events.Shared.Subscriptions;
using Created = Domain.Events.Shared.Organizations.Created;
using Deleted = Domain.Events.Shared.Organizations.Deleted;

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

    Task<Result<Error>> HandleSubscriptionPlanChangedAsync(ICallerContext caller, SubscriptionPlanChanged domainEvent,
        CancellationToken cancellationToken);

    Task<Result<Error>> HandleSubscriptionTransferredAsync(ICallerContext caller, SubscriptionTransferred domainEvent,
        CancellationToken cancellationToken);
}