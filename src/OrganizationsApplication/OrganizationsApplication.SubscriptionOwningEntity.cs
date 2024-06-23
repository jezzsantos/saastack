using Application.Interfaces;
using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Shared;

namespace OrganizationsApplication;

partial class OrganizationsApplication
{
    public async Task<Result<Permission, Error>> CanCancelSubscriptionAsync(ICallerContext caller, string id,
        string cancellerId, CancellationToken cancellationToken)
    {
        var retrieved = await _repository.LoadAsync(id.ToId(), cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        var organization = retrieved.Value;
        var cancellerRoles = await GetMemberRolesAsync(caller, cancellerId.ToId(), organization.Id, cancellationToken);
        if (cancellerRoles.IsFailure)
        {
            return cancellerRoles.Error;
        }

        return organization.CanCancelBillingSubscription(cancellerId.ToId(), cancellerRoles.Value);
    }

    public async Task<Result<Permission, Error>> CanChangeSubscriptionPlanAsync(ICallerContext caller, string id,
        string modifierId, CancellationToken cancellationToken)
    {
        var retrieved = await _repository.LoadAsync(id.ToId(), cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        var organization = retrieved.Value;
        var modifierRoles = await GetMemberRolesAsync(caller, modifierId.ToId(), organization.Id, cancellationToken);
        if (modifierRoles.IsFailure)
        {
            return modifierRoles.Error;
        }

        return organization.CanChangeBillingSubscriptionPlan(modifierId.ToId(), modifierRoles.Value);
    }

    public async Task<Result<Permission, Error>> CanTransferSubscriptionAsync(ICallerContext caller, string id,
        string transfererId, string transfereeId, CancellationToken cancellationToken)
    {
        var retrieved = await _repository.LoadAsync(id.ToId(), cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        var organization = retrieved.Value;
        var transfereeRoles =
            await GetMemberRolesAsync(caller, transfereeId.ToId(), organization.Id, cancellationToken);
        if (transfereeRoles.IsFailure)
        {
            return transfereeRoles.Error;
        }

        return organization.CanTransferBillingSubscription(transfererId.ToId(), transfereeId.ToId(),
            transfereeRoles.Value);
    }

    public async Task<Result<Permission, Error>> CanUnsubscribeAsync(ICallerContext caller, string id,
        string unsubscriberId, CancellationToken cancellationToken)
    {
        var retrieved = await _repository.LoadAsync(id.ToId(), cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        var organization = retrieved.Value;
        return organization.CanUnsubscribeBillingSubscription(unsubscriberId.ToId());
    }

    public async Task<Result<Permission, Error>> CanViewSubscriptionAsync(ICallerContext caller, string id,
        string viewerId, CancellationToken cancellationToken)
    {
        var retrieved = await _repository.LoadAsync(id.ToId(), cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        var organization = retrieved.Value;
        var viewerRoles = await GetMemberRolesAsync(caller, viewerId.ToId(), organization.Id, cancellationToken);
        if (viewerRoles.IsFailure)
        {
            return viewerRoles.Error;
        }

        return organization.CanViewBillingSubscription(viewerId.ToId(), viewerRoles.Value);
    }

    private async Task<Result<Roles, Error>> GetMemberRolesAsync(ICallerContext caller, Identifier userId,
        Identifier organizationId, CancellationToken cancellationToken)
    {
        var retrievedUser = await _endUsersService.GetMembershipsPrivateAsync(caller, userId, cancellationToken);
        if (retrievedUser.IsFailure)
        {
            return retrievedUser.Error;
        }

        var memberships = retrievedUser.Value;
        var membership = memberships.Memberships.Find(membership => membership.OrganizationId == organizationId);
        if (membership.NotExists())
        {
            return Error.EntityNotFound();
        }

        var roles = Roles.Create(membership.Roles.ToArray());
        if (roles.IsFailure)
        {
            return roles.Error;
        }

        return roles.Value;
    }
}