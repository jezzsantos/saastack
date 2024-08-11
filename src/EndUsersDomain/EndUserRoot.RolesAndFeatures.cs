using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Authorization;
using Domain.Shared;
using Domain.Shared.Subscriptions;

namespace EndUsersDomain;

public delegate Result<Error> AssignRolesAction(Roles roles, Identifier assignerId, Identifier assigneeId);

public delegate Result<Error> UnassignRolesAction(Roles roles, Identifier unassignerId, Identifier unassigneeId);

public delegate Result<Error> AssignTenantRolesAction(Roles roles, Identifier assignerId, Identifier assigneeId,
    Identifier membershipId);

public delegate Result<Error> UnassignTenantRolesAction(Roles roles, Identifier unassignerId, Identifier unassigneeId,
    Identifier membershipId);

public delegate Result<Error> AssignFeaturesAction(Features features, Identifier assignerId, Identifier assigneeId);

public delegate Result<Error> UnassignFeaturesAction(Features features, Identifier unassignerId,
    Identifier unassigneeId);

public delegate Result<Error> AssignTenantFeaturesAction(Features roles, Identifier assignerId, Identifier assigneeId,
    Identifier membershipId);

public delegate Result<Error> UnassignTenantFeaturesAction(Features roles, Identifier assignerId, Identifier assigneeId,
    Identifier membershipId);

