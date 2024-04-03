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
using Domain.Services.Shared.DomainServices;
using Domain.Shared;

namespace EndUsersDomain;

public delegate Task<Result<Error>> InvitationCallback(Identifier inviterId, string token);

public sealed class EndUserRoot : AggregateRootBase
{
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

    public Features Features { get; private set; } = Features.Create();

    public GuestInvitation GuestInvitation { get; private set; } = GuestInvitation.Empty;

    private bool IsMachine => Classification == UserClassification.Machine;

    public bool IsPerson => Classification == UserClassification.Person;

    public bool IsRegistered => Status == UserStatus.Registered;

    public Memberships Memberships { get; } = new();

    public Roles Roles { get; private set; } = Roles.Create();

    public UserStatus Status { get; private set; }

    public static AggregateRootFactory<EndUserRoot> Rehydrate()
    {
        return (identifier, container, _) => new EndUserRoot(container.GetRequiredService<IRecorder>(),
            container.GetRequiredService<IIdentifierFactory>(), identifier);
    }

    public override Result<Error> EnsureInvariants()
    {
        var ensureInvariants = base.EnsureInvariants();
        if (!ensureInvariants.IsSuccessful)
        {
            return ensureInvariants.Error;
        }

        var memberships = Memberships.EnsureInvariants();
        if (!memberships.IsSuccessful)
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
                Access = created.Access.ToEnumOrDefault(UserAccess.Enabled);
                Status = created.Status.ToEnumOrDefault(UserStatus.Unregistered);
                Classification = created.Classification.ToEnumOrDefault(UserClassification.Person);
                Features = Features.Create();
                Roles = Roles.Create();
                return Result.Ok;
            }

            case Registered changed:
            {
                Access = changed.Access.ToEnumOrDefault(UserAccess.Enabled);
                Status = changed.Status.ToEnumOrDefault(UserStatus.Unregistered);
                Classification = changed.Classification.ToEnumOrDefault(UserClassification.Person);

                var roles = Roles.Create(changed.Roles.ToArray());
                if (!roles.IsSuccessful)
                {
                    return roles.Error;
                }

                Roles = roles.Value;
                var features = Features.Create(changed.Features.ToArray());
                if (!features.IsSuccessful)
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
                if (!membership.IsSuccessful)
                {
                    return membership.Error;
                }

                Memberships.Add(membership.Value);
                Recorder.TraceDebug(null,
                    "EndUser {Id} added membership {Membership} to organization {Organization}", Id,
                    membership.Value.Id, added.OrganizationId);
                return Result.Ok;
            }

            case MembershipDefaultChanged changed:
            {
                var fromMembership = Memberships.FindByMembershipId(changed.FromMembershipId.ToId());
                if (!fromMembership.HasValue)
                {
                    return Error.RuleViolation(Resources.EndUserRoot_MissingMembership);
                }

                var from = RaiseEventToChildEntity(changed, fromMembership.Value);
                if (!from.IsSuccessful)
                {
                    return from.Error;
                }

                var toMembership = Memberships.FindByMembershipId(changed.ToMembershipId.ToId());
                if (!toMembership.HasValue)
                {
                    return Error.RuleViolation(Resources.EndUserRoot_NoMembership);
                }

                var to = RaiseEventToChildEntity(changed, toMembership.Value);
                if (!to.IsSuccessful)
                {
                    return to.Error;
                }

                Recorder.TraceDebug(null,
                    "EndUser {Id} changed default membership from {FromMembership} to {ToMembership}", Id,
                    changed.FromMembershipId, changed.ToMembershipId);

                return Result.Ok;
            }

            case MembershipRoleAssigned added:
            {
                var membershipId = added.MembershipId.ToId();
                var membership = Memberships.FindByMembershipId(membershipId);
                if (!membership.HasValue)
                {
                    return Error.RuleViolation(Resources.EndUserRoot_NoMembership);
                }

                var assigned = RaiseEventToChildEntity(added, membership.Value);
                if (!assigned.IsSuccessful)
                {
                    return assigned.Error;
                }

                Recorder.TraceDebug(null, "EndUser {Id} added role {Role} to membership {MembershipId}", Id, added.Role,
                    membershipId);
                return Result.Ok;
            }

            case MembershipFeatureAssigned added:
            {
                var membershipId = added.MembershipId.ToId();
                var membership = Memberships.FindByMembershipId(membershipId);
                if (!membership.HasValue)
                {
                    return Error.RuleViolation(Resources.EndUserRoot_NoMembership);
                }

                var assigned = RaiseEventToChildEntity(added, membership.Value);
                if (!assigned.IsSuccessful)
                {
                    return assigned.Error;
                }

                Recorder.TraceDebug(null, "EndUser {Id} added feature {Role} to membership {MembershipId}", Id,
                    added.Feature, membershipId);
                return Result.Ok;
            }

