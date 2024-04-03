using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.EndUsers;

public sealed class MembershipAdded : DomainEvent
{
    public MembershipAdded(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public MembershipAdded()
    {
    }

    public required List<string> Features { get; set; }

    public required bool IsDefault { get; set; }

    public string? MembershipId { get; set; }

    public required string OrganizationId { get; set; }

    public required List<string> Roles { get; set; }
}