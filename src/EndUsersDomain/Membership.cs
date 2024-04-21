using Common;
using Common.Extensions;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.EndUsers;
using Domain.Interfaces.Authorization;
using Domain.Interfaces.Entities;
using Domain.Shared;
using Domain.Shared.Organizations;

namespace EndUsersDomain;

public sealed class Membership : EntityBase
{
    internal static readonly FeatureLevel DefaultFeature = TenantFeatures.Basic;
    internal static readonly RoleLevel DefaultRole = TenantRoles.Member;

    public static Result<Membership, Error> Create(IRecorder recorder, IIdentifierFactory idFactory,
        RootEventHandler rootEventHandler)
    {
        return new Membership(recorder, idFactory, rootEventHandler);
    }

    private Membership(IRecorder recorder, IIdentifierFactory idFactory,
        RootEventHandler rootEventHandler) : base(recorder, idFactory, rootEventHandler)
    {
    }

    public Features Features { get; private set; } = Features.Empty;

    public bool IsDefault { get; private set; }

    public bool IsShared => Ownership is { HasValue: true, Value: OrganizationOwnership.Shared };

    public Optional<Identifier> OrganizationId { get; private set; } = Optional<Identifier>.None;

    public Optional<OrganizationOwnership> Ownership { get; private set; }

    public Roles Roles { get; private set; } = Roles.Empty;

    public Optional<Identifier> RootId { get; private set; } = Optional<Identifier>.None;

    protected override Result<Error> OnStateChanged(IDomainEvent @event)
    {
        switch (@event)
        {
            case MembershipAdded added:
            {
                RootId = added.RootId.ToId();
                OrganizationId = added.OrganizationId.ToId();
                IsDefault = added.IsDefault;
                Ownership = added.Ownership;
                var roles = Roles.Create(added.Roles.ToArray());
                if (!roles.IsSuccessful)
                {
                    return roles.Error;
                }

                Roles = roles.Value;
                var features = Features.Create(added.Features.ToArray());
                if (!features.IsSuccessful)
                {
                    return features.Error;
                }

                Features = features.Value;
                return Result.Ok;
            }

            case DefaultMembershipChanged changed:
            {
                if (changed.FromMembershipId == Id)
                {
                    IsDefault = false;
                }

                if (changed.ToMembershipId == Id)
                {
                    IsDefault = true;
                }

                return Result.Ok;
            }

            case MembershipRoleAssigned assigned:
            {
                var roles = Roles.Add(assigned.Role);
                if (!roles.IsSuccessful)
                {
                    return roles.Error;
                }

                Roles = roles.Value;
                return Result.Ok;
            }

            case MembershipRoleUnassigned unassigned:
            {
                var roles = Roles.Remove(unassigned.Role);
                Roles = roles;
                return Result.Ok;
            }

            case MembershipFeatureAssigned assigned:
            {
                var features = Features.Add(assigned.Feature);
                if (!features.IsSuccessful)
                {
                    return features.Error;
                }

                Features = features.Value;
                return Result.Ok;
            }

            default:
                return HandleUnKnownStateChangedEvent(@event);
        }
    }

    public override Result<Error> EnsureInvariants()
    {
        var ensureInvariants = base.EnsureInvariants();
        if (!ensureInvariants.IsSuccessful)
        {
            return ensureInvariants.Error;
        }

        if (!Roles.HasRole(DefaultRole))
        {
            return Error.RuleViolation(Resources.Membership_MissingDefaultRole.Format(DefaultRole.Name));
        }

        if (!Features.HasFeature(DefaultFeature))
        {
            return Error.RuleViolation(Resources.Membership_MissingDefaultFeature.Format(DefaultFeature.Name));
        }

        return Result.Ok;
    }

#if TESTINGONLY
    public void TestingOnly_ChangeOrganizationId(string organizationId)
    {
        OrganizationId = organizationId.ToId();
    }
#endif
}