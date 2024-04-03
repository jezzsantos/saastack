using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.UserProfiles;

public sealed class DisplayNameChanged : DomainEvent
{
    public DisplayNameChanged(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public DisplayNameChanged()
    {
    }

    public required string DisplayName { get; set; }

    public required string UserId { get; set; }
}