            case PlatformRoleAssigned added:
            {
                var roles = Roles.Add(added.Role);
                if (!roles.IsSuccessful)
                {
                    return roles.Error;
                }

                Roles = roles.Value;
                Recorder.TraceDebug(null, "EndUser {Id} added role {Role}", Id, added.Role);
                return Result.Ok;
            }

            case PlatformRoleUnassigned added:
            {
                var roles = Roles.Remove(added.Role);
                Roles = roles;
                Recorder.TraceDebug(null, "EndUser {Id} removed role {Role}", Id, added.Role);
                return Result.Ok;
            }

            case PlatformFeatureAssigned added:
            {
                var features = Features.Add(added.Feature);
                if (!features.IsSuccessful)
                {
                    return features.Error;
                }

                Features = features.Value;
                Recorder.TraceDebug(null, "EndUser {Id} added feature {Feature}", Id, added.Feature);
                return Result.Ok;
            }

            case GuestInvitationCreated added:
            {
                var inviteeEmailAddress = EmailAddress.Create(added.EmailAddress);
                if (!inviteeEmailAddress.IsSuccessful)
                {
                    return inviteeEmailAddress.Error;
                }

                var invited = GuestInvitation.IsStillOpen
                    ? GuestInvitation.Renew(added.Token, inviteeEmailAddress.Value)
                    : GuestInvitation.Invite(added.Token, inviteeEmailAddress.Value, added.InvitedById.ToId());
                if (!invited.IsSuccessful)
                {
                    return invited.Error;
                }

                GuestInvitation = invited.Value;
                Recorder.TraceDebug(null, "EndUser {Id} invited as a guest by {InvitedBy}", Id, added.InvitedById);
                return Result.Ok;
            }

            case GuestInvitationAccepted changed:
            {
                var acceptedEmailAddress = EmailAddress.Create(changed.AcceptedEmailAddress);
                if (!acceptedEmailAddress.IsSuccessful)
                {
                    return acceptedEmailAddress.Error;
                }

                var accepted = GuestInvitation.Accept(acceptedEmailAddress.Value);
                if (!accepted.IsSuccessful)
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
        if (!verified.IsSuccessful)
        {
            return verified.Error;
        }

        return RaiseChangeEvent(EndUsersDomain.Events.GuestInvitationAccepted(Id, emailAddress));
    }

    public Result<Error> AddMembership(Identifier organizationId, Roles tenantRoles, Features tenantFeatures)
    {
        //TODO: check that the adder is a member of this organization, and an owner of it

        var existing = Memberships.FindByOrganizationId(organizationId);
        if (existing.HasValue)
        {
            return Result.Ok;
        }

        var isDefault = Memberships.HasNone();
        var added = RaiseChangeEvent(
            EndUsersDomain.Events.MembershipAdded(Id, organizationId, isDefault, tenantRoles, tenantFeatures));
        if (!added.IsSuccessful)
        {
            return added.Error;
        }

        if (!isDefault)
        {
            var defaultMembership = Memberships.DefaultMembership;
            var addedMembership = Memberships.FindByOrganizationId(organizationId);
            return RaiseChangeEvent(
                EndUsersDomain.Events.MembershipDefaultChanged(Id, defaultMembership.Id,
                    addedMembership.Value.Id));
        }

        return Result.Ok;
    }

    public Result<Error> AssignMembershipFeatures(EndUserRoot assigner, Identifier organizationId,
        Features tenantFeatures)
    {
        if (!IsOrganizationOwner(assigner, organizationId))
        {
            return Error.RuleViolation(Resources.EndUserRoot_NotOrganizationOwner);
        }

        var membership = Memberships.FindByOrganizationId(organizationId);
        if (!membership.HasValue)
        {
            return Error.RuleViolation(Resources.EndUserRoot_NoMembership.Format(organizationId));
        }

        if (tenantFeatures.HasAny())
        {
            foreach (var feature in tenantFeatures.Items)
            {
                if (!TenantFeatures.IsTenantAssignableFeature(feature.Identifier))
                {
                    return Error.RuleViolation(
                        Resources.EndUserRoot_UnassignableTenantFeature.Format(feature.Identifier));
                }

                var addedFeature =
                    RaiseChangeEvent(
                        EndUsersDomain.Events.MembershipFeatureAssigned(Id, organizationId, membership.Value.Id,
                            feature));
                if (!addedFeature.IsSuccessful)
                {
                    return addedFeature.Error;
                }
            }
        }

        return Result.Ok;
    }

