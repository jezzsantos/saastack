using Common;
using Common.Extensions;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.EndUsers;
using Domain.Interfaces;
using Domain.Interfaces.Authorization;
using Domain.Interfaces.Entities;
using Domain.Interfaces.ValueObjects;
using Domain.Services.Shared;
using Domain.Shared;
using Domain.Shared.EndUsers;
using Domain.Shared.Organizations;
using JetBrains.Annotations;

namespace EndUsersDomain;

public delegate Task<Result<Error>> InviteAction(Identifier inviterId, string token);

public sealed partial class EndUserRoot : AggregateRootBase
{
#if TESTINGONLY
    private const string TestingToken = "Ll4qhv77XhiXSqsTUc6icu56ZLrqu5p1gH9kT5IlHio";
#endif

    public static Result<EndUserRoot, Error> Create(IRecorder recorder, IIdentifierFactory idFactory,
        UserClassification classification)
    {
        var root = new EndUserRoot(recorder, idFactory);
        root.RaiseCreateEvent(EndUsersDomain.Events.Created(root.Id, classification));
        return root;
    }

    private EndUserRoot(IRecorder recorder, IIdentifierFactory idFactory) : base(recorder, idFactory)
    {
    }

    private EndUserRoot(IRecorder recorder, IIdentifierFactory idFactory, ISingleValueObject<string> identifier) : base(
        recorder, idFactory, identifier)
    {
    }

    public UserAccess Access { get; private set; }

    public UserClassification Classification { get; private set; }

    public Membership DefaultMembership => Memberships.DefaultMembership;

    public Features Features { get; private set; } = Features.Empty;

    public GuestInvitation GuestInvitation { get; private set; } = GuestInvitation.Empty;

    public bool IsMachine => Classification == UserClassification.Machine;

    public bool IsPerson => Classification == UserClassification.Person;

    public bool IsRegistered => Status == UserStatus.Registered;

    public Memberships Memberships { get; } = new();

    public Roles Roles { get; private set; } = Roles.Empty;

    public UserStatus Status { get; private set; }

    [UsedImplicitly]
    public static AggregateRootFactory<EndUserRoot> Rehydrate()
    {
        return (identifier, container, _) => new EndUserRoot(container.GetRequiredService<IRecorder>(),
            container.GetRequiredService<IIdentifierFactory>(), identifier);
    }

