using Domain.Common;
using Domain.Common.ValueObjects;
using Domain.Shared.EndUsers;
using JetBrains.Annotations;

namespace Domain.Events.Shared.EndUsers;

public sealed class Created : DomainEvent
{
    public Created(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public Created()
    {
    }

    public UserAccess Access { get; set; }

    public UserClassification Classification { get; set; }

    public required string HostRegion { get; set; }

    public UserStatus Status { get; set; }
}