using Application.Interfaces;
using Common;

namespace OrganizationsApplication;

partial interface IOrganizationsApplication
{
    Task<Result<Permission, Error>> CanCancelSubscriptionAsync(ICallerContext caller, string id, string cancellerId,
        CancellationToken cancellationToken);

    Task<Result<Permission, Error>> CanChangeSubscriptionPlanAsync(ICallerContext caller, string id, string modifierId,
        CancellationToken cancellationToken);

    Task<Result<Permission, Error>> CanTransferSubscriptionAsync(ICallerContext caller, string id, string transfererId,
        string transfereeId, CancellationToken cancellationToken);

    Task<Result<Permission, Error>> CanUnsubscribeAsync(ICallerContext caller, string id, string unsubscriberId,
        CancellationToken cancellationToken);

    Task<Result<Permission, Error>> CanViewSubscriptionAsync(ICallerContext caller, string id, string viewerId,
        CancellationToken cancellationToken);
}