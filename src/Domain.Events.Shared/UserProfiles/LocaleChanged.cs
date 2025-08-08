using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.UserProfiles;

public sealed class LocaleChanged : DomainEvent
{
    public LocaleChanged(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public LocaleChanged()
    {
    }

    public required string Locale { get; set; }

    public required string UserId { get; set; }
}