    public override Result<Error> EnsureInvariants()
    {
        var ensureInvariants = base.EnsureInvariants();
        if (ensureInvariants.IsFailure)
        {
            return ensureInvariants.Error;
        }

        var memberships = Memberships.EnsureInvariants();
        if (memberships.IsFailure)
        {
            return memberships.Error;
        }

        if (IsMachine && !IsRegistered)
        {
            return Error.RuleViolation(Resources.EndUserRoot_MachineNotRegistered);
        }

        if (IsRegistered)
        {
            if (Roles.HasNone())
            {
                return Error.RuleViolation(Resources.EndUserRoot_AllPersonsMustHaveDefaultRole);
            }

            if (Features.HasNone())
            {
                return Error.RuleViolation(Resources.EndUserRoot_AllPersonsMustHaveDefaultFeature);
            }

            if (GuestInvitation.IsStillOpen)
            {
                return Error.RuleViolation(Resources.EndUserRoot_GuestAlreadyRegistered);
            }
        }

        return Result.Ok;
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event, bool isReconstituting)
    {
        switch (@event)
        {
            case Created created:
            {
                Access = created.Access;
                Status = created.Status;
                Classification = created.Classification;
                Features = Features.Empty;
                Roles = Roles.Empty;
                return Result.Ok;
            }

            case Registered changed:
            {
                Access = changed.Access;
                Status = changed.Status;
                Classification = changed.Classification;

                var roles = Roles.Create(changed.Roles.ToArray());
                if (roles.IsFailure)
                {
                    return roles.Error;
                }

                Roles = roles.Value;
                var features = Features.Create(changed.Features.ToArray());
                if (features.IsFailure)
                {
                    return features.Error;
                }

                Features = features.Value;
                Recorder.TraceDebug(null, "EndUser {Id} was registered", Id);
                return Result.Ok;
            }

            case MembershipAdded added:
            {
                var membership = RaiseEventToChildEntity(isReconstituting, added, idFactory =>
                    Membership.Create(Recorder, idFactory, RaiseChangeEvent), e => e.MembershipId);
                if (membership.IsFailure)
                {
                    return membership.Error;
                }

                Memberships.Add(membership.Value);
                Recorder.TraceDebug(null,
                    "EndUser {Id} added membership {Membership} to organization {Organization}", Id,
                    membership.Value.Id, added.OrganizationId);
                return Result.Ok;
            }

            case MembershipRemoved removed:
            {
                var membership = Memberships.FindByMembershipId(removed.MembershipId.ToId());
                if (!membership.HasValue)
                {
                    return Error.RuleViolation(Resources.EndUserRoot_MissingMembership);
                }

                Memberships.Remove(membership.Value.Id);
                Recorder.TraceDebug(null,
                    "EndUser {Id} removed membership {Membership} from organization {Organization}", Id,
                    membership.Value.Id, removed.OrganizationId);
                return Result.Ok;
            }

            case DefaultMembershipChanged changed:
            {
                if (changed.FromMembershipId.Exists())
                {
                    var fromMembership = Memberships.FindByMembershipId(changed.FromMembershipId.ToId());
                    if (!fromMembership.HasValue)
                    {
                        return Error.RuleViolation(Resources.EndUserRoot_MissingMembership);
                    }

                    var from = RaiseEventToChildEntity(changed, fromMembership.Value);
                    if (from.IsFailure)
                    {
                        return from.Error;
                    }
                }

                var toMembership = Memberships.FindByMembershipId(changed.ToMembershipId.ToId());
                if (!toMembership.HasValue)
                {
                    return Error.RuleViolation(Resources.EndUserRoot_NoMembership);
                }

                var to = RaiseEventToChildEntity(changed, toMembership.Value);
                if (to.IsFailure)
                {
                    return to.Error;
                }

                Recorder.TraceDebug(null,
                    "EndUser {Id} changed default membership from {FromMembership} to {ToMembership}", Id,
                    changed.FromMembershipId ?? "none", changed.ToMembershipId);

                return Result.Ok;
            }

            case MembershipRoleAssigned assigned:
            {
                var membershipId = assigned.MembershipId.ToId();
                var membership = Memberships.FindByMembershipId(membershipId);
                if (!membership.HasValue)
                {
                    return Error.RuleViolation(Resources.EndUserRoot_NoMembership);
                }

                var forwarded = RaiseEventToChildEntity(assigned, membership.Value);
                if (forwarded.IsFailure)
                {
                    return forwarded.Error;
                }

                Recorder.TraceDebug(null, "EndUser {Id} assigned role {Role} to membership {MembershipId}", Id,
                    assigned.Role,
                    membershipId);
                return Result.Ok;
            }

            case MembershipRoleUnassigned unassigned:
            {
                var membershipId = unassigned.MembershipId.ToId();
                var membership = Memberships.FindByMembershipId(membershipId);
                if (!membership.HasValue)
                {
                    return Error.RuleViolation(Resources.EndUserRoot_NoMembership);
                }

                var forwarded = RaiseEventToChildEntity(unassigned, membership.Value);
                if (forwarded.IsFailure)
                {
                    return forwarded.Error;
                }

                Recorder.TraceDebug(null, "EndUser {Id} unassigned role {Role} from membership {MembershipId}", Id,
                    unassigned.Role,
                    membershipId);
                return Result.Ok;
            }

            case MembershipFeatureAssigned assigned:
            {
                var membershipId = assigned.MembershipId.ToId();
                var membership = Memberships.FindByMembershipId(membershipId);
                if (!membership.HasValue)
                {
                    return Error.RuleViolation(Resources.EndUserRoot_NoMembership);
                }

                var forwarded = RaiseEventToChildEntity(assigned, membership.Value);
                if (forwarded.IsFailure)
                {
                    return forwarded.Error;
                }

                Recorder.TraceDebug(null, "EndUser {Id} assigned feature {Feature} to membership {MembershipId}", Id,
                    assigned.Feature, membershipId);
                return Result.Ok;
            }

            case MembershipFeatureUnassigned unassigned:
            {
                var membershipId = unassigned.MembershipId.ToId();
                var membership = Memberships.FindByMembershipId(membershipId);
                if (!membership.HasValue)
                {
                    return Error.RuleViolation(Resources.EndUserRoot_NoMembership);
                }

                var forwarded = RaiseEventToChildEntity(unassigned, membership.Value);
                if (forwarded.IsFailure)
                {
                    return forwarded.Error;
                }

                Recorder.TraceDebug(null, "EndUser {Id} unassigned feature {Feature} from membership {MembershipId}",
                    Id,
                    unassigned.Feature, membershipId);
                return Result.Ok;
            }

            case MembershipFeaturesReset reset:
            {
                var membershipId = reset.MembershipId.ToId();
                var membership = Memberships.FindByMembershipId(membershipId);
                if (!membership.HasValue)
                {
                    return Error.RuleViolation(Resources.EndUserRoot_NoMembership);
                }

                var forwarded = RaiseEventToChildEntity(reset, membership.Value);
                if (forwarded.IsFailure)
                {
                    return forwarded.Error;
                }

                Recorder.TraceDebug(null, "EndUser {Id} reset features {Features} to membership {MembershipId}", Id,
                    reset.Features.JoinAsOredChoices(), membershipId);
                return Result.Ok;
            }

            case PlatformRoleAssigned assigned:
            {
                var roles = Roles.Add(assigned.Role);
                if (roles.IsFailure)
                {
                    return roles.Error;
                }

                Roles = roles.Value;
                Recorder.TraceDebug(null, "EndUser {Id} assigned role {Role}", Id, assigned.Role);
                return Result.Ok;
            }

            case PlatformRoleUnassigned unassigned:
            {
                var roles = Roles.Remove(unassigned.Role);
                Roles = roles;
                Recorder.TraceDebug(null, "EndUser {Id} unassigned role {Role}", Id, unassigned.Role);
                return Result.Ok;
            }

            case PlatformFeatureAssigned assigned:
            {
                var features = Features.Add(assigned.Feature);
                if (features.IsFailure)
                {
                    return features.Error;
                }

                Features = features.Value;
                Recorder.TraceDebug(null, "EndUser {Id} assigned feature {Feature}", Id, assigned.Feature);
                return Result.Ok;
            }

            case PlatformFeatureUnassigned unassigned:
            {
                var features = Features.Remove(unassigned.Feature);
                Features = features;
                Recorder.TraceDebug(null, "EndUser {Id} unassigned feature {Feature}", Id, unassigned.Feature);
                return Result.Ok;
            }

            case PlatformFeaturesReset reset:
            {
                var features = Features.Create(reset.Features.ToArray());
                if (features.IsFailure)
                {
                    return features.Error;
                }

                Features = features.Value;
                Recorder.TraceDebug(null, "EndUser {Id} reset features {Features}", Id,
                    reset.Features.JoinAsOredChoices());
                return Result.Ok;
            }

            case GuestInvitationCreated created:
            {
                var inviteeEmailAddress = EmailAddress.Create(created.EmailAddress);
                if (inviteeEmailAddress.IsFailure)
                {
                    return inviteeEmailAddress.Error;
                }

                var invited = GuestInvitation.IsStillOpen
                    ? GuestInvitation.Renew(created.Token, inviteeEmailAddress.Value)
                    : GuestInvitation.Invite(created.Token, inviteeEmailAddress.Value, created.InvitedById.ToId());
                if (invited.IsFailure)
                {
                    return invited.Error;
                }

                GuestInvitation = invited.Value;
                Recorder.TraceDebug(null, "EndUser {Id} invited as a guest by {InvitedBy}", Id, created.InvitedById);
                return Result.Ok;
            }

            case GuestInvitationAccepted changed:
            {
                var acceptedEmailAddress = EmailAddress.Create(changed.AcceptedEmailAddress);
                if (acceptedEmailAddress.IsFailure)
                {
                    return acceptedEmailAddress.Error;
                }

                var accepted = GuestInvitation.Accept(acceptedEmailAddress.Value);
                if (accepted.IsFailure)
                {
                    return accepted.Error;
                }

                GuestInvitation = accepted.Value;
                Recorder.TraceDebug(null, "EndUser {Id} accepted their guest invitation", Id);
                return Result.Ok;
            }

            default:
                return HandleUnKnownStateChangedEvent(@event);
        }
    }