    public Result<Membership, Error> AssignMembershipRoles(EndUserRoot assigner, Identifier organizationId,
        Roles tenantRoles)
    {
        if (!IsOrganizationOwner(assigner, organizationId))
        {
            return Error.RuleViolation(Resources.EndUserRoot_NotOrganizationOwner);
        }

        var membership = Memberships.FindByOrganizationId(organizationId);
        if (!membership.HasValue)
        {
            return Error.RuleViolation(Resources.EndUserRoot_NoMembership.Format(organizationId));
        }

        if (tenantRoles.HasAny())
        {
            foreach (var role in tenantRoles.Items)
            {
                if (!TenantRoles.IsTenantAssignableRole(role.Identifier))
                {
                    return Error.RuleViolation(Resources.EndUserRoot_UnassignableTenantRole.Format(role.Identifier));
                }

                var addedRole =
                    RaiseChangeEvent(
                        EndUsersDomain.Events.MembershipRoleAssigned(Id, organizationId, membership.Value.Id,
                            role));
                if (!addedRole.IsSuccessful)
                {
                    return addedRole.Error;
                }
            }
        }

        return membership.Value;
    }

    public Result<Error> AssignPlatformFeatures(EndUserRoot assigner, Features platformFeatures)
    {
        if (!IsPlatformOperator(assigner))
        {
            return Error.RuleViolation(Resources.EndUserRoot_NotOperator);
        }

        if (platformFeatures.HasAny())
        {
            foreach (var feature in platformFeatures.Items)
            {
                if (!PlatformFeatures.IsPlatformAssignableFeature(feature.Identifier))
                {
                    return Error.RuleViolation(
                        Resources.EndUserRoot_UnassignablePlatformFeature.Format(feature.Identifier));
                }

                var addedFeature =
                    RaiseChangeEvent(
                        EndUsersDomain.Events.PlatformFeatureAssigned(Id, feature));
                if (!addedFeature.IsSuccessful)
                {
                    return addedFeature.Error;
                }
            }
        }

        return Result.Ok;
    }

    public Result<Error> AssignPlatformRoles(EndUserRoot assigner, Roles platformRoles)
    {
        if (!IsPlatformOperator(assigner))
        {
            return Error.RuleViolation(Resources.EndUserRoot_NotOperator);
        }

        if (platformRoles.HasAny())
        {
            foreach (var role in platformRoles.Items)
            {
                if (!PlatformRoles.IsPlatformAssignableRole(role.Identifier))
                {
                    return Error.RuleViolation(Resources.EndUserRoot_UnassignablePlatformRole.Format(role.Identifier));
                }

                var addedRole =
                    RaiseChangeEvent(
                        EndUsersDomain.Events.PlatformRoleAssigned(Id, role));
                if (!addedRole.IsSuccessful)
                {
                    return addedRole.Error;
                }
            }
        }

        return Result.Ok;
    }

    public Optional<Membership> FindMembership(Identifier organizationId)
    {
        return Memberships.FindByOrganizationId(organizationId);
    }

    /// <summary>
    ///     EXTEND: change this to assign initial roles and features for persons and machines
    /// </summary>
    public static (Roles PlatformRoles, Features PlatformFeatures, Roles TenantRoles, Features TenantFeatures)
        GetInitialRolesAndFeatures(RolesAndFeaturesUseCase useCase, bool isAuthenticated,
            EmailAddress? username = null, List<EmailAddress>? permittedOperators = null)
    {
        var platformRoles = Roles.Create();
        platformRoles = platformRoles.Add(PlatformRoles.Standard).Value;
        if (username.Exists() && permittedOperators.Exists())
        {
            if (permittedOperators
                .Select(x => x.Address)
                .ContainsIgnoreCase(username))
            {
                platformRoles = platformRoles.Add(PlatformRoles.Operations).Value;
            }
        }

        var platformFeatures = Features.Create();
        Roles tenantRoles;
        var tenantFeatures = Features.Create();
        switch (useCase)
        {
            case RolesAndFeaturesUseCase.CreatingMachine:
                platformFeatures = platformFeatures.Add(isAuthenticated
                    ? PlatformFeatures.PaidTrial
                    : PlatformFeatures.Basic).Value;
                tenantRoles = Roles.Create(TenantRoles.Owner, TenantRoles.Member).Value;
                tenantFeatures = tenantFeatures.Add(isAuthenticated
                    ? TenantFeatures.PaidTrial
                    : TenantFeatures.Basic).Value;
                break;

            case RolesAndFeaturesUseCase.CreatingPerson:
            case RolesAndFeaturesUseCase.CreatingOrg:
                platformFeatures = platformFeatures.Add(PlatformFeatures.PaidTrial).Value;
                tenantRoles = Roles.Create(TenantRoles.BillingAdmin, TenantRoles.Owner, TenantRoles.Member).Value;
                tenantFeatures = tenantFeatures.Add(TenantFeatures.PaidTrial).Value;
                break;

            case RolesAndFeaturesUseCase.InvitingMemberToOrg:
                platformFeatures = platformFeatures.Add(PlatformFeatures.PaidTrial).Value;
                tenantFeatures = tenantFeatures.Add(TenantFeatures.PaidTrial).Value;
                tenantRoles = Roles.Create(TenantRoles.Member).Value;
                break;

            case RolesAndFeaturesUseCase.InvitingMachineToCreatorOrg:
                platformFeatures = platformFeatures.Add(PlatformFeatures.PaidTrial).Value;
                tenantRoles = Roles.Create(TenantRoles.Member).Value;
                tenantFeatures = tenantFeatures.Add(TenantFeatures.PaidTrial).Value;
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(useCase), useCase, null);
        }

