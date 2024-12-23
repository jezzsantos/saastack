using Domain.Common;
using Domain.Common.ValueObjects;
using Domain.Shared.Ancillary;
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

    public string? Body { get; set; }

    public required DeliveredEmailContentType ContentType { get; set; }

    public string? Subject { get; set; }

    public Dictionary<string, string>? Substitutions { get; set; }

    public required List<string> Tags { get; set; }

    public string? TemplateId { get; set; }

    public required string ToDisplayName { get; set; }

    public required string ToEmailAddress { get; set; }
}