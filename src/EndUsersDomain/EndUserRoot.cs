using Common;
using Common.Extensions;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Authorization;
using Domain.Interfaces.Entities;
using Domain.Interfaces.ValueObjects;
using Domain.Shared;

namespace EndUsersDomain;

public sealed class EndUserRoot : AggregateRootBase
{
    public static Result<EndUserRoot, Error> Create(IRecorder recorder, IIdentifierFactory idFactory,
        UserClassification classification)
    {
        var root = new EndUserRoot(recorder, idFactory);
        root.RaiseCreateEvent(EndUsersDomain.Events.Created.Create(root.Id, classification));
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

    private bool IsMachine => Classification == UserClassification.Machine;

    public bool IsPerson => Classification == UserClassification.Person;

    public bool IsRegistered => Status == UserStatus.Registered;

    public Memberships Memberships { get; } = new();

    public Roles Roles { get; private set; } = Roles.Create();

    public UserStatus Status { get; private set; }

    public static AggregateRootFactory<EndUserRoot> Rehydrate()
    {
        return (identifier, container, _) => new EndUserRoot(container.Resolve<IRecorder>(),
            container.Resolve<IIdentifierFactory>(), identifier);
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
        }

        return Result.Ok;
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event, bool isReconstituting)
    {
        switch (@event)
        {
            case Events.Created created:
            {
                Access = created.Access.ToEnumOrDefault(UserAccess.Enabled);
                Status = created.Status.ToEnumOrDefault(UserStatus.Unregistered);
                Classification = created.Classification.ToEnumOrDefault(UserClassification.Person);
                Features = Features.Create();
                Roles = Roles.Create();
                return Result.Ok;
            }

            case Events.Registered changed:
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

            case Events.MembershipAdded added:
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

            case Events.MembershipDefaultChanged changed:
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

            case Events.MembershipRoleAssigned added:
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

            case Events.MembershipFeatureAssigned added:
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

            case Events.PlatformRoleAssigned added:
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

            case Events.PlatformFeatureAssigned added:
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

            default:
                return HandleUnKnownStateChangedEvent(@event);
        }
    }

    public Result<Error> AddMembership(Identifier organizationId, Roles tenantRoles, Features tenantFeatures)
    {
        if (!IsRegistered)
        {
            return Error.RuleViolation(Resources.EndUserRoot_NotRegistered);
        }

        var existing = Memberships.FindByOrganizationId(organizationId);
        if (existing.HasValue)
        {
            return Result.Ok;
        }

        var isDefault = Memberships.HasNone();
        var added = RaiseChangeEvent(
            EndUsersDomain.Events.MembershipAdded.Create(Id, organizationId, isDefault, tenantRoles, tenantFeatures));
        if (!added.IsSuccessful)
        {
            return added.Error;
        }

        if (!isDefault)
        {
            var defaultMembership = Memberships.DefaultMembership;
            var addedMembership = Memberships.FindByOrganizationId(organizationId);
            return RaiseChangeEvent(
                EndUsersDomain.Events.MembershipDefaultChanged.Create(Id, defaultMembership.Id,
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
                        EndUsersDomain.Events.MembershipFeatureAssigned.Create(Id, organizationId, membership.Value.Id,
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
                        EndUsersDomain.Events.MembershipRoleAssigned.Create(Id, organizationId, membership.Value.Id,
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
                        EndUsersDomain.Events.PlatformFeatureAssigned.Create(Id, feature));
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
                        EndUsersDomain.Events.PlatformRoleAssigned.Create(Id, role));
                if (!addedRole.IsSuccessful)
                {
                    return addedRole.Error;
                }
            }
        }

        return Result.Ok;
    }

    /// <summary>
    ///     EXTEND: change this to assign initial roles and features for persons and machines
    /// </summary>
    public static (Roles PlatformRoles, Features PlatformFeatures, Roles TenantRoles, Features TenantFeatures)
        GetInitialRolesAndFeatures(UserClassification classification, bool isAuthenticated,
            Optional<EmailAddress> username, Optional<List<EmailAddress>> permittedOperators)
    {
        var platformRoles = Roles.Create();
        platformRoles = platformRoles.Add(PlatformRoles.Standard).Value;
        if (username.HasValue && permittedOperators.HasValue)
        {
            if (permittedOperators.Value
                .Select(x => x.Address)
                .ContainsIgnoreCase(username.Value))
            {
                platformRoles = platformRoles.Add(PlatformRoles.Operations).Value;
            }
        }

        var tenantRoles = Roles.Create();
        tenantRoles = tenantRoles.Add(TenantRoles.Member).Value;

        var platformFeatures = Features.Create();
        var tenantFeatures = Features.Create();
        if (classification == UserClassification.Machine)
        {
            platformFeatures = platformFeatures.Add(isAuthenticated
                ? PlatformFeatures.PaidTrial
                : PlatformFeatures.Basic).Value;
            tenantFeatures = tenantFeatures.Add(isAuthenticated
                ? TenantFeatures.PaidTrial
                : TenantFeatures.Basic).Value;
        }
        else
        {
            platformFeatures = platformFeatures.Add(PlatformFeatures.PaidTrial).Value;
            tenantFeatures = tenantFeatures.Add(TenantFeatures.PaidTrial).Value;
        }

        return (platformRoles, platformFeatures, tenantRoles, tenantFeatures);
    }

    public Result<Error> Register(Roles roles, Features levels, Optional<EmailAddress> username)
    {
        if (Status != UserStatus.Unregistered)
        {
            return Error.RuleViolation(Resources.EndUserRoot_AlreadyRegistered);
        }

        return RaiseChangeEvent(EndUsersDomain.Events.Registered.Create(Id, username, Classification,
            UserAccess.Enabled, UserStatus.Registered, roles, levels));
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

    public Optional<Membership> FindMembership(Identifier organizationId)
    {
        return Memberships.FindByOrganizationId(organizationId);
    }
}