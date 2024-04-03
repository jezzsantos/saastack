using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.UserProfiles;

public sealed class TimezoneChanged : DomainEvent
{
    public TimezoneChanged(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public TimezoneChanged()
    {
    }

    public required string Timezone { get; set; }

    public required string UserId { get; set; }
}