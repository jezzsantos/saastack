#if TESTINGONLY
using System.Text.Json.Serialization;
using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.TestingOnly;

[Obsolete($"Use {nameof(HappenedV2)} instead")]
public sealed class Happened : DomainEvent
{
    public Happened(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public Happened()
    {
    }

    public required string Message1 { get; set; }
}

public sealed class HappenedV2 : DomainEvent
{
    public HappenedV2(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public HappenedV2()
    {
    }

    [JsonIgnore] public string? Message1 { get; set; }

    public string Message2 { get; set; } = "amessage2"; // A new property with a default value

    [JsonPropertyName(nameof(Message1))] public string Message3 { get; set; } = string.Empty; // A renamed property
}
#endif