    public Result<Error> AcceptGuestInvitation(Identifier acceptedById, EmailAddress emailAddress)
    {
        if (IsNotAnonymousUser(acceptedById))
        {
            return Error.ForbiddenAccess(Resources.EndUserRoot_GuestInvitationAcceptedByNonAnonymousUser);
        }

        var verified = VerifyGuestInvitation();
        if (verified.IsFailure)
        {
            return verified.Error;
        }

        return RaiseChangeEvent(EndUsersDomain.Events.GuestInvitationAccepted(Id, emailAddress));
    }

    public Result<Error> AddMembership(EndUserRoot adder, OrganizationOwnership ownership, Identifier organizationId,
        Roles tenantRoles, Features tenantFeatures)
    {
        var skipOwnershipCheck = IsSelf(adder);
        if (!skipOwnershipCheck)
        {
            if (!IsAnOrganizationOwner(adder, organizationId))
            {
                return Error.RoleViolation(Resources.EndUserRoot_NotOrganizationOwner);
            }

            if (ownership == OrganizationOwnership.Personal)
            {
                return Error.RuleViolation(Resources.EndUserRoot_AddMembership_SharedOwnershipRequired);
            }
        }

        if (ownership == OrganizationOwnership.Personal
            && Memberships.HasPersonalOrganization)
        {
            return Error.RuleViolation(Resources.EndUserRoot_AddMembership_OnlyOnePersonalOrganization);
        }

        var existing = Memberships.FindByOrganizationId(organizationId);
        if (existing.HasValue)
        {
            return Result.Ok;
        }

        var isFirst = Memberships.HasNone();
        var added = RaiseChangeEvent(
            EndUsersDomain.Events.MembershipAdded(Id, organizationId, ownership, isFirst, tenantRoles, tenantFeatures));
        if (added.IsFailure)
        {
            return added.Error;
        }

        var defaultMembershipId = isFirst
            ? Optional<Identifier>.None
            : Memberships.DefaultMembership.Id.ToOptional();
        var addedMembership = Memberships.FindByOrganizationId(organizationId);

        return RaiseChangeEvent(EndUsersDomain.Events.DefaultMembershipChanged(Id, defaultMembershipId,
            addedMembership.Value.Id, addedMembership.Value.OrganizationId, tenantRoles, tenantFeatures));
    }

