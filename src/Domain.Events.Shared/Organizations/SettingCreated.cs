using Domain.Common;
using Domain.Common.ValueObjects;
using Domain.Shared.Organizations;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Organizations;

public sealed class SettingCreated : DomainEvent
{
    public SettingCreated(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public SettingCreated()
    {
    }

    public required bool IsEncrypted { get; set; }

    public required string Name { get; set; }

    public required string StringValue { get; set; }

    public required SettingValueType ValueType { get; set; }
}