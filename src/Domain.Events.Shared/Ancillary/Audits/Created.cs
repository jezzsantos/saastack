using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Ancillary.Audits;

public sealed class Created : DomainEvent
{
    public Created(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public Created()
    {
    }

    public required string AgainstId { get; set; }

    public required string AuditCode { get; set; }

    public required string MessageTemplate { get; set; }

    public string? OrganizationId { get; set; }

    public required List<string> TemplateArguments { get; set; }
}