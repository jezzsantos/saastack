using Domain.Interfaces.Entities;

namespace Domain.Events.Shared.Ancillary.Audits;

public sealed class Created : IDomainEvent
{
    public required string AgainstId { get; set; }

    public required string AuditCode { get; set; }

    public required string MessageTemplate { get; set; }

    public string? OrganizationId { get; set; }

    public required List<string> TemplateArguments { get; set; }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }
}