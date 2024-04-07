using Common;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.Organizations;
using Domain.Shared;
using Domain.Shared.Organizations;

namespace OrganizationsDomain;

public static class Events
{
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

    public static MembershipAdded MembershipAdded(Identifier id, Identifier invitedById, Optional<Identifier> userId,
        Optional<EmailAddress> userEmailAddress)
    {
        return new MembershipAdded(id)
        {
            InvitedById = invitedById,
            UserId = userId.ValueOrDefault!,
            EmailAddress = userEmailAddress.ValueOrDefault?.Address
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