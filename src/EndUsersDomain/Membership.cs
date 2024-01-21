using Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Authorization;
using Domain.Interfaces.Entities;
using Domain.Shared;

namespace EndUsersDomain;

public sealed class Membership : EntityBase
{
    internal static readonly FeatureLevel DefaultFeature = TenantFeatures.Basic;

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

    public Optional<Identifier> OrganizationId { get; private set; } = Optional<Identifier>.None;

    public Roles Roles { get; private set; } = Roles.Empty;

    public Optional<Identifier> RootId { get; private set; } = Optional<Identifier>.None;

    protected override Result<Error> OnStateChanged(IDomainEvent @event)
    {
        switch (@event)
        {
            case Events.MembershipAdded added:
            {
                RootId = added.RootId.ToId();
                OrganizationId = added.OrganizationId.ToId();
                IsDefault = added.IsDefault;
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

            case Events.MembershipDefaultChanged changed:
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

            case Events.MembershipRoleAssigned added:
            {
                var role = Roles.Add(added.Role);
                if (!role.IsSuccessful)
                {
                    return role.Error;
                }

                Roles = role.Value;
                return Result.Ok;
            }

            case Events.MembershipFeatureAssigned added:
            {
                var feature = Features.Add(added.Feature);
                if (!feature.IsSuccessful)
                {
                    return feature.Error;
                }

                Features = feature.Value;

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

        if (!Features.HasFeature(DefaultFeature))
        {
            return Error.RuleViolation(Resources.Membership_MissingDefaultFeature);
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