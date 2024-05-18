using Application.Common.Extensions;
using Application.Interfaces;
using Application.Resources.Shared;
using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.Organizations;
using Domain.Shared;
using Domain.Shared.EndUsers;
using EndUsersDomain;
using Membership = Application.Resources.Shared.Membership;

namespace EndUsersApplication;

partial class EndUsersApplication
{
    public async Task<Result<Error>> HandleOrganizationCreatedAsync(ICallerContext caller, Created domainEvent,
        CancellationToken cancellationToken)
    {
        var ownership = domainEvent.Ownership.ToEnumOrDefault(OrganizationOwnership.Shared);
        var membership = await CreateMembershipAsync(caller, domainEvent.CreatedById.ToId(), domainEvent.RootId.ToId(),
            ownership, cancellationToken);
        if (membership.IsFailure)
        {
            return membership.Error;
        }

        return Result.Ok;
    }

    public async Task<Result<Error>> HandleOrganizationDeletedAsync(ICallerContext caller,
        Deleted domainEvent, CancellationToken cancellationToken)
    {
        var deleted = await RemoveMembershipFromDeletedOrganizationAsync(caller, domainEvent.RootId.ToId(),
            domainEvent.DeletedById.ToId(), cancellationToken);
        if (deleted.IsFailure)
        {
            return deleted.Error;
        }

        return Result.Ok;
    }

    public async Task<Result<Error>> HandleOrganizationRoleAssignedAsync(ICallerContext caller,
        RoleAssigned domainEvent,
        CancellationToken cancellationToken)
    {
        var assigned = await AssignTenantRolesAsync(caller, domainEvent.AssignedById.ToId(), domainEvent.RootId.ToId(),
            domainEvent.UserId.ToId(), [domainEvent.Role], cancellationToken);
        if (assigned.IsFailure)
        {
            return assigned.Error;
        }

        return Result.Ok;
    }

    public async Task<Result<Error>> HandleOrganizationRoleUnassignedAsync(ICallerContext caller,
        RoleUnassigned domainEvent,
        CancellationToken cancellationToken)
    {
        var assigned = await UnassignTenantRolesAsync(caller, domainEvent.UnassignedById.ToId(),
            domainEvent.RootId.ToId(), domainEvent.UserId.ToId(),
            [domainEvent.Role], cancellationToken);
        if (assigned.IsFailure)
        {
            return assigned.Error;
        }

        return Result.Ok;
    }

    private async Task<Result<Error>> RemoveMembershipFromDeletedOrganizationAsync(ICallerContext caller,
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

    private async Task<Result<Membership, Error>> CreateMembershipAsync(ICallerContext caller,
        Identifier createdById, Identifier organizationId, OrganizationOwnership ownership,
        CancellationToken cancellationToken)
    {
        var retrievedInviter = await _endUserRepository.LoadAsync(createdById, cancellationToken);
        if (retrievedInviter.IsFailure)
        {
            return retrievedInviter.Error;
        }

        var inviter = retrievedInviter.Value;
        var useCase = ownership switch
        {
            OrganizationOwnership.Shared => RolesAndFeaturesUseCase.CreatingOrg,
            OrganizationOwnership.Personal => inviter.Classification == UserClassification.Person
                ? RolesAndFeaturesUseCase.CreatingPerson
                : RolesAndFeaturesUseCase.CreatingMachine,
            _ => RolesAndFeaturesUseCase.CreatingOrg
        };
        var (_, _, tenantRoles, tenantFeatures) =
            EndUserRoot.GetInitialRolesAndFeatures(useCase, caller.IsAuthenticated);
        var inviterOwnership = ownership.ToEnumOrDefault(Domain.Shared.Organizations.OrganizationOwnership.Shared);
        var membered = inviter.AddMembership(inviter, inviterOwnership, organizationId, tenantRoles, tenantFeatures);
        if (membered.IsFailure)
        {
            return membered.Error;
        }

        var saved = await _endUserRepository.SaveAsync(inviter, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        inviter = saved.Value;
        _recorder.TraceInformation(caller.ToCall(), "EndUser {Id} has become a member of organization {Organization}",
            inviter.Id, organizationId);

        var membership = inviter.FindMembership(organizationId);
        if (!membership.HasValue)
        {
            return Error.EntityNotFound(Resources.EndUsersApplication_MembershipNotFound);
        }

        return membership.Value.ToMembership();
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

        var assigned = assignee.AssignMembershipRoles(assigner, organizationId, assigneeRoles.Value);
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
        _recorder.AuditAgainst(caller.ToCall(), assignee.Id,
            Audits.EndUserApplication_TenantRolesAssigned,
            "EndUser {AssignerId} assigned the tenant roles {Roles} to assignee {AssigneeId} for membership {Membership}",
            assigner.Id, roles.JoinAsOredChoices(), assignee.Id, membership.Id);

        return Result.Ok;
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

        var assigned = assignee.UnassignMembershipRoles(assigner, organizationId, assigneeRoles.Value);
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
        _recorder.AuditAgainst(caller.ToCall(), assignee.Id,
            Audits.EndUserApplication_TenantRolesUnassigned,
            "EndUser {AssignerId} unassigned the tenant roles {Roles} from assignee {AssigneeId} for membership {Membership}",
            assigner.Id, roles.JoinAsOredChoices(), assignee.Id, membership.Id);

        return Result.Ok;
    }
}