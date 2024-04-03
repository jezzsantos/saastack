using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.UserProfiles;

public sealed class NameChanged : DomainEvent
{
    public NameChanged(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public NameChanged()
    {
    }

    public required string FirstName { get; set; }

    public string? LastName { get; set; }

    public required string UserId { get; set; }
}