    public Result<Error> ChangeDefaultMembership(Identifier organizationId)
    {
        var targetMembership = Memberships.FindByOrganizationId(organizationId);
        if (!targetMembership.HasValue)
        {
            return Error.RuleViolation(Resources.EndUserRoot_NoMembership.Format(organizationId));
        }

        var defaultMembership = Memberships.DefaultMembership;
        if (defaultMembership.Equals(targetMembership.Value))
        {
            return Result.Ok;
        }

        return RaiseChangeEvent(EndUsersDomain.Events.DefaultMembershipChanged(Id, defaultMembership.Id.ToOptional(),
            targetMembership.Value.Id, targetMembership.Value.OrganizationId, targetMembership.Value.Roles,
            targetMembership.Value.Features));
    }

    public Optional<Membership> FindMembership(Identifier organizationId)
    {
        return Memberships.FindByOrganizationId(organizationId);
    }

    public PersonName GuessGuestInvitationName()
    {
        return GuestInvitation.InviteeEmailAddress!.GuessPersonFullName();
    }

    public async Task<Result<Error>> InviteGuestAsync(ITokensService tokensService, Identifier inviterId,
        EmailAddress inviteeEmailAddress, InviteAction onInvited)
    {
        if (IsRegistered)
        {
            return Result.Ok;
        }

        var token = tokensService.CreateGuestInvitationToken();
        var raised =
            RaiseChangeEvent(
                EndUsersDomain.Events.GuestInvitationCreated(Id, token, inviteeEmailAddress, inviterId));
        if (raised.IsFailure)
        {
            return raised.Error;
        }

        return await onInvited(inviterId, token);
    }

