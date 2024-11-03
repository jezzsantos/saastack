using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Ancillary.EmailDelivery;

public sealed class EmailDetailsChanged : DomainEvent
{
    public EmailDetailsChanged(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public EmailDetailsChanged()
    {
    }

    public required string Body { get; set; }

    public required string Subject { get; set; }

    public required List<string> Tags { get; set; }

    public required string ToDisplayName { get; set; }

    public required string ToEmailAddress { get; set; }
}