using Application.Common.Extensions;
using Application.Interfaces;
using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.Organizations;
using Domain.Events.Shared.Subscriptions;
using Domain.Shared;
using Domain.Shared.Organizations;
using Domain.Shared.Subscriptions;
using EndUsersDomain;
using Created = Domain.Events.Shared.Organizations.Created;
using Deleted = Domain.Events.Shared.Organizations.Deleted;

namespace EndUsersApplication;

partial class EndUsersApplication
{
    public async Task<Result<Error>> HandleOrganizationCreatedAsync(ICallerContext caller, Created domainEvent,
        CancellationToken cancellationToken)
    {
        var ownership = domainEvent.Ownership.ToEnumOrDefault(OrganizationOwnership.Shared);
        return await AddMembershipAsync(caller, domainEvent.CreatedById.ToId(), domainEvent.RootId.ToId(),
            ownership, domainEvent.Name, cancellationToken);
    }

    public async Task<Result<Error>> HandleOrganizationDeletedAsync(ICallerContext caller,
        Deleted domainEvent, CancellationToken cancellationToken)
    {
        return await RemoveOwnerMembershipFromDeletedOrganizationAsync(caller, domainEvent.RootId.ToId(),
            domainEvent.DeletedById.ToId(), cancellationToken);
    }

    public async Task<Result<Error>> HandleOrganizationRoleAssignedAsync(ICallerContext caller,
        RoleAssigned domainEvent,
        CancellationToken cancellationToken)
    {
        return await AssignTenantRolesAsync(caller, domainEvent.AssignedById.ToId(), domainEvent.RootId.ToId(),
            domainEvent.UserId.ToId(), [domainEvent.Role], cancellationToken);
    }

    public async Task<Result<Error>> HandleOrganizationRoleUnassignedAsync(ICallerContext caller,
        RoleUnassigned domainEvent,
        CancellationToken cancellationToken)
    {
        return await UnassignTenantRolesAsync(caller, domainEvent.UnassignedById.ToId(),
            domainEvent.RootId.ToId(), domainEvent.UserId.ToId(),
            [domainEvent.Role], cancellationToken);
    }

    public async Task<Result<Error>> HandleSubscriptionPlanChangedAsync(ICallerContext caller,
        SubscriptionPlanChanged domainEvent, CancellationToken cancellationToken)
    {
        return await HandleChangeSubscriptionPlanAsync(caller, domainEvent.RootId.ToId(),
            domainEvent.OwningEntityId.ToId(), domainEvent.PlanId, cancellationToken);
    }

    public async Task<Result<Error>> HandleSubscriptionTransferredAsync(ICallerContext caller,
        SubscriptionTransferred domainEvent, CancellationToken cancellationToken)
    {
        return await HandleChangeSubscriptionPlanAsync(caller, domainEvent.RootId.ToId(),
            domainEvent.OwningEntityId.ToId(), domainEvent.PlanId, cancellationToken);
    }

