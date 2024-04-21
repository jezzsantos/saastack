using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.EndUsers;

public sealed class MembershipRoleUnassigned : DomainEvent
{
    public MembershipRoleUnassigned(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public MembershipRoleUnassigned()
    {
    }

    public required string MembershipId { get; set; }

    public required string OrganizationId { get; set; }

    public required string Role { get; set; }
}