using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.EndUsers;

public sealed class MembershipFeatureUnassigned : DomainEvent
{
    public MembershipFeatureUnassigned(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public MembershipFeatureUnassigned()
    {
    }

    public required string Feature { get; set; }

    public required string MembershipId { get; set; }

    public required string OrganizationId { get; set; }
}