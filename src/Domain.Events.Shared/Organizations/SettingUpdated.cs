using Domain.Interfaces.Entities;
using Domain.Shared.Organizations;

namespace Domain.Events.Shared.Organizations;

public sealed class SettingUpdated : IDomainEvent
{
    public required string From { get; set; }

    public required SettingValueType FromType { get; set; }

    public required bool IsEncrypted { get; set; }

    public required string Name { get; set; }

    public required string To { get; set; }

    public required SettingValueType ToType { get; set; }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }
}