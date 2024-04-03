using Domain.Interfaces.Entities;
using Domain.Shared.Organizations;

namespace Domain.Events.Shared.Organizations;

public sealed class SettingCreated : IDomainEvent
{
    public required bool IsEncrypted { get; set; }

    public required string Name { get; set; }

    public required string StringValue { get; set; }

    public required SettingValueType ValueType { get; set; }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }
}