using Application.Interfaces;
using Common;
using Domain.Events.Shared.Organizations;

namespace SubscriptionsApplication;

partial interface ISubscriptionsApplication
{
    Task<Result<Error>> HandleOrganizationCreatedAsync(ICallerContext caller, Created domainEvent,
        CancellationToken cancellationToken);

    Task<Result<Error>> HandleOrganizationDeletedAsync(ICallerContext caller, Deleted domainEvent,
        CancellationToken cancellationToken);
}