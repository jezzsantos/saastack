using Domain.Common;
using Domain.Common.ValueObjects;
using Domain.Shared.EndUsers;
using JetBrains.Annotations;

namespace Domain.Events.Shared.EndUsers;

public sealed class Registered : DomainEvent
{
    public Registered(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public Registered()
    {
    }

    public UserAccess Access { get; set; }

    public UserClassification Classification { get; set; }

    public required List<string> Features { get; set; }

    public required List<string> Roles { get; set; }

    public UserStatus Status { get; set; }

    public string? Username { get; set; }

    public required RegisteredUserProfile UserProfile { get; set; }
}