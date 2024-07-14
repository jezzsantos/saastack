using Application.Common.Extensions;
using Application.Interfaces;
using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.Organizations;
using EndUsersDomain;
using Membership = Application.Resources.Shared.Membership;
using OrganizationOwnership = Domain.Shared.Organizations.OrganizationOwnership;

namespace EndUsersApplication;

partial class InvitationsApplication
{
    public async Task<Result<Error>> HandleOrganizationMemberInvitedAsync(ICallerContext caller,
        MemberInvited domainEvent, CancellationToken cancellationToken)
    {
        var membership = await InviteMemberToOrganizationAsync(caller, domainEvent.RootId.ToId(),
            domainEvent.InvitedById, domainEvent.InvitedId, domainEvent.EmailAddress, cancellationToken);
        if (membership.IsFailure)
        {
            return membership.Error;
        }

        return Result.Ok;
    }

    public async Task<Result<Error>> HandleOrganizationMemberUnInvitedAsync(ICallerContext caller,
        MemberUnInvited domainEvent,
        CancellationToken cancellationToken)
    {
        var membership = await UnInviteMemberFromOrganizationAsync(caller, domainEvent.RootId.ToId(),
            domainEvent.UninvitedById, domainEvent.UninvitedId, cancellationToken);
        if (membership.IsFailure)
        {
            return membership.Error;
        }

        return Result.Ok;
    }

    private async Task<Result<Membership, Error>> InviteMemberToOrganizationAsync(ICallerContext caller,
        string organizationId, string invitedById, string? userId, string? emailAddress,
        CancellationToken cancellationToken)
    {
        if (emailAddress.HasNoValue() && userId.HasNoValue())
        {
            return Error.RuleViolation(Resources
                .InvitationsApplication_InviteMemberToOrganization_NoUserIdNorEmailAddress);
        }

        var retrievedInviter = await _repository.LoadAsync(invitedById.ToId(), cancellationToken);
        if (retrievedInviter.IsFailure)
        {
            return retrievedInviter.Error;
        }

        var inviter = retrievedInviter.Value;
        EndUserRoot invitee = null!;
        if (emailAddress.HasValue())
        {
            var retrievedByEmail =
                await InviteGuestByEmailInternalAsync(caller, invitedById, emailAddress, cancellationToken);
            if (retrievedByEmail.IsFailure)
            {
                return retrievedByEmail.Error;
            }

            invitee = retrievedByEmail.Value.Invitee;
        }

        if (userId.HasValue())
        {
            var retrievedById = await InviteGuestByUserIdInternalAsync(caller, invitedById, userId, cancellationToken);
            if (retrievedById.IsFailure)
            {
                return retrievedById.Error;
            }

            invitee = retrievedById.Value.Invitee;
        }

        var (_, _, tenantRoles, tenantFeatures) =
            EndUserRoot.GetInitialRolesAndFeatures(RolesAndFeaturesUseCase.InvitingMemberToOrg,
                caller.IsAuthenticated);
        var enrolled = invitee.AddMembership(inviter, OrganizationOwnership.Shared, organizationId.ToId(),
            tenantRoles, tenantFeatures);
        if (enrolled.IsFailure)
        {
            return enrolled.Error;
        }

        var membership = invitee.FindMembership(organizationId.ToId());
        var saved = await _repository.SaveAsync(invitee, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        invitee = saved.Value;
        _recorder.TraceInformation(caller.ToCall(),
            "EndUser {Id} has been invited to organization {Organization}", invitee.Id, organizationId);
        _recorder.TrackUsage(caller.ToCall(), UsageConstants.Events.UsageScenarios.Generic.MembershipAdded,
            invitee.ToMembershipAddedUsageEvent(membership.Value));

        return membership.Value.ToMembership();
    }

    private async Task<Result<Error>> UnInviteMemberFromOrganizationAsync(ICallerContext caller,
        string organizationId, string unInvitedById, string? userId, CancellationToken cancellationToken)
    {
        var retrievedUninviter = await _repository.LoadAsync(unInvitedById.ToId(), cancellationToken);
        if (retrievedUninviter.IsFailure)
        {
            return retrievedUninviter.Error;
        }

        var uninviter = retrievedUninviter.Value;
        var retrievedUninvitee = await _repository.LoadAsync(userId.ToId(), cancellationToken);
        if (retrievedUninvitee.IsFailure)
        {
            return retrievedUninvitee.Error;
        }

        var uninvitee = retrievedUninvitee.Value;
        var uninvited = uninvitee.RemoveMembership(uninviter, organizationId.ToId());
        if (uninvited.IsFailure)
        {
            return uninvited.Error;
        }

        var saved = await _repository.SaveAsync(uninvitee, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        uninvitee = saved.Value;
        _recorder.TraceInformation(caller.ToCall(),
            "EndUser {Id} has been uninvited from organization {Organization}", uninvitee.Id, organizationId);

        return Result.Ok;
    }
}