    private async Task<Result<Error>> RemoveOwnerMembershipFromDeletedOrganizationAsync(ICallerContext caller,
        Identifier organizationId, Identifier deletedById, CancellationToken cancellationToken)
    {
        var retrievedDeleter = await _endUserRepository.LoadAsync(deletedById, cancellationToken);
        if (retrievedDeleter.IsFailure)
        {
            return retrievedDeleter.Error;
        }

        var deleter = retrievedDeleter.Value;
        var removed = deleter.RemoveMembership(deleter, organizationId);
        if (removed.IsFailure)
        {
            return removed.Error;
        }

        var saved = await _endUserRepository.SaveAsync(deleter, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        deleter = saved.Value;
        _recorder.TraceInformation(caller.ToCall(),
            "EndUser {Id} has been removed from the deleted organization {Organization}",
            deleter.Id, organizationId);

        return Result.Ok;
    }

    private async Task<Result<Error>> AddMembershipAsync(ICallerContext caller,
        Identifier createdById, Identifier organizationId, OrganizationOwnership ownership,
        string organizationName, CancellationToken cancellationToken)
    {
        var retrievedOwner = await _endUserRepository.LoadAsync(createdById, cancellationToken);
        if (retrievedOwner.IsFailure)
        {
            return retrievedOwner.Error;
        }

        var owner = retrievedOwner.Value;
        var useCase = ownership switch
        {
            OrganizationOwnership.Shared => RolesAndFeaturesUseCase.CreatingOrg,
            OrganizationOwnership.Personal => owner.IsPerson
                ? RolesAndFeaturesUseCase.CreatingPerson
                : RolesAndFeaturesUseCase.CreatingMachine,
            _ => RolesAndFeaturesUseCase.CreatingOrg
        };
        var (_, _, tenantRoles, tenantFeatures) =
            EndUserRoot.GetInitialRolesAndFeatures(useCase, caller.IsAuthenticated);
        var membered = owner.AddMembership(owner, ownership, organizationId, tenantRoles, tenantFeatures);
        if (membered.IsFailure)
        {
            return membered.Error;
        }

        var saved = await _endUserRepository.SaveAsync(owner, true, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        owner = saved.Value;
        _recorder.TraceInformation(caller.ToCall(), "EndUser {Id} has become a member of organization {Organization}",
            owner.Id, organizationId);
        var membership = owner.DefaultMembership;
        _recorder.TrackUsage(caller.ToCall(), UsageConstants.Events.UsageScenarios.Generic.MembershipAdded,
            owner.ToMembershipAddedUsageEvent(membership, organizationName));

        if (owner.IsMachine)
        {
            var previousMembership = owner.Memberships.FirstOrDefault(m => m.OrganizationId != organizationId);
            if (previousMembership.Exists())
            {
                var changed = owner.ChangeDefaultMembership(previousMembership.OrganizationId);
                if (changed.IsFailure)
                {
                    return changed.Error;
                }

                saved = await _endUserRepository.SaveAsync(owner, cancellationToken);
                if (saved.IsFailure)
                {
                    return saved.Error;
                }

                var profiled = await GetUserProfileAsync(caller, owner.Id, cancellationToken);
                if (profiled.IsFailure)
                {
                    return profiled.Error;
                }

                var profile = profiled.Value;
                owner = saved.Value;
                _recorder.TraceInformation(caller.ToCall(),
                    "Machine {Id} has become a member of organization {Organization}",
                    owner.Id, previousMembership.OrganizationId);
                _recorder.TrackUsage(caller.ToCall(), UsageConstants.Events.UsageScenarios.Generic.MembershipChanged,
                    owner.ToMembershipChangeUsageEvent(previousMembership, profile));
            }
        }

        return Result.Ok;
    }

    private async Task<Result<Error>> AssignTenantRolesAsync(ICallerContext caller, Identifier assignerId,
        Identifier organizationId, Identifier assigneeId, List<string> roles, CancellationToken cancellationToken)
    {
        var retrievedAssigner = await _endUserRepository.LoadAsync(assignerId, cancellationToken);
        if (retrievedAssigner.IsFailure)
        {
            return retrievedAssigner.Error;
        }

        var retrievedAssignee = await _endUserRepository.LoadAsync(assigneeId, cancellationToken);
        if (retrievedAssignee.IsFailure)
        {
            return retrievedAssignee.Error;
        }

        var assigner = retrievedAssigner.Value;
        var assignee = retrievedAssignee.Value;
        var assigneeRoles = Roles.Create(roles.ToArray());
        if (assigneeRoles.IsFailure)
        {
            return assigneeRoles.Error;
        }

        var assigned = assignee.AssignMembershipRoles(assigner, organizationId, assigneeRoles.Value, OnAssign);
        if (assigned.IsFailure)
        {
            return assigned.Error;
        }

        var membership = assigned.Value;
        var saved = await _endUserRepository.SaveAsync(assignee, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        assignee = saved.Value;
        _recorder.TraceInformation(caller.ToCall(),
            "EndUser {Id} has been assigned tenant roles {Roles} to membership {Membership}",
            assignee.Id, roles.JoinAsOredChoices(), membership.Id);

        return Result.Ok;

        Result<Error> OnAssign(Roles assignedRoles, Identifier assignerId1, Identifier assigneeId1,
            Identifier membershipId)
        {
            _recorder.AuditAgainst(caller.ToCall(), assigneeId1,
                Audits.EndUsersApplication_TenantRolesAssigned,
                "EndUser {AssignerId} assigned the tenant roles {Roles} to assignee {AssigneeId} for membership {Membership}",
                assignerId1, assignedRoles.Items.Select(rol => rol.Identifier).JoinAsOredChoices(), assigneeId1,
                membershipId);
            return Result.Ok;
        }
    }

    private async Task<Result<Error>> UnassignTenantRolesAsync(ICallerContext caller, Identifier unassignerId,
        Identifier organizationId, Identifier assigneeId, List<string> roles, CancellationToken cancellationToken)
    {
        var retrievedAssigner = await _endUserRepository.LoadAsync(unassignerId, cancellationToken);
        if (retrievedAssigner.IsFailure)
        {
            return retrievedAssigner.Error;
        }

        var retrievedAssignee = await _endUserRepository.LoadAsync(assigneeId, cancellationToken);
        if (retrievedAssignee.IsFailure)
        {
            return retrievedAssignee.Error;
        }

        var assigner = retrievedAssigner.Value;
        var assignee = retrievedAssignee.Value;
        var assigneeRoles = Roles.Create(roles.ToArray());
        if (assigneeRoles.IsFailure)
        {
            return assigneeRoles.Error;
        }

        var assigned = assignee.UnassignMembershipRoles(assigner, organizationId, assigneeRoles.Value, OnUnassign);
        if (assigned.IsFailure)
        {
            return assigned.Error;
        }

        var membership = assigned.Value;
        var saved = await _endUserRepository.SaveAsync(assignee, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        assignee = saved.Value;
        _recorder.TraceInformation(caller.ToCall(),
            "EndUser {Id} has been unassigned tenant roles {Roles} from membership {Membership}",
            assignee.Id, roles.JoinAsOredChoices(), membership.Id);

        return Result.Ok;

        Result<Error> OnUnassign(Roles unassignedRoles, Identifier unassignerId1, Identifier unassigneeId1,
            Identifier membershipId)
        {
            _recorder.AuditAgainst(caller.ToCall(), unassigneeId1,
                Audits.EndUsersApplication_TenantRolesUnassigned,
                "EndUser {AssignerId} unassigned the tenant roles {Roles} from assignee {AssigneeId} for membership {Membership}",
                unassignerId, unassignedRoles.Items.Select(fs => fs.Identifier).JoinAsOredChoices(), unassigneeId1,
                membershipId);
            return Result.Ok;
        }
    }

    private async Task<Result<Error>> HandleChangeSubscriptionPlanAsync(ICallerContext caller,
        Identifier subscriptionId, Identifier organizationId, string planId, CancellationToken cancellationToken)
    {
        var subscription = await _subscriptionsService.GetSubscriptionAsync(caller, subscriptionId, cancellationToken);
        if (subscription.IsFailure)
        {
            return subscription.Error;
        }

        var plan = subscription.Value.Plan;
        var searched =
            await _endUserRepository.SearchAllMembershipsByOrganizationAsync(organizationId, new SearchOptions(),
                cancellationToken);
        if (searched.IsFailure)
        {
            return searched.Error;
        }

        var memberships = searched.Value.Results;
        foreach (var membership in memberships)
        {
            var retrievedUser = await _endUserRepository.LoadAsync(membership.UserId.Value.ToId(), cancellationToken);
            if (retrievedUser.IsFailure)
            {
                return retrievedUser.Error;
            }

            var assignee = retrievedUser.Value;
            var assignerId = caller.ToCallerId();
            var tier = plan.Tier.ToEnumOrDefault(BillingSubscriptionTier.Unsubscribed);
            var reconciled = assignee.ResetMembershipFeatures(assignerId, organizationId, tier, planId,
                OnAssignPlatformFeature, OnAssignTenantFeature);
            if (reconciled.IsFailure)
            {
                return reconciled.Error;
            }

            var saved = await _endUserRepository.SaveAsync(assignee, cancellationToken);
            if (saved.IsFailure)
            {
                return saved.Error;
            }

            assignee = saved.Value;
            _recorder.TraceInformation(caller.ToCall(),
                "EndUser {Id} has reconciled its features with the new subscription plan {Plan} for {Organization}",
                assignee.Id, planId, organizationId);
        }

        return Result.Ok;

        Result<Error> OnAssignPlatformFeature(Features features, Identifier assignerId, Identifier assigneeId)
        {
            _recorder.Audit(caller.ToCall(),
                Audits.EndUsersApplication_PlatformFeaturesAssigned,
                "EndUser {AssignerId} assigned the platform features {Features} to assignee {AssigneeId}",
                assignerId, features.Items.Select(fs => fs.Identifier).JoinAsOredChoices(), assigneeId);
            return Result.Ok;
        }

        Result<Error> OnAssignTenantFeature(Features features, Identifier assignerId, Identifier assigneeId,
            Identifier membershipId)
        {
            _recorder.Audit(caller.ToCall(),
                Audits.EndUsersApplication_TenantFeaturesAssigned,
                "EndUser {AssignerId} assigned the tenant features {Features} to assignee {AssigneeId} for membership {Membership}",
                assignerId, features.Items.Select(fs => fs.Identifier).JoinAsOredChoices(), assigneeId, membershipId);
            return Result.Ok;
        }
    }
}