#pragma warning disable SAASDDD018
#pragma warning disable SAASDDD010
#pragma warning disable SAASDDD014
partial class EndUserRoot
#pragma warning restore SAASDDD014
#pragma warning restore SAASDDD010
#pragma warning restore SAASDDD018
{
    public Result<Error> AssignMembershipFeatures(Identifier assignerId, Identifier organizationId,
        Features featuresToAssign, AssignTenantFeaturesAction onAssign)
    {
        var membership = Memberships.FindByOrganizationId(organizationId);
        if (!membership.HasValue)
        {
            return Error.RuleViolation(Resources.EndUserRoot_NoMembership.Format(organizationId));
        }

        var membershipId = membership.Value.Id;
        if (featuresToAssign.HasAny())
        {
            var assignedFeatures = Features.Empty;
            foreach (var feature in featuresToAssign.Items)
            {
                if (!TenantFeatures.IsTenantAssignableFeature(feature.Identifier))
                {
                    return Error.RuleViolation(
                        Resources.EndUserRoot_UnassignableTenantFeature.Format(feature.Identifier));
                }

                if (membership.Value.Features.HasFeature(feature))
                {
                    continue;
                }

                assignedFeatures = assignedFeatures.Add(feature).Value;
                var addedFeature =
                    RaiseChangeEvent(
                        EndUsersDomain.Events.MembershipFeatureAssigned(Id, organizationId, membership.Value.Id,
                            feature));
                if (addedFeature.IsFailure)
                {
                    return addedFeature.Error;
                }
            }

            onAssign(assignedFeatures, assignerId, Id, membershipId);
        }

        return Result.Ok;
    }

    public Result<Membership, Error> AssignMembershipRoles(EndUserRoot assigner, Identifier organizationId,
        Roles rolesToAssign, AssignTenantRolesAction onAssign)
    {
        if (!IsAnOrganizationOwner(assigner, organizationId))
        {
            return Error.RoleViolation(Resources.EndUserRoot_NotOrganizationOwner);
        }

        var membership = Memberships.FindByOrganizationId(organizationId);
        if (!membership.HasValue)
        {
            return Error.RuleViolation(Resources.EndUserRoot_NoMembership.Format(organizationId));
        }

        var membershipId = membership.Value.Id;
        if (rolesToAssign.HasNone())
        {
            return membership.Value;
        }

        var assignedRoles = Roles.Empty;
        foreach (var role in rolesToAssign.Items)
        {
            if (!TenantRoles.IsTenantAssignableRole(role.Identifier))
            {
                return Error.RuleViolation(Resources.EndUserRoot_UnassignableTenantRole.Format(role.Identifier));
            }

            if (membership.Value.Roles.HasRole(role))
            {
                continue;
            }

            assignedRoles = assignedRoles.Add(role).Value;
            var addedRole =
                RaiseChangeEvent(
                    EndUsersDomain.Events.MembershipRoleAssigned(Id, organizationId, membership.Value.Id,
                        role));
            if (addedRole.IsFailure)
            {
                return addedRole.Error;
            }
        }

        onAssign(assignedRoles, assigner.Id, Id, membershipId);

        return membership.Value;
    }

    public Result<Error> AssignPlatformFeatures(Identifier assignerId, Features featuresToAssign,
        AssignFeaturesAction onAssign)
    {
        if (featuresToAssign.HasAny())
        {
            var assignedFeatures = Features.Empty;
            foreach (var feature in featuresToAssign.Items)
            {
                if (!PlatformFeatures.IsPlatformAssignableFeature(feature.Identifier))
                {
                    return Error.RuleViolation(
                        Resources.EndUserRoot_UnassignablePlatformFeature.Format(feature.Identifier));
                }

                if (Features.HasFeature(feature))
                {
                    continue;
                }

                assignedFeatures = assignedFeatures.Add(feature).Value;
                var addedFeature =
                    RaiseChangeEvent(
                        EndUsersDomain.Events.PlatformFeatureAssigned(Id, feature));
                if (addedFeature.IsFailure)
                {
                    return addedFeature.Error;
                }
            }

            onAssign(assignedFeatures, assignerId, Id);
        }

        return Result.Ok;
    }

    public Result<Error> AssignPlatformRoles(EndUserRoot assigner, Roles rolesToAssign, AssignRolesAction onAssign)
    {
        if (!IsOperations(assigner.Roles))
        {
            return Error.RuleViolation(Resources.EndUserRoot_NotOperator);
        }

        if (rolesToAssign.HasAny())
        {
            var assignedRoles = Roles.Empty;
            foreach (var role in rolesToAssign.Items)
            {
                if (!PlatformRoles.IsPlatformAssignableRole(role.Identifier))
                {
                    return Error.RuleViolation(Resources.EndUserRoot_UnassignablePlatformRole.Format(role.Identifier));
                }

                if (Roles.HasRole(role))
                {
                    continue;
                }

                assignedRoles = assignedRoles.Add(role).Value;
                var addedRole =
                    RaiseChangeEvent(
                        EndUsersDomain.Events.PlatformRoleAssigned(Id, role));
                if (addedRole.IsFailure)
                {
                    return addedRole.Error;
                }
            }

            onAssign(assignedRoles, assigner.Id, Id);
        }

        return Result.Ok;
    }

    public static (Roles PlatformRoles, Features PlatformFeatures, Roles TenantRoles, Features TenantFeatures)
        GetInitialRolesAndFeatures(RolesAndFeaturesUseCase useCase, bool isAuthenticated,
            EmailAddress? username = null, List<EmailAddress>? permittedOperators = null)
    {
        var platformRoles = Roles.Empty;
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

        // EXTEND: change this to assign initial roles and features for persons and machines
        const BillingSubscriptionTier initialTier = BillingSubscriptionTier.Standard;
        var (platformFeatures, tenantFeatures) = GetFeaturesForPlan(initialTier, null);
        Roles tenantRoles;
        switch (useCase)
        {
            case RolesAndFeaturesUseCase.CreatingMachine:
                tenantRoles = Roles.Create(TenantRoles.Owner, TenantRoles.Member).Value;
                if (!isAuthenticated)
                {
                    platformFeatures = platformFeatures.Remove(PlatformFeatures.PaidTrial);
                    tenantFeatures = tenantFeatures.Remove(TenantFeatures.PaidTrial);
                }

                break;

            case RolesAndFeaturesUseCase.CreatingPerson:
            case RolesAndFeaturesUseCase.CreatingOrg:
                tenantRoles = Roles.Create(TenantRoles.BillingAdmin, TenantRoles.Owner, TenantRoles.Member).Value;
                break;

            case RolesAndFeaturesUseCase.InvitingMemberToOrg:
                tenantRoles = Roles.Create(TenantRoles.Member).Value;
                break;

            case RolesAndFeaturesUseCase.InvitingMachineToCreatorOrg:
                tenantRoles = Roles.Create(TenantRoles.Member).Value;
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(useCase), useCase, null);
        }

        return (platformRoles, platformFeatures, tenantRoles, tenantFeatures);
    }

    public Result<Error> ResetMembershipFeatures(Identifier assignerId, Identifier organizationId,
        BillingSubscriptionTier tier, string planId, AssignFeaturesAction onAssignPlatformFeature,
        AssignTenantFeaturesAction onAssignTenantFeature)
    {
        if (!IsServiceAccount(assignerId))
        {
            return Error.RoleViolation(Resources.EndUserRoot_NotServiceAccount);
        }

        var (platformFeatures, tenantFeatures) = GetFeaturesForPlan(tier, planId);

        if (platformFeatures.Exists()
            && platformFeatures.HasAny())
        {
            var platformed = ResetPlatformFeatures(assignerId, platformFeatures, onAssignPlatformFeature);
            if (platformed.IsFailure)
            {
                return platformed.Error;
            }
        }

        if (tenantFeatures.Exists()
            && tenantFeatures.HasAny())
        {
            var tenanted = ResetMembershipFeatures(assignerId, organizationId, tenantFeatures,
                onAssignTenantFeature);
            if (tenanted.IsFailure)
            {
                return tenanted.Error;
            }
        }

        return Result.Ok;
    }

    public Result<Membership, Error> UnassignMembershipFeatures(Identifier unassignerId, Identifier organizationId,
        Features featuresToUnassign, UnassignTenantFeaturesAction onUnassign)
    {
        var membership = Memberships.FindByOrganizationId(organizationId);
        if (!membership.HasValue)
        {
            return Error.RuleViolation(Resources.EndUserRoot_NoMembership.Format(organizationId));
        }

        if (featuresToUnassign.HasNone())
        {
            return membership.Value;
        }

        var membershipId = membership.Value.Id;
        var unassignedFeatures = Features.Empty;
        foreach (var feature in featuresToUnassign.Items)
        {
            if (!TenantFeatures.IsTenantAssignableFeature(feature.Identifier))
            {
                return Error.RuleViolation(Resources.EndUserRoot_UnassignableTenantFeature.Format(feature.Identifier));
            }

            if (!membership.Value.Features.HasFeature(feature))
            {
                continue;
            }

            unassignedFeatures = unassignedFeatures.Add(feature).Value;
            var removedFeature =
                RaiseChangeEvent(
                    EndUsersDomain.Events.MembershipFeatureUnassigned(Id, organizationId, membership.Value.Id,
                        feature));
            if (removedFeature.IsFailure)
            {
                return removedFeature.Error;
            }
        }

        onUnassign(unassignedFeatures, unassignerId, Id, membershipId);

        return membership.Value;
    }

    public Result<Membership, Error> UnassignMembershipRoles(EndUserRoot unassigner, Identifier organizationId,
        Roles rolesToUnassign, UnassignTenantRolesAction onUnassign)
    {
        if (!IsAnOrganizationOwner(unassigner, organizationId))
        {
            return Error.RoleViolation(Resources.EndUserRoot_NotOrganizationOwner);
        }

        var membership = Memberships.FindByOrganizationId(organizationId);
        if (!membership.HasValue)
        {
            return Error.RuleViolation(Resources.EndUserRoot_NoMembership.Format(organizationId));
        }

        if (rolesToUnassign.HasNone())
        {
            return membership.Value;
        }

        var membershipId = membership.Value.Id;
        var unassignedRoles = Roles.Empty;
        foreach (var role in rolesToUnassign.Items)
        {
            if (!TenantRoles.IsTenantAssignableRole(role.Identifier))
            {
                return Error.RuleViolation(Resources.EndUserRoot_UnassignableTenantRole.Format(role.Identifier));
            }

            if (!membership.Value.Roles.HasRole(role))
            {
                continue;
            }

            unassignedRoles = unassignedRoles.Add(role).Value;
            var removedRole =
                RaiseChangeEvent(
                    EndUsersDomain.Events.MembershipRoleUnassigned(Id, organizationId, membership.Value.Id,
                        role));
            if (removedRole.IsFailure)
            {
                return removedRole.Error;
            }
        }

        onUnassign(unassignedRoles, unassigner.Id, Id, membershipId);

        return membership.Value;
    }

    public Result<Error> UnassignPlatformFeatures(Identifier unassignerId, Features featuresToUnassign,
        UnassignFeaturesAction onUnassign)
    {
        if (featuresToUnassign.HasAny())
        {
            var unassignedFeatures = Features.Empty;
            foreach (var feature in featuresToUnassign.Items)
            {
                if (!PlatformFeatures.IsPlatformAssignableFeature(feature.Identifier))
                {
                    return Error.RuleViolation(
                        Resources.EndUserRoot_UnassignablePlatformFeature.Format(feature.Identifier));
                }

                if (feature.Identifier == PlatformFeatures.Basic.Name)
                {
                    return Error.RuleViolation(
                        Resources.EndUserRoot_CannotUnassignBaselinePlatformFeature
                            .Format(PlatformFeatures.Basic.Name));
                }

                if (!Features.HasFeature(feature))
                {
                    continue;
                }

                unassignedFeatures = unassignedFeatures.Add(feature).Value;
                var removedFeature = RaiseChangeEvent(EndUsersDomain.Events.PlatformFeatureUnassigned(Id, feature));
                if (removedFeature.IsFailure)
                {
                    return removedFeature.Error;
                }
            }

            onUnassign(unassignedFeatures, unassignerId, Id);
        }

        return Result.Ok;
    }

    public Result<Error> UnassignPlatformRoles(EndUserRoot assigner, Roles rolesToUnassign,
        UnassignRolesAction onUnassign)
    {
        if (!IsOperations(assigner.Roles))
        {
            return Error.RuleViolation(Resources.EndUserRoot_NotOperator);
        }

        if (rolesToUnassign.HasAny())
        {
            var unassignedRoles = Roles.Empty;
            foreach (var role in rolesToUnassign.Items)
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

                if (!Roles.HasRole(role))
                {
                    continue;
                }

                unassignedRoles = unassignedRoles.Add(role).Value;
                var removedRole = RaiseChangeEvent(EndUsersDomain.Events.PlatformRoleUnassigned(Id, role));
                if (removedRole.IsFailure)
                {
                    return removedRole.Error;
                }
            }

            onUnassign(unassignedRoles, assigner.Id, Id);
        }

        return Result.Ok;
    }

    // ReSharper disable once UnusedParameter.Local
    private static (Features Platform, Features Tenant) GetFeaturesForPlan(BillingSubscriptionTier tier, string? planId)
    {
        Features platformFeatures;
        Features tenantFeatures;

        // EXTEND: modify this to define the new features that a user gets when the subscription tier/plan changes
        switch (tier)
        {
            case BillingSubscriptionTier.Unsubscribed:
                platformFeatures = Features.Create(PlatformFeatures.Basic).Value;
                tenantFeatures = Features.Create(TenantFeatures.Basic).Value;
                break;

            case BillingSubscriptionTier.Standard:
                platformFeatures = Features.Create(PlatformFeatures.PaidTrial).Value;
                tenantFeatures = Features.Create(TenantFeatures.PaidTrial).Value;
                break;

            case BillingSubscriptionTier.Professional:
                platformFeatures = Features.Create(PlatformFeatures.Paid2).Value;
                tenantFeatures = Features.Create(TenantFeatures.Paid2).Value;
                break;

            case BillingSubscriptionTier.Enterprise:
                platformFeatures = Features.Create(PlatformFeatures.Paid3).Value;
                tenantFeatures = Features.Create(TenantFeatures.Paid3).Value;
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(tier), tier, null);
        }

        return (platformFeatures, tenantFeatures);
    }

    private Result<Error> ResetMembershipFeatures(Identifier assignerId, Identifier organizationId,
        Features features, AssignTenantFeaturesAction onAssign)
    {
        var membership = Memberships.FindByOrganizationId(organizationId);
        if (!membership.HasValue)
        {
            return Error.RuleViolation(Resources.EndUserRoot_NoMembership.Format(organizationId));
        }

        var membershipId = membership.Value.Id;
        var featuresToReset = features;
        if (!features.HasFeature(TenantFeatures.Basic))
        {
            features = features.Add(TenantFeatures.Basic).Value;
        }

        var reset = RaiseChangeEvent(
            EndUsersDomain.Events.MembershipFeaturesReset(Id, assignerId, organizationId, membershipId,
                featuresToReset));
        if (reset.IsFailure)
        {
            return reset.Error;
        }

        var assigned = onAssign(features, assignerId, Id, membershipId);
        if (assigned.IsFailure)
        {
            return assigned.Error;
        }

        return Result.Ok;
    }

    private Result<Error> ResetPlatformFeatures(Identifier assignerId, Features features,
        AssignFeaturesAction onAssign)
    {
        var featuresToReset = features;
        if (!features.HasFeature(PlatformFeatures.Basic))
        {
            features = features.Add(PlatformFeatures.Basic).Value;
        }

        var reset = RaiseChangeEvent(EndUsersDomain.Events.PlatformFeaturesReset(Id, assignerId, featuresToReset));
        if (reset.IsFailure)
        {
            return reset.Error;
        }

        var assigned = onAssign(features, assignerId, Id);
        if (assigned.IsFailure)
        {
            return assigned.Error;
        }

        return Result.Ok;
    }
}