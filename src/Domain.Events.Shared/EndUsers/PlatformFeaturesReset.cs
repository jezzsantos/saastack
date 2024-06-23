using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.EndUsers;

#pragma warning disable SAASDDD043
public sealed class PlatformFeaturesReset : DomainEvent
#pragma warning restore SAASDDD043
{
    public PlatformFeaturesReset(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public PlatformFeaturesReset()
    {
    }

    public required string AssignedById { get; set; }

    public required List<string> Features { get; set; }
}