    public Result<Error> Register(Roles roles, Features levels, EndUserProfile profile, Optional<EmailAddress> username)
    {
        if (Status != UserStatus.Unregistered)
        {
            return Error.RuleViolation(Resources.EndUserRoot_AlreadyRegistered);
        }

        if (GuestInvitation.CanAccept)
        {
            if (username.HasValue)
            {
                var accepted = RaiseChangeEvent(EndUsersDomain.Events.GuestInvitationAccepted(Id, username));
                if (accepted.IsFailure)
                {
                    return accepted.Error;
                }
            }
        }

        return RaiseChangeEvent(EndUsersDomain.Events.Registered(Id, profile, username, Classification,
            UserAccess.Enabled, UserStatus.Registered, roles, levels));
    }

    public async Task<Result<Error>> ReInviteGuestAsync(ITokensService tokensService, Identifier inviterId,
        InviteAction onInvited)
    {
        if (!GuestInvitation.IsInvited)
        {
            return Error.RuleViolation(Resources.EndUserRoot_GuestInvitationNeverSent);
        }

        if (!GuestInvitation.IsStillOpen)
        {
            return Error.RuleViolation(Resources.EndUserRoot_GuestInvitationHasExpired);
        }

        return await InviteGuestAsync(tokensService, inviterId, GuestInvitation.InviteeEmailAddress!, onInvited);
    }

    public Result<Error> RemoveMembership(EndUserRoot remover, Identifier organizationId)
    {
        if (!IsAnOrganizationOwner(remover, organizationId))
        {
            return Error.RoleViolation(Resources.EndUserRoot_NotOrganizationOwner);
        }

        var membership = Memberships.FindByOrganizationId(organizationId);
        if (!membership.HasValue)
        {
            return Result.Ok;
        }

        if (membership.Value.Ownership == OrganizationOwnership.Personal)
        {
            return Error.RuleViolation(Resources.EndUserRoot_RemoveMembership_SharedOwnershipRequired);
        }

        var isLastMembership = Memberships.Count == 1;
        if (!isLastMembership)
        {
            if (membership.Value.Equals(Memberships.DefaultMembership))
            {
                var defaultMembership = Memberships.DefaultMembership;
                var newDefaultMembership = Memberships.FindNextDefaultMembership();
                var defaulted = RaiseChangeEvent(EndUsersDomain.Events.DefaultMembershipChanged(Id,
                    defaultMembership.Id,
                    newDefaultMembership.Id, newDefaultMembership.OrganizationId, newDefaultMembership.Roles,
                    newDefaultMembership.Features));
                if (defaulted.IsFailure)
                {
                    return defaulted.Error;
                }
            }
        }

        return RaiseChangeEvent(
            EndUsersDomain.Events.MembershipRemoved(Id, membership.Value.Id, organizationId, remover.Id));
    }

#if TESTINGONLY
    public void TestingOnly_ExpireGuestInvitation()
    {
        GuestInvitation = GuestInvitation.TestingOnly_ExpireNow();
    }
#endif

#if TESTINGONLY
    public void TestingOnly_InviteGuest(EmailAddress emailAddress)
    {
        GuestInvitation = GuestInvitation.Invite(TestingToken, emailAddress, "aninviter".ToId()).Value;
    }
#endif

    public Result<Error> VerifyGuestInvitation()
    {
        if (IsRegistered)
        {
            return Error.EntityExists(Resources.EndUserRoot_GuestAlreadyRegistered);
        }

        if (!GuestInvitation.IsInvited)
        {
            return Error.PreconditionViolation(Resources.EndUserRoot_GuestInvitationNeverSent);
        }

        if (!GuestInvitation.IsStillOpen)
        {
            return Error.PreconditionViolation(Resources.EndUserRoot_GuestInvitationHasExpired);
        }

        return Result.Ok;
    }

    private bool IsSelf(EndUserRoot user)
    {
        return user.Id == Id;
    }

    private static bool IsServiceAccount(Identifier userId)
    {
        return CallerConstants.IsServiceAccount(userId);
    }

    private static bool IsOperations(Roles roles)
    {
        return roles.HasRole(PlatformRoles.Operations);
    }

    private static bool IsAnOrganizationOwner(EndUserRoot assigner, Identifier organizationId)
    {
        var retrieved = assigner.Memberships.FindByOrganizationId(organizationId);
        if (!retrieved.HasValue)
        {
            return false;
        }

        return retrieved.Value.Roles.HasRole(TenantRoles.Owner);
    }

    private static bool IsNotAnonymousUser(Identifier userId)
    {
        return userId != CallerConstants.AnonymousUserId;
    }
}