        return (platformRoles, platformFeatures, tenantRoles, tenantFeatures);
    }

    public PersonName GuessGuestInvitationName()
    {
        return GuestInvitation.InviteeEmailAddress!.GuessPersonFullName();
    }

    public async Task<Result<Error>> InviteGuestAsync(ITokensService tokensService, Identifier inviterId,
        EmailAddress inviteeEmailAddress, InvitationCallback onInvited)
    {
        if (IsRegistered)
        {
            return Result.Ok;
        }

        var token = tokensService.CreateGuestInvitationToken();
        var raised =
            RaiseChangeEvent(
                EndUsersDomain.Events.GuestInvitationCreated(Id, token, inviteeEmailAddress, inviterId));
        if (!raised.IsSuccessful)
        {
            return raised.Error;
        }

        return await onInvited(inviterId, token);
    }

    public Result<Error> Register(Roles roles, Features levels, Optional<EmailAddress> username)
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
                if (!accepted.IsSuccessful)
                {
                    return accepted.Error;
                }
            }
        }

        return RaiseChangeEvent(EndUsersDomain.Events.Registered(Id, username, Classification,
            UserAccess.Enabled, UserStatus.Registered, roles, levels));
    }

    public async Task<Result<Error>> ReInviteGuestAsync(ITokensService tokensService, Identifier inviterId,
        InvitationCallback onInvited)
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

#if TESTINGONLY
    public void TestingOnly_ExpireGuestInvitation()
    {
        GuestInvitation = GuestInvitation.TestingOnly_ExpireNow();
    }
#endif

#if TESTINGONLY
    public void TestingOnly_InviteGuest(EmailAddress emailAddress)
    {
        GuestInvitation = GuestInvitation.Invite("atoken", emailAddress, "aninviter".ToId()).Value;
    }
#endif

    public Result<Error> UnassignPlatformRoles(EndUserRoot assigner, Roles platformRoles)
    {
        if (!IsPlatformOperator(assigner))
        {
            return Error.RuleViolation(Resources.EndUserRoot_NotOperator);
        }

        if (platformRoles.HasAny())
        {
            foreach (var role in platformRoles.Items)
            {
                if (!PlatformRoles.IsPlatformAssignableRole(role.Identifier))
                {
                    return Error.RuleViolation(Resources.EndUserRoot_UnassignablePlatformRole.Format(role.Identifier));
                }

                if (role.Identifier == PlatformRoles.Standard.Name)
                {
                    return Error.RuleViolation(
                        Resources.EndUserRoot_CannotUnassignBaselinePlatformRole
                            .Format(PlatformRoles.Standard.Name));
                }

                if (!Roles.HasRole(role.Identifier))
                {
                    return Error.RuleViolation(
                        Resources.EndUserRoot_CannotUnassignUnassignedRole.Format(role.Identifier));
                }

                var removedRole =
                    RaiseChangeEvent(
                        EndUsersDomain.Events.PlatformRoleUnassigned(Id, role));
                if (!removedRole.IsSuccessful)
                {
                    return removedRole.Error;
                }
            }
        }

        return Result.Ok;
    }

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

    private static bool IsPlatformOperator(EndUserRoot assigner)
    {
        return assigner.Roles.HasRole(PlatformRoles.Operations);
    }

    private static bool IsOrganizationOwner(EndUserRoot assigner, Identifier organizationId)
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