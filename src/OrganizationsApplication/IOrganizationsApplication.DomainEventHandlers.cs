using Application.Interfaces;
using Common;
using Domain.Events.Shared.EndUsers;
using Domain.Events.Shared.Subscriptions;
using Created = Domain.Events.Shared.Subscriptions.Created;
using Deleted = Domain.Events.Shared.Images.Deleted;

namespace OrganizationsApplication;

partial interface IOrganizationsApplication
{
    Task<Result<Error>> HandleEndUserMembershipAddedAsync(ICallerContext caller, MembershipAdded domainEvent,
        CancellationToken cancellationToken);

    Task<Result<Error>> HandleEndUserMembershipRemovedAsync(ICallerContext caller, MembershipRemoved domainEvent,
        CancellationToken cancellationToken);

    Task<Result<Error>> HandleEndUserRegisteredAsync(ICallerContext caller, Registered domainEvent,
        CancellationToken cancellationToken);

    Task<Result<Error>> HandleImageDeletedAsync(ICallerContext caller, Deleted domainEvent,
        CancellationToken cancellationToken);

    Task<Result<Error>> HandleSubscriptionCreatedAsync(ICallerContext caller, Created domainEvent,
        CancellationToken cancellationToken);

    Task<Result<Error>> HandleSubscriptionTransferredAsync(ICallerContext caller, SubscriptionTransferred domainEvent,
        CancellationToken cancellationToken);
}