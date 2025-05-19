using Common;
using Common.Extensions;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.Organizations;
using Domain.Interfaces;
using Domain.Interfaces.Authorization;
using Domain.Interfaces.Entities;
using Domain.Interfaces.Services;
using Domain.Interfaces.ValueObjects;
using Domain.Shared;
using Domain.Shared.EndUsers;
using Domain.Shared.Organizations;
using Domain.Shared.Subscriptions;
using JetBrains.Annotations;

namespace OrganizationsDomain;

public delegate Task<Result<Error>> DeleteAction(Identifier deleterId);

public sealed class OrganizationRoot : AggregateRootBase
{
    private readonly ITenantSettingService _tenantSettingService;

    public static Result<OrganizationRoot, Error> Create(IRecorder recorder, IIdentifierFactory idFactory,
        ITenantSettingService tenantSettingService, OrganizationOwnership ownership, Identifier createdBy,
        UserClassification classification, DisplayName name, DatacenterLocation hostRegion)
    {
        if (ownership == OrganizationOwnership.Shared
            && classification != UserClassification.Person)
        {
            return Error.RuleViolation(Resources.OrganizationRoot_Create_SharedRequiresPerson);
        }

        var root = new OrganizationRoot(recorder, idFactory, tenantSettingService);
        root.RaiseCreateEvent(OrganizationsDomain.Events.Created(root.Id, ownership, createdBy, name, hostRegion));
        return root;
    }

    private OrganizationRoot(IRecorder recorder, IIdentifierFactory idFactory,
        ITenantSettingService tenantSettingService) :
        base(recorder, idFactory)
    {
        _tenantSettingService = tenantSettingService;
    }

    private OrganizationRoot(IRecorder recorder, IIdentifierFactory idFactory,
        ITenantSettingService tenantSettingService,
        ISingleValueObject<string> identifier) : base(
        recorder, idFactory, identifier)
    {
        _tenantSettingService = tenantSettingService;
    }

    public Optional<Avatar> Avatar { get; private set; }

    public Optional<BillingSubscriber> BillingSubscriber { get; private set; }

    public Identifier CreatedById { get; private set; } = Identifier.Empty();

    public Memberships Memberships { get; private set; } = Memberships.Empty;

    public DisplayName Name { get; private set; } = DisplayName.Empty;

    public OrganizationOwnership Ownership { get; private set; }

    public DatacenterLocation HostRegion { get; private set; } = DatacenterLocations.Unknown;

    public Settings Settings { get; private set; } = Settings.Empty;

    [UsedImplicitly]
    public static AggregateRootFactory<OrganizationRoot> Rehydrate()
    {
        return (identifier, container, _) => new OrganizationRoot(container.GetRequiredService<IRecorder>(),
            container.GetRequiredService<IIdentifierFactory>(), container.GetRequiredService<ITenantSettingService>(),
            identifier);
    }

    public override Result<Error> EnsureInvariants()
    {
        var ensureInvariants = base.EnsureInvariants();
        if (ensureInvariants.IsFailure)
        {
            return ensureInvariants.Error;
        }

        return Result.Ok;
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event, bool isReconstituting)
    {
        switch (@event)
        {
            case Created created:
            {
                var name = DisplayName.Create(created.Name);
                if (name.IsFailure)
                {
                    return name.Error;
                }

                Name = name.Value;
                Ownership = created.Ownership;
                CreatedById = created.CreatedById.ToId();
                HostRegion = DatacenterLocations.FindOrDefault(created.HostRegion);
                return Result.Ok;
            }

            case SettingCreated created:
            {
                var value = Setting.From(created.StringValue, created.ValueType, created.IsEncrypted,
                    _tenantSettingService);
                if (value.IsFailure)
                {
                    return value.Error;
                }

                var settings = Settings.AddOrUpdate(created.Name, value.Value);
                if (settings.IsFailure)
                {
                    return settings.Error;
                }

                Settings = settings.Value;
                Recorder.TraceDebug(null, "Organization {Id} created settings", Id);
                return Result.Ok;
            }

            case SettingUpdated updated:
            {
                var to = Setting.From(updated.To, updated.ToType, updated.IsEncrypted, _tenantSettingService);
                if (to.IsFailure)
                {
                    return to.Error;
                }

                var settings = Settings.AddOrUpdate(updated.Name, to.Value);
                if (settings.IsFailure)
                {
                    return settings.Error;
                }

                Settings = settings.Value;
                Recorder.TraceDebug(null, "Organization {Id} created settings", Id);
                return Result.Ok;
            }

            case MembershipAdded added:
            {
                var membership = Membership.Create(added.RootId, added.UserId);
                if (membership.IsFailure)
                {
                    return membership.Error;
                }

                Memberships = Memberships.Add(membership.Value);
                Recorder.TraceDebug(null, "Organization {Id} added member {User}", Id, added.UserId);
                return Result.Ok;
            }

            case MembershipRemoved removed:
            {
                Memberships = Memberships.Remove(removed.UserId);
                Recorder.TraceDebug(null, "Organization {Id} removed member {User}", Id, removed.UserId);
                return Result.Ok;
            }

            case MemberInvited invited:
            {
                Recorder.TraceDebug(null, "Organization {Id} invited member {User}", Id,
                    (invited.EmailAddress.HasValue()
                        ? invited.EmailAddress
                        : invited.InvitedId)!);
                return Result.Ok;
            }

            case MemberUnInvited unInvited:
            {
                Recorder.TraceDebug(null, "Organization {Id} uninvited member {User}", Id, unInvited.UninvitedId);
                return Result.Ok;
            }

            case AvatarAdded added:
            {
                var avatar = Domain.Shared.Avatar.Create(added.AvatarId.ToId(), added.AvatarUrl);
                if (avatar.IsFailure)
                {
                    return avatar.Error;
                }

                Avatar = avatar.Value;
                Recorder.TraceDebug(null, "Organization {Id} added avatar {Image}", Id, avatar.Value.ImageId);
                return Result.Ok;
            }

            case AvatarRemoved _:
            {
                Avatar = Optional<Avatar>.None;
                Recorder.TraceDebug(null, "Organization {Id} removed avatar", Id);
                return Result.Ok;
            }

            case NameChanged changed:
            {
                var name = DisplayName.Create(changed.Name);
                if (name.IsFailure)
                {
                    return name.Error;
                }

                Name = name.Value;
                Recorder.TraceDebug(null, "Organization {Id} changed name", Id);
                return Result.Ok;
            }

            case RoleAssigned assigned:
            {
                Recorder.TraceDebug(null, "Organization {Id} assigned role {Role} to {User}", Id, assigned.Role,
                    assigned.UserId);
                return Result.Ok;
            }

            case RoleUnassigned unassigned:
            {
                Recorder.TraceDebug(null, "Organization {Id} unassigned role {Role} to {User}", Id, unassigned.Role,
                    unassigned.UserId);
                return Result.Ok;
            }

            case BillingSubscribed changed:
            {
                var subscriber =
                    Domain.Shared.Subscriptions.BillingSubscriber.Create(changed.SubscriptionId, changed.SubscriberId);
                if (subscriber.IsFailure)
                {
                    return subscriber.Error;
                }

                BillingSubscriber = subscriber.Value;
                Recorder.TraceDebug(null, "Organization {Id} subscribed to billing for {Subscriber}", Id,
                    changed.SubscriberId);
                return Result.Ok;
            }

            case BillingSubscriberChanged changed:
            {
                if (!BillingSubscriber.HasValue)
                {
                    return Error.RuleViolation(Resources.OrganizationRoot_NoSubscriber);
                }

                BillingSubscriber = BillingSubscriber.Value.ChangeSubscriber(changed.ToSubscriberId.ToId());
                Recorder.TraceDebug(null, "Organization {Id} changed billing subscriber from {From} to {To}", Id,
                    changed.FromSubscriberId, changed.ToSubscriberId);
                return Result.Ok;
            }

            default:
                return HandleUnKnownStateChangedEvent(@event);
        }
    }

    public Result<Error> AddMembership(Identifier userId)
    {
        if (Memberships.HasMember(userId))
        {
            return Result.Ok;
        }

        return RaiseChangeEvent(OrganizationsDomain.Events.MembershipAdded(Id, userId));
    }

    public Result<Error> AssignRoles(Identifier assignerId, Roles assignerRoles, Identifier userId, Roles rolesToAssign)
    {
        if (!IsOwner(assignerRoles))
        {
            return Error.RoleViolation(Resources.OrganizationRoot_UserNotOrgOwner);
        }

        if (!IsMember(userId))
        {
            return Error.RuleViolation(Resources.OrganizationRoot_UserNotMember);
        }

        foreach (var role in rolesToAssign.Items)
        {
            if (!TenantRoles.IsTenantAssignableRole(role))
            {
                return Error.RuleViolation(Resources.OrganizationRoot_RoleNotAssignable.Format(role));
            }

            var assigned = RaiseChangeEvent(OrganizationsDomain.Events.RoleAssigned(Id, assignerId, userId, role));
            if (assigned.IsFailure)
            {
                return assigned.Error;
            }
        }

        return Result.Ok;
    }

    public Result<Permission, Error> CanCancelBillingSubscription(Identifier cancellerId, Roles cancellerRoles)
    {
        if (IsBillingSubscriber(cancellerId))
        {
            return Permission.Allowed;
        }

        return Permission.Denied_Role(Resources.OrganizationRoot_UserNotBillingSubscriber);
    }

    public Result<Permission, Error> CanChangeBillingSubscriptionPlan(Identifier modifierId, Roles modifierRoles)
    {
        if (IsBillingAdminOrBillingSubscriber(modifierId, modifierRoles))
        {
            return Permission.Allowed;
        }

        return Permission.Denied_Role(Resources.OrganizationRoot_NotBillingSubscriberNorBillingAdmin);
    }

    public Result<Permission, Error> CanTransferBillingSubscription(Identifier transfererId, Identifier transfereeId,
        Roles transfereeRoles)
    {
        if (!IsBillingSubscriber(transfererId))
        {
            return Permission.Denied_Role(Resources.OrganizationRoot_UserNotBillingSubscriber);
        }

        if (IsBillingAdminAndNotBillingSubscriber(transfereeId, transfereeRoles))
        {
            return Permission.Allowed;
        }

        return Permission.Denied_Rule(
            Resources.OrganizationRoot_CanTransferBillingSubscription_TransfereeNotBillingAdmin);
    }

    public Result<Permission, Error> CanUnsubscribeBillingSubscription(Identifier unsubscriberId)
    {
        if (IsBillingSubscriber(unsubscriberId))
        {
            return Permission.Allowed;
        }

        return Permission.Denied_Role(Resources.OrganizationRoot_UserNotBillingSubscriber);
    }

    public Result<Permission, Error> CanViewBillingSubscription(Identifier viewerId, Roles viewerRoles)
    {
        if (IsBillingAdminOrBillingSubscriber(viewerId, viewerRoles))
        {
            return Permission.Allowed;
        }

        return Permission.Denied_Role(Resources.OrganizationRoot_NotBillingSubscriberNorBillingAdmin);
    }

    public async Task<Result<Error>> ChangeAvatarAsync(Identifier modifierId, Roles modifierRoles,
        CreateAvatarAction onCreateNew, RemoveAvatarAction onRemoveOld)
    {
        if (!IsOwner(modifierRoles))
        {
            return Error.RoleViolation(Resources.OrganizationRoot_UserNotOrgOwner);
        }

        var existingAvatarId = Avatar.HasValue
            ? Avatar.Value.ImageId.ToOptional()
            : Optional<Identifier>.None;
        var created = await onCreateNew(Domain.Shared.Name.Create(Name.Name).Value);
        if (created.IsFailure)
        {
            return created.Error;
        }

        if (existingAvatarId.HasValue)
        {
            var removed = await onRemoveOld(existingAvatarId.Value);
            if (removed.IsFailure)
            {
                return removed.Error;
            }
        }

        return RaiseChangeEvent(OrganizationsDomain.Events.AvatarAdded(Id, created.Value));
    }

    public Result<Error> ChangeName(Identifier modifierId, Roles modifierRoles, DisplayName name)
    {
        if (!IsOwner(modifierRoles))
        {
            return Error.RoleViolation(Resources.OrganizationRoot_UserNotOrgOwner);
        }

        var nothingHasChanged = name == Name;
        if (nothingHasChanged)
        {
            return Result.Ok;
        }

        return RaiseChangeEvent(OrganizationsDomain.Events.NameChanged(Id, name));
    }

    public Result<Error> CreateSettings(Settings settings)
    {
        foreach (var (key, value) in settings.Properties)
        {
            var valueValue = value.IsEncrypted
                ? _tenantSettingService.Encrypt(value.Value.ToString() ?? string.Empty)
                : value.Value.ToString() ?? string.Empty;
            RaiseChangeEvent(OrganizationsDomain.Events.SettingCreated(Id, key, valueValue, value.ValueType,
                value.IsEncrypted));
        }

        return Result.Ok;
    }

    public async Task<Result<Error>> DeleteOrganizationAsync(Identifier deleterId, Roles deleterRoles,
        bool canBillingSubscriptionBeUnsubscribed, DeleteAction onDelete, CancellationToken cancellationToken)
    {
        if (!IsOwner(deleterRoles))
        {
            return Error.RoleViolation(Resources.OrganizationRoot_UserNotOrgOwner);
        }

        if (!IsBillingSubscriber(deleterId))
        {
            return Error.RoleViolation(Resources.OrganizationRoot_UserNotBillingSubscriber);
        }

        if (!canBillingSubscriptionBeUnsubscribed)
        {
            return Error.RuleViolation(Resources
                .OrganizationRoot_DeleteOrganization_BillingSubscriptionCannotBeUnsubscribed);
        }

        var otherMembers = Memberships.Members
            .Select(m => m.UserId)
            .Except(new[] { deleterId })
            .ToList();
        if (otherMembers.HasAny())
        {
            return Error.RuleViolation(Resources.OrganizationRoot_DeleteOrganization_MembersStillExist);
        }

        var deleted = RaisePermanentDeleteEvent(OrganizationsDomain.Events.Deleted(Id, deleterId));
        if (deleted.IsFailure)
        {
            return deleted.Error;
        }

        return await onDelete(deleterId);
    }

    public Result<Error> ForceRemoveAvatar(Identifier deleterId)
    {
        if (IsNotServiceAccount(deleterId))
        {
            return Error.RoleViolation(Resources.OrganizationRoot_UserNotServiceAccount);
        }

        if (!Avatar.HasValue)
        {
            return Result.Ok;
        }

        var avatarId = Avatar.Value.ImageId;
        return RaiseChangeEvent(OrganizationsDomain.Events.AvatarRemoved(Id, avatarId));
    }

    public Result<Error> InviteMember(Identifier inviterId, Roles inviterRoles, Optional<Identifier> userId,
        Optional<EmailAddress> emailAddress)
    {
        if (!IsOwner(inviterRoles))
        {
            return Error.RoleViolation(Resources.OrganizationRoot_UserNotOrgOwner);
        }

        if (Ownership == OrganizationOwnership.Personal)
        {
            return Error.RuleViolation(Resources.OrganizationRoot_InviteMember_PersonalOrgMembershipNotAllowed);
        }

        if (!userId.HasValue
            && !emailAddress.HasValue)
        {
            return Error.RuleViolation(Resources.OrganizationRoot_InviteMember_UserIdAndEmailMissing);
        }

        return RaiseChangeEvent(OrganizationsDomain.Events.MemberInvited(Id, inviterId, userId, emailAddress));
    }

    public async Task<Result<Error>> RemoveAvatarAsync(Identifier deleterId, Roles deleterRoles,
        RemoveAvatarAction onRemoveOld)
    {
        if (!IsOwner(deleterRoles))
        {
            return Error.RoleViolation(Resources.OrganizationRoot_UserNotOrgOwner);
        }

        if (!Avatar.HasValue)
        {
            return Error.RuleViolation(Resources.OrganizationRoot_NoAvatar);
        }

        var avatarId = Avatar.Value.ImageId;
        var removed = await onRemoveOld(avatarId);
        if (removed.IsFailure)
        {
            return removed.Error;
        }

        return RaiseChangeEvent(OrganizationsDomain.Events.AvatarRemoved(Id, avatarId));
    }

    public Result<Error> RemoveMembership(Identifier userId)
    {
        if (!Memberships.HasMember(userId))
        {
            return Result.Ok;
        }

        return RaiseChangeEvent(OrganizationsDomain.Events.MembershipRemoved(Id, userId));
    }

    public Result<Error> SubscribeBilling(Identifier subscriptionId, Identifier subscriberId)
    {
        return RaiseChangeEvent(OrganizationsDomain.Events.BillingSubscribed(Id, subscriptionId, subscriberId));
    }

#if TESTINGONLY

    public void TestingOnly_ChangeOwnership(OrganizationOwnership ownership)
    {
        Ownership = ownership;
    }
#endif

    public Result<Error> TransferBillingSubscriber(Identifier transfererId, Identifier transfereeId)
    {
        if (!IsBillingSubscriber(transfererId))
        {
            return Error.RoleViolation(Resources.OrganizationRoot_UserNotBillingSubscriber);
        }

        if (!BillingSubscriber.HasValue)
        {
            return Error.RuleViolation(Resources.OrganizationRoot_NoSubscriber);
        }

        return RaiseChangeEvent(OrganizationsDomain.Events.BillingSubscriberChanged(Id, transfererId, transfereeId));
    }

    public Result<Error> UnassignRoles(Identifier assignerId, Roles assignerRoles, Identifier userId,
        Roles rolesToUnassign)
    {
        if (!IsOwner(assignerRoles))
        {
            return Error.RoleViolation(Resources.OrganizationRoot_UserNotOrgOwner);
        }

        if (!IsMember(userId))
        {
            return Error.RuleViolation(Resources.OrganizationRoot_UserNotMember);
        }

        foreach (var role in rolesToUnassign.Items)
        {
            if (!TenantRoles.IsTenantAssignableRole(role))
            {
                return Error.RuleViolation(Resources.OrganizationRoot_RoleNotAssignable.Format(role));
            }

            var assigned = RaiseChangeEvent(OrganizationsDomain.Events.RoleUnassigned(Id, assignerId, userId, role));
            if (assigned.IsFailure)
            {
                return assigned.Error;
            }
        }

        return Result.Ok;
    }

    public Result<Error> UnInviteMember(Identifier removerId, Roles removerRoles, Identifier uninvitedId)
    {
        if (!IsOwner(removerRoles))
        {
            return Error.RoleViolation(Resources.OrganizationRoot_UserNotOrgOwner);
        }

        if (IsBillingSubscriber(uninvitedId))
        {
            return Error.RoleViolation(Resources.OrganizationRoot_UninviteMember_BillingSubscriber);
        }

        if (Ownership == OrganizationOwnership.Personal)
        {
            return Error.RuleViolation(Resources.OrganizationRoot_UnInviteMember_PersonalOrg);
        }

        if (!Memberships.HasMember(uninvitedId))
        {
            return Result.Ok;
        }

        return RaiseChangeEvent(OrganizationsDomain.Events.MemberUnInvited(Id, removerId, uninvitedId));
    }

    public Result<Error> UpdateSettings(Settings settings)
    {
        foreach (var (key, value) in settings.Properties)
        {
            if (Settings.TryGet(key, out var oldSetting))
            {
                var valueValue = value.IsEncrypted
                    ? _tenantSettingService.Encrypt(value.Value.ToString() ?? string.Empty)
                    : value.Value.ToString() ?? string.Empty;
                var oldValue = oldSetting!.Value.ToString() ?? string.Empty;
                RaiseChangeEvent(OrganizationsDomain.Events.SettingUpdated(Id, key, oldValue,
                    oldSetting.ValueType,
                    valueValue, value.ValueType, value.IsEncrypted));
            }
            else
            {
                var valueValue = value.IsEncrypted
                    ? _tenantSettingService.Encrypt(value.Value.ToString() ?? string.Empty)
                    : value.Value.ToString() ?? string.Empty;
                RaiseChangeEvent(
                    OrganizationsDomain.Events.SettingCreated(Id, key, valueValue, value.ValueType,
                        value.IsEncrypted));
            }
        }

        return Result.Ok;
    }

    private static bool IsNotServiceAccount(Identifier deleterId)
    {
        return !CallerConstants.IsServiceAccount(deleterId);
    }

    private static bool IsOwner(Roles roles)
    {
        return roles.HasRole(TenantRoles.Owner);
    }

    private bool IsMember(Identifier userId)
    {
        return Memberships.HasMember(userId);
    }

    private bool IsBillingSubscriber(Identifier userId)
    {
        if (!BillingSubscriber.HasValue)
        {
            return false;
        }

        return userId == BillingSubscriber.Value.SubscriberId;
    }

    private bool IsBillingAdminAndNotBillingSubscriber(Identifier userId, Roles roles)
    {
        return !IsBillingSubscriber(userId)
               && roles.HasRole(TenantRoles.BillingAdmin);
    }

    private bool IsBillingAdminOrBillingSubscriber(Identifier userId, Roles roles)
    {
        return IsBillingSubscriber(userId)
               || roles.HasRole(TenantRoles.BillingAdmin);
    }
}