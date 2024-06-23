using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.EndUsers;

#pragma warning disable SAASDDD043
public sealed class MembershipFeaturesReset : DomainEvent
#pragma warning restore SAASDDD043
{
    public MembershipFeaturesReset(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public MembershipFeaturesReset()
    {
    }

    public required string AssignedById { get; set; }

    public required string OrganizationId { get; set; }

    public required string MembershipId { get; set; }

    public required List<string> Features { get; set; }
}