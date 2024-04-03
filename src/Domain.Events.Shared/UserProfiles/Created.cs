using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.UserProfiles;

public sealed class Created : DomainEvent
{
    public Created(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public Created()
    {
    }

    public required string DisplayName { get; set; }

    public required string FirstName { get; set; }

    public string? LastName { get; set; }

    public required string Type { get; set; }

    public required string UserId { get; set; }
}