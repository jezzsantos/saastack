using Domain.Common;
using Domain.Common.ValueObjects;
using Domain.Shared.Organizations;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Organizations;

public sealed class SettingUpdated : DomainEvent
{
    public SettingUpdated(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public SettingUpdated()
    {
    }

    public required string From { get; set; }

    public required SettingValueType FromType { get; set; }

    public required bool IsEncrypted { get; set; }

    public required string Name { get; set; }

    public required string To { get; set; }

    public required SettingValueType ToType { get; set; }
}