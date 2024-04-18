using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.UserProfiles;

public sealed class DefaultOrganizationChanged : DomainEvent
{
    public DefaultOrganizationChanged(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public DefaultOrganizationChanged()
    {
    }

    public string? FromOrganizationId { get; set; }

    public required string ToOrganizationId { get; set; }
}