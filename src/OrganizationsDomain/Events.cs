using Domain.Common.ValueObjects;
using Domain.Events.Shared.Organizations;
using Domain.Shared.Organizations;

namespace OrganizationsDomain;

public static class Events
{
    public static Created Created(Identifier id, OrganizationOwnership ownership, Identifier createdBy,
        DisplayName name)
    {
        return new Created
        {
            Name = name,
            Ownership = ownership,
            CreatedById = createdBy,
            RootId = id,
            OccurredUtc = DateTime.UtcNow
        };
    }

    public static SettingCreated SettingCreated(Identifier id, string name, string value, SettingValueType valueType,
        bool isEncrypted)
    {
        return new SettingCreated
        {
            RootId = id,
            Name = name,
            StringValue = value,
            ValueType = valueType,
            IsEncrypted = isEncrypted,
            OccurredUtc = DateTime.UtcNow
        };
    }

    public static SettingUpdated SettingUpdated(Identifier id, string name, string from, SettingValueType fromType,
        string to, SettingValueType toType, bool isEncrypted)
    {
        return new SettingUpdated
        {
            RootId = id,
            Name = name,
            From = from,
            FromType = fromType,
            To = to,
            ToType = toType,
            IsEncrypted = isEncrypted,
            OccurredUtc = DateTime.UtcNow
        };
    }
}