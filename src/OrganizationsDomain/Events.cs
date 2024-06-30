using Common;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.Organizations;
using Domain.Shared;
using Domain.Shared.Organizations;
using Created = Domain.Events.Shared.Organizations.Created;
using MembershipAdded = Domain.Events.Shared.Organizations.MembershipAdded;
using MembershipRemoved = Domain.Events.Shared.Organizations.MembershipRemoved;

namespace OrganizationsDomain;

public static class Events
{
    public static AvatarAdded AvatarAdded(Identifier id, Avatar avatar)
    {
        return new AvatarAdded(id)
        {
            AvatarId = avatar.ImageId,
            AvatarUrl = avatar.Url
        };
    }

    public static AvatarRemoved AvatarRemoved(Identifier id, Identifier avatarId)
    {
        return new AvatarRemoved(id)
        {
            AvatarId = avatarId
        };
    }

    public static BillingSubscribed BillingSubscribed(Identifier id, Identifier subscriptionId, Identifier subscriberId)
    {
        return new BillingSubscribed(id)
        {
            SubscriptionId = subscriptionId,
            SubscriberId = subscriberId
        };
    }

    public static BillingSubscriberChanged BillingSubscriberChanged(Identifier id, Identifier transfererId,
        Identifier transfereeId)
    {
        return new BillingSubscriberChanged(id)
        {
            FromSubscriberId = transfererId,
            ToSubscriberId = transfereeId
        };
    }

    public static Created Created(Identifier id, OrganizationOwnership ownership, Identifier createdBy,
        DisplayName name)
    {
        return new Created(id)
        {
            Name = name,
            Ownership = ownership,
            CreatedById = createdBy
        };
    }

    public static Deleted Deleted(Identifier id, Identifier deletedById)
    {
        return new Deleted(id, deletedById);
    }

    public static MemberInvited MemberInvited(Identifier id, Identifier invitedById, Optional<Identifier> invitedId,
        Optional<EmailAddress> userEmailAddress)
    {
        return new MemberInvited(id)
        {
            InvitedById = invitedById,
            InvitedId = invitedId.ValueOrDefault!,
            EmailAddress = userEmailAddress.ValueOrDefault?.Address
        };
    }

    public static MembershipAdded MembershipAdded(Identifier id, Identifier userId)
    {
        return new MembershipAdded(id)
        {
            UserId = userId
        };
    }

    public static MembershipRemoved MembershipRemoved(Identifier id, Identifier userId)
    {
        return new MembershipRemoved(id)
        {
            UserId = userId
        };
    }

    public static MemberUnInvited MemberUnInvited(Identifier id, Identifier uninvitedById, Identifier uninvitedId)
    {
        return new MemberUnInvited(id)
        {
            UninvitedById = uninvitedById,
            UninvitedId = uninvitedId
        };
    }

    public static NameChanged NameChanged(Identifier id, DisplayName name)
    {
        return new NameChanged(id)
        {
            Name = name
        };
    }

    public static RoleAssigned RoleAssigned(Identifier id, Identifier assignerId, Identifier userId, Role role)
    {
        return new RoleAssigned(id)
        {
            AssignedById = assignerId,
            UserId = userId,
            Role = role.Identifier
        };
    }

    public static RoleUnassigned RoleUnassigned(Identifier id, Identifier unassignerId, Identifier userId, Role role)
    {
        return new RoleUnassigned(id)
        {
            UnassignedById = unassignerId,
            UserId = userId,
            Role = role.Identifier
        };
    }

    public static SettingCreated SettingCreated(Identifier id, string name, string value, SettingValueType valueType,
        bool isEncrypted)
    {
        return new SettingCreated(id)
        {
            Name = name,
            StringValue = value,
            ValueType = valueType,
            IsEncrypted = isEncrypted
        };
    }

    public static SettingUpdated SettingUpdated(Identifier id, string name, string from, SettingValueType fromType,
        string to, SettingValueType toType, bool isEncrypted)
    {
        return new SettingUpdated(id)
        {
            Name = name,
            From = from,
            FromType = fromType,
            To = to,
            ToType = toType,
            IsEncrypted = isEncrypted